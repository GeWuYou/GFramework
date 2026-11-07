// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Player-owned activation choices, applied only when starting a new content session.</summary>
public sealed class ContentPackProfile
{
    /// <summary>Gets enabled optional package IDs; the host's base package remains enabled independently.</summary>
    public IReadOnlyList<string> EnabledPackages { get; init; } = Array.Empty<string>();
    /// <summary>Gets preferred order; dependencies constrain this preference, and later packages win field conflicts.</summary>
    public IReadOnlyList<string> LoadOrder { get; init; } = Array.Empty<string>();
}
