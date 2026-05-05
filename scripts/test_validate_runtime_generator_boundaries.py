#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Regression tests for runtime/source-generator boundary validation."""

from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parent / "validate-runtime-generator-boundaries.py"
MODULE_SPEC = importlib.util.spec_from_file_location("validate_runtime_generator_boundaries", MODULE_PATH)
if MODULE_SPEC is None or MODULE_SPEC.loader is None:
    raise RuntimeError(f"Unable to load module spec from {MODULE_PATH}")

validate_runtime_generator_boundaries = importlib.util.module_from_spec(MODULE_SPEC)
sys.modules[MODULE_SPEC.name] = validate_runtime_generator_boundaries
MODULE_SPEC.loader.exec_module(validate_runtime_generator_boundaries)


class ValidateRuntimeGeneratorBoundariesTests(unittest.TestCase):
    """Covers attribute matching edge cases that previously caused false negatives."""

    def setUp(self) -> None:
        self.patterns = validate_runtime_generator_boundaries.compile_attribute_patterns()

    def test_matches_standalone_attribute(self) -> None:
        pattern = self.patterns["GenerateEnumExtensions"]

        self.assertIsNotNone(pattern.search("[GenerateEnumExtensions]"))

    def test_matches_parameterized_attribute(self) -> None:
        pattern = self.patterns["GenerateEnumExtensions"]

        self.assertIsNotNone(pattern.search("[GenerateEnumExtensions(typeof(string))]"))

    def test_matches_non_leading_attribute_in_attribute_list(self) -> None:
        pattern = self.patterns["GenerateEnumExtensions"]

        self.assertIsNotNone(pattern.search("[Serializable, GenerateEnumExtensions]"))

    def test_matches_fully_qualified_attribute(self) -> None:
        pattern = self.patterns["Priority"]

        self.assertIsNotNone(
            pattern.search("[global::GFramework.Core.SourceGenerators.Abstractions.Bases.PriorityAttribute(10)]")
        )

    def test_ignores_xml_doc_example_attribute(self) -> None:
        text = "///     [ContextAware]\npublic interface IController;\n"
        pattern = self.patterns["ContextAware"]
        match = pattern.search(text)

        self.assertIsNotNone(match)
        self.assertTrue(validate_runtime_generator_boundaries.is_comment_attribute_match(text, match.start()))


if __name__ == "__main__":
    unittest.main()
