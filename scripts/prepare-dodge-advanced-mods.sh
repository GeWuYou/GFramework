#!/usr/bin/env bash
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

# Build only independently shipped mod assemblies; the advanced host supplies shared contracts.
set -euo pipefail
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
configuration="${1:-Release}"
case "$configuration" in
    Debug|Release) ;;
    *) echo "Configuration must be Debug or Release." >&2; exit 2 ;;
esac

for entry in "BonusMod:code-bonus" "HarmonyMod:code-double"; do
    suffix="${entry%%:*}"
    pack_id="${entry#*:}"
    name="DodgeTheCreeps.$suffix"
    project="$repo_root/Samples/$name/$name.csproj"
    dotnet build "$project" -c "$configuration"
    output="$repo_root/Samples/$name/bin/$configuration/net8.0"
    destination="$repo_root/Samples/DodgeTheCreeps.Advanced/content_sources/$pack_id"
    mkdir -p "$destination"
    for extension in .dll .deps.json; do
        cp -- "$output/$name$extension" "$destination/$name$extension.source"
    done
    echo "Staged advanced package $pack_id"
done
