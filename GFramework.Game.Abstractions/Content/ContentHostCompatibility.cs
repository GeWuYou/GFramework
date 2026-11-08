// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Host-owned compatibility identity; package authors cannot redefine these requirements.</summary>
public sealed class ContentHostCompatibility
{
    /// <summary>Gets the target game's stable identity.</summary>
    public string GameId { get; init; } = string.Empty;
    /// <summary>Gets the running game's numeric content compatibility version.</summary>
    public string GameVersion { get; init; } = "1.0.0";
    /// <summary>Gets the game-owned data interpretation API version.</summary>
    public string ContentApiVersion { get; init; } = "1.0.0";
    /// <summary>Gets the game-owned code API version, or null when this host does not support code mods.</summary>
    public string? CodeApiVersion
    {
        get; init;
    }
    /// <summary>Gets the package that is mandatory and always loads first.</summary>
    public string BasePackId { get; init; } = "base";
}
