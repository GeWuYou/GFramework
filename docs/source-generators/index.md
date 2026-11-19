---
title: "Source generators"
description: "Choose GFramework generators by compile-time task."
---

# Source generators {#overview}

Add the matching `GeWuYou.GFramework.*.SourceGenerators` package to the consuming project. The package supplies its associated attributes and generated helpers. After a build, inspect generated sources and diagnostics in the IDE.

| Package suffix | Features |
| --- | --- |
| `Core.SourceGenerators` | [Enum extensions](../source-generators/enum-generator.md) and [priority](../source-generators/priority-generator.md) |
| `Game.SourceGenerators` | [Configuration types and tables](../source-generators/schema-config-generator.md) |
| `Cqrs.SourceGenerators` | [Handler registries](../source-generators/cqrs-handler-registry-generator.md) |
| `Godot.SourceGenerators` | [Project metadata](../source-generators/godot-project-generator.md), [node lookups](../source-generators/get-node-generator.md), [signal bindings](../source-generators/bind-node-signal-generator.md), [scene helpers](../source-generators/auto-scene-generator.md), and [collection registration](../source-generators/auto-register-exported-collections-generator.md) |
