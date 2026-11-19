---
title: "Data and versioned saves"
description: "Choose a repository, then save, prepare, and restore game progress."
---

# Data and versioned saves {#overview}

| Task | Repository |
| --- | --- |
| Read or write one independent object | `DataRepository` |
| Store several settings sections in one file | `UnifiedSettingsDataRepository` |
| Use slots, versions, and content pack snapshots | `SaveRepository<TSaveData>` |

## Configure a save repository {#repository}

Provide storage that implements `IStorageBackup`, such as `FileStorage`, in the composition root. Set the save root and slot naming through `SaveConfiguration`. Set the game ID, version, migrations, and validation in `SaveRepositoryOptions<T>`. Implement `IData` for the game snapshot. The repository wraps it in a versioned document.

## Load and confirm {#load}

- Use `ExistsAsync` to decide whether to show Continue. Use `LoadAsync` for a slot that can be read directly.
- For a migration or content reconciliation, call `PrepareLoadAsync` to get a candidate. Check `RequiresConfirmation` and let the player approve changes that need consent.
- When `RequiresCommit` is set, call `CommitPreparedAsync` to retain the original backup and commit. Restore the world from the returned data after it succeeds.
- Use `RestoreBackupAsync` to restore a selected backup, then load the slot through the normal flow.

## Mod save data {#mod-data}

<span id="模组数据"></span>

Configure a `ModSavePolicy` for enabled mods. Start a new game with an empty draft and resume with `ModState` from the prepared save. Give each mod its own scope, then save the game and draft with `SaveDocumentAsync`.

Continue with [implementing saves](../tutorials/save-system.md) or [bidirectional migrations](../tutorials/data-migration.md).
