// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Data;

/// <summary>
/// One explicitly implemented schema edge. The callback is a pure document transformation: it must not
/// persist data, mutate a live game, or depend on unavailable mod code. Opposite edges are registered separately.
/// </summary>
public sealed class SaveMigrationStep
{
    /// <summary>Creates a forward or backward conversion edge.</summary>
    /// <param name="fromVersion">Positive source schema version.</param>
    /// <param name="toVersion">Different positive destination schema version.</param>
    /// <param name="description">Human-readable conversion/loss explanation.</param>
    /// <param name="convertAsync">Pure callback returning an independent block at the declared version.</param>
    /// <param name="isLossy">Whether confirmation is required even for an upward conversion.</param>
    public SaveMigrationStep(int fromVersion, int toVersion, string description,
        Func<SaveDataBlock, CancellationToken, Task<SaveDataBlock>> convertAsync, bool isLossy = false)
    {
        if (fromVersion < 1 || toVersion < 1 || fromVersion == toVersion)
            throw new ArgumentException("Migration edges require distinct positive versions.", nameof(toVersion));
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        FromVersion = fromVersion;
        ToVersion = toVersion;
        Description = description;
        ConvertAsync = convertAsync ?? throw new ArgumentNullException(nameof(convertAsync));
        IsLossy = isLossy;
    }

    /// <summary>Gets the source schema version.</summary>
    public int FromVersion { get; }
    /// <summary>Gets the destination schema version.</summary>
    public int ToVersion { get; }
    /// <summary>Gets the explanation displayed in preparation results.</summary>
    public string Description { get; }
    /// <summary>Gets whether this conversion can discard information.</summary>
    public bool IsLossy { get; }
    /// <summary>Gets the owner-provided pure asynchronous conversion.</summary>
    internal Func<SaveDataBlock, CancellationToken, Task<SaveDataBlock>> ConvertAsync { get; }
}
