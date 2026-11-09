#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Resolve framework SemVer tags before starting package or compliance work."""
import argparse
import os
import re

NUMBER = r"(?:0|[1-9][0-9]*)"
IDENTIFIER = r"(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)"
SEMVER = re.compile(rf"[vV]?({NUMBER}\.{NUMBER}\.{NUMBER}(?:-{IDENTIFIER}(?:\.{IDENTIFIER})*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?)")


def version(tag: str) -> str | None:
    """Extension and unrelated tags cannot enter framework publishing."""
    match = SEMVER.fullmatch(tag)
    return match[1] if match else None


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("tag", nargs="?", default=os.environ.get("GITHUB_REF_NAME", ""))
    args = parser.parse_args()
    value = version(args.tag)
    print(value or "Not a framework version tag")
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
            output.write("framework=" + str(value is not None).lower() + "\n")
            output.write("version=" + (value or "") + "\n")
            stable_docs = bool(value and args.tag.startswith("v") and "-" not in value.split("+", 1)[0])
            output.write("docs=" + str(stable_docs).lower() + "\n")


if __name__ == "__main__":
    main()
