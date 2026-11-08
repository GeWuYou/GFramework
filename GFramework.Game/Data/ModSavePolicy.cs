// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Data;

/// <summary>Declares one active mod's independent data schema and pure conversion graph.</summary>
public sealed class ModSavePolicy
{
    /// <summary>Creates an active mod policy.</summary>
    /// <param name="modId">Canonical package ID.</param>
    /// <param name="currentVersion">Current mod-data schema version.</param>
    /// <param name="migrations">Explicit transformations, if any.</param>
    public ModSavePolicy(string modId, int currentVersion, SaveMigrationGraph? migrations = null)
    {
        if (!ContentId.IsCanonicalSegment(modId))
            throw new ArgumentException("A mod namespace must be canonical.", nameof(modId));
        ArgumentOutOfRangeException.ThrowIfLessThan(currentVersion, 1);
        ModId = modId;
        CurrentVersion = currentVersion;
        Migrations = migrations ?? new SaveMigrationGraph();
    }
    /// <summary>Gets the owning mod ID.</summary>
    public string ModId
    {
        get;
    }
    /// <summary>Gets the current schema version.</summary>
    public int CurrentVersion
    {
        get;
    }
    /// <summary>Gets an optional complete version path to resolve equal shortest routes.</summary>
    public IReadOnlyList<int>? VersionPath
    {
        get; init;
    }
    /// <summary>Gets the owner's immutable conversion graph.</summary>
    public SaveMigrationGraph Migrations
    {
        get;
    }
}
