// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using DodgeTheCreeps.Advanced.Gameplay;

namespace DodgeTheCreeps.Advanced;

/// <summary>The explicit native board reference shared by scene, mod and run adapters.</summary>
internal sealed class WorldState
{
    /// <summary>The current native board; SceneRouter owns its attachment and removal.</summary>
    internal DodgeWorld? World
    {
        get; set;
    }

    /// <summary>The content arena that the scene factory must prepare next.</summary>
    internal string ArenaId { get; set; } = "base:yard";
}
