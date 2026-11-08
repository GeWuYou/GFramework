// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Data;

/// <summary>Provides a structured difference suitable for a game's load/recovery dialog.</summary>
/// <remarks>Creates one content difference.</remarks>
/// <param name="kind">Stable category.</param>
/// <param name="detail">Human-readable explanation.</param>
/// <param name="blocking">Whether ordinary loading must stop.</param>
/// <param name="packId">Related package, if any.</param>
/// <param name="reference">Unresolved content reference, if any.</param>
public sealed class ContentSaveIssue(ContentSaveIssueKind kind, string detail, bool blocking,
    string? packId = null, ContentReference? reference = null)
{

    /// <summary>Gets the stable difference category.</summary>
    public ContentSaveIssueKind Kind { get; } = kind;
    /// <summary>Gets the explanation for logs or a load dialog.</summary>
    public string Detail { get; } = detail;
    /// <summary>Gets whether the game must use explicit recovery/import before loading.</summary>
    public bool Blocking { get; } = blocking;
    /// <summary>Gets the affected package identifier.</summary>
    public string? PackId { get; } = packId;
    /// <summary>Gets the unresolved content reference.</summary>
    public ContentReference? Reference { get; } = reference;
}
