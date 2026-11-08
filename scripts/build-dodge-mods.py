#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Build independent example DLLs and stage portable files for the Godot sample export."""

import argparse
from pathlib import Path
import shutil
import subprocess


def main() -> None:
    """Use the member's ordinary build entrypoint and copy only mod-owned artifacts."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--configuration", default="Release", choices=("Debug", "Release"))
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    examples = (("BonusMod", "code-bonus"), ("HarmonyMod", "code-double"))
    for suffix, pack_id in examples:
        name = "DodgeTheCreeps." + suffix
        project = root / "Samples" / name / (name + ".csproj")
        subprocess.run(["dotnet", "build", str(project), "-c", args.configuration], cwd=root, check=True)
        output = project.parent / "bin" / args.configuration / "net8.0"
        destination = root / "Samples" / "DodgeTheCreeps" / "content_sources" / pack_id
        destination.mkdir(parents=True, exist_ok=True)
        # Shared contracts and Harmony are supplied by the host, never bundled as private mod copies.
        for extension in (".dll", ".deps.json"):
            shutil.copyfile(output / (name + extension), destination / (name + extension + ".source"))
        print("Staged", pack_id)


if __name__ == "__main__":
    main()
