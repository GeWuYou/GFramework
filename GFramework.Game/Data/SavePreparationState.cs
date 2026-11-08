// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Storage;

namespace GFramework.Game.Data;

/// <summary>Groups immutable preparation evidence without a positional constructor spanning unrelated concerns.</summary>
internal sealed class SavePreparationState
{
    /// <summary>Gets the issuing repository.</summary>
    public Guid Owner
    {
        get; init;
    }
    /// <summary>Gets the inspected slot.</summary>
    public int Slot
    {
        get; init;
    }
    /// <summary>Gets original bytes.</summary>
    public required StorageSnapshot Original
    {
        get; init;
    }
    /// <summary>Gets validated candidate bytes.</summary>
    public required StorageSnapshot Candidate
    {
        get; init;
    }
    /// <summary>Gets the selected content session.</summary>
    public ContentSnapshot? CurrentContent
    {
        get; init;
    }
    /// <summary>Gets original content diagnostics.</summary>
    public IReadOnlyList<ContentSaveIssue> Issues { get; init; } = [];
    /// <summary>Gets selected transformation edges.</summary>
    public IReadOnlyList<SaveMigrationStep> MigrationPath { get; init; } = [];
    /// <summary>Gets whether a transformed file must be explicitly committed.</summary>
    public bool RequiresCommit
    {
        get; init;
    }
    /// <summary>Gets whether the full candidate needs a player decision.</summary>
    public bool RequiresConfirmation
    {
        get; init;
    }
}
