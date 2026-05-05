#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

from __future__ import annotations

import argparse
import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parent.parent

RUNTIME_PROJECTS = (
    "GFramework",
    "GFramework.Core",
    "GFramework.Core.Abstractions",
    "GFramework.Cqrs",
    "GFramework.Cqrs.Abstractions",
    "GFramework.Game",
    "GFramework.Game.Abstractions",
    "GFramework.Godot",
    "GFramework.Ecs.Arch",
    "GFramework.Ecs.Arch.Abstractions",
)

FORBIDDEN_ATTRIBUTE_NAMES = (
    "GenerateEnumExtensions",
    "ContextAware",
    "GetModel",
    "GetModels",
    "GetSystem",
    "GetSystems",
    "GetUtility",
    "GetUtilities",
    "GetService",
    "GetServices",
    "GetAll",
    "Log",  # GFramework.Core.SourceGenerators.Abstractions.Logging.LogAttribute
    "Priority",  # GFramework.Core.SourceGenerators.Abstractions.Bases.PriorityAttribute
)

FORBIDDEN_PROJECT_REFERENCE_PREFIX = "GFramework."
FORBIDDEN_PACKAGE_REFERENCE_PREFIX = "GeWuYou.GFramework."
PACKAGE_NAMESPACE = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"
PACKAGE_NAMESPACE_2012 = "http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd"


@dataclass(frozen=True)
class Violation:
    location: str
    message: str


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate that runtime and abstractions modules do not depend on source-generator packages or attributes."
    )
    parser.add_argument(
        "--package-dir",
        type=Path,
        help="Optional package directory. When supplied, validate packed runtime nuspec dependencies as well.",
    )
    return parser.parse_args()


def get_project_file(project_name: str) -> Path:
    if project_name == "GFramework":
        return REPO_ROOT / "GFramework.csproj"

    return REPO_ROOT / project_name / f"{project_name}.csproj"


def get_source_root(project_name: str) -> Path | None:
    if project_name == "GFramework":
        return None

    return REPO_ROOT / project_name


def get_local_name(tag: str) -> str:
    return tag.split("}", 1)[-1]


def get_xml_text(element: ET.Element) -> str:
    return (element.text or "").strip()


def get_runtime_package_ids() -> set[str]:
    package_ids: set[str] = set()
    for project_name in RUNTIME_PROJECTS:
        project_file = get_project_file(project_name)
        tree = ET.parse(project_file)
        root = tree.getroot()
        package_id = None
        assembly_name = None

        for element in root.iter():
            local_name = get_local_name(element.tag)
            if local_name == "PackageId" and not package_id:
                package_id = get_xml_text(element)
            elif local_name == "AssemblyName" and not assembly_name:
                assembly_name = get_xml_text(element)

        resolved_package_id = package_id or f"GeWuYou.{assembly_name or project_name}"
        package_ids.add(resolved_package_id)

    return package_ids


def validate_project_references() -> list[Violation]:
    violations: list[Violation] = []

    for project_name in RUNTIME_PROJECTS:
        project_file = get_project_file(project_name)
        tree = ET.parse(project_file)

        for element in tree.getroot().iter():
            local_name = get_local_name(element.tag)
            if local_name not in {"ProjectReference", "PackageReference"}:
                continue

            include = element.attrib.get("Include", "").strip()
            if local_name == "ProjectReference":
                if FORBIDDEN_PROJECT_REFERENCE_PREFIX not in include or "SourceGenerators" not in include:
                    continue
            else:
                if not include.startswith(FORBIDDEN_PACKAGE_REFERENCE_PREFIX) or "SourceGenerators" not in include:
                    continue

            violations.append(
                Violation(
                    location=str(project_file.relative_to(REPO_ROOT)),
                    message=f"forbidden {local_name} -> {include}",
                )
            )

    return violations


def compile_attribute_patterns() -> dict[str, re.Pattern[str]]:
    patterns: dict[str, re.Pattern[str]] = {}
    for attribute_name in FORBIDDEN_ATTRIBUTE_NAMES:
        escaped_attribute_name = re.escape(attribute_name)
        patterns[attribute_name] = re.compile(
            rf"\[[^\]]*(?:(?<=\[)|(?<=[\s,(]))(?:global::)?(?:[A-Za-z_][A-Za-z0-9_]*\.)*{escaped_attribute_name}(?:Attribute)?(?=\s*(?:\(|,|\]))[^\]]*\]",
            re.MULTILINE,
        )

    return patterns


