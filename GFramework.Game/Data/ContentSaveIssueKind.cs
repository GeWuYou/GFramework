// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Data;

/// <summary>Identifies a content difference between a save and the running content session.</summary>
public enum ContentSaveIssueKind
{
    /// <summary>A previously used package is no longer enabled.</summary>
    MissingPack,
    /// <summary>A package version or fingerprint is not declared compatible.</summary>
    IncompatiblePack,
    /// <summary>A stored content reference no longer resolves.</summary>
    MissingContent,
    /// <summary>A new package is enabled; this is advisory.</summary>
    AdditionalPack,
    /// <summary>The order of retained packages changed; this is advisory.</summary>
    LoadOrderChanged,
    /// <summary>The envelope lacks content metadata and requires an explicit game-owned recovery decision.</summary>
    MissingMetadata,
}
