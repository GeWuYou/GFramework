// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Game.Abstractions.Content;

namespace DodgeTheCreeps.Gameplay;

/// <summary>Portable state of one live enemy, independent of native resources and installation paths.</summary>
public sealed class DodgeMobProgress
{
    /// <summary>Gets or sets the stable enemy definition used to resolve the active session's media.</summary>
    public string EnemyId { get; set; } = "base:walker";
    /// <summary>Gets or sets logical horizontal position.</summary>
    public float X
    {
        get; set;
    }
    /// <summary>Gets or sets logical vertical position.</summary>
    public float Y
    {
        get; set;
    }
    /// <summary>Gets or sets horizontal movement per second.</summary>
    public float VelocityX
    {
        get; set;
    }
    /// <summary>Gets or sets vertical movement per second.</summary>
    public float VelocityY
    {
        get; set;
    }
    /// <summary>Gets or sets the original tutorial appearance, zero through two.</summary>
    public int Appearance
    {
        get; set;
    }
    /// <summary>Gets or sets the current animation frame.</summary>
    public int AnimationFrame
    {
        get; set;
    }
    /// <summary>Gets or sets progress through the current frame.</summary>
    public float AnimationProgress
    {
        get; set;
    }

    /// <summary>Checks finite board coordinates, bounded speed and animation state before native allocation.</summary>
    /// <exception cref="InvalidOperationException">Any portable enemy state is invalid.</exception>
    public void Validate()
    {
        _ = new ContentId(EnemyId);
        if (!float.IsFinite(X) || !float.IsFinite(Y) || X is < -125 or > 605 || Y is < -125 or > 845 ||
            !float.IsFinite(VelocityX) || !float.IsFinite(VelocityY) ||
            (double)VelocityX * VelocityX + (double)VelocityY * VelocityY > 1100d * 1100 ||
            Appearance is < 0 or > 2 || AnimationFrame is < 0 or > 1 ||
            !float.IsFinite(AnimationProgress) || AnimationProgress is < 0 or > 1)
            throw new InvalidOperationException("The save has invalid enemy coordinates, velocity or animation state.");
    }
}
