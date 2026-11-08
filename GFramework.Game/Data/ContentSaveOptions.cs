// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;
using GFramework.Game.Content;

namespace GFramework.Game.Data;

/// <summary>
/// Game-owned content policy operating on current-schema documents before CLR DTO conversion. Preparation
/// callbacks must be pure: no world mutation, file writes, or calls to unavailable mod code are permitted.
/// </summary>
public sealed class ContentSaveOptions
{
    /// <summary>Gets the game's immutable active content session.</summary>
    public required Func<ContentCatalog> CurrentCatalog
    {
        get; init;
    }
    /// <summary>Gets every content reference across current-schema game and mod blocks.</summary>
    public Func<SaveDocument, IEnumerable<ContentReference>> EnumerateReferences { get; init; } = static _ => [];
    /// <summary>Gets a pure recovery transformation; absent means blocking differences cannot be repaired.</summary>
    public Func<SaveDocument, ContentSaveReport, CancellationToken, Task<SaveDocument>>? RecoverAsync
    {
        get; init;
    }
    /// <summary>Gets a compatibility override; otherwise exact stamps or declared old-version ranges apply.</summary>
    public Func<ContentPackStamp, ContentPackStamp, bool>? IsPackCompatible
    {
        get; init;
    }
}
