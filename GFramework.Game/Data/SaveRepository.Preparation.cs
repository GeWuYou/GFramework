// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Storage;

namespace GFramework.Game.Data;

/// <summary>Pure preparation followed by a separate fingerprint-bound, losslessly backed-up commit.</summary>
public sealed partial class SaveRepository<TSaveData>
{
    /// <summary>
    /// Prepares a complete private document without writes or live-world mutation. Conversion and recovery hooks
    /// must be pure. Only the successfully transformed document is deserialized as the current game DTO.
    /// </summary>
    /// <param name="slot">Existing slot to inspect.</param>
    /// <param name="cancellationToken">Cancels reading and pure transformations.</param>
    /// <returns>An immutable candidate; display its losses before explicitly committing it.</returns>
    public Task<PreparedSave<TSaveData>> PrepareLoadAsync(int slot, CancellationToken cancellationToken = default) =>
        PrepareCoreAsync(slot, null, cancellationToken);

    /// <summary>Prepares using an explicitly selected complete game-version route.</summary>
    /// <param name="slot">Existing slot.</param>
    /// <param name="versionPath">Complete source-to-target version route, resolving graph ambiguity.</param>
    /// <param name="cancellationToken">Cancels pure preparation.</param>
    /// <returns>An immutable candidate.</returns>
    public Task<PreparedSave<TSaveData>> PrepareLoadAsync(int slot, IReadOnlyList<int> versionPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(versionPath);
        return PrepareCoreAsync(slot, Array.AsReadOnly(versionPath.ToArray()), cancellationToken);
    }

