// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;

namespace DodgeTheCreeps.Advanced.Core.Gameplay;

/// <summary>Pure validation and scoring limits shared by saving and board restoration.</summary>
public static class RunRules
{
    /// <summary>Validates finite board values, timers and every enemy before effects are performed.</summary>
    /// <param name="save">The detached board to validate.</param>
    public static void ValidateSnapshot(RunSnapshot save)
    {
        ArgumentNullException.ThrowIfNull(save);
        ValidateLegacyValues(save);
        if (save.X is < 26 or > 454 || save.Y is < 82 or > 686 ||
            save.RandomState is null || save.PlayerFacing is < 0 or > 7 || !double.IsFinite(save.ScoreElapsed) || save.ScoreElapsed is < 0 or >= 1 ||
            !double.IsFinite(save.SpawnRemaining) || save.SpawnRemaining is < 0 or > 10 ||
            !double.IsFinite(save.GraceRemaining) || save.GraceRemaining is < 0 or > 1.5 ||
            save.Enemies is null || save.Enemies.Count > RunSnapshot.MaxSavedEnemies)
            throw new InvalidOperationException("The save has invalid timers, facing or enemy count.");
        foreach (var enemy in save.Enemies)
        {
            if (enemy is null)
                throw new InvalidOperationException("The save contains an empty enemy record.");
            enemy.Validate();
        }
    }

    /// <summary>Checks the limited fields actually recorded by old sparse saves without inventing missing board state.</summary>
    /// <param name="save">Sparse or complete portable data.</param>
    public static void ValidateLegacyValues(RunSnapshot save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Score < 0 || !float.IsFinite(save.X) || !float.IsFinite(save.Y) ||
            save.X is < 0 or > 480 || save.Y is < 0 or > 720)
            throw new InvalidOperationException("The save has invalid score or player coordinates.");
        RequireContentId(save.ArenaId);
    }

    /// <summary>Checks the game's namespaced identifiers without depending on a content framework.</summary>
    /// <param name="id">Stable package-qualified identity.</param>
    internal static void RequireContentId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var separator = id.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0 || separator == id.Length - 1 || id.IndexOf(':', separator + 1) >= 0)
            throw new InvalidOperationException("Content identities must contain one package and one local name.");
    }
}
