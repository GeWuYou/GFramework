// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using GFramework.Game.Data;
using Newtonsoft.Json.Linq;

namespace DodgeTheCreeps.Advanced;

/// <summary>Game-owned pure document conversions; no current DTO or native object is constructed by an edge.</summary>
public static class DodgeProgressMigration
{
    /// <summary>Creates conversions for sparse v1 and a nested-coordinate v3 demonstration format.</summary>
    /// <param name="initialRandomState">State captured by the host for the known seed before conversion runs.</param>
    /// <returns>The immutable graph targeting this game's v2 complete-board schema.</returns>
    public static SaveMigrationGraph CreateGraph(ulong initialRandomState) => new(
    [
        new SaveMigrationStep(1, 2, "Initialize board state absent from sparse saves", (block, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = (JObject)block.Data.DeepClone();
            data["X"] = Math.Clamp((float?)data["X"] ?? 240, 26, 454);
            data["Y"] = Math.Clamp((float?)data["Y"] ?? 580, 82, 686);
            data["ArenaId"] ??= "base:yard";
            data["PlayerFacing"] = 0;
            data["ScoreElapsed"] = 0;
            data["SpawnRemaining"] = 1.5;
            data["GraceRemaining"] = 1.5;
            data["RandomSeed"] = 0xD0D63UL;
            data["RandomState"] = initialRandomState;
            data["Enemies"] = new JArray();
            return Task.FromResult(new SaveDataBlock { Version = 2, Data = data });
        }),
        new SaveMigrationStep(3, 2, "Convert nested coordinates; discard unsupported future mode", (block, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = (JObject)block.Data.DeepClone();
            var position = data["Position"] as JObject ?? throw new InvalidOperationException("Version 3 requires Position.");
            data["X"] = position["X"]?.DeepClone() ?? throw new InvalidOperationException("Position.X is missing.");
            data["Y"] = position["Y"]?.DeepClone() ?? throw new InvalidOperationException("Position.Y is missing.");
            data.Remove("Position");
            data.Remove("FutureMode");
            return Task.FromResult(new SaveDataBlock { Version = 2, Data = data });
        }, isLossy: true),
    ]);
}