    /// <summary>Joins slot admission, pure block conversion and final candidate validation.</summary>
    private async Task<PreparedSave<TSaveData>> PrepareCoreAsync(int slot, IReadOnlyList<int>? versionPath,
        CancellationToken cancellationToken)
    {
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var snapshot = await ((IStorageBackup)GetSlotStorage(slot)).ReadSnapshotAsync(_configuration.SaveFileName, cancellationToken)
                .ConfigureAwait(false);
            var document = ReadDocument(snapshot);
            var catalog = GetCurrentCatalog();
            var paths = new List<SaveMigrationStep>();
            var gamePath = _options.Migrations.Plan(document.Game.Version, _options.CurrentVersion, versionPath);
            paths.AddRange(gamePath);
            document.Game = await SaveMigrationGraph.ConvertAsync(document.Game, gamePath, cancellationToken).ConfigureAwait(false);
            foreach (var policy in _modPolicies.Values.OrderBy(static policy => policy.ModId, StringComparer.Ordinal))
            {
                if (!document.Mods.TryGetValue(policy.ModId, out var block)) continue;
                var path = policy.Migrations.Plan(block.Version, policy.CurrentVersion, policy.VersionPath);
                paths.AddRange(path);
                document.Mods[policy.ModId] = await SaveMigrationGraph.ConvertAsync(block, path, cancellationToken).ConfigureAwait(false);
            }
            // Content checks see the current schema, but run before current DTO construction. Missing mods are never invoked.
            var report = catalog is null ? null : BuildContentReport(slot, snapshot.Fingerprint, document, catalog);
            var repaired = report?.RequiresRecovery == true;
            if (repaired)
            {
                var recover = _options.Content!.RecoverAsync ?? throw new InvalidOperationException("Content differences require a game-owned document recovery policy.");
                var result = await recover(document.Clone(), report!, cancellationToken).ConfigureAwait(false);
                if (result is null) throw new InvalidOperationException("Content recovery returned null.");
                // The host can deliberately remove/repair opaque mod blocks, but cannot change envelope identity or versions.
                document = ReadDocument(_codec.Encode(result));
            }
            var contentChanged = !SnapshotsEqual(document.Content, catalog?.Snapshot);
            document.Content = catalog?.Snapshot;
            _ = ValidateDocumentForCurrentGame(document);
            cancellationToken.ThrowIfCancellationRequested();
            if (!SnapshotsEqual(catalog?.Snapshot, GetCurrentCatalog()?.Snapshot))
                throw new InvalidOperationException("Content changed while preparing the save.");
            var candidate = _codec.Encode(document);
            return new PreparedSave<TSaveData>(new SavePreparationState
            {
                Owner = _owner, Slot = slot, Original = snapshot, Candidate = candidate, CurrentContent = catalog?.Snapshot,
                Issues = report?.Issues ?? [], MigrationPath = paths,
                RequiresCommit = paths.Count != 0 || repaired || contentChanged,
                RequiresConfirmation = repaired || paths.Any(static step => step.IsLossy || step.ToVersion < step.FromVersion)
            }, _codec, _modPolicies);
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Commits exactly the candidate shown to and accepted by the caller. The caller must obtain explicit
    /// confirmation when RequiresConfirmation is true. No callbacks are rerun and no mutable public DTO is trusted.
    /// </summary>
    /// <param name="slot">Previously prepared slot.</param>
    /// <param name="prepared">Unmodified approval candidate issued by this repository.</param>
    /// <param name="cancellationToken">Cancels before accepted writes; started writes are joined.</param>
    /// <returns>The committed DTO and original backup location.</returns>
    public async Task<SaveCommitResult<TSaveData>> CommitPreparedAsync(int slot, PreparedSave<TSaveData> prepared,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var storage = (IStorageBackup)GetSlotStorage(slot);
            var current = await storage.ReadSnapshotAsync(_configuration.SaveFileName, cancellationToken).ConfigureAwait(false);
            ValidateApproval(slot, prepared, current);
            var document = ReadDocument(prepared.Candidate);
            var data = ValidateDocumentForCurrentGame(document);
            var backupKey = CreateBackupKey("pre-transform");
            await storage.WriteSnapshotAsync(backupKey, prepared.Original, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            // An external writer may have acted while the backup was being stored. Never overwrite that newer file.
            current = await storage.ReadSnapshotAsync(_configuration.SaveFileName, cancellationToken).ConfigureAwait(false);
            ValidateApproval(slot, prepared, current);
            await storage.WriteSnapshotAsync(_configuration.SaveFileName, prepared.Candidate, cancellationToken).ConfigureAwait(false);
            return new SaveCommitResult<TSaveData>(data, backupKey);
        }
        finally { gate.Release(); }
    }

    /// <summary>Restores exact bytes while first preserving the currently occupied slot in a separate backup.</summary>
    /// <param name="slot">Slot whose backup namespace to use.</param>
    /// <param name="backupKey">Slot-relative key returned by a previous commit or restoration.</param>
    /// <param name="cancellationToken">Cancels before accepted writes.</param>
    /// <returns>The displaced file's backup key, or null for an originally empty slot.</returns>
    public async Task<string?> RestoreBackupAsync(int slot, string backupKey, CancellationToken cancellationToken = default)
    {
        ValidateBackupKey(backupKey);
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var ordinary = GetSlotStorage(slot);
            var storage = (IStorageBackup)ordinary;
            var restore = await storage.ReadSnapshotAsync(backupKey, cancellationToken).ConfigureAwait(false);
            string? displaced = null;
            if (await ordinary.ExistsAsync(_configuration.SaveFileName).ConfigureAwait(false))
            {
                var previous = await storage.ReadSnapshotAsync(_configuration.SaveFileName, cancellationToken).ConfigureAwait(false);
                displaced = CreateBackupKey("pre-restore");
                await storage.WriteSnapshotAsync(displaced, previous, cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            await storage.WriteSnapshotAsync(_configuration.SaveFileName, restore, cancellationToken).ConfigureAwait(false);
            return displaced;
        }
        finally { gate.Release(); }
    }

    /// <summary>Rejects cross-slot, cross-repository, source-file and target-session substitution.</summary>
    private void ValidateApproval(int slot, PreparedSave<TSaveData> prepared, StorageSnapshot current)
    {
        if (prepared.Owner != _owner || prepared.Slot != slot ||
            !string.Equals(prepared.SourceFingerprint, current.Fingerprint, StringComparison.Ordinal) ||
            !SnapshotsEqual(prepared.CurrentContent, GetCurrentCatalog()?.Snapshot))
            throw new InvalidOperationException("The save or target session changed; prepare again and obtain a new decision.");
    }

    /// <summary>Creates collision-resistant keys within the selected slot.</summary>
    private static string CreateBackupKey(string reason) =>
        $"backups/{reason}-{DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff", CultureInfo.InvariantCulture)}-{Guid.NewGuid():N}";

    /// <summary>Limits restoration to this slot's namespace.</summary>
    private static void ValidateBackupKey(string backupKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupKey);
        if (!backupKey.StartsWith("backups/", StringComparison.Ordinal) || backupKey.Contains("..", StringComparison.Ordinal) ||
            backupKey.Contains('\\', StringComparison.Ordinal) || backupKey.Contains(':', StringComparison.Ordinal))
            throw new ArgumentException("A backup key must stay within the slot's backups directory.", nameof(backupKey));
    }
}
