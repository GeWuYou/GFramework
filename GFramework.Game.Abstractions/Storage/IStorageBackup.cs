// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Storage;

/// <summary>
/// Optional lossless snapshot capability. Ordinary <c>IStorage</c> implementers need not implement it;
/// content recovery requires it before invoking any destructive game repair hook.
/// Implementations serialize access with their normal per-key lock and atomically replace destination files.
/// </summary>
public interface IStorageBackup
{
    /// <summary>Captures the complete serialized file without interpreting or migrating it.</summary>
    /// <param name="key">Storage-relative key.</param>
    /// <param name="cancellationToken">Cancels admission and the file read.</param>
    /// <returns>An immutable snapshot and fingerprint.</returns>
    Task<StorageSnapshot> ReadSnapshotAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Interprets the captured bytes with this storage's configured serializer.</summary>
    /// <typeparam name="T">Expected DTO type.</typeparam>
    /// <param name="snapshot">Captured serialized file.</param>
    /// <returns>A new object, never an in-memory active game instance.</returns>
    T DeserializeSnapshot<T>(StorageSnapshot snapshot);

    /// <summary>Atomically writes the exact snapshot bytes to another storage key.</summary>
    /// <param name="key">Destination storage-relative key.</param>
    /// <param name="snapshot">Bytes to preserve.</param>
    /// <param name="cancellationToken">Cancels admission; an accepted atomic write is awaited to completion.</param>
    /// <returns>Completion after the replacement has finished.</returns>
    Task WriteSnapshotAsync(string key, StorageSnapshot snapshot, CancellationToken cancellationToken = default);
}
