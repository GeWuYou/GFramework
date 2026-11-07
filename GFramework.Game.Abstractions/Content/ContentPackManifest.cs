// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Declares data and resource entrypoints; it never authorizes code or establishes package trust.</summary>
public sealed class ContentPackManifest
{
    /// <summary>Gets the manifest format understood by this framework.</summary>
    public int FormatVersion { get; init; } = 1;
    /// <summary>Gets the stable canonical package identity.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Gets the player-facing display name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets the package's three-component numeric version.</summary>
    public string Version { get; init; } = "1.0.0";
    /// <summary>Gets the target game's identity.</summary>
    public string GameId { get; init; } = string.Empty;
    /// <summary>Gets the accepted game version range.</summary>
    public ContentVersionRange GameVersions { get; init; } = new();
    /// <summary>Gets the accepted game-owned content API version range.</summary>
    public ContentVersionRange ContentApiVersions { get; init; } = new();
    /// <summary>Gets explicitly declared dependencies.</summary>
    public IReadOnlyList<ContentPackDependency> Dependencies { get; init; } = Array.Empty<ContentPackDependency>();
    /// <summary>Gets package IDs that must not be enabled alongside this package.</summary>
    public IReadOnlyList<string> IncompatiblePackages { get; init; } = Array.Empty<string>();
    /// <summary>Gets package-relative YAML operation files, applied in declared order.</summary>
    public IReadOnlyList<string> ContentFiles { get; init; } = Array.Empty<string>();
    /// <summary>Gets package-relative PCK paths for an explicitly trusted host adapter; declaration is not trust.</summary>
    public IReadOnlyList<string> ResourcePacks { get; init; } = Array.Empty<string>();
    /// <summary>Gets old package versions explicitly accepted for saves; absent means exact version and fingerprint.</summary>
    public ContentVersionRange? SaveCompatibleVersions { get; init; }
}
