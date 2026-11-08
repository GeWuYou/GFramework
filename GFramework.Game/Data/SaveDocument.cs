// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Data;

/// <summary>
/// Stable outer save envelope. Metadata can be inspected without constructing game DTOs or loading missing mods.
/// Payload versions belong to their game/mod owners; FormatVersion belongs to this envelope codec.
/// </summary>
public sealed class SaveDocument
{
    /// <summary>Gets or sets the envelope format; this implementation understands format 1 only.</summary>
    public int FormatVersion { get; set; } = 1;

    /// <summary>Gets or sets the owning game's stable identity.</summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>Gets or sets the versioned game payload.</summary>
    public SaveDataBlock Game { get; set; } = new();

    /// <summary>Gets or sets the effective content set, independently of game data.</summary>
    public ContentSnapshot? Content { get; set; }

    /// <summary>Gets or sets namespaced mod payloads. Unavailable mods remain represented as plain data.</summary>
    public IDictionary<string, SaveDataBlock> Mods { get; set; } = new Dictionary<string, SaveDataBlock>(StringComparer.Ordinal);

    /// <summary>Creates a completely independent document for transformation.</summary>
    /// <returns>A deep copy without sharing mutable payloads.</returns>
    public SaveDocument Clone() => new()
    {
        FormatVersion = FormatVersion,
        GameId = GameId,
        Game = Game.Clone(),
        Content = Content is null ? null : new ContentSnapshot(Content.Packs),
        Mods = Mods.ToDictionary(static pair => pair.Key, static pair => pair.Value.Clone(), StringComparer.Ordinal)
    };
}
