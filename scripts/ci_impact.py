#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Select cumulative PR validation scopes without requiring a .NET installation."""
from __future__ import annotations

import argparse
import fnmatch
from functools import cached_property
import json
import os
from pathlib import Path, PurePosixPath
import posixpath
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
FLAGS = ("dotnet", "tool", "docs", "lint", "codeql")
GLOBAL_INPUTS = {
    "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props",
    "GFramework.sln", "global.json", "nuget.config", "NuGet.Config", ".editorconfig",
    ".config/dotnet-tools.json", ".mega-linter.yml", ".github/workflows/ci.yml",
    "scripts/ci_impact.py", "scripts/ci_run.py", "scripts/ci_release_tag.py",
    "scripts/test_ci_impact.py", "scripts/test_ci_run.py", "scripts/test_ci_workflows.py",
}
LINT_SUFFIXES = {".cs", ".js", ".ts", ".mts", ".json", ".yaml", ".yml", ".csproj", ".props", ".targets"}


def git(root: Path, *args: str) -> str:
    """Use argument arrays, revision verification and NUL-separated file names."""
    return subprocess.check_output(["git", *args], cwd=root, stderr=subprocess.PIPE).decode("utf-8")


def revision(root: Path, value: str) -> str:
    return git(root, "rev-parse", "--verify", "--end-of-options", value + "^{commit}").strip()


def normalize(parent: str, value: str) -> str:
    name = posixpath.normpath(posixpath.join(parent, value.replace("\\", "/")))
    if name.startswith(("../", "/")) or name == "..":
        raise ValueError("Input escapes repository: " + value)
    return name


class Snapshot:
    """Read graph and file ownership from one immutable Git tree, not a dirty checkout."""

    def __init__(self, root: Path, ref: str):
        self.root, self.ref = root, ref
        self._texts: dict[str, str] = {}

    def text(self, name: str) -> str:
        if name not in self._texts:
            self._texts[name] = git(self.root, "show", f"{self.ref}:{name}").lstrip("\ufeff")
        return self._texts[name]

    @cached_property
    def projects(self) -> dict[str, dict]:
        # Only solution projects are build targets. Third-party reference copies are never targets.
        names = re.findall(r'^Project\([^\n]+?= "[^"]+", "([^"]+\.csproj)"',
                           self.text("GFramework.sln"), re.M)
        if not names:
            raise ValueError("Solution has no projects")
        projects = {}
        for value in names:
            path = normalize("", value)
            tree = ET.fromstring(self.text(path))
            props = {e.tag.split("}")[-1]: (e.text or "").strip()
                     for group in tree if group.tag.split("}")[-1] == "PropertyGroup" for e in group}
            assembly = props.get("AssemblyName", PurePosixPath(path).stem)
            variables = {"AssemblyName": assembly, "MSBuildProjectName": PurePosixPath(path).stem}

            def expand(value: str) -> str:
                for key, replacement in variables.items():
                    value = value.replace("$(" + key + ")", replacement)
                if "$(" in value or "@(" in value or "%(" in value:
                    raise ValueError("Unresolved graph input: " + path + ": " + value)
                return value

            parent = posixpath.dirname(path)
            references, inputs, pack_inputs = set(), set(), set()
            for e in tree.iter():
                tag = e.tag.split("}")[-1]
                value = e.get("Include") or (e.get("Project") if tag == "Import" else None)
                if not value:
                    continue
                if tag == "ProjectReference":
                    references.update(normalize(parent, expand(v)) for v in value.split(";"))
                elif tag == "Import":
                    inputs.add(normalize(parent, expand(value)))
                elif tag in {"Compile", "AdditionalFiles", "EmbeddedResource"}:
                    inputs.update(normalize(parent, expand(v)) for v in value.split(";"))
                elif tag == "None" and e.get("Pack", "").lower() == "true":
                    # Generated outputs belong to their project; only source assets need a separate edge.
                    if "$(OutputPath)" not in value:
                        pack_inputs.add(normalize(parent, expand(value)))
            readme = props.get("PackageReadmeFile")
            if readme:
                pack_inputs.add(normalize(parent, expand(readme)))
            projects[path] = {
                "references": references, "inputs": inputs, "pack_inputs": pack_inputs,
                "test": props.get("IsTestProject", "false").lower() == "true",
                "pack": props.get("IsPackable", "true").lower() != "false",
                "package_id": expand(props.get("PackageId", assembly)),
            }
        for path, project in projects.items():
            missing = project["references"] - projects.keys()
            if missing:
                raise ValueError(f"Unknown project references in {path}: {sorted(missing)}")
        return projects


def matches(name: str, pattern: str) -> bool:
    # Match repository-relative paths, never just their leaf names. '**/' may match zero directories.
    return fnmatch.fnmatchcase(name, pattern) or (
        "**/" in pattern and matches(name, pattern.replace("**/", "", 1)))


def owners(snapshot: Snapshot, name: str) -> tuple[set[str], set[str]]:
    """Return compile owners and packaging-only owners, including linked inputs."""
    compile_owners, package_owners = set(), set()
    for path, project in snapshot.projects.items():
        parent = posixpath.dirname(path)
        explicit = any(matches(name, pattern) for pattern in project["inputs"])
        packed = any(matches(name, pattern) for pattern in project["pack_inputs"])
        if explicit or (parent and name.startswith(parent + "/") and (
                not name.lower().endswith(".md") or PurePosixPath(name).name.startswith("AnalyzerReleases."))):
            compile_owners.add(path)
        if packed:
            package_owners.add(path)
    return compile_owners, package_owners


