#!/usr/bin/env python3
"""Check and apply repository license headers."""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


COPYRIGHT_LINE = "Copyright (c) 2025-2026 GeWuYou"
SPDX_LINE = "SPDX-License-Identifier: Apache-2.0"

LINE_COMMENT_EXTENSIONS = {
    ".cs": "//",
    ".js": "//",
    ".ts": "//",
    ".py": "#",
    ".sh": "#",
    ".yml": "#",
    ".yaml": "#",
}

XML_COMMENT_EXTENSIONS = {".csproj", ".props", ".targets"}

SUPPORTED_EXTENSIONS = set(LINE_COMMENT_EXTENSIONS) | XML_COMMENT_EXTENSIONS

EXCLUDED_PREFIXES = (
    ".agents/",
    "ai-libs/",
    "third-party-licenses/",
    "bin/",
    "obj/",
    "node_modules/",
    "packages/",
    "TestResults/",
    "megalinter-reports/",
)

EXCLUDED_EXACT_PATHS = {
    ".ai/environment/tools.ai.yaml",
    ".ai/environment/tools.raw.yaml",
}

EXCLUDED_FILE_NAMES = {
    "LICENSE",
    "LICENSE.md",
}

HEADER_MARKERS = (
    SPDX_LINE,
    "Licensed under the Apache License",
)


@dataclass(frozen=True)
class HeaderResult:
    """Describes the license header status for a repository-tracked file."""

    path: Path
    relative_path: str
    has_header: bool
    needs_repair: bool = False


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Check or apply Apache-2.0 source file headers.")
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true", help="Fail when supported files are missing a license header.")
    mode.add_argument("--fix", action="store_true", help="Write missing license headers to supported files.")
    parser.add_argument("--dry-run", action="store_true", help="List files that would be changed without writing them.")
    parser.add_argument(
        "--root",
        type=Path,
        default=Path.cwd(),
        help="Repository root. Defaults to the current working directory.",
    )
    parser.add_argument(
        "--paths",
        nargs="*",
        help="Optional repository-relative file list. Intended for tests and targeted local checks.",
    )

    args = parser.parse_args(argv)
    root = args.root.resolve()
    relative_paths = args.paths if args.paths is not None else list_tracked_files(root)
    results = list(scan_files(root, relative_paths))
    missing = [result for result in results if not result.has_header]
    repairs = [result for result in results if result.needs_repair]
    updates = missing + repairs

    if not updates:
        print("All supported files include an Apache-2.0 license header.")
        return 0

    if args.check or args.dry_run:
        if missing:
            print_missing(missing, "Missing Apache-2.0 license header:")
        if repairs:
            print_missing(repairs, "License header needs repair:")

    if args.check:
        return 1

    if args.dry_run:
        return 0

    for result in missing:
        add_license_header(result.path)

    for result in repairs:
        repair_license_header(result.path)

    if missing:
        print_missing(missing, "Added Apache-2.0 license header:")
    if repairs:
        print_missing(repairs, "Repaired Apache-2.0 license header:")
    return 0


def list_tracked_files(root: Path) -> list[str]:
    """Returns repository-tracked paths so generated build output is not scanned."""

    command = ["git", "ls-files"]
    completed = subprocess.run(command, cwd=root, check=True, capture_output=True, text=True)
    return completed.stdout.splitlines()


def scan_files(root: Path, relative_paths: Iterable[str]) -> Iterable[HeaderResult]:
    for relative_path in relative_paths:
        normalized = relative_path.replace(os.sep, "/")
        if not is_supported_path(normalized):
            continue

        path = root / normalized
        if not path.is_file():
            continue

        text, _ = read_text_preserving_bom(path)
        yield HeaderResult(
            path=path,
            relative_path=normalized,
            has_header=has_license_header(text),
            needs_repair=needs_header_repair(path, text),
        )


def is_supported_path(relative_path: str) -> bool:
    if relative_path in EXCLUDED_EXACT_PATHS:
        return False

    if any(relative_path.startswith(prefix) for prefix in EXCLUDED_PREFIXES):
        return False

    if "/snapshots/" in relative_path or relative_path.startswith(".git/"):
        return False

    path = Path(relative_path)
    if path.name in EXCLUDED_FILE_NAMES:
        return False

    return path.suffix in SUPPORTED_EXTENSIONS


def has_license_header(text: str) -> bool:
    search_window = text[:4096]
    return any(marker in search_window for marker in HEADER_MARKERS)


def needs_header_repair(path: Path, text: str) -> bool:
    if path.suffix not in XML_COMMENT_EXTENSIONS:
        return False

    newline = detect_newline(text)
    header = build_header(path.suffix, newline)
    return text.startswith(header) and text[len(header) :].startswith("<?xml")


def add_license_header(path: Path) -> None:
    text, had_bom = read_text_preserving_bom(path)
    updated = insert_header(path, text)
    write_text_preserving_bom(path, updated, had_bom)


def repair_license_header(path: Path) -> None:
    text, had_bom = read_text_preserving_bom(path)
    updated = repair_xml_header_position(path, text)
    write_text_preserving_bom(path, updated, had_bom)


def insert_header(path: Path, text: str) -> str:
    newline = detect_newline(text)
    header = build_header(path.suffix, newline)

    if path.suffix in XML_COMMENT_EXTENSIONS and text.startswith("<?xml"):
        line_end = find_first_line_end(text, newline)
        return f"{text[:line_end]}{header}{text[line_end:]}"

    if text.startswith("#!"):
        line_end = find_first_line_end(text, newline)
        if line_end == len(text):
            return f"{text}{newline}{header}"

        return f"{text[:line_end]}{header}{text[line_end:]}"

    return f"{header}{text}"


def repair_xml_header_position(path: Path, text: str) -> str:
    newline = detect_newline(text)
    header = build_header(path.suffix, newline)
    if not text.startswith(header):
        return text

    remainder = text[len(header) :]
    if not remainder.startswith("<?xml"):
        return text

    line_end = find_first_line_end(remainder, newline)
    return f"{remainder[:line_end]}{header}{remainder[line_end:]}"


def find_first_line_end(text: str, newline: str) -> int:
    first_newline = text.find("\n")
    if first_newline == -1:
        return len(text)

    return first_newline + len("\n")


def build_header(suffix: str, newline: str) -> str:
    if suffix in XML_COMMENT_EXTENSIONS:
        return (
            f"<!--{newline}"
            f"  {COPYRIGHT_LINE}{newline}"
            f"  {SPDX_LINE}{newline}"
            f"-->{newline}"
            f"{newline}"
        )

    comment = LINE_COMMENT_EXTENSIONS[suffix]
    return (
        f"{comment} {COPYRIGHT_LINE}{newline}"
        f"{comment} {SPDX_LINE}{newline}"
        f"{newline}"
    )


def detect_newline(text: str) -> str:
    return "\r\n" if "\r\n" in text[:4096] else "\n"


def read_text_preserving_bom(path: Path) -> tuple[str, bool]:
    content = path.read_bytes()
    had_bom = content.startswith(b"\xef\xbb\xbf")
    if had_bom:
        content = content[3:]
    return content.decode("utf-8"), had_bom


def write_text_preserving_bom(path: Path, text: str, had_bom: bool) -> None:
    content = text.encode("utf-8")
    if had_bom:
        content = b"\xef\xbb\xbf" + content
    path.write_bytes(content)


def print_missing(results: Iterable[HeaderResult], heading: str) -> None:
    print(heading)
    for result in results:
        print(f"  {result.relative_path}")


if __name__ == "__main__":
    sys.exit(main())
