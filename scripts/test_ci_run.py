#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Exercise failure propagation and execution boundaries without a .NET toolchain."""
from contextlib import redirect_stdout
from io import StringIO
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import MagicMock, patch

import ci_run
from ci_impact import select


class RunTests(unittest.TestCase):
    def test_selected_jobs_must_succeed_and_unselected_skips_are_allowed(self):
        plan = dict.fromkeys(ci_run.FLAGS, False)
        plan["dotnet"] = True
        needs = {"impact": {"result": "success"}, "governance": {"result": "success"},
                 "build-and-test": {"result": "success"}, "docs": {"result": "skipped"}}
        self.assertEqual(ci_run.summary(plan, needs), [])
        for result in ("failure", "cancelled", "skipped", "missing"):
            with self.subTest(result=result):
                needs["build-and-test"] = {"result": result}
                self.assertTrue(ci_run.summary(plan, needs))
        del needs["build-and-test"]
        self.assertTrue(ci_run.summary(plan, needs))

    def test_every_selected_pipeline_failure_blocks_merge_summary(self):
        plan = dict.fromkeys(ci_run.FLAGS, True)
        needs = {name: {"result": "success"}
                 for name in ("impact", "governance", *ci_run.JOBS.values())}
        self.assertEqual(ci_run.summary(plan, needs), [])
        for name in ci_run.JOBS.values():
            for outcome in ("failure", "cancelled", "skipped", "missing"):
                with self.subTest(job=name, outcome=outcome):
                    failed = {**needs, name: {"result": outcome}}
                    self.assertIn(f"{name}: {outcome}", ci_run.summary(plan, failed))

    def test_governance_and_routing_failure_always_fail_summary(self):
        plan = dict.fromkeys(ci_run.FLAGS, False)
        for job in ("impact", "governance"):
            with self.subTest(job=job):
                needs = {name: {"result": "success"} for name in ("impact", "governance")}
                needs[job]["result"] = "failure"
                self.assertTrue(ci_run.summary(plan, needs))

    def test_plan_cannot_be_reused_at_another_revision(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "plan.json"
            path.write_text(json.dumps({"schema_version": 1, "tested_sha": "old"}))
            with patch("ci_run.revision", return_value="new"):
                with self.assertRaisesRegex(ValueError, "revision"):
                    ci_run.load_plan(path)

    def test_empty_test_selection_creates_no_reports_or_commands(self):
        with patch("ci_run.command") as command, patch("ci_run.subprocess.Popen") as process:
            self.assertEqual(ci_run.tests({"full": False, "test_projects": []}), 0)
            command.assert_not_called()
            process.assert_not_called()

    def test_full_fallback_runs_tests_without_reparsing_unresolved_graph(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True, capture_output=True)
            for project, is_test in (("Library", False), ("Tests", True)):
                directory = root / project
                directory.mkdir()
                (directory / f"{project}.csproj").write_text(
                    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                    f'<IsTestProject>{str(is_test).lower()}</IsTestProject></PropertyGroup>'
                    '<Import Project="$(MSBuildThisFileDirectory)shared.targets"/></Project>', encoding="utf-8")
                (directory / "shared.targets").write_text("<Project/>", encoding="utf-8")
            (root / "GFramework.sln").write_text(
                'Project("{type}") = "Library", "Library/Library.csproj", "{library}"\nEndProject\n'
                'Project("{type}") = "Tests", "Tests/Tests.csproj", "{tests}"\nEndProject\n', encoding="utf-8")
            subprocess.run(["git", "add", "."], cwd=root, check=True, capture_output=True)
            subprocess.run(["git", "-c", "user.name=CI fixture", "-c", "user.email=ci@example.invalid",
                            "-c", "commit.gpgsign=false", "-c", "core.hooksPath=" + os.devnull,
                            "commit", "-qm", "fixture"], cwd=root, check=True, capture_output=True)
            plan = select(root, None, "HEAD")
            plan["tested_sha"] = plan["head"]
            self.assertTrue(plan["full"])
            self.assertEqual(plan["test_projects"], [])
            path = root / "plan.json"
            path.write_text(json.dumps(plan), encoding="utf-8")
            plan = ci_run.load_plan(path, root)

            real_popen = subprocess.Popen

            def successful_test(argv, **kwargs):
                if argv[0] != "dotnet":
                    return real_popen(argv, **kwargs)
                self.assertEqual(argv[2], "Tests/Tests.csproj")
                trx = root / "TestResults/Tests.trx"
                trx.write_text('<TestRun><ResultSummary><Counters executed="1"/></ResultSummary></TestRun>')
                instance = MagicMock()
                instance.__enter__.return_value = instance
                instance.stdout = ["one real selected fixture\n"]
                instance.wait.return_value = 0
                return instance

            with patch("ci_run.ROOT", root), patch("ci_run.command"), \
                    patch("ci_run.subprocess.Popen", side_effect=successful_test) as process:
                self.assertEqual(ci_run.tests(plan), 0)
                self.assertEqual([call.args[0][2] for call in process.call_args_list
                                  if call.args[0][0] == "dotnet"], ["Tests/Tests.csproj"])

    def test_failed_test_exit_is_not_hidden_by_report_generation(self):
        with tempfile.TemporaryDirectory() as folder, patch("ci_run.ROOT", Path(folder)), \
                patch("ci_run.command"), patch("ci_run.subprocess.Popen") as process:
            instance = process.return_value.__enter__.return_value
            instance.stdout = ["failed test host\n"]
            instance.wait.return_value = 1
            # The expected failure remains asserted, but its GitHub annotation must not escape the unit fixture.
            output = StringIO()
            with redirect_stdout(output):
                self.assertEqual(ci_run.tests({"full": False, "test_projects": ["Tests/Tests.csproj"]}), 1)
            self.assertIn("::error::Test project failed (1): Tests/Tests.csproj", output.getvalue())
            self.assertIn("failed test host", (Path(folder) / "TestResults/Tests.console.log").read_text())

    def test_build_uses_single_solution_filter_and_pack_targets(self):
        with tempfile.TemporaryDirectory() as folder, patch("ci_run.ROOT", Path(folder)), \
                patch("ci_run.command") as command:
            ci_run.build({"full": False, "build_projects": ["Tests/Tests.csproj"],
                          "pack_projects": ["Library/Library.csproj"]})
            data = json.loads((Path(folder) / ".local/ci/affected.slnf").read_text())
            self.assertEqual(data["solution"]["projects"], ["Library/Library.csproj", "Tests/Tests.csproj"])
            self.assertEqual(command.call_count, 2)

    def test_success_without_executed_tests_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            trx = Path(folder) / "results.trx"
            self.assertFalse(ci_run.has_test_results(trx))
            for executed, expected in (("0", False), ("3", True)):
                trx.write_text(f'<TestRun xmlns="urn:test"><ResultSummary><Counters executed="{executed}"/></ResultSummary></TestRun>')
                self.assertEqual(ci_run.has_test_results(trx), expected)


if __name__ == "__main__":
    unittest.main()
