---
title: "Implement saves"
description: "Persist a recoverable game snapshot and continue after restart."
---

# Implement saves {#overview}

## Define a snapshot {#snapshot}

Implement `IData` for a snapshot with the values needed to restore the game. The example records the player, enemies, timers, and random state.

<<< ../../Samples/DodgeTheCreeps/DodgeProgress.cs{csharp}

## Create a repository and save {#repository}

Use `GodotFileStorage` and `user://` for player files. Set the game identity, slot path, and current schema version, then save through `SaveRepository<T>`. Update the Continue action after saving completes.

<<< ../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-save-game{csharp}

## Load, restore, and retire a save {#restore}

Use `PrepareLoadAsync` to prepare a document that needs conversion. Ask the player for confirmation where required and restore the world after a successful commit. Preserve stable content keys and apply [explicit migration rules](../tutorials/data-migration.md). When a run ends, retire its save according to the game rules.

Run the persistence flow from the [native smoke tests](../tutorials/godot-complete-project.md#export) and use the first run's ID to verify continuing in a new process.
