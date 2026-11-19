---
title: "Structure an advanced Godot project"
description: "Separate pure C# gameplay rules, use cases, and Godot adapters."
---

# Structure an advanced Godot project {#overview}

## Run the example {#run}

```bash
bash scripts/prepare-dodge-advanced-mods.sh
dotnet build Samples/DodgeTheCreeps.Advanced/DodgeTheCreeps.Advanced.csproj -c Debug
godot --headless --editor --path Samples/DodgeTheCreeps.Advanced --import
godot --path Samples/DodgeTheCreeps.Advanced
```

## Organize features {#features}

| Project | Responsibility |
| --- | --- |
| `DodgeTheCreeps.Advanced.Core` | Game rules, progress snapshots, and application ports |
| `DodgeTheCreeps.Advanced` | UI, native gameplay adapters, settings, and content |
| `DodgeTheCreeps.Advanced.Tests` | Tests for rules and use cases |

Start with rules and snapshots. Define ports where the use case crosses a host boundary. The use case coordinates saving, restoring, and scoring, then reports the result to the native view.

<<< ../../Samples/DodgeTheCreeps.Advanced.Core/Gameplay/RunUseCases.cs{csharp}

## Compose dependencies {#composition}

Register dependencies in the composition root. Give startup components responsibility for loading and wait for content, UI, and storage to stop before releasing native objects.

<<< ../../Samples/DodgeTheCreeps.Advanced/Composition/AdvancedGameArchitecture.cs#advanced-composition{csharp}

## Verify behavior {#verify}

Run use case tests with `dotnet test Samples/DodgeTheCreeps.Advanced.Tests -c Release`. Run `godot --headless --path Samples/DodgeTheCreeps.Advanced -- --smoke` for the native flow and use its output ID to continue in a new process.
