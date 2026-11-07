// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace DodgeTheCreeps;

/// <summary>Persisted preferences separate from a saved run.</summary>
public sealed class DodgePreferences
{
    /// <summary>Gets or sets master music and effects volume.</summary>
    public float Volume { get; set; } = 0.65f;
    /// <summary>Gets or sets the music-bus volume independently of the master and sound effects.</summary>
    public float BgmVolume { get; set; } = 1;
    /// <summary>Gets or sets the sound-effects-bus volume independently of music.</summary>
    public float SfxVolume { get; set; } = 1;
    /// <summary>Gets or sets whether the game uses fullscreen presentation.</summary>
    public bool Fullscreen { get; set; }
    /// <summary>Gets or sets the windowed output width; the logical game remains 480 by 720.</summary>
    public int ResolutionWidth { get; set; } = 480;
    /// <summary>Gets or sets the windowed output height.</summary>
    public int ResolutionHeight { get; set; } = 720;
    /// <summary>Gets or sets the best completed score.</summary>
    public int BestScore { get; set; }
    /// <summary>Gets or sets the explicit UI locale; null uses the OS locale on first launch.</summary>
    public string? Language { get; set; }

    /// <summary>Gets three portrait output presets; the middle choice is the logical game size.</summary>
    internal static IReadOnlyList<(int Width, int Height)> ResolutionChoices { get; } = Array.AsReadOnly(new[]
    {
        (360, 540), (480, 720), (640, 960)
    });

    /// <summary>Creates an independent draft without sharing mutable preference state with a view.</summary>
    internal DodgePreferences Copy() => new()
    {
        Volume = Volume, BgmVolume = BgmVolume, SfxVolume = SfxVolume,
        Fullscreen = Fullscreen, ResolutionWidth = ResolutionWidth, ResolutionHeight = ResolutionHeight,
        Language = Language, BestScore = BestScore
    };
}
