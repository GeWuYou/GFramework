// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;

namespace DodgeTheCreeps.Content;

/// <summary>Player-owned next-start selection, separate from the immutable active session.</summary>
public sealed class DodgeContentPreferences
{
    /// <summary>Gets or sets activation and preferred order for the next process start.</summary>
    public ContentPackProfile Profile { get; set; } = new();
    /// <summary>Gets or sets the chosen arena among currently active definitions.</summary>
    public string ArenaId { get; set; } = "base:yard";
}
