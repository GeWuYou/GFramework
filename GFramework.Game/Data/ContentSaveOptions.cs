// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Content;

namespace GFramework.Game.Data;

/// <summary>
/// Opt-in game-owned save policy. The game controls referenced IDs and repairs; the repository owns
/// checking, byte backups, migration ordering and atomic persistence. No hook is loaded from a content pack.
/// </summary>
/// <typeparam name="TSaveData">Game DTO implementing <see cref="IContentAwareSaveData"/>.</typeparam>
public sealed class ContentSaveOptions<TSaveData> where TSaveData : class, IData, new()
{
    /// <summary>Gets the immutable session used for this operation; changing sessions requires a game restart.</summary>
    public required Func<ContentCatalog> CurrentCatalog { get; init; }

    /// <summary>Gets the game's references to validate before accepting or persisting a save.</summary>
    public Func<TSaveData, IEnumerable<ContentReference>> EnumerateReferences { get; init; } = static _ => [];

    /// <summary>Gets optional game validation; throwing rejects a prepared save without overwriting the original.</summary>
    public Action<TSaveData>? Validate { get; init; }

    /// <summary>
    /// Gets the explicitly approved repair hook. It receives a newly deserialized object after a lossless backup;
    /// it must return a valid save or throw. It must not mutate the running game or persist files itself.
    /// </summary>
    public Func<TSaveData, ContentSaveReport, CancellationToken, Task<TSaveData>>? RecoverAsync { get; init; }

    /// <summary>Gets an explicit declaration accepting a known metadata-free legacy format for import.</summary>
    public Func<TSaveData, bool>? AcceptLegacy { get; init; }

    /// <summary>
    /// Gets an optional compatibility decision for changed package versions/fingerprints.
    /// Overrides installed-package old-version declarations when provided. Without either an explicit host decision
    /// or a declared old-version range, only exact version and fingerprint matches are compatible.
    /// </summary>
    public Func<ContentPackStamp, ContentPackStamp, bool>? IsPackCompatible { get; init; }
}
