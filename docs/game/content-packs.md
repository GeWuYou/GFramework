---
title: "DLC and data mods"
description: "Discover, order, and merge game content packs."
---

# DLC and data mods {#overview}

## Prepare a pack {#packages}

Give each pack a directory and a `content-pack.json` manifest. Declare its ID, version, game compatibility range, dependencies, and content files. Use `major.minor.patch` versions. Lower range endpoints are inclusive and upper endpoints are exclusive. Declare optional dependencies and incompatible packages explicitly.

## Modify content {#patches}

Store and reference stable IDs, such as `base:walker`. Use `add` to introduce a record, `replace` for a full replacement, `patch` for fields, and `remove` to delete a record.

```yaml
- op: patch
  table: enemies
  id: base:walker
  value:
    speed: 180
```

A patch preserves the ID, merges objects recursively, and replaces arrays and scalars as whole values. `null` removes a field. To set an explicit null, replace the full record. The final catalog is validated against the game's schema and reference rules.

## Load a game session {#session}

Use `ContentPackDiscovery` to find packs under roots the game supplies. Call `ContentPackResolver` with a `ContentPackProfile` and host compatibility information. Register each table's CLR type, schema, and ID selector with `ContentCatalogLoader`. After loading, prepare native assets and open the game.

## Save dependencies and recovery {#save-recovery}

<span id="存档依赖与降级恢复"></span>

A save records the enabled pack snapshot and stable content IDs. If content is missing, show the differences from save preparation and let the game decide which references to replace or remove. After player approval, back up and commit. See [prepare a save load](./data.md#load).

See the [Godot content extension example](../tutorials/godot-complete-project.md#content-extension) for a complete flow. For DLL entry points, see [code mods](../game/code-mods.md).
