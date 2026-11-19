---
title: "Typed game configuration"
description: "Define, load, and query static game content with YAML and JSON Schema."
---

# Typed game configuration {#overview}

## Files and project setup {#project}

1. Install `GeWuYou.GFramework.Game` and `GeWuYou.GFramework.Game.SourceGenerators`.
2. Put schemas under `schemas/`. Put YAML files in the directory specified by `x-gframework-config-path`, with one record per file.
3. The NuGet generator includes `schemas/**/*.schema.json` as compilation inputs. With project references, configure `AdditionalFiles` in the consuming project.
4. After building, use the generated tables and registration helpers in `GFramework.Game.Config.Generated`.

## Schema and data {#schema}

The GodotIntegration example defines an integer ID and a speed range for each record:

<<< ../../Samples/GodotIntegration/schemas/gameplay.schema.json{json}

<<< ../../Samples/GodotIntegration/config/gameplay/default.yaml{yaml}

Set `additionalProperties: false` explicitly on objects. Declare one `items` schema for an array. Represent variations with explicit object fields and use supported object conditions for validation. See the [configuration generator reference](../source-generators/schema-config-generator.md) for keywords and diagnostics.

## Load and read tables {#load}

```csharp
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

var registry = new ConfigRegistry();
var loader = new YamlConfigLoader("config-root")
    .RegisterAllGeneratedConfigTables();
await loader.LoadAsync(registry);
```

Set `config-root` to the directory containing `config/` and `schemas/`. After loading, resolve a generated table from the registry and look up records by ID. Provide the registry to consumers as an instance dependency. Use `GameConfigBootstrap` to manage loading and the reload handle together.

## Godot exports and reloads {#godot}

Use `GodotYamlConfigLoader` to read schema and YAML files from `res://`. Include these text files in the export preset. During development, enable hot reload for an accessible filesystem path. Log reload diagnostics and keep the previous table when a reload fails. Dispose the reload handle when the session ends.

To combine base and extension data, see [content packs](../game/content-packs.md). To edit configuration, see the [VS Code configuration tool](../game/config-tool.md).
