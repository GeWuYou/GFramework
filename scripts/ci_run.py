#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Execute a revision-bound CI plan, and reject missing or skipped selected jobs."""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

from ci_impact import ROOT, FLAGS, Snapshot, revision

JOBS = {"dotnet": "build-and-test", "tool": "config-tool", "docs": "docs", "lint": "megalinter", "codeql": "codeql"}


def load_plan(path: Path, root: Path = ROOT) -> dict:
    plan = json.loads(path.read_text(encoding="utf-8"))
    if plan.get("schema_version") != 1 or plan.get("tested_sha") != revision(root, "HEAD"):
        raise ValueError("CI plan does not match checked-out revision")
    if any(type(plan.get(flag)) is not bool for flag in (*FLAGS, "full")):
        raise ValueError("Invalid CI scope flags")
    known = set(Snapshot(root, "HEAD").projects) if not plan["full"] else None
    for key in ("build_projects", "test_projects", "pack_projects"):
        values = plan.get(key)
        if not isinstance(values, list) or any(not isinstance(p, str) or not p.endswith(".csproj")
                                               or p.startswith(("-", "/")) or ".." in Path(p).parts for p in values):
            raise ValueError("Invalid project selection: " + key)
        if known is not None and not set(values) <= known:
            raise ValueError("Plan contains unknown projects")
    return plan


def command(argv: list[str]) -> None:
    print("+ " + " ".join(argv), flush=True)
    subprocess.run(argv, cwd=ROOT, check=True)


def build(plan: dict) -> None:
    # Solution filters share MSBuild's graph and compile dependencies once in a single invocation.
    if plan["full"]:
        target = "GFramework.sln"
    else:
        selected = sorted(set(plan["build_projects"] + plan["pack_projects"]))
        if not selected:
            raise ValueError("Selected .NET job has no build or pack targets")
        folder = ROOT / ".local/ci"
        folder.mkdir(parents=True, exist_ok=True)
        target = str(folder / "affected.slnf")
        Path(target).write_text(json.dumps({"solution": {"path": "../../GFramework.sln", "projects": selected}}))
    command(["dotnet", "restore", target])
    command(["dotnet", "build", target, "-c", "Release", "--no-restore"])


def pack(plan: dict) -> None:
    if not plan["pack_projects"] and not plan["full"]:
        print("No affected packable projects")
        return
    output = ROOT / ".local/ci/packages"
    output.mkdir(parents=True, exist_ok=True)
    # A retry cannot accidentally validate stale packages from a previous selection.
    for package in output.glob("*.nupkg"):
        package.unlink()
    targets = ["GFramework.sln"] if plan["full"] else plan["pack_projects"]
    for target in targets:
        command(["dotnet", "pack", target, "-c", "Release", "--no-build", "--no-restore",
                 "-o", str(output), "-p:IncludeSymbols=false"])
    validation = ["bash", "scripts/validate-packed-modules.sh", str(output)]
    if not plan["full"]:
        expected = ROOT / ".local/ci/expected-packages.txt"
        expected.write_text("\n".join(plan["package_ids"]) + "\n", encoding="utf-8")
        validation.extend(["--expected-packages", str(expected)])
    command(validation)
    command(["python3", "scripts/validate-runtime-generator-boundaries.py", "--package-dir", str(output)])


def tests(plan: dict) -> int:
    selected = plan["test_projects"]
    if plan["full"]:
        selected = sorted(p for p, value in Snapshot(ROOT, "HEAD").projects.items() if value["test"])
    if not selected:
        print("No affected test projects; no test report generated")
        return 0
    results = ROOT / "TestResults"
    results.mkdir(exist_ok=True)
    command(["dotnet", "tool", "restore"])
    failed = []
    for project in selected:
        name = Path(project).stem
        trx = results / f"{name}.trx"
        trx.unlink(missing_ok=True)
        argv = ["dotnet", "test", project, "-c", "Release", "--no-build", "--no-restore",
                "--logger", f"trx;LogFileName={name}.trx", "--results-directory", str(results)]
        print("::group::" + " ".join(argv), flush=True)
        log = results / f"{name}.console.log"
        # Preserve streamed console output and each exit code; a green TRX alone cannot hide a failed host.
        with log.open("w", encoding="utf-8") as output:
            with subprocess.Popen(argv, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True) as process:
                for line in process.stdout:
                    print(line, end="", flush=True)
                    output.write(line)
                code = process.wait()
        if code:
            failed.append(project)
            print(f"::error::Test project failed ({code}): {project}")
        elif not has_test_results(trx):
            failed.append(project)
            print(f"::error::Selected test project produced no executed tests: {project}")
        print("::endgroup::", flush=True)
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
            output.write("ran_tests=true\n")
            output.write("godot_failed=" + str("GFramework.Godot.Tests/GFramework.Godot.Tests.csproj" in failed).lower() + "\n")
    # Reports are attempted for real results even when another project has failed.
    ctrf = ROOT / "ctrf"
    ctrf.mkdir(exist_ok=True)
    for trx in sorted(results.glob("*.trx")):
        try:
            command(["dotnet", "tool", "run", "DotnetCtrfJsonReporter", "-p", str(trx),
                     "-t", "nunit", "-d", str(ctrf), "-f", trx.stem + ".json"])
        except subprocess.CalledProcessError:
            failed.append("CTRF conversion: " + trx.name)
    if failed:
        print("Failed test projects/reports:\n" + "\n".join(failed), file=sys.stderr)
    return int(bool(failed))


def has_test_results(trx: Path) -> bool:
    """A successful process with no executed tests is not acceptance evidence."""
    try:
        counters = ET.parse(trx).getroot().find(".//{*}Counters")
        return counters is not None and int(counters.get("executed", "0")) > 0
    except (OSError, ET.ParseError, ValueError):
        return False


def summary(plan: dict, needs: dict) -> list[str]:
    """Unexpected skips, cancellations, missing jobs and routing failures are failures."""
    required = {"impact", "governance"}
    required.update(job for flag, job in JOBS.items() if plan.get(flag) is True)
    return [f"{job}: {needs.get(job, {}).get('result', 'missing')}"
            for job in sorted(required) if needs.get(job, {}).get("result") != "success"]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=["build", "pack", "test", "summary"])
    parser.add_argument("--plan", type=Path, default=Path(".local/ci/impact.json"))
    args = parser.parse_args()
    try:
        plan = load_plan(args.plan)
        if args.operation == "summary":
            failures = summary(plan, json.loads(os.environ["CI_NEEDS"]))
            if failures:
                raise ValueError("Required CI jobs did not succeed: " + "; ".join(failures))
            print("All selected checks passed")
        elif not plan["dotnet"]:
            raise ValueError(".NET execution requested for an unselected scope")
        elif args.operation == "build":
            build(plan)
        elif args.operation == "pack":
            pack(plan)
        else:
            return tests(plan)
        return 0
    except (ValueError, KeyError, OSError, subprocess.SubprocessError) as error:
        print(str(error), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