def line_number_for_offset(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def is_comment_attribute_match(text: str, offset: int) -> bool:
    line_start = text.rfind("\n", 0, offset) + 1
    line_prefix = text[line_start:offset].lstrip()
    return line_prefix.startswith("///") or line_prefix.startswith("//") or line_prefix.startswith("/*") or line_prefix.startswith("*")


def validate_source_attributes() -> list[Violation]:
    violations: list[Violation] = []
    patterns = compile_attribute_patterns()

    for project_name in RUNTIME_PROJECTS:
        source_root = get_source_root(project_name)
        if source_root is None or not source_root.is_dir():
            continue

        for file_path in source_root.rglob("*.cs"):
            if any(part in {"bin", "obj"} for part in file_path.parts):
                continue

            text = file_path.read_text(encoding="utf-8-sig")
            for attribute_name, pattern in patterns.items():
                for match in pattern.finditer(text):
                    if is_comment_attribute_match(text, match.start()):
                        continue

                    line_number = line_number_for_offset(text, match.start())
                    relative_path = file_path.relative_to(REPO_ROOT)
                    violations.append(
                        Violation(
                            location=f"{relative_path}:{line_number}",
                            message=f"forbidden source-generator attribute [{attribute_name}] in runtime module",
                        )
                    )

    return violations


def iter_dependency_ids(nuspec_root: ET.Element) -> list[str]:
    dependency_ids: list[str] = []
    for namespace in (PACKAGE_NAMESPACE, PACKAGE_NAMESPACE_2012):
        dependency_ids.extend(
            element.attrib["id"]
            for element in nuspec_root.findall(f".//{{{namespace}}}dependency")
            if "id" in element.attrib
        )

    if dependency_ids:
        return dependency_ids

    dependency_ids.extend(
        element.attrib["id"]
        for element in nuspec_root.findall(".//dependency")
        if "id" in element.attrib
    )
    return dependency_ids


def validate_packed_dependencies(package_dir: Path) -> list[Violation]:
    violations: list[Violation] = []
    runtime_package_ids = get_runtime_package_ids()

    for package_path in sorted(package_dir.glob("*.nupkg")):
        with zipfile.ZipFile(package_path) as archive:
            nuspec_entries = [name for name in archive.namelist() if name.endswith(".nuspec")]
            if not nuspec_entries:
                violations.append(
                    Violation(
                        location=str(package_path.relative_to(REPO_ROOT if package_path.is_relative_to(REPO_ROOT) else package_dir.parent)),
                        message="missing nuspec entry",
                    )
                )
                continue

            nuspec_root = ET.fromstring(archive.read(nuspec_entries[0]))

        package_id_element = nuspec_root.find(f".//{{{PACKAGE_NAMESPACE}}}id")
        if package_id_element is None:
            package_id_element = nuspec_root.find(f".//{{{PACKAGE_NAMESPACE_2012}}}id")
        if package_id_element is None:
            package_id_element = nuspec_root.find(".//id")

        package_id = get_xml_text(package_id_element) if package_id_element is not None else package_path.stem
        if package_id not in runtime_package_ids:
            continue

        dependency_ids = iter_dependency_ids(nuspec_root)
        for dependency_id in dependency_ids:
            if not dependency_id.startswith(FORBIDDEN_PACKAGE_REFERENCE_PREFIX) and not dependency_id.startswith(
                FORBIDDEN_PROJECT_REFERENCE_PREFIX
            ):
                continue

            if "SourceGenerators" not in dependency_id:
                continue

            violations.append(
                Violation(
                    location=str(package_path),
                    message=f"runtime package {package_id} depends on forbidden package {dependency_id}",
                )
            )

    return violations


def print_violations(violations: list[Violation]) -> None:
    for violation in violations:
        print(f"- {violation.location}: {violation.message}")


def main() -> int:
    args = parse_args()

    violations: list[Violation] = []
    violations.extend(validate_project_references())
    violations.extend(validate_source_attributes())

    if args.package_dir is not None:
        package_dir = args.package_dir if args.package_dir.is_absolute() else REPO_ROOT / args.package_dir
        if not package_dir.is_dir():
            print(f"Package directory does not exist: {package_dir}", file=sys.stderr)
            return 2

        violations.extend(validate_packed_dependencies(package_dir))

    if violations:
        print("Runtime/source-generator boundary validation failed.")
        print_violations(violations)
        return 1

    print("Runtime/source-generator boundary validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
