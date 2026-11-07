// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace DodgeTheCreeps;

/// <summary>Persisted preferences separate from a saved run.</summary>
public sealed class DodgePreferences
{
    /// <summary>Gets or sets master music and effects volume.</summary>
    public float Volume { get; set; } = 0.65f;
    /// <summary>Gets or sets whether the game uses fullscreen presentation.</summary>
    public bool Fullscreen { get; set; }
    /// <summary>Gets or sets the best completed score.</summary>
    public int BestScore { get; set; }
}
