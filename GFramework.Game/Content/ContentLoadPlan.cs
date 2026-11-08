// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Content;

/// <summary>A complete dependency-resolved session plan; native mounting remains the host's responsibility.</summary>
public sealed class ContentLoadPlan
{
    /// <summary>Captures actual package order after dependency and player-preference resolution.</summary>
    /// <param name="packages">Ordered packages.</param>
    internal ContentLoadPlan(IEnumerable<ContentPack> packages)
    {
        Packages = Array.AsReadOnly(packages.ToArray());
        Snapshot = new ContentSnapshot([.. Packages.Select(static pack =>
            new ContentPackStamp(pack.Manifest.Id, pack.Manifest.Version, pack.Fingerprint))]);
    }
    /// <summary>Gets the actual immutable package sequence.</summary>
    public IReadOnlyList<ContentPack> Packages
    {
        get;
    }
    /// <summary>Gets the exact set and order for save compatibility checks.</summary>
    public ContentSnapshot Snapshot
    {
        get;
    }
}
