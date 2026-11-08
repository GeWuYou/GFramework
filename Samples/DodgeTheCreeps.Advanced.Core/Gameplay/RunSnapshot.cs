// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;




namespace DodgeTheCreeps.Advanced.Core.Gameplay;

/// <summary>
/// Portable snapshot of one paused survival board. No native nodes, resources or engine vectors are serialized.
/// Version two preserves enemies, fractional timers and the RNG state rather than generating a new board on continue.
/// </summary>
public class RunSnapshot
{
    /// <summary>Bounds native allocations before restoring an externally supplied snapshot.</summary>
    public const int MaxSavedEnemies = 512;

    /// <summary>Gets or sets the time this portable board was captured.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets completed survival seconds.</summary>
    public int Score
    {
        get; set;
    }
    /// <summary>Gets or sets logical horizontal position.</summary>
    public float X { get; set; } = 240;
    /// <summary>Gets or sets logical vertical position.</summary>
    public float Y { get; set; } = 580;
    /// <summary>Gets or sets the stable arena ID instead of an engine resource path.</summary>
    public string ArenaId { get; set; } = "base:yard";
    /// <summary>Gets or sets the player's last heading, clockwise from east in sectors zero through seven.</summary>
    public int PlayerFacing
    {
        get; set;
    }
    /// <summary>Gets or sets the unscored fraction of one survival second.</summary>
    public double ScoreElapsed
    {
        get; set;
    }
    /// <summary>Gets or sets the exact time until the next enemy spawn.</summary>
    public double SpawnRemaining { get; set; } = 1.5;
    /// <summary>Gets or sets remaining collision grace; exact restoration never grants additional grace.</summary>
    public double GraceRemaining { get; set; } = 1.5;
    /// <summary>Gets or sets the RNG seed, restored before its state.</summary>
    public ulong RandomSeed { get; set; } = 1;
    /// <summary>Gets or sets the RNG state immediately before the next random draw.</summary>
    public ulong? RandomState
    {
        get; set;
    }
    /// <summary>Gets or sets every living enemy in the world's stable iteration order.</summary>
    public IList<EnemySnapshot> Enemies { get; set; } = [];

}
