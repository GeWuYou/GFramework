---
title: "Build a Godot game"
description: "Run and extend Dodge the Creeps with gameplay, UI, saves, and content."
---

# Build a Godot game {#overview}

The example connects movement, spawning, collisions, and scoring to typed UI, pause, and saves. Run the complete project first, then follow the wiring below.

## Prepare the tools {#prepare}

Install the [repository SDK and matching Godot .NET editor](../getting-started/installation.md#requirements). Run the commands below from the repository root.

## Build and launch {#project}

```bash
dotnet build Samples/DodgeTheCreeps/DodgeTheCreeps.csproj -c Debug
godot --headless --editor --path Samples/DodgeTheCreeps --import
godot --path Samples/DodgeTheCreeps
```

## Assets {#assets}

See the [asset credits and licenses](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/ASSETS.md). Keep the relevant notices and credits if you reuse assets.

## Player movement {#player}

Use the native character to read directional input and play its animation.

<<< ../../Samples/DodgeTheCreeps/Gameplay/DodgePlayer.cs#tutorial-movement{csharp}

## Enemies, collisions, and scoring {#world}

Update enemies and scoring in the game loop. Close the current run's save when the game ends.

<<< ../../Samples/DodgeTheCreeps/Gameplay/DodgeWorld.cs#tutorial-loop{csharp}

<<< ../../Samples/DodgeTheCreeps/Gameplay/DodgeWorld.cs#tutorial-collision{csharp}

## Pages and windows {#routes}

Define typed arguments and policies for the HUD, settings, and quit confirmation.

<<< ../../Samples/DodgeTheCreeps/DodgeRoutes.cs{csharp}

## Compose the services {#services}

Create a shared dispatcher, view root, and storage on the host thread. Register the page and window factories.

<<< ../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-services{csharp}

## Start a run {#start-game}

Call the application use case from the menu and show the corresponding UI.

<<< ../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-start-game{csharp}

## Pause and settings {#pause-settings}

Open the pause menu during play, then open Settings on top. Closing Settings leaves the main pause request active, and returning to the game resumes play. Keep settings preview separate from saving confirmed settings.

<<< ../../Samples/DodgeTheCreeps/DodgeApp.Settings.cs#tutorial-save-settings{csharp}

## Save and continue {#save-confirm}

Capture and store a game snapshot. Restart, choose Continue, and check position and score. See [implementing saves](../tutorials/save-system.md) for the full flow.

<<< ../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-save-game{csharp}

## Shut down the host {#shutdown}

Wait for in-flight operations and native services to stop before releasing scene tree resources.

<<< ../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-shutdown{csharp}

## Smoke test and export {#export}

```bash
godot --headless --path Samples/DodgeTheCreeps -- --smoke
godot --headless --path Samples/DodgeTheCreeps -- --persistence-smoke
```

Check for `DODGE_SMOKE_RESULT PASS`. Then use the ID printed by each flow with `--smoke-continue=<id>` or `--persistence-smoke-continue=<id>` to verify reading from a new process. Install matching .NET export templates and select Windows Desktop or Linux Desktop in Godot's Export panel.

## Add DLC and mods {#content-extension}

Use [content packs](../game/content-packs.md) to add records and assets, or implement a [code mod contract](../game/code-mods.md). Preserve stable content IDs and verify that saved progress resolves the records it references.

Next, [move pure C# use cases out of the host](../tutorials/godot-advanced-project.md).
