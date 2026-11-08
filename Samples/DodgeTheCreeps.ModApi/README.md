# Dodge the Creeps mod API

This engine-neutral contract is owned by the sample game. The game and its external mods share this assembly at runtime. `IDodgeModApi` exposes scoped score registrations and plain save data without exposing native nodes.

Build the external examples with `python3 scripts/build-dodge-mods.py` from the repository root. See the [code mod guide](../../docs/zh-CN/game/code-mods.md) for the host lifecycle and package format.
