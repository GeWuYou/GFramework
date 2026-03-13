#!/usr/bin/env bash
set -euo pipefail

if ! command -v git >/dev/null 2>&1; then
    echo "git is required to enumerate tracked C# files." >&2
    exit 2
fi

if ! command -v python3 >/dev/null 2>&1; then
    echo "python3 is required to validate C# naming conventions." >&2
    exit 2
fi

repo_root="$(git rev-parse --show-toplevel)"
cd "$repo_root"

python3 - <<'PY'
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path.cwd()
EXCLUDED_PREFIXES = (
    "Godot/script_templates/",
)

NAMESPACE_PATTERN = re.compile(r"^\s*namespace\s+([A-Za-z][A-Za-z0-9_.]*)\s*(?:[;{]|$)")
SEGMENT_PATTERN = re.compile(r"^[A-Za-z][A-Za-z0-9]*$")
PASCAL_CASE_PATTERN = re.compile(
    r"^(?:[A-Z](?=[A-Z][a-z0-9])|[A-Z]{2}(?=$|[A-Z][a-z0-9])|[A-Z][a-z0-9]+)+$"
)


def is_excluded(path: str) -> bool:
    normalized = path.strip("./")
    return any(
        normalized == prefix.rstrip("/") or normalized.startswith(prefix)
        for prefix in EXCLUDED_PREFIXES
    )


def validate_segment(segment: str) -> str | None:
    if not SEGMENT_PATTERN.fullmatch(segment):
        return "must start with a letter and contain only letters or digits"

    if not segment[0].isupper():
        return "must start with an uppercase letter"

    if segment.isupper():
        if len(segment) <= 2:
            return None
        return "acronyms longer than 2 letters must use PascalCase"

    if not PASCAL_CASE_PATTERN.fullmatch(segment):
        return "must use PascalCase; only 2-letter acronyms may stay fully uppercase"

    return None


def tracked_csharp_files() -> list[str]:
    result = subprocess.run(
        ["git", "ls-files", "--", "*.cs"],
        check=True,
        capture_output=True,
        text=True,
    )
    return [
        line
        for line in result.stdout.splitlines()
        if line and not is_excluded(line)
    ]


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        return path.read_text(encoding="utf-8")


files = tracked_csharp_files()
namespace_violations: list[tuple[str, int, str, list[str]]] = []
directory_violations: list[tuple[str, str, str]] = []
seen_directories: set[str] = set()

for relative_file in files:
    file_path = ROOT / relative_file
    for line_number, line in enumerate(read_text(file_path).splitlines(), start=1):
        match = NAMESPACE_PATTERN.match(line)
        if not match:
            continue

        namespace = match.group(1)
        segment_errors = []
        for segment in namespace.split("."):
            reason = validate_segment(segment)
            if reason is not None:
                segment_errors.append(f'{segment}: {reason}')

        if segment_errors:
            namespace_violations.append((relative_file, line_number, namespace, segment_errors))

    parent = Path(relative_file).parent
    while parent != Path("."):
        relative_dir = parent.as_posix()
        if relative_dir not in seen_directories:
            seen_directories.add(relative_dir)
            for raw_segment in relative_dir.split("/"):
                segments = raw_segment.split(".")
                for segment in segments:
                    reason = validate_segment(segment)
                    if reason is not None:
                        directory_violations.append((relative_dir, segment, reason))
                        break
                else:
                    continue
                break

        parent = parent.parent

if namespace_violations or directory_violations:
    print("C# naming validation failed.")

    if namespace_violations:
        print("\nNamespace violations:")
        for file_path, line_number, namespace, errors in namespace_violations:
            print(f"- {file_path}:{line_number} -> {namespace}")
            for error in errors:
                print(f"  * {error}")

    if directory_violations:
        print("\nDirectory violations:")
        for directory_path, segment, reason in sorted(set(directory_violations)):
            print(f'- {directory_path} -> "{segment}": {reason}')

    sys.exit(1)

print(f"C# naming validation passed for {len(files)} tracked C# files.")
PY
