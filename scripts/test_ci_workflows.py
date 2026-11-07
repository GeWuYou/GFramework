#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Check release routing and package-set contracts without publishing anything."""
import fnmatch
import os
from pathlib import Path
import re
import subprocess
import tempfile
import unittest

from ci_impact import ROOT, Snapshot
from ci_release_tag import version


class ReleaseTests(unittest.TestCase):
    def test_framework_tag_versions(self):
        for tag, expected in (("v1.2.3", "1.2.3"), ("V1.2.3", "1.2.3"), ("1.2.3", "1.2.3"),
                              ("v0.1.0-beta.1", "0.1.0-beta.1"), ("v1.2.3+build-x.1", "1.2.3+build-x.1")):
            with self.subTest(tag=tag):
                self.assertEqual(version(tag), expected)
        for tag in ("gframework-config-tool-v1.2.3", "example", "vnext", "v1.2", "v01.2.3",
                    "v1.2.3-beta.01", "v1.2.3-", "v1.2.3\n", "v1.2.3;echo bad"):
            with self.subTest(tag=tag):
                self.assertIsNone(version(tag))

    def test_actual_tag_filters_isolate_workflow_domains(self):
        expected = {
            "v1.2.3": {"publish", "license-compliance", "publish-docs"},
            "V1.2.3": {"publish", "license-compliance"},
            "1.2.3": {"publish", "license-compliance"},
            # Docs workflow may enter its cheap version gate, but does not deploy a prerelease.
            "v1.2.3-beta.1": {"publish", "license-compliance", "publish-docs"},
            "gframework-config-tool-v1.2.3": {"publish-vscode-extension"},
            "unrelated": set(),
        }
        filters = {}
        for name in ("publish", "license-compliance", "publish-docs", "publish-vscode-extension"):
            text = (ROOT / f".github/workflows/{name}.yml").read_text()
            block = re.search(r"(?m)^    tags:\n((?:      - [^\n]+\n)+)", text)[1]
            filters[name] = re.findall(r"- '([^']+)'", block)
        for tag, workflows in expected.items():
            with self.subTest(tag=tag):
                actual = {name for name, patterns in filters.items() if any(fnmatch.fnmatchcase(tag, p) for p in patterns)}
                self.assertEqual(actual, workflows)

    def test_docs_deploy_gate_only_accepts_stable_lowercase_v_tags(self):
        for tag, expected in (("v1.2.3", "true"), ("v1.2.3+build-x", "true"),
                              ("v1.2.3-beta.1", "false"), ("V1.2.3", "false"),
                              ("1.2.3", "false"), ("gframework-config-tool-v1.2.3", "false")):
            with self.subTest(tag=tag), tempfile.TemporaryDirectory() as folder:
                output = Path(folder) / "outputs"
                subprocess.run(["python3", "scripts/ci_release_tag.py", tag], cwd=ROOT,
                               env={**os.environ, "GITHUB_OUTPUT": str(output)}, check=True, capture_output=True)
                self.assertIn("docs=" + expected + "\n", output.read_text())

    def test_full_and_partial_package_sets_keep_exact_inventory_checks(self):
        packages = sorted(p["package_id"] for p in Snapshot(ROOT, "HEAD").projects.values() if p["pack"])
        with tempfile.TemporaryDirectory() as folder:
            directory = Path(folder) / "packages"
            directory.mkdir()

            def validate(*extra):
                return subprocess.run(["bash", "scripts/validate-packed-modules.sh", str(directory), *extra],
                                      cwd=ROOT, capture_output=True).returncode

            (directory / f"{packages[0]}.1.2.3.nupkg").touch()
            expected = Path(folder) / "expected"
            expected.write_text(packages[0] + "\n")
            self.assertEqual(validate("--expected-packages", str(expected)), 0)
            self.assertNotEqual(validate(), 0)
            (directory / "Unexpected.1.2.3.nupkg").touch()
            self.assertNotEqual(validate("--expected-packages", str(expected)), 0)
            (directory / "Unexpected.1.2.3.nupkg").unlink()
            for package in packages:
                (directory / f"{package}.1.2.3.nupkg").touch()
            self.assertEqual(validate(), 0)
            expected.write_text("")
            self.assertNotEqual(validate("--expected-packages", str(expected)), 0)

    def test_reports_are_guarded_and_summary_cannot_be_skipped(self):
        text = (ROOT / ".github/workflows/ci.yml").read_text()
        self.assertIn("name: CI Result\n    if: always()", text)
        self.assertIn("needs: [impact, governance, megalinter, build-and-test, config-tool, docs, codeql]", text)
        self.assertEqual(text.count("if: always() && steps.tests.outputs.ran_tests == 'true'"), 3)
        codeql = (ROOT / ".github/workflows/codeql.yml").read_text()
        self.assertIn("  workflow_call:", codeql)
        self.assertIn("  schedule:", codeql)
        self.assertNotIn("  pull_request:", codeql)

    def test_megalinter_reports_on_main_and_reuses_the_pr_scanner(self):
        ci = (ROOT / ".github/workflows/ci.yml").read_text()
        scan = (ROOT / ".github/workflows/megalinter.yml").read_text()
        config = (ROOT / ".mega-linter.yml").read_text(encoding="utf-8-sig")
        self.assertIn("uses: ./.github/workflows/megalinter.yml", ci)
        self.assertIn("  workflow_call:\n", scan)
        self.assertIn("  push:\n    branches: [main]\n", scan)
        self.assertIn("VALIDATE_ALL_CODEBASE: ${{ github.event_name != 'pull_request' }}", scan)
        self.assertIn("  - REPOSITORY_BETTERLEAKS\n", config)
        self.assertNotIn("REPOSITORY_GITLEAKS", config)

        # The v10 aggregate file must be uploaded even after lint findings; a missing report is an error.
        upload = scan.split("      - name: Upload SARIF\n", 1)[1]
        self.assertIn("if: ${{ !cancelled() }}", upload)
        self.assertNotIn("hashFiles", upload)
        self.assertIn("sarif_file: megalinter-reports/megalinter-report.sarif", upload)
        self.assertIn("category: megalinter", upload)


if __name__ == "__main__":
    unittest.main()
