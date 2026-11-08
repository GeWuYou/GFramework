#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Run the real normalization hook against disposable indexes without altering user work."""
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parent.parent
HOOK = ROOT / ".githooks/pre-commit"


class PreCommitTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.git("init", "-q")
        self.git("config", "core.autocrlf", "false")
        self.git("config", "core.safecrlf", "false")
        (self.root / ".gitattributes").write_text("* text=auto eol=lf\n*.bin binary\n", encoding="utf-8")
        self.git("add", ".gitattributes")

    def git(self, *args, data=None):
        return subprocess.run(["git", *args], cwd=self.root, input=data, check=True,
                              stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout

    def stage_raw(self, name, staged, working=None, mode="100644"):
        path = self.root / name
        path.write_bytes(staged if working is None else working)
        object_id = self.git("hash-object", "-w", "--no-filters", "--stdin", data=staged).decode().strip()
        self.git("update-index", "--add", "--cacheinfo", mode, object_id, name)

    def run_hook(self):
        subprocess.run(["bash", str(HOOK)], cwd=self.root, check=True,
                       stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    def test_partial_staging_preserves_index_selection_and_working_tree(self):
        working = b"staged line\r\nunfinished line\r\n"
        self.stage_raw("partial.txt", b"staged line\r\n", working)
        self.run_hook()
        self.assertEqual(self.git("show", ":partial.txt"), b"staged line\n")
        self.assertEqual((self.root / "partial.txt").read_bytes(), working)

    def test_space_unicode_tab_and_newline_paths_are_not_split(self):
        for name in ("some file.txt", "\u4e2d\u6587\u8bf4\u660e.txt", "tab\tfile.txt", "line\nfile.txt"):
            self.stage_raw(name, b"one\r\ntwo\r\n")
        self.run_hook()
        for name in ("some file.txt", "\u4e2d\u6587\u8bf4\u660e.txt", "tab\tfile.txt", "line\nfile.txt"):
            self.assertEqual(self.git("show", ":" + name), b"one\ntwo\n")
            self.assertEqual((self.root / name).read_bytes(), b"one\r\ntwo\r\n")

    def test_binary_attributes_and_auto_binary_content_are_untouched(self):
        self.stage_raw("declared.bin", b"binary by attribute\r\n")
        self.stage_raw("detected.txt", b"\0binary by content\r\n")
        self.run_hook()
        self.assertEqual(self.git("show", ":declared.bin"), b"binary by attribute\r\n")
        self.assertEqual(self.git("show", ":detected.txt"), b"\0binary by content\r\n")

    def test_normalization_does_not_apply_non_idempotent_clean_filter_again(self):
        attributes = self.root / ".gitattributes"
        attributes.write_text("* text=auto eol=lf\n*.filtered filter=prefix\n", encoding="utf-8")
        self.git("config", "filter.prefix.clean", "sed 's/^/CLEAN:/'")
        (self.root / "message.filtered").write_bytes(b"original line\n")
        self.git("add", ".gitattributes", "message.filtered")
        original = self.git("show", ":message.filtered")
        self.assertEqual(original, b"CLEAN:original line\n")

        self.run_hook()

        self.assertEqual(self.git("show", ":message.filtered"), original)
        self.assertEqual((self.root / "message.filtered").read_bytes(), b"original line\n")

    def test_only_cached_attributes_determine_text_and_binary(self):
        attributes = self.root / ".gitattributes"
        attributes.write_text("* text=auto eol=lf\n*.special binary\n", encoding="utf-8")
        self.git("add", ".gitattributes")
        self.stage_raw("cached-binary.special", b"staged binary\r\n")
        self.stage_raw("cached-text.txt", b"staged text\r\n")
        working_attributes = "* text=auto eol=lf\n*.special text\n*.txt binary\n"
        attributes.write_text(working_attributes, encoding="utf-8")

        self.run_hook()

        self.assertEqual(self.git("show", ":cached-binary.special"), b"staged binary\r\n")
        self.assertEqual(self.git("show", ":cached-text.txt"), b"staged text\n")
        self.assertEqual(attributes.read_text(encoding="utf-8"), working_attributes)

    def test_binary_nul_after_large_text_prefix_is_not_modified(self):
        payload = b"first line\r\n" + b"x" * 150000 + b"\0binary tail\r\n"
        self.stage_raw("late-binary.txt", payload)

        self.run_hook()

        self.assertEqual(self.git("show", ":late-binary.txt"), payload)

    def test_index_only_file_and_executable_mode_are_preserved(self):
        self.stage_raw("executable.sh", b"echo staged\r\n", mode="100755")
        self.stage_raw("index-only.txt", b"staged only\r\n")
        (self.root / "index-only.txt").unlink()
        self.run_hook()
        self.assertEqual(self.git("show", ":index-only.txt"), b"staged only\n")
        self.assertFalse((self.root / "index-only.txt").exists())
        self.assertTrue(self.git("ls-files", "--stage", "executable.sh").startswith(b"100755 "))

    def test_symlink_blob_is_not_line_normalized(self):
        self.stage_raw("link", b"target\r\n", mode="120000")
        self.run_hook()
        self.assertEqual(self.git("show", ":link"), b"target\r\n")

    def test_git_enumeration_failure_is_not_a_successful_hook(self):
        stub = self.root / "stub-bin"
        stub.mkdir()
        executable = stub / "git"
        executable.write_text("#!/bin/sh\nexit 17\n", encoding="utf-8")
        executable.chmod(0o755)
        environment = {**os.environ, "PATH": str(stub) + os.pathsep + os.environ["PATH"]}
        result = subprocess.run(["bash", str(HOOK)], cwd=self.root, env=environment,
                                stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        self.assertNotEqual(result.returncode, 0)


class BuildHookSetupTests(unittest.TestCase):
    def test_hook_configuration_uses_portable_command_and_msbuild_error_handling(self):
        tree = ET.parse(ROOT / "Directory.Build.targets")
        command = tree.find(".//Target[@Name='EnsureGitHooksPath']/Exec")
        self.assertIsNotNone(command)
        self.assertEqual(command.get("Command"), "git config core.hooksPath .githooks")
        self.assertEqual(command.get("IgnoreExitCode"), "true")
        self.assertEqual(command.get("IgnoreStandardErrorWarningFormat"), "true")
        self.assertEqual(command.get("StandardErrorImportance"), "low")


if __name__ == "__main__":
    unittest.main()
