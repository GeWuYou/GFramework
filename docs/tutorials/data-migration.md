---
title: "Migrate versioned saves"
description: "Define upgrades and downgrades, confirm changes, and commit the conversion."
---

# Migrate versioned saves {#overview}

## Define conversions {#migration}

Implement conversions as `SaveMigrationStep` entries and compose them in a `SaveMigrationGraph`. Each step performs a pure data transformation on the historical `SaveDataBlock` and specifies its output version. Register a reverse step when downgrades are supported. Mark lossy steps so the player can confirm them before commit.

<<< ../../Samples/DodgeTheCreeps/DodgeProgressMigration.cs{csharp}

## Prepare and commit {#commit}

Call `PrepareLoadAsync` and inspect whether confirmation or commit is required. End the operation if the player declines. Call `CommitPreparedAsync` after confirmation and restore progress from the successful result. Test old data, missing content, and cancellation, then check the original file and backup.

See [versioned saves](../game/data.md) for the full slot workflow.
