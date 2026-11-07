// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using DodgeTheCreeps.Gameplay;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Data;

namespace DodgeTheCreeps;

/// <summary>
/// Portable snapshot of one paused survival board. No native nodes, resources or engine vectors are serialized.
/// Version two preserves enemies, fractional timers and the RNG state rather than generating a new board on continue.
/// </summary>
public sealed class DodgeProgress : IVersionedData, IContentAwareSaveData
{
    /// <summary>Gets the complete-board format written by this game.</summary>
    public const int CurrentVersion = 2;
    /// <summary>Bounds native allocations before restoring an externally supplied snapshot.</summary>
    public const int MaxSavedEnemies = 512;

    /// <inheritdoc />
    public int Version { get; set; } = CurrentVersion;
    /// <inheritdoc />
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets completed survival seconds.</summary>
    public int Score { get; set; }
    /// <summary>Gets or sets logical horizontal position.</summary>
    public float X { get; set; } = 240;
    /// <summary>Gets or sets logical vertical position.</summary>
    public float Y { get; set; } = 580;
    /// <summary>Gets or sets the stable arena ID instead of an engine resource path.</summary>
    public string ArenaId { get; set; } = "base:yard";
    /// <summary>Gets or sets the player's last heading, clockwise from east in sectors zero through seven.</summary>
    public int PlayerFacing { get; set; }
    /// <summary>Gets or sets the unscored fraction of one survival second.</summary>
    public double ScoreElapsed { get; set; }
    /// <summary>Gets or sets the exact time until the next enemy spawn.</summary>
    public double SpawnRemaining { get; set; } = 1.5;
    /// <summary>Gets or sets remaining collision grace; exact restoration never grants additional grace.</summary>
    public double GraceRemaining { get; set; } = 1.5;
    /// <summary>Gets or sets the RNG seed, restored before its state.</summary>
    public ulong RandomSeed { get; set; } = 1;
    /// <summary>Gets or sets the RNG state immediately before the next random draw.</summary>
    public ulong? RandomState { get; set; }
    /// <summary>Gets or sets every living enemy in the world's stable iteration order.</summary>
    public IList<DodgeMobProgress> Enemies { get; set; } = new List<DodgeMobProgress>();
    /// <inheritdoc />
    public ContentSnapshot? Content { get; set; }

    /// <summary>Rejects unsupported or dangerous state before allocating native enemies or modifying a live board.</summary>
    /// <param name="save">Snapshot to validate; this method has no engine or persistence side effects.</param>
    /// <exception cref="InvalidOperationException">The format, values, enemy count or identity syntax is invalid.</exception>
    public static void ValidateSnapshot(DodgeProgress save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != CurrentVersion)
            throw new InvalidOperationException("The save must be migrated to the supported complete-board format before restoring.");
        ValidateLegacyValues(save);
        if (save.X is < 26 or > 454 || save.Y is < 82 or > 686 ||
            save.RandomState is null || save.PlayerFacing is < 0 or > 7 || !double.IsFinite(save.ScoreElapsed) || save.ScoreElapsed is < 0 or >= 1 ||
            !double.IsFinite(save.SpawnRemaining) || save.SpawnRemaining is < 0 or > 10 ||
            !double.IsFinite(save.GraceRemaining) || save.GraceRemaining is < 0 or > 1.5 ||
            save.Enemies is null || save.Enemies.Count > MaxSavedEnemies)
            throw new InvalidOperationException("The save has invalid timers, facing or enemy count.");
        foreach (var enemy in save.Enemies)
        {
            if (enemy is null) throw new InvalidOperationException("The save contains an empty enemy record.");
            enemy.Validate();
        }
    }

    /// <summary>Checks the limited fields actually recorded by old sparse saves without inventing missing board state.</summary>
    /// <param name="save">Sparse or complete portable data.</param>
    internal static void ValidateLegacyValues(DodgeProgress save)
    {
        if (save.Score < 0 || !float.IsFinite(save.X) || !float.IsFinite(save.Y) ||
            save.X is < 0 or > 480 || save.Y is < 0 or > 720)
            throw new InvalidOperationException("The save has invalid score or player coordinates.");
        _ = new ContentId(save.ArenaId);
    }
}
