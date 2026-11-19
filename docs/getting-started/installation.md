---
title: "Installation"
description: "Choose NuGet packages and set up the source build and Godot environment."
---

# Installation {#overview}

## Choose a runtime {#packages}

| Need | NuGet package |
| --- | --- |
| Lifecycle, events, state, resources, and coroutines | `GeWuYou.GFramework.Core` |
| Requests, notifications, and streams | `GeWuYou.GFramework.Cqrs` |
| Configuration, content packs, mods, saves, scenes, and UI | `GeWuYou.GFramework.Game` |
| Use Game features in Godot | `GeWuYou.GFramework.Godot` |
| Run ECS systems on Arch | `GeWuYou.GFramework.Ecs.Arch` |
| Add Harmony method patches to code mods | `GeWuYou.GFramework.Game.Harmony` |

```bash
dotnet add package GeWuYou.GFramework.Core
```

`Game` brings in `Core`, and `Godot` brings in `Game`. A project that defines contracts can reference the relevant `.Abstractions` package directly. The `GeWuYou.GFramework` metapackage bundles the Core, Game, and CQRS runtimes.

## Requirements {#requirements}

Runtime packages target `net8.0`, `net9.0`, and `net10.0`. Check the target frameworks and dependencies of the NuGet version you choose. To build the repository or run its examples, use .NET SDK 10.0.403 or a newer patch in the same feature band, as specified in `global.json`. The Godot examples use Godot .NET 4.7.2.

## Source generation {#generators}

Add the `.SourceGenerators` package for the feature you use. The [generator guide](../source-generators/index.md) lists the entry points for enums, configuration, CQRS, and Godot.

NuGet consumers can set `<EnableGFrameworkGlobalUsings>true</EnableGFrameworkGlobalUsings>` in the project properties to enable package-provided global imports. Exclude a namespace with a `GFrameworkExcludedUsing` item.

Next, [run the minimal example](../getting-started/quick-start.md).
