#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Exercise cumulative selection with real project wiring in isolated Git repositories."""
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

from ci_impact import ROOT, Snapshot, select


class ImpactTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        snapshot = Snapshot(ROOT, "HEAD")
        cls.project_files = {p: snapshot.text(p) for p in snapshot.projects}
        cls.project_files["GFramework.sln"] = snapshot.text("GFramework.sln")
        # Own the cross-module edge in this isolated fixture. Production test wiring can
        # evolve without removing coverage for transitive and deleted-reference selection.
        consumer = "GFramework.Game.Tests/GFramework.Game.Tests.csproj"
        if "GFramework.Godot.csproj" not in cls.project_files[consumer]:
            cls.project_files[consumer] = cls.project_files[consumer].replace("</Project>",
                '<ItemGroup>\n<ProjectReference Include="../GFramework.Godot/GFramework.Godot.csproj"/>\n</ItemGroup>\n</Project>')

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.git("init", "-q")
        self.git("config", "user.name", "CI selection test")
        self.git("config", "user.email", "ci@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        for path, text in self.project_files.items():
            self.write(path, text)
        self.write("engineering/README.md", "Engineering")
        self.write("GFramework.Ecs.Arch/Example.cs", "class Example {}")
        self.write("GFramework.Game/Example.cs", "class Example {}")
        self.base = self.commit()

    def git(self, *args):
        return subprocess.check_output(["git", *args], cwd=self.root, stderr=subprocess.PIPE).decode().strip()

    def write(self, path, text):
        output = self.root / path
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(text, encoding="utf-8")

    def commit(self):
        self.git("add", ".")
        self.git("commit", "-qm", "test fixture")
        return self.git("rev-parse", "HEAD")

    def change(self, path, text="changed"):
        self.write(path, text)
        return select(self.root, self.base, self.commit())

    def test_governance_skips_heavy_jobs(self):
        plan = self.change("engineering/README.md")
        self.assertFalse(any(plan[k] for k in ("full", "dotnet", "tool", "docs", "lint", "codeql")))

    def test_personal_skill_configuration_is_governance(self):
        plan = self.change(".agents/skills/example/config.yaml")
        self.assertFalse(plan["dotnet"] or plan["lint"])

    def test_docs_and_tool_are_independent(self):
        for name, flag in (("docs/.vitepress/config.mts", "docs"),
                           ("tools/gframework-config-tool/src/configValidation.js", "tool")):
            with self.subTest(name=name):
                plan = self.change(name)
                self.assertTrue(plan[flag])
                self.assertFalse(plan["dotnet"] or plan["codeql"])
                self.git("reset", "--hard", self.base)

    def test_ecs_runs_its_own_tests_without_game_or_godot(self):
        plan = self.change("GFramework.Ecs.Arch/Example.cs")
        self.assertEqual(plan["test_projects"], ["GFramework.Ecs.Arch.Tests/GFramework.Ecs.Arch.Tests.csproj"])
        self.assertEqual(plan["package_ids"], ["GeWuYou.GFramework.Ecs.Arch"])
        self.assertFalse(plan["tool"] or plan["docs"] or plan["full"])

    def test_godot_includes_declared_cross_module_test_consumer(self):
        plan = self.change("GFramework.Godot/Example.cs")
        # Adoption compilation tests consume real Godot runtime metadata as well as generators.
        self.assertEqual(set(plan["test_projects"]), {
            "GFramework.Game.Tests/GFramework.Game.Tests.csproj",
            "GFramework.Godot.Tests/GFramework.Godot.Tests.csproj",
            "GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj"})
        self.assertNotIn("GFramework.Ecs.Arch/GFramework.Ecs.Arch.csproj", plan["build_projects"])

    def test_test_only_change_does_not_pack_runtime(self):
        plan = self.change("GFramework.Core.Tests/Example.cs")
        self.assertEqual(plan["test_projects"], ["GFramework.Core.Tests/GFramework.Core.Tests.csproj"])
        self.assertEqual(plan["pack_projects"], [])

    def test_shared_test_helpers_propagate_to_consumers(self):
        plan = self.change("GFramework.Tests.Common/Example.cs")
        self.assertEqual(set(plan["test_projects"]), {
            "GFramework.Core.Tests/GFramework.Core.Tests.csproj", "GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj"})
        self.assertEqual(plan["pack_projects"], [])

    def test_generator_private_references_and_linked_source_propagate(self):
        plan = self.change("GFramework.SourceGenerators.Common/Internals/IsExternalInit.cs")
        self.assertIn("GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj", plan["build_projects"])
        self.assertIn("GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj", plan["test_projects"])
        self.assertNotIn("GFramework.Godot/GFramework.Godot.csproj", plan["pack_projects"])

    def test_imported_targets_select_consumer(self):
        plan = self.change("GFramework.Game.SourceGenerators/GeWuYou.GFramework.Game.SourceGenerators.targets", "<Project/>")
        self.assertIn("GFramework.Game.Tests/GFramework.Game.Tests.csproj", plan["test_projects"])

    def test_package_readme_is_not_treated_as_plain_documentation(self):
        plan = self.change("README.md")
        self.assertTrue(plan["dotnet"])
        self.assertEqual(plan["pack_projects"], ["GFramework.csproj"])
        self.assertEqual(plan["test_projects"], [])
        self.assertFalse(plan["codeql"])

    def test_unpacked_module_readme_does_not_match_root_package_readme(self):
        # The shared generator helper is not published; its guide must not inherit the root package's README input.
        plan = self.change("GFramework.SourceGenerators.Common/README.md")
        self.assertFalse(plan["dotnet"] or plan["codeql"])
        self.assertEqual(plan["pack_projects"], [])

    def test_packed_module_readme_selects_only_its_package(self):
        plan = self.change("GFramework.Ecs.Arch/README.md")
        self.assertTrue(plan["dotnet"])
        self.assertEqual(plan["pack_projects"], ["GFramework.Ecs.Arch/GFramework.Ecs.Arch.csproj"])
        self.assertEqual(plan["package_ids"], ["GeWuYou.GFramework.Ecs.Arch"])
        self.assertEqual(plan["build_projects"], [])
        self.assertEqual(plan["test_projects"], [])
        self.assertFalse(plan["full"] or plan["codeql"] or plan["docs"] or plan["tool"])

    def test_analyzer_releases_markdown_is_compile_input(self):
        plan = self.change("GFramework.SourceGenerators.Common/AnalyzerReleases.Unshipped.md")
        self.assertTrue(plan["dotnet"] and plan["codeql"])
        self.assertIn("GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj", plan["test_projects"])

    def test_module_build_configuration_is_scoped(self):
        plan = self.change("GFramework.Ecs.Arch.Abstractions/Directory.Build.props", "<Project/>")
        self.assertEqual(plan["test_projects"], ["GFramework.Ecs.Arch.Tests/GFramework.Ecs.Arch.Tests.csproj"])
        self.assertFalse(plan["full"])

    def test_multiple_commits_keep_earlier_module_changes(self):
        self.change("GFramework.Ecs.Arch/Example.cs")
        plan = self.change("tools/gframework-config-tool/src/configValidation.js")
        self.assertTrue(plan["dotnet"] and plan["tool"])
        self.assertIn("GFramework.Ecs.Arch/Example.cs", plan["files"])

    def test_deletion_and_cross_module_rename_select_both_sides(self):
        self.git("mv", "GFramework.Ecs.Arch/Example.cs", "GFramework.Game/Moved.cs")
        plan = select(self.root, self.base, self.commit())
        self.assertIn("GFramework.Ecs.Arch.Tests/GFramework.Ecs.Arch.Tests.csproj", plan["test_projects"])
        self.assertIn("GFramework.Game.Tests/GFramework.Game.Tests.csproj", plan["test_projects"])

    def test_removed_reference_cannot_hide_previously_affected_consumer(self):
        project = "GFramework.Game.Tests/GFramework.Game.Tests.csproj"
        text = self.project_files[project]
        text = "\n".join(line for line in text.splitlines() if "GFramework.Godot.csproj" not in line)
        self.write(project, text)
        plan = self.change("GFramework.Godot/Example.cs")
        self.assertIn(project, plan["test_projects"])

    def test_merge_revision_consumers_are_included(self):
        # Simulate a consumer introduced by the target branch in the tested merge tree.
        self.write("GFramework.Ecs.Arch/Example.cs", "changed")
        head = self.commit()
        project = "GFramework.Game.Tests/GFramework.Game.Tests.csproj"
        text = self.project_files[project].replace("</Project>",
            '<ItemGroup><ProjectReference Include="../GFramework.Ecs.Arch/GFramework.Ecs.Arch.csproj"/></ItemGroup></Project>')
        self.write(project, text)
        self.commit()
        plan = select(self.root, self.base, head)
        self.assertIn(project, plan["test_projects"])

    def test_missing_baseline_and_shared_inputs_fail_broad(self):
        head = self.git("rev-parse", "HEAD")
        self.assertTrue(select(self.root, None, head)["full"])
        self.assertTrue(select(self.root, "missing-baseline", head)["full"])
        self.assertTrue(self.change("Directory.Build.props", "<Project/>")["full"])

    def test_unknown_input_and_unresolved_reference_fail_broad(self):
        self.assertTrue(self.change("new-module/unknown.dat")["full"])
        project = "GFramework.Ecs.Arch/GFramework.Ecs.Arch.csproj"
        text = self.project_files[project].replace("..\\GFramework.Core\\", "$(Unsupported)\\")
        plan = self.change(project, text)
        self.assertTrue(plan["full"] and plan["dotnet"])
        self.assertIn("Unresolved", json.dumps(plan["reasons"]))

    def test_codeql_configuration_does_not_trigger_tool_or_docs(self):
        plan = self.change(".github/workflows/codeql.yml")
        self.assertTrue(plan["codeql"] and plan["lint"])
        self.assertFalse(plan["tool"] or plan["docs"] or plan["dotnet"])

    def test_megalinter_configuration_only_selects_lint(self):
        plan = self.change(".github/workflows/megalinter.yml")
        self.assertTrue(plan["lint"])
        self.assertFalse(any(plan[k] for k in ("full", "dotnet", "tool", "docs", "codeql")))

    def test_unknown_workflow_or_engineering_program_fails_broad(self):
        for name in (".github/workflows/new-workflow.yml", "engineering/new-build.py"):
            with self.subTest(name=name):
                self.assertTrue(self.change(name)["full"])
                self.git("reset", "--hard", self.base)


if __name__ == "__main__":
    unittest.main()
