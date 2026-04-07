#!/usr/bin/env python3
"""Validate that docs/zh-CN markdown pages are reachable from docs/.vitepress/config.mts links."""

from __future__ import annotations

import re
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parent.parent
DOCS_ROOT = REPO_ROOT / "docs" / "zh-CN"
CONFIG_PATH = REPO_ROOT / "docs" / ".vitepress" / "config.mts"
LINK_PATTERN = re.compile(r"link:\s*'(/zh-CN/[^']*)'")


def normalize_link(link: str) -> str:
    normalized = link.removeprefix("/zh-CN/").strip("/")
    return normalized or "index"


def collect_docs() -> set[str]:
    docs: set[str] = set()
    for path in DOCS_ROOT.rglob("*.md"):
        relative = path.relative_to(DOCS_ROOT).as_posix()
        if relative.startswith("tutorials/assets/"):
            continue

        normalized = relative.removesuffix(".md")
        if normalized.endswith("/index"):
            normalized = normalized[: -len("/index")]

        docs.add(normalized or "index")

    return docs


def collect_links() -> set[str]:
    content = CONFIG_PATH.read_text(encoding="utf-8")
    return {normalize_link(match) for match in LINK_PATTERN.findall(content)}


def main() -> int:
    docs = collect_docs()
    links = collect_links()

    missing = sorted(docs - links)
    broken = sorted(links - docs)

    if missing:
        print("Unindexed docs:")
        for item in missing:
            print(f"  - {item}")

    if broken:
        print("Broken links:")
        for item in broken:
            print(f"  - {item}")

    if missing or broken:
        return 1

    print("All docs/zh-CN markdown pages are indexed by docs/.vitepress/config.mts.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
