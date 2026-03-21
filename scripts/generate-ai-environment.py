#!/usr/bin/env python3

from __future__ import annotations

from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ROOT_DIR = Path(__file__).resolve().parent.parent
RAW_PATH = ROOT_DIR / ".ai" / "environment" / "tools.raw.yaml"
AI_PATH = ROOT_DIR / ".ai" / "environment" / "tools.ai.yaml"


def parse_scalar(value: str) -> Any:
    if value == "true":
        return True
    if value == "false":
        return False
    if value.startswith('"') and value.endswith('"'):
        return value[1:-1]
    return value


def parse_simple_yaml(path: Path) -> dict[str, Any]:
    root: dict[str, Any] = {}
    stack: list[tuple[int, dict[str, Any]]] = [(-1, root)]

    for raw_line in path.read_text(encoding="utf-8").splitlines():
        if not raw_line.strip():
            continue
        if raw_line.lstrip().startswith("#"):
            continue

        indent = len(raw_line) - len(raw_line.lstrip(" "))
        key, _, tail = raw_line.strip().partition(":")

        while len(stack) > 1 and indent <= stack[-1][0]:
            stack.pop()

        current = stack[-1][1]
        value = tail.strip()

        if value == "":
            child: dict[str, Any] = {}
            current[key] = child
            stack.append((indent, child))
            continue

        current[key] = parse_scalar(value)

    return root


def bool_value(data: dict[str, Any], *keys: str) -> bool:
    current: Any = data
    for key in keys:
        current = current[key]
    return bool(current)


def string_value(data: dict[str, Any], *keys: str) -> str:
    current: Any = data
    for key in keys:
        current = current[key]
    return str(current)


def choose(preferred: str | None, fallback: str | None) -> str:
    if preferred:
        return preferred
    return fallback or "unavailable"


def available_tool(raw: dict[str, Any], section: str, name: str) -> bool:
    return bool_value(raw, section, name, "installed")


def select_tool(
    use_for: str,
    preferred: str | None,
    fallback: str | None,
) -> dict[str, str]:
    return {
        "preferred": choose(preferred, fallback),
        "fallback": fallback or "unavailable",
        "use_for": use_for,
    }


def build_ai_inventory(raw: dict[str, Any]) -> dict[str, Any]:
    has_python = available_tool(raw, "required_runtimes", "python3")
    has_node = available_tool(raw, "required_runtimes", "node")
    has_bun = available_tool(raw, "required_runtimes", "bun")
    has_dotnet = available_tool(raw, "required_runtimes", "dotnet")
    has_rg = available_tool(raw, "required_tools", "rg")
    has_jq = available_tool(raw, "required_tools", "jq")
    has_bash = available_tool(raw, "required_tools", "bash")
    has_docker = available_tool(raw, "project_tools", "docker")

    search = select_tool(
        use_for="Repository text search.",
        preferred="rg" if has_rg else None,
        fallback="grep",
    )
    json = select_tool(
        use_for="Inspecting or transforming JSON command output.",
        preferred="jq" if has_jq else None,
        fallback="python3" if has_python else None,
    )
    scripting = select_tool(
        use_for="Non-trivial local automation and helper scripts.",
        preferred="python3" if has_python else None,
        fallback="bash" if has_bash else None,
    )
    shell = select_tool(
        use_for="Repository shell scripts and command execution.",
        preferred="bash" if has_bash else None,
        fallback="sh",
    )
    docs = select_tool(
        use_for="Installing and previewing the docs site.",
        preferred="bun" if has_bun else None,
        fallback="npm" if has_node else None,
    )
    build = select_tool(
        use_for="Build, test, restore, and solution validation.",
        preferred="dotnet" if has_dotnet else None,
        fallback=None,
    )

    if bool_value(raw, "platform", "wsl"):
        platform_family = "wsl-linux"
    else:
        platform_family = string_value(raw, "platform", "os").lower()

    return {
        "schema_version": 1,
        "generated_at_utc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "generated_from": ".ai/environment/tools.raw.yaml",
        "generator": "scripts/generate-ai-environment.py",
        "platform": {
            "family": platform_family,
            "os": string_value(raw, "platform", "os"),
            "distro": string_value(raw, "platform", "distro"),
            "shell": string_value(raw, "platform", "shell"),
        },
        "capabilities": {
            "dotnet": has_dotnet,
            "python": has_python,
            "node": has_node,
            "bun": has_bun,
            "docker": has_docker,
            "fast_search": has_rg,
            "json_cli": has_jq,
        },
        "tool_selection": {
            "search": search,
            "json": json,
            "shell": shell,
            "scripting": scripting,
            "docs_package_manager": docs,
            "build_and_test": build,
        },
        "python": {
            "available": has_python,
            "helper_packages": {
                "requests": bool_value(raw, "python_packages", "requests", "installed"),
                "rich": bool_value(raw, "python_packages", "rich", "installed"),
                "openai": bool_value(raw, "python_packages", "openai", "installed"),
                "tiktoken": bool_value(raw, "python_packages", "tiktoken", "installed"),
                "pydantic": bool_value(raw, "python_packages", "pydantic", "installed"),
                "pytest": bool_value(raw, "python_packages", "pytest", "installed"),
            },
        },
        "preferences": {
            "prefer_project_listed_tools": True,
            "prefer_python_for_non_trivial_automation": has_python,
            "avoid_unlisted_system_tools": True,
        },
        "rules": [
            "Use rg instead of grep for repository search when rg is available.",
            "Use jq for JSON inspection; fall back to python3 if jq is unavailable.",
            "Prefer python3 over complex bash for non-trivial scripting when python3 is available.",
            "Use bun for docs preview workflows when bun is available; otherwise fall back to npm.",
            "Use dotnet for repository build and test workflows.",
            "Do not assume unrelated system tools are part of the supported project environment.",
        ],
    }


def emit_yaml(value: Any, indent: int = 0) -> list[str]:
    prefix = " " * indent

    if isinstance(value, dict):
        lines: list[str] = []
        for key, nested in value.items():
            if isinstance(nested, (dict, list)):
                lines.append(f"{prefix}{key}:")
                lines.extend(emit_yaml(nested, indent + 2))
            else:
                lines.append(f"{prefix}{key}: {format_scalar(nested)}")
        return lines

    if isinstance(value, list):
        lines = []
        for item in value:
            if isinstance(item, (dict, list)):
                lines.append(f"{prefix}-")
                lines.extend(emit_yaml(item, indent + 2))
            else:
                lines.append(f"{prefix}- {format_scalar(item)}")
        return lines

    return [f"{prefix}{format_scalar(value)}"]


def format_scalar(value: Any) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, int):
        return str(value)
    text = str(value).replace('"', '\\"')
    return f'"{text}"'


def main() -> None:
    raw = parse_simple_yaml(RAW_PATH)
    ai_inventory = build_ai_inventory(raw)
    AI_PATH.parent.mkdir(parents=True, exist_ok=True)
    AI_PATH.write_text("\n".join(emit_yaml(ai_inventory)) + "\n", encoding="utf-8")
    print(f"Wrote {AI_PATH}")


if __name__ == "__main__":
    main()
