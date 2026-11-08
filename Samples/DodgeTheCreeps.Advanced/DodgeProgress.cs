// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Data;

namespace DodgeTheCreeps.Advanced;

/// <summary>Framework document DTO; the inherited values and rules remain independent of serialization and Godot.</summary>
public sealed class DodgeProgress : RunSnapshot, IData
{
    /// <summary>The host's current document schema version.</summary>
    public const int CurrentVersion = 2;

    /// <summary>Validates domain values and the host's stable content identities.</summary>
    /// <param name="save">The full document payload.</param>
    public static void ValidateSnapshot(DodgeProgress save)
    {
        RunRules.ValidateSnapshot(save);
        _ = new ContentId(save.ArenaId);
        foreach (var enemy in save.Enemies)
            _ = new ContentId(enemy.EnemyId);
    }

    /// <summary>Checks a legacy document before its explicit migration fills full-board fields.</summary>
    /// <param name="save">The sparse document payload.</param>
    internal static void ValidateLegacyValues(DodgeProgress save) => RunRules.ValidateLegacyValues(save);

    /// <summary>Maps an application checkpoint to the framework DTO without exposing framework contracts to Core.</summary>
    /// <param name="snapshot">The application checkpoint.</param>
    /// <returns>A detached document payload.</returns>
    internal static DodgeProgress FromSnapshot(RunSnapshot snapshot) => new()
    {
        LastModified = snapshot.LastModified,
        Score = snapshot.Score,
        X = snapshot.X,
        Y = snapshot.Y,
        ArenaId = snapshot.ArenaId,
        PlayerFacing = snapshot.PlayerFacing,
        ScoreElapsed = snapshot.ScoreElapsed,
        SpawnRemaining = snapshot.SpawnRemaining,
        GraceRemaining = snapshot.GraceRemaining,
        RandomSeed = snapshot.RandomSeed,
        RandomState = snapshot.RandomState,
        Enemies = new List<EnemySnapshot>(snapshot.Enemies),
    };
}
