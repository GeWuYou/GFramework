// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DodgeTheCreeps.Content;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Data;

namespace DodgeTheCreeps;

/// <summary>Game-owned content selection, reference checks and player-confirmed save repair.</summary>
public partial class DodgeApp
{
    /// <summary>Opens package management without requiring a valid gameplay catalog.</summary>
    internal Task OpenContentAsync() => _ui.ShowAsync(DodgeRoutes.Content, true, _lifetime.Token);

    /// <summary>Records package activation for the next process start.</summary>
    internal Task ToggleContentAsync(string id) => _content.ToggleAsync(id, _lifetime.Token);

    /// <summary>Records player ordering while retaining dependency constraints.</summary>
    internal Task MoveContentAsync(string id, int direction) => _content.MoveAsync(id, direction, _lifetime.Token);

    /// <summary>Explicitly resets unreadable or invalid preferences without deleting installed files.</summary>
    internal Task ResetContentAsync() => _content.ResetAsync(_lifetime.Token);

    /// <summary>Selects an arena already present in this process's immutable session.</summary>
    internal Task CycleArenaAsync() => _content.CycleArenaAsync(_lifetime.Token);

    /// <summary>Restores the exact previous file only after another explicit player decision.</summary>
    internal async Task RestoreContentBackupAsync()
    {
        if (_latestBackup is null) return;
        var decision = await _ui.ShowDialogAsync(DodgeRoutes.Recover,
            () => Text.Get("restore_question", ("backup", _latestBackup ?? string.Empty)),
            _lifetime.Token).ConfigureAwait(true);
        if (!decision.Accepted || decision.Value != true) return;
        var backup = await _saves.RestoreBackupAsync(1, _latestBackup, _lifetime.Token).ConfigureAwait(true);
        _latestBackup = backup;
        await StoreBackupKeyAsync().ConfigureAwait(true);
        _menus.SetStatus(() => Text.Get("save_restored"));
    }

    private ContentSaveOptions<DodgeProgress> CreateContentSaveOptions() => new()
    {
        CurrentCatalog = () => _content.Catalog,
        EnumerateReferences = EnumerateProgressReferences,
        Validate = ValidateProgress,
        // This sample's historical v1 contained only score and numeric coordinates in the base game.
        AcceptLegacy = static save => save.Version == 1 && string.Equals(save.ArenaId, "base:yard", StringComparison.Ordinal),
        RecoverAsync = (save, _, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_content.Catalog.Contains(new ContentReference("arenas", new ContentId(save.ArenaId))))
                save.ArenaId = "base:yard";
            if (save.Version == DodgeProgress.CurrentVersion)
            {
                // The repository already made a lossless backup. Keep every valid enemy unchanged;
                // only references whose providing content disappeared are removed by this game's explicit policy.
                save.Enemies = save.Enemies.Where(enemy => _content.Catalog.Contains(
                    new ContentReference("enemies", new ContentId(enemy.EnemyId)))).ToList();
                foreach (var enemy in save.Enemies)
                {
                    var definition = _content.Catalog.GetTable<DodgeEnemyDefinition>("enemies").Get(new ContentId(enemy.EnemyId));
                    var frameCount = definition.Texture.Length > 0 ? 1 : 2;
                    if (enemy.AnimationFrame >= frameCount)
                    {
                        enemy.AnimationFrame = 0;
                        enemy.AnimationProgress = 0;
                    }
                }
            }
            return Task.FromResult(save);
        }
    };

    private async Task<DodgeProgress?> LoadContentSaveAsync()
    {
        var report = await _saves.InspectContentAsync(1, _lifetime.Token).ConfigureAwait(true);
        if (!report.RequiresRecovery)
        {
            if (report.HasChanges)
                _menus.SetStatus(() => FormatContentReport(report));
            return await _saves.LoadAsync(1, _lifetime.Token).ConfigureAwait(true);
        }

        var decision = await _ui.ShowDialogAsync(DodgeRoutes.Recover,
            () => report.IsLegacy ? Text.Get("save_update_question") :
                Text.Get("recovery_question") + "\n\n" + FormatContentReport(report),
            _lifetime.Token).ConfigureAwait(true);
        if (!decision.Accepted || decision.Value != true) return null;
        var result = report.IsLegacy
            ? await _saves.ImportLegacyAsync(1, report, _lifetime.Token).ConfigureAwait(true)
            : await _saves.RecoverContentAsync(1, report, _lifetime.Token).ConfigureAwait(true);
        _latestBackup = result.BackupKey;
        await StoreBackupKeyAsync().ConfigureAwait(true);
        return result.Data;
    }

    private Task StoreBackupKeyAsync() => _latestBackup is null
        ? _storage.DeleteAsync(_dataRoot + "/last-content-backup.json")
        : _storage.WriteAsync(_dataRoot + "/last-content-backup.json", _latestBackup);

    /// <summary>Localizes stable issue categories while preserving package IDs, versions and target fields.</summary>
    private string FormatContentReport(ContentSaveReport report) => string.Join("\n", report.Issues.Select(issue =>
    {
        var id = issue.PackId ?? string.Empty;
        var previous = report.SavedContent?.Packs.FirstOrDefault(pack => string.Equals(pack.Id, id, StringComparison.Ordinal))?.Version ?? "?";
        var current = report.CurrentContent.Packs.FirstOrDefault(pack => string.Equals(pack.Id, id, StringComparison.Ordinal))?.Version ?? "?";
        return issue.Kind switch
        {
            ContentSaveIssueKind.MissingPack => Text.Get("issue_missing_pack", ("id", id), ("version", previous)),
            ContentSaveIssueKind.IncompatiblePack => Text.Get("issue_incompatible_pack", ("id", id), ("old", previous), ("current", current)),
            ContentSaveIssueKind.MissingContent => Text.Get("issue_missing_content", ("id", issue.Reference?.Id.ToString() ?? "?"), ("table", issue.Reference?.TableName ?? "?")),
            ContentSaveIssueKind.LegacyMetadata => Text.Get("issue_legacy"),
            ContentSaveIssueKind.AdditionalPack => Text.Get("issue_added_pack", ("id", id)),
            ContentSaveIssueKind.LoadOrderChanged => Text.Get("issue_order"),
            _ => issue.Detail
        };
    }));

    /// <summary>Enumerates every persistent content reference before migrations or repairs may write.</summary>
    private static IEnumerable<ContentReference> EnumerateProgressReferences(DodgeProgress save)
    {
        if (save.Version == DodgeProgress.CurrentVersion) DodgeProgress.ValidateSnapshot(save);
        else if (save.Version == 1) DodgeProgress.ValidateLegacyValues(save);
        else throw new InvalidOperationException("The save format is newer than this game supports.");
        yield return new ContentReference("arenas", new ContentId(save.ArenaId));
        if (save.Version != DodgeProgress.CurrentVersion) yield break;
        foreach (var enemy in save.Enemies)
            yield return new ContentReference("enemies", new ContentId(enemy.EnemyId));
    }

    /// <summary>Validates the complete portable board before the repository commits migration or recovery.</summary>
    private void ValidateProgress(DodgeProgress save)
    {
        DodgeProgress.ValidateSnapshot(save);
        foreach (var enemy in save.Enemies)
        {
            var definition = _content.Catalog.GetTable<DodgeEnemyDefinition>("enemies").Get(new ContentId(enemy.EnemyId));
            if (enemy.AnimationFrame >= (definition.Texture.Length > 0 ? 1 : 2))
                throw new InvalidOperationException("The saved enemy animation is incompatible with its current content: " + enemy.EnemyId);
        }
    }
}
