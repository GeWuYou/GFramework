// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Storage;
using GFramework.Game.Content;
using GFramework.Core.Abstractions.Storage;

namespace GFramework.Game.Data;

/// <summary>Content compatibility and explicitly approved recovery for slot-based saves.</summary>
public partial class SaveRepository<TSaveData>
{
    /// <summary>
    /// Reads content metadata and referenced IDs without invoking migrations, repair hooks or writes.
    /// Keep the returned report until the player confirms; changed bytes or content invalidate that confirmation.
    /// </summary>
    /// <param name="slot">Slot to inspect.</param>
    /// <param name="cancellationToken">Cancels admission and reading.</param>
    /// <returns>Blocking and advisory differences bound to exact serialized bytes.</returns>
    public async Task<ContentSaveReport> InspectContentAsync(int slot, CancellationToken cancellationToken = default)
    {
        EnsureContentEnabled();
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var storage = (IStorageBackup)GetSlotStorage(slot);
            var snapshot = await storage.ReadSnapshotAsync(_config.SaveFileName, cancellationToken).ConfigureAwait(false);
            var data = storage.DeserializeSnapshot<TSaveData>(snapshot);
            return BuildReport(slot, snapshot.Fingerprint, data, GetCurrentCatalog());
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Repairs content only after the game obtained player confirmation of <paramref name="approvedReport"/>.
    /// A byte-for-byte backup commits before the game hook runs. Only validated, fully migrated results replace the save.
    /// </summary>
    /// <param name="slot">Previously inspected slot.</param>
    /// <param name="approvedReport">Exact report displayed to and accepted by the player.</param>
    /// <param name="cancellationToken">Cancels before commit; accepted file writes finish before return.</param>
    /// <returns>Committed data and the original-file backup key.</returns>
    public Task<ContentSaveRecoveryResult<TSaveData>> RecoverContentAsync(int slot,
        ContentSaveReport approvedReport, CancellationToken cancellationToken = default)
    {
        EnsureContentEnabled();
        if (_contentOptions!.RecoverAsync is null)
        {
            throw new InvalidOperationException("This game has not configured a content recovery hook.");
        }
        return RecoverOrImportAsync(slot, approvedReport, importingLegacy: false, cancellationToken);
    }

    /// <summary>
    /// Imports a known metadata-free format only after confirmation and a lossless backup.
    /// The game's <c>AcceptLegacy</c> declaration must recognize the original format; unknown saves are never guessed.
    /// </summary>
    /// <param name="slot">Previously inspected legacy slot.</param>
    /// <param name="approvedReport">Report displayed before import.</param>
    /// <param name="cancellationToken">Cancels before commit.</param>
    /// <returns>Committed data and the legacy-file backup key.</returns>
    public Task<ContentSaveRecoveryResult<TSaveData>> ImportLegacyAsync(int slot,
        ContentSaveReport approvedReport, CancellationToken cancellationToken = default)
    {
        EnsureContentEnabled();
        if (_contentOptions!.AcceptLegacy is null)
        {
            throw new InvalidOperationException("This game has not declared an accepted legacy save format.");
        }
        return RecoverOrImportAsync(slot, approvedReport, importingLegacy: true, cancellationToken);
    }

    /// <summary>
    /// Restores exact backup bytes, keeping another backup of the current file first.
    /// Restoring does not bypass content checks: subsequent loading may still require re-enabling the original packages.
    /// </summary>
    /// <param name="slot">Destination slot.</param>
    /// <param name="backupKey">Slot-relative backup key returned by recovery or import.</param>
    /// <param name="cancellationToken">Cancels admission and reading.</param>
    /// <returns>The pre-restore backup key, or null when the slot had no current file.</returns>
    public async Task<string?> RestoreBackupAsync(int slot, string backupKey,
        CancellationToken cancellationToken = default)
    {
        EnsureContentEnabled();
        ValidateBackupKey(backupKey);
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var storage = GetSlotStorage(slot);
            var backups = (IStorageBackup)storage;
            var restore = await backups.ReadSnapshotAsync(backupKey, cancellationToken).ConfigureAwait(false);
            string? currentBackupKey = null;
            if (await storage.ExistsAsync(_config.SaveFileName).ConfigureAwait(false))
            {
                var current = await backups.ReadSnapshotAsync(_config.SaveFileName, cancellationToken).ConfigureAwait(false);
                currentBackupKey = CreateBackupKey("pre-restore");
                await backups.WriteSnapshotAsync(currentBackupKey, current, cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            await backups.WriteSnapshotAsync(_config.SaveFileName, restore, cancellationToken).ConfigureAwait(false);
            return currentBackupKey;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Holds the slot through backup, game-owned transformation and final atomic commit.</summary>
    private async Task<ContentSaveRecoveryResult<TSaveData>> RecoverOrImportAsync(int slot,
        ContentSaveReport approvedReport, bool importingLegacy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(approvedReport);
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var storage = GetSlotStorage(slot);
            var backups = (IStorageBackup)storage;
            var snapshot = await backups.ReadSnapshotAsync(_config.SaveFileName, cancellationToken).ConfigureAwait(false);
            var catalog = GetCurrentCatalog();
            ValidateApproval(slot, approvedReport, snapshot, catalog);
            var data = backups.DeserializeSnapshot<TSaveData>(snapshot);
            var currentReport = BuildReport(slot, snapshot.Fingerprint, data, catalog);
            ValidateRecoveryMode(currentReport, importingLegacy, data);

            var backupKey = CreateBackupKey(importingLegacy ? "pre-import" : "pre-recovery");
            await backups.WriteSnapshotAsync(backupKey, snapshot, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!importingLegacy)
            {
                data = await _contentOptions!.RecoverAsync!(data, currentReport, cancellationToken).ConfigureAwait(false);
                if (data is null)
                {
                    throw new InvalidOperationException("The content recovery hook returned null.");
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            var prepared = MigrateInMemory(slot, data);
            ValidatePreparedSave(prepared, catalog);

            // A hook can await game work. Recheck the bytes after it returns so a changed file is not silently replaced.
            var beforeCommit = await backups.ReadSnapshotAsync(_config.SaveFileName, cancellationToken).ConfigureAwait(false);
            ValidateApproval(slot, approvedReport, beforeCommit, GetCurrentCatalog());
            await WriteWithContentAsync(storage, prepared, catalog.Snapshot, cancellationToken).ConfigureAwait(false);
            return new ContentSaveRecoveryResult<TSaveData>(prepared, backupKey);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Rejects accidental use of repair for legacy import or a save with no blocking content difference.</summary>
    private void ValidateRecoveryMode(ContentSaveReport report, bool importingLegacy, TSaveData data)
    {
        if (importingLegacy)
        {
            if (!report.IsLegacy || !_contentOptions!.AcceptLegacy!(data))
            {
                throw new InvalidOperationException("The save is not a recognized metadata-free legacy format.");
            }
        }
        else if (report.IsLegacy || !report.RequiresRecovery)
        {
            throw new InvalidOperationException("Recovery requires a non-legacy save with blocking content differences.");
        }
    }

    /// <summary>Checks report provenance as well as file and target-session stability.</summary>
    private void ValidateApproval(int slot, ContentSaveReport report, StorageSnapshot snapshot, ContentCatalog catalog)
    {
        if (report.Owner != _reportOwner || report.Slot != slot ||
            !string.Equals(report.SourceFingerprint, snapshot.Fingerprint, StringComparison.Ordinal) ||
            !SnapshotsEqual(report.CurrentContent, catalog.Snapshot))
        {
            throw new InvalidOperationException("The save or content session changed. Inspect again and request new confirmation.");
        }
    }

    /// <summary>Reads a new DTO and blocks incompatible content before version migration can run or persist anything.</summary>
    private async Task<TSaveData> ReadForLoadAsync(int slot, IStorage storage, CancellationToken cancellationToken)
    {
        if (_contentOptions is null)
        {
            var ordinary = await storage.ReadAsync<TSaveData>(_config.SaveFileName).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return ordinary;
        }
        var backups = (IStorageBackup)storage;
        var snapshot = await backups.ReadSnapshotAsync(_config.SaveFileName, cancellationToken).ConfigureAwait(false);
        var data = backups.DeserializeSnapshot<TSaveData>(snapshot);
        var report = BuildReport(slot, snapshot.Fingerprint, data, GetCurrentCatalog());
        if (report.RequiresRecovery)
        {
            throw new ContentSaveCompatibilityException(report);
        }
        return data;
    }

    /// <summary>Creates deterministic package and content differences without executing game repair or migration.</summary>
    private ContentSaveReport BuildReport(int slot, string fingerprint, TSaveData data, ContentCatalog catalog)
    {
        var saved = ((IContentAwareSaveData)data).Content;
        var issues = new List<ContentSaveIssue>();
        if (saved is null)
        {
            issues.Add(new ContentSaveIssue(ContentSaveIssueKind.LegacyMetadata,
                "The save has no content metadata and requires explicit legacy import.", blocking: true));
        }
        else
        {
            CollectPackageIssues(saved, catalog, issues);
        }
        foreach (var reference in EnumerateReferences(data))
        {
            if (!catalog.Contains(reference))
            {
                issues.Add(new ContentSaveIssue(ContentSaveIssueKind.MissingContent,
                    $"Content '{reference.Id}' in table '{reference.TableName}' is unavailable.",
                    blocking: true, reference.Id.PackId, reference));
            }
        }
        return new ContentSaveReport(_reportOwner, slot, fingerprint, saved, catalog.Snapshot, issues);
    }

    /// <summary>Compares all saved packages, preserving order and surfacing additive changes as advisory.</summary>
    private void CollectPackageIssues(ContentSnapshot saved, ContentCatalog catalog, List<ContentSaveIssue> issues)
    {
        var current = catalog.Snapshot;
        var previous = saved.Packs.ToDictionary(static pack => pack.Id, StringComparer.Ordinal);
        var active = current.Packs.ToDictionary(static pack => pack.Id, StringComparer.Ordinal);
        foreach (var oldPack in saved.Packs)
        {
            if (!active.TryGetValue(oldPack.Id, out var newPack))
            {
                issues.Add(new ContentSaveIssue(ContentSaveIssueKind.MissingPack,
                    $"Package '{oldPack.Id}' ({oldPack.Version}) is not enabled.", blocking: true, oldPack.Id));
            }
            else if (!IsPackCompatible(oldPack, newPack, catalog))
            {
                issues.Add(new ContentSaveIssue(ContentSaveIssueKind.IncompatiblePack,
                    $"Package '{oldPack.Id}' changed from {oldPack.Version} to {newPack.Version} without declared compatibility.",
                    blocking: true, oldPack.Id));
            }
        }
        foreach (var newPack in current.Packs.Where(pack => !previous.ContainsKey(pack.Id)))
        {
            issues.Add(new ContentSaveIssue(ContentSaveIssueKind.AdditionalPack,
                $"Package '{newPack.Id}' is newly enabled.", blocking: false, newPack.Id));
        }
        var oldOrder = saved.Packs.Where(pack => active.ContainsKey(pack.Id)).Select(static pack => pack.Id);
        var newOrder = current.Packs.Where(pack => previous.ContainsKey(pack.Id)).Select(static pack => pack.Id);
        if (!oldOrder.SequenceEqual(newOrder, StringComparer.Ordinal))
        {
            issues.Add(new ContentSaveIssue(ContentSaveIssueKind.LoadOrderChanged,
                "The effective order of previously enabled packages changed.", blocking: false));
        }
    }

    /// <summary>
    /// Uses exact identity first, then an explicit host override or the installed package's declared old-version range.
    /// A range never silently accepts edited files at the same version; that requires a deliberate host decision.
    /// </summary>
    private bool IsPackCompatible(ContentPackStamp previous, ContentPackStamp current, ContentCatalog catalog)
    {
        if (PackStampsEqual(previous, current)) return true;
        if (_contentOptions!.IsPackCompatible is { } hostPolicy) return hostPolicy(previous, current);
        if (string.Equals(previous.Version, current.Version, StringComparison.Ordinal)) return false;
        var declaration = catalog.Packages.Single(pack => string.Equals(pack.Manifest.Id, current.Id, StringComparison.Ordinal));
        var accepted = declaration.Manifest.SaveCompatibleVersions;
        return accepted is not null && ContentVersionRules.Contains(previous.Version, accepted);
    }

    /// <summary>Enforces game-owned content references and business validation on a fully prepared object.</summary>
    private void ValidatePreparedSave(TSaveData data, ContentCatalog catalog)
    {
        foreach (var reference in EnumerateReferences(data))
        {
            if (!catalog.Contains(reference))
            {
                throw new InvalidOperationException($"Prepared save references missing content '{reference.Id}' in '{reference.TableName}'.");
            }
        }
        _contentOptions!.Validate?.Invoke(data);
    }

    /// <summary>Rejects a broken game reference enumerator rather than accepting an unvalidated save.</summary>
    private IEnumerable<ContentReference> EnumerateReferences(TSaveData data) =>
        _contentOptions!.EnumerateReferences(data) ?? throw new InvalidOperationException("Save references cannot be null.");

    /// <summary>Attaches the session stamp only for a validated write and rolls metadata back when storage fails.</summary>
    private async Task WriteWithContentAsync(IStorage storage, TSaveData data, ContentSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var contentData = (IContentAwareSaveData)data;
        var previous = contentData.Content;
        contentData.Content = snapshot;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await storage.WriteAsync(_config.SaveFileName, data).ConfigureAwait(false);
        }
        catch
        {
            contentData.Content = previous;
            throw;
        }
    }

    /// <summary>Gets the host's already-loaded immutable content session.</summary>
    private ContentCatalog GetCurrentCatalog() => _contentOptions!.CurrentCatalog() ??
        throw new InvalidOperationException("The current content catalog is unavailable.");

    /// <summary>Rejects recovery APIs when the repository uses the unchanged ordinary-save path.</summary>
    private void EnsureContentEnabled()
    {
        if (_contentOptions is null)
        {
            throw new InvalidOperationException("This repository has not enabled content-aware saves.");
        }
    }

    /// <summary>Compares the complete effective package sequence, including development edits at unchanged versions.</summary>
    private static bool SnapshotsEqual(ContentSnapshot left, ContentSnapshot right) =>
        left.Packs.Count == right.Packs.Count && left.Packs.Zip(right.Packs).All(static pair => PackStampsEqual(pair.First, pair.Second));

    /// <summary>Uses explicit ordinal comparisons so fingerprints do not depend on current culture.</summary>
    private static bool PackStampsEqual(ContentPackStamp left, ContentPackStamp right) =>
        string.Equals(left.Id, right.Id, StringComparison.Ordinal) &&
        string.Equals(left.Version, right.Version, StringComparison.Ordinal) &&
        string.Equals(left.Fingerprint, right.Fingerprint, StringComparison.Ordinal);

    /// <summary>Produces non-overwriting backup names that remain ordinary slot-relative storage keys.</summary>
    private static string CreateBackupKey(string reason) =>
        $"backups/{reason}-{DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff", CultureInfo.InvariantCulture)}-{Guid.NewGuid():N}";

    /// <summary>Limits restore to the selected slot's backup namespace.</summary>
    private static void ValidateBackupKey(string backupKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupKey);
        if (!backupKey.StartsWith("backups/", StringComparison.Ordinal) || backupKey.Contains("..", StringComparison.Ordinal) ||
            backupKey.Contains('\\', StringComparison.Ordinal) || backupKey.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException("A backup key must stay within the slot's backups directory.", nameof(backupKey));
        }
    }
}
