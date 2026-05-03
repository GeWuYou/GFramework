#!/usr/bin/env python3
"""Unit tests for Apache-2.0 license header automation."""

from __future__ import annotations

import tempfile
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import license_header


class LicenseHeaderTests(unittest.TestCase):
    """Covers file format and exclusion behavior for the license header script."""

    def test_inserts_csharp_header(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "Sample.cs"
            path.write_text("namespace Demo;\n", encoding="utf-8")

            license_header.add_license_header(path)

            text = path.read_text(encoding="utf-8")
            self.assertTrue(text.startswith("// Copyright (c) 2025-2026 GeWuYou\n"))
            self.assertIn("// SPDX-License-Identifier: Apache-2.0\n\nnamespace Demo;", text)

    def test_existing_apache_header_is_compliant(self) -> None:
        text = (
            "// Copyright (c) 2026 GeWuYou\n"
            "// Licensed under the Apache License, Version 2.0 (the \"License\");\n"
            "namespace Demo;\n"
        )

        self.assertTrue(license_header.has_license_header(text))

    def test_inserts_after_shebang(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "tool.py"
            path.write_text("#!/usr/bin/env python3\nprint('ok')\n", encoding="utf-8")

            license_header.add_license_header(path)

            self.assertEqual(
                "#!/usr/bin/env python3\n"
                "# Copyright (c) 2025-2026 GeWuYou\n"
                "# SPDX-License-Identifier: Apache-2.0\n"
                "\n"
                "print('ok')\n",
                path.read_text(encoding="utf-8"),
            )

    def test_uses_xml_comment_for_msbuild_files(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "Directory.Build.props"
            path.write_text("<Project>\n</Project>\n", encoding="utf-8")

            license_header.add_license_header(path)

            self.assertEqual(
                "<!--\n"
                "  Copyright (c) 2025-2026 GeWuYou\n"
                "  SPDX-License-Identifier: Apache-2.0\n"
                "-->\n"
                "\n"
                "<Project>\n"
                "</Project>\n",
                path.read_text(encoding="utf-8"),
            )

    def test_inserts_xml_header_after_declaration(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "Package.targets"
            path.write_text("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Project />\n", encoding="utf-8")

            license_header.add_license_header(path)

            self.assertEqual(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                "<!--\n"
                "  Copyright (c) 2025-2026 GeWuYou\n"
                "  SPDX-License-Identifier: Apache-2.0\n"
                "-->\n"
                "\n"
                "<Project />\n",
                path.read_text(encoding="utf-8"),
            )

    def test_repairs_xml_header_before_declaration(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "Package.targets"
            path.write_text(
                "<!--\n"
                "  Copyright (c) 2025-2026 GeWuYou\n"
                "  SPDX-License-Identifier: Apache-2.0\n"
                "-->\n"
                "\n"
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                "<Project />\n",
                encoding="utf-8",
            )

            self.assertTrue(license_header.needs_header_repair(path, path.read_text(encoding="utf-8")))
            license_header.repair_license_header(path)

            self.assertEqual(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                "<!--\n"
                "  Copyright (c) 2025-2026 GeWuYou\n"
                "  SPDX-License-Identifier: Apache-2.0\n"
                "-->\n"
                "\n"
                "<Project />\n",
                path.read_text(encoding="utf-8"),
            )

    def test_excludes_generated_snapshots_and_third_party_paths(self) -> None:
        self.assertFalse(license_header.is_supported_path("ai-libs/project/file.cs"))
        self.assertFalse(license_header.is_supported_path(".agents/skills/_shared/module-config.sh"))
        self.assertFalse(license_header.is_supported_path("third-party-licenses/package/LICENSE"))
        self.assertFalse(license_header.is_supported_path("GFramework.Tests/snapshots/Generated.g.cs"))
        self.assertFalse(license_header.is_supported_path(".ai/environment/tools.ai.yaml"))
        self.assertTrue(license_header.is_supported_path("GFramework.Core/Result.cs"))


if __name__ == "__main__":
    unittest.main()