def closure(seeds: set[str], projects: dict[str, dict]) -> set[str]:
    """Follow consumers, including private/analyzer/conditional references conservatively."""
    selected = set(seeds)
    while True:
        consumers = {path for path, project in projects.items() if project["references"] & selected}
        added = consumers - selected
        if not added:
            return selected
        selected.update(added)


def all_projects(snapshot: Snapshot) -> dict[str, dict]:
    try:
        return snapshot.projects
    except (ValueError, ET.ParseError, subprocess.SubprocessError):
        # Full mode uses the real solution entrypoint even when graph selection cannot be trusted.
        return {}


def select(root: Path, base: str | None, head: str, *, full: bool = False) -> dict:
    head = revision(root, head)
    current = Snapshot(root, head)
    tested = Snapshot(root, revision(root, "HEAD"))
    report = {"schema_version": 1, "head": head, "base": "", "files": [], "full": False,
              **dict.fromkeys(FLAGS, False), "build_projects": [], "test_projects": [],
              "pack_projects": [], "package_ids": [], "reasons": []}
    projects = all_projects(tested)

    def fallback(reason: str) -> dict:
        report["reasons"].append(reason)
        fill(set(projects), set())
        report.update(full=True, **dict.fromkeys(FLAGS, True))
        return report

    def fill(build: set[str], package_only: set[str]) -> None:
        report["build_projects"] = sorted(build & projects.keys())
        report["test_projects"] = sorted(p for p in build & projects.keys() if projects[p]["test"])
        report["pack_projects"] = sorted(p for p in (build | package_only) & projects.keys() if projects[p]["pack"])
        report["package_ids"] = sorted(projects[p]["package_id"] for p in report["pack_projects"])
        report["dotnet"] = bool(build or package_only)

    if full or not base:
        return fallback("Explicit full validation" if full else "Comparison baseline unavailable")
    try:
        baseline = git(root, "merge-base", revision(root, base), head).strip()
        report["base"] = baseline
        files = sorted(set(git(root, "diff", "--name-only", "--no-renames", "-z", baseline, head).split("\0")) - {""})
        report["files"] = files
        old = Snapshot(root, baseline)
        # Union prevents deletions or removed edges from silently removing a consumer from validation.
        graph: dict[str, dict] = {}
        for snapshot in (old, current, tested):
            for p, value in snapshot.projects.items():
                if p in graph:
                    graph[p]["references"].update(value["references"])
                else:
                    graph[p] = dict(value, references=set(value["references"]))
        seeds, packaged = set(), set()
        for name in files:
            if name in GLOBAL_INPUTS:
                return fallback("Shared validation/build input: " + name)
            if name.startswith((".agents/", ".ai/", "ai-libs/", "third-party-licenses/")):
                continue
            if name.startswith("engineering/") and PurePosixPath(name).suffix.lower() in {".md", ".svg", ".png", ".jpg"}:
                continue
            report["lint"] |= PurePosixPath(name).suffix in LINT_SUFFIXES
            if name == ".github/workflows/codeql.yml":
                report["codeql"] = True
                continue
            if name == ".github/workflows/megalinter.yml":
                report["lint"] = True
                continue
            if name.startswith("docs/"):
                report["docs"] = True
                continue
            if name.startswith("tools/gframework-config-tool/"):
                report["tool"] = True
                continue
            compiled, packages = set(), set()
            for snapshot in (old, current, tested):
                c, p = owners(snapshot, name)
                compiled.update(c)
                packages.update(p)
            if compiled or packages:
                seeds.update(compiled)
                packaged.update(packages)
                report["codeql"] |= bool(compiled)
                continue
            if name.endswith(".md") or name in {"LICENSE", "NOTICE", "CLAUDE.md", ".gitignore", ".gitattributes", ".coderabbit.yaml"}:
                continue
            # Workflow changes validate definitions, not deployment; package jobs still prove their build contract.
            if name.startswith(".github/"):
                if name in {".github/workflows/publish.yml", ".github/workflows/license-compliance.yml"}:
                    seeds.update(graph)
                    report["codeql"] = True
                elif name == ".github/workflows/publish-vscode-extension.yml":
                    report["tool"] = True
                elif name == ".github/workflows/publish-docs.yml":
                    report["docs"] = True
                elif name == ".github/workflows/benchmark.yml":
                    seeds.add("GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj")
                    report["codeql"] = True
                elif not (name.startswith(".github/ISSUE_TEMPLATE/") or name in {
                        ".github/dependabot.yml", ".github/cliff.toml", ".github/actions/validate-pat/action.yml",
                        ".github/workflows/auto-tag.yml", ".github/workflows/license-header-fix.yml"}):
                    return fallback("Unclassified workflow input: " + name)
                continue
            return fallback("Unclassified input: " + name)
        selected = closure(seeds, graph)
        fill(selected, packaged)
        report["reasons"].append("Cumulative PR diff and before/after project dependency closure")
        return report
    except (ValueError, ET.ParseError, subprocess.SubprocessError, KeyError) as error:
        return fallback("Impact evidence unavailable; full validation: " + str(error))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default=os.environ.get("CI_BASE_SHA"))
    parser.add_argument("--head", default=os.environ.get("CI_HEAD_SHA", "HEAD"))
    parser.add_argument("--full", action="store_true")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    report = select(ROOT, args.base, args.head, full=args.full)
    # Execution consumes this exact plan artifact; checkout binding is verified by ci_run.py.
    report["tested_sha"] = revision(ROOT, "HEAD")
    if subprocess.call(["git", "merge-base", "--is-ancestor", report["head"], report["tested_sha"]], cwd=ROOT):
        parser.error("Source head is not part of the checked-out revision")
    print(json.dumps(report, indent=2) if args.json else "\n".join(report["reasons"]))
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
            for key in (*FLAGS, "full"):
                output.write(f"{key}={str(report[key]).lower()}\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
