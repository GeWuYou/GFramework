// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Data;

namespace GFramework.Game.Data;

/// <summary>Explicit immutable-at-composition save policy. No version is inferred by constructing a DTO.</summary>
/// <typeparam name="TSaveData">The current game DTO.</typeparam>
public sealed class SaveRepositoryOptions<TSaveData> where TSaveData : class, IData, new()
{
    /// <summary>Gets the owning game's stable identity.</summary>
    public required string GameId { get; init; }
    /// <summary>Gets the schema accepted by the current game.</summary>
    public required int CurrentVersion { get; init; }
    /// <summary>Gets the outer document codec and plain DTO converter.</summary>
    public ISaveDocumentCodec Codec { get; init; } = new JsonSaveDocumentCodec();
    /// <summary>Gets separately declared upward and downward game transformations.</summary>
    public SaveMigrationGraph Migrations { get; init; } = new();
    /// <summary>Gets optional immutable startup content policy.</summary>
    public ContentSaveOptions? Content { get; init; }
    /// <summary>Gets current-DTO business validation performed before any write or game activation.</summary>
    public Action<TSaveData>? Validate { get; init; }
    /// <summary>Gets active mod schema policies; missing owners' blocks are preserved without executing their code.</summary>
    public IReadOnlyList<ModSavePolicy> ModPolicies { get; init; } = Array.Empty<ModSavePolicy>();
}
