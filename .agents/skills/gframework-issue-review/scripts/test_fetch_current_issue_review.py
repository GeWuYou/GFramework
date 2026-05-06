#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Regression tests for the GFramework issue review fetch helper."""

from __future__ import annotations

import importlib.util
from pathlib import Path
import unittest


SCRIPT_PATH = Path(__file__).with_name("fetch_current_issue_review.py")
MODULE_SPEC = importlib.util.spec_from_file_location("fetch_current_issue_review", SCRIPT_PATH)
if MODULE_SPEC is None or MODULE_SPEC.loader is None:
    raise RuntimeError(f"Unable to load module from {SCRIPT_PATH}.")

MODULE = importlib.util.module_from_spec(MODULE_SPEC)
MODULE_SPEC.loader.exec_module(MODULE)


class SelectSingleOpenIssueNumberTests(unittest.TestCase):
    """Cover auto-resolution rules for open GitHub issues."""

    def test_select_single_open_issue_number_filters_pull_requests(self) -> None:
        """Pull requests in the issues API must not block the single-open-issue path."""
        selected = MODULE.select_single_open_issue_number(
            [
                {"number": 10, "pull_request": {"url": "https://example.test/pr/10"}},
                {"number": 11},
            ]
        )

        self.assertEqual(selected, 11)

    def test_select_single_open_issue_number_rejects_multiple_plain_issues(self) -> None:
        """Auto-resolution must stop when more than one plain issue is open."""
        with self.assertRaisesRegex(RuntimeError, "Multiple open GitHub issues found"):
            MODULE.select_single_open_issue_number([{"number": 11}, {"number": 12}])


class ExtractReferencesFromTextTests(unittest.TestCase):
    """Cover lightweight reference extraction used by the text and JSON output."""

    def test_extract_references_from_text_finds_issue_commit_and_path_mentions(self) -> None:
        """The helper should retain the high-signal references needed for follow-up triage."""
        references = MODULE.extract_references_from_text(
            "See #123, commit abcdef1234567890, and GFramework.Core/Systems/Runner.cs for the failing path."
        )

        self.assertEqual(references["issues"], ["#123"])
        self.assertEqual(references["commit_shas"], ["abcdef1234567890"])
        self.assertEqual(references["file_paths"], ["GFramework.Core/Systems/Runner.cs"])


class BuildTriageHintsTests(unittest.TestCase):
    """Cover next-action classification for non-bug issue flows."""

    def test_build_triage_hints_routes_docs_issue_to_docs_topic_without_bug_style_clarification(self) -> None:
        """Docs issues with a clear requested change should not be forced through bug-style clarification."""
        triage_hints = MODULE.build_triage_hints(
            {
                "title": "Update documentation landing page",
                "labels": ["docs"],
                "body": "The guide should explain the landing-page layout for new contributors.",
            },
            [],
        )

        self.assertEqual(triage_hints["issue_type_candidates"][0], "docs")
        self.assertEqual(triage_hints["affected_active_topics"], [])
        self.assertFalse(triage_hints["information_flags"]["needs_clarification"])
        self.assertEqual(triage_hints["next_action"], "start-new-docs-topic-with-boot")

    def test_build_triage_hints_routes_feature_issue_to_new_topic_when_request_is_clear(self) -> None:
        """Feature requests with explicit desired behavior should stay actionable without fake bug repro gates."""
        triage_hints = MODULE.build_triage_hints(
            {
                "title": "Support release note previews",
                "labels": ["enhancement"],
                "body": "The workflow should support previewing generated notes before completion.",
            },
            [],
        )

        self.assertEqual(triage_hints["issue_type_candidates"][0], "feature")
        self.assertEqual(triage_hints["affected_active_topics"], [])
        self.assertFalse(triage_hints["information_flags"]["needs_clarification"])
        self.assertEqual(triage_hints["next_action"], "start-new-topic-with-boot")


if __name__ == "__main__":
    unittest.main()
