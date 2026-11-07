// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using DodgeTheCreeps.Gameplay;
using GFramework.Game.Abstractions.Data;
using Godot;

namespace DodgeTheCreeps;

/// <summary>
/// Explicitly upgrades sparse v1 progress. That format never recorded enemies, timers or RNG state;
/// it therefore starts an empty, briefly protected layout and marks the loss of layout fidelity.
/// </summary>
public sealed class DodgeProgressMigration : ISaveMigration<DodgeProgress>
{
    /// <inheritdoc />
    public int FromVersion => 1;
    /// <inheritdoc />
    public int ToVersion => DodgeProgress.CurrentVersion;

    /// <inheritdoc />
    public DodgeProgress Migrate(DodgeProgress oldData)
    {
        DodgeProgress.ValidateLegacyValues(oldData);
        using var random = new RandomNumberGenerator { Seed = 0xD0D63 };
        return new DodgeProgress
        {
            Version = ToVersion, LastModified = oldData.LastModified, Score = oldData.Score,
            X = Math.Clamp(oldData.X, 26, 454), Y = Math.Clamp(oldData.Y, 82, 686),
            ArenaId = oldData.ArenaId, Content = oldData.Content,
            PlayerFacing = 0, ScoreElapsed = 0, SpawnRemaining = 1.5, GraceRemaining = 1.5,
            RandomSeed = random.Seed, RandomState = random.State,
            Enemies = new List<DodgeMobProgress>(), LegacyLayoutReset = true
        };
    }
}
