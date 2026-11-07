// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Scene;

/// <summary>Describes the observable outcome of a completed scene operation.</summary>
public enum SceneOperationResult
{
    /// <summary>The requested transition changed the scene stack.</summary>
    Applied,
    /// <summary>A guard refused the transition; the scene stack was retained.</summary>
    Rejected,
    /// <summary>The requested stack state already existed.</summary>
    NoOp
}
