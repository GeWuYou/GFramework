#!/usr/bin/env python3
"""Normalize a GFramework docs module input and report its evidence surface."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parents[3]
MODULE_MAP_PATH = REPO_ROOT / ".agents/skills/_shared/module-map.json"


def load_module_map() -> dict[str, Any]:
    return json.loads(MODULE_MAP_PATH.read_text(encoding="utf-8"))


def normalize_key(value: str) -> str:
    return value.strip().lower().replace("_", "-").replace(" ", "-")


def resolve_module(raw_input: str, module_map: dict[str, Any]) -> dict[str, Any]:
    modules = module_map["modules"]
    docs_section_aliases = module_map.get("docs_section_aliases", {})
    normalized = normalize_key(raw_input)

    for canonical_name in modules:
        if normalize_key(canonical_name) == normalized:
            return {"status": "ok", "module": canonical_name, "reason": "canonical"}

    for canonical_name, config in modules.items():
        aliases = config.get("aliases", [])
        if normalized in {normalize_key(alias) for alias in aliases}:
            return {"status": "ok", "module": canonical_name, "reason": "alias"}

    if normalized in docs_section_aliases:
        candidates = docs_section_aliases[normalized]
        if len(candidates) == 1:
            return {"status": "ok", "module": candidates[0], "reason": "docs_section"}
        return {
            "status": "ambiguous",
            "reason": "docs_section",
            "input": raw_input,
            "candidates": candidates,
        }

    fuzzy = [
        canonical_name
        for canonical_name in modules
        if normalized in normalize_key(canonical_name) or normalize_key(canonical_name) in normalized
    ]
    if fuzzy:
        return {"status": "unknown", "reason": "closest_match", "input": raw_input, "candidates": fuzzy}

    return {"status": "unknown", "reason": "no_match", "input": raw_input, "candidates": []}


def collect_path_state(paths: list[str]) -> list[dict[str, Any]]:
    states: list[dict[str, Any]] = []
    for relative_path in paths:
        absolute_path = REPO_ROOT / relative_path
        states.append(
            {
                "path": relative_path,
                "exists": absolute_path.exists(),
                "kind": "dir" if absolute_path.is_dir() else "file",
            }
        )
    return states


def assess_docs(module_config: dict[str, Any]) -> list[str]:
    docs_config = module_config["docs"]
    landing = collect_path_state(docs_config.get("landing", []))
    topics = collect_path_state(docs_config.get("topics", []))
    assessment: list[str] = []

    if landing and not any(item["exists"] for item in landing):
        assessment.append("landing_missing")
    elif landing:
        assessment.append("landing_present")

    if not topics:
        assessment.append("topic_docs_not_mapped")
    else:
        existing_topics = sum(1 for item in topics if item["exists"])
        if existing_topics == 0:
            assessment.append("topic_docs_missing")
        elif existing_topics < len(topics):
            assessment.append("topic_docs_partial")
        else:
            assessment.append("topic_docs_present")

    return assessment


def build_report(module_name: str, module_config: dict[str, Any]) -> dict[str, Any]:
    source_paths = collect_path_state(module_config.get("source_paths", []))
    test_projects = collect_path_state(module_config.get("test_projects", []))
    readmes = collect_path_state(module_config.get("readme_paths", []))
    docs_config = module_config["docs"]
    ai_libs = module_config.get("ai_libs", {})

    report = {
        "status": "ok",
        "module": module_name,
        "source_paths": source_paths,
        "project_file": collect_path_state([module_config["project_file"]])[0],
        "test_projects": test_projects,
        "readme_paths": readmes,
        "docs": {
          "landing": collect_path_state(docs_config.get("landing", [])),
          "topics": collect_path_state(docs_config.get("topics", [])),
          "fallback": collect_path_state(docs_config.get("fallback", []))
        },
        "ai_libs": {
            "paths": collect_path_state(ai_libs.get("paths", [])),
            "search_hints": ai_libs.get("search_hints", []),
        },
        "assessment": assess_docs(module_config),
    }

    if readmes and not any(item["exists"] for item in readmes):
        report["assessment"].append("readme_missing")

    if test_projects and not any(item["exists"] for item in test_projects):
        report["assessment"].append("tests_missing")

    if not ai_libs.get("paths"):
        report["assessment"].append("ai_libs_optional")

    if not docs_config.get("topics"):
        report["assessment"].append("fallback_docs_only")

    return report


def print_text_report(report: dict[str, Any]) -> None:
    if report["status"] != "ok":
        print(json.dumps(report, ensure_ascii=False, indent=2))
        return

    print(f"module: {report['module']}")
    print("assessment:")
    for item in report["assessment"]:
        print(f"  - {item}")

    print("source:")
    for item in report["source_paths"]:
        print(f"  - {'OK' if item['exists'] else 'MISS'} {item['path']}")

    project_file = report["project_file"]
    print(f"project: {'OK' if project_file['exists'] else 'MISS'} {project_file['path']}")

    print("tests:")
    for item in report["test_projects"]:
        print(f"  - {'OK' if item['exists'] else 'MISS'} {item['path']}")

    print("readme:")
    if report["readme_paths"]:
        for item in report["readme_paths"]:
            print(f"  - {'OK' if item['exists'] else 'MISS'} {item['path']}")
    else:
        print("  - none mapped")

    print("docs landing:")
    for item in report["docs"]["landing"]:
        print(f"  - {'OK' if item['exists'] else 'MISS'} {item['path']}")

    print("docs topics:")
    if report["docs"]["topics"]:
        for item in report["docs"]["topics"]:
            print(f"  - {'OK' if item['exists'] else 'MISS'} {item['path']}")
    else:
        print("  - none mapped")

    print("docs fallback:")
    for item in report["docs"]["fallback"]:
        print(f"  - {'OK' if item['exists'] else 'MISS'} {item['path']}")

    print("ai-libs:")
    if report["ai_libs"]["paths"]:
        for item in report["ai_libs"]["paths"]:
            print(f"  - {'OK' if item['exists'] else 'MISS'} {item['path']}")
    else:
        print("  - none mapped")

    if report["ai_libs"]["search_hints"]:
        print("ai-libs search hints:")
        for item in report["ai_libs"]["search_hints"]:
            print(f"  - {item}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("module", help="Canonical module name, alias, or docs section name.")
    parser.add_argument("--json", action="store_true", help="Emit JSON instead of text.")
    args = parser.parse_args()

    module_map = load_module_map()
    resolution = resolve_module(args.module, module_map)

    if resolution["status"] != "ok":
        if args.json:
            print(json.dumps(resolution, ensure_ascii=False, indent=2))
        else:
            print(json.dumps(resolution, ensure_ascii=False, indent=2))
        return 1

    report = build_report(resolution["module"], module_map["modules"][resolution["module"]])
    report["resolution"] = resolution

    if args.json:
        print(json.dumps(report, ensure_ascii=False, indent=2))
    else:
        print_text_report(report)

    return 0


if __name__ == "__main__":
    sys.exit(main())
