// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Records the exact effective content set; package sequence is significant.</summary>
public sealed class ContentSnapshot
{
    /// <summary>Copies an effective package sequence so later profile changes cannot change the snapshot.</summary>
    /// <param name="packs">Packages in actual dependency-resolved load order.</param>
    public ContentSnapshot(IReadOnlyList<ContentPackStamp> packs)
    {
        if (packs is null) throw new ArgumentNullException(nameof(packs));
        Packs = Array.AsReadOnly(packs.ToArray());
    }

    /// <summary>Gets packages in effective load order.</summary>
    public IReadOnlyList<ContentPackStamp> Packs { get; }
}
