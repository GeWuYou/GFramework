// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Data;
using Godot;
using Newtonsoft.Json.Linq;

namespace DodgeTheCreeps;

/// <summary>Game-owned raw-document references, pure repair policies and explicit player decisions.</summary>
public partial class DodgeApp
{
    /// <summary>Opens package management even when gameplay startup failed.</summary>
    internal Task OpenContentAsync() => _ui.ShowAsync(DodgeRoutes.Content, true, _lifetime.Token);

    /// <summary>Requires exact installed-code approval before recording an enabled next-start choice.</summary>
    internal async Task ToggleContentAsync(string id)
    {
        var pack = _content.Packages.SingleOrDefault(item => string.Equals(item.Manifest.Id, id, StringComparison.Ordinal));
        if (pack?.Manifest.Code is not null && !_content.RequestedProfile.EnabledPackages.Contains(id, StringComparer.Ordinal))
        {
            var stamp = new ContentPackStamp(id, pack.Manifest.Version, pack.Fingerprint);
            if (!_content.IsCodeApproved(stamp))
            {
                var decision = await _ui.ShowDialogAsync(DodgeRoutes.CodePermission,
                    () => Text.Get("code_execution_question", ("name", Text.PackageName(id, pack.Manifest.Name)), ("version", pack.Manifest.Version)),
                    _lifetime.Token).ConfigureAwait(true);
                if (!decision.Accepted || decision.Value != true) return;
                await _content.ApproveCodeAsync(id, _lifetime.Token).ConfigureAwait(true);
            }
        }
        await _content.ToggleAsync(id, _lifetime.Token).ConfigureAwait(true);
    }

    /// <summary>Lets an already enabled but updated package obtain fresh exact-byte approval for the next start.</summary>
    internal async Task ReapproveCodeAsync(string id)
    {
        var pack = _content.Packages.Single(item => string.Equals(item.Manifest.Id, id, StringComparison.Ordinal));
        var decision = await _ui.ShowDialogAsync(DodgeRoutes.CodePermission,
            () => Text.Get("code_execution_question", ("name", Text.PackageName(id, pack.Manifest.Name)), ("version", pack.Manifest.Version)),
            _lifetime.Token).ConfigureAwait(true);
        if (decision.Accepted && decision.Value == true) await _content.ApproveCodeAsync(id, _lifetime.Token).ConfigureAwait(true);
    }

    /// <summary>Records ordering without mutating the active content session.</summary>
    internal Task MoveContentAsync(string id, int direction) => _content.MoveAsync(id, direction, _lifetime.Token);
    /// <summary>Explicitly replaces broken selection preferences with base-only choices.</summary>
    internal Task ResetContentAsync() => _content.ResetAsync(_lifetime.Token);
    /// <summary>Selects an already loaded arena.</summary>
    internal Task CycleArenaAsync() => _content.CycleArenaAsync(_lifetime.Token);

    /// <summary>Restores original bytes; a subsequent continue still checks their content and schema.</summary>
    internal async Task RestoreContentBackupAsync()
    {
        if (_latestBackup is null) return;
        var decision = await _ui.ShowDialogAsync(DodgeRoutes.Recover,
            () => Text.Get("restore_question", ("backup", _latestBackup ?? string.Empty)), _lifetime.Token).ConfigureAwait(true);
        if (!decision.Accepted || decision.Value != true) return;
        _latestBackup = await _saves.RestoreBackupAsync(1, _latestBackup, _lifetime.Token).ConfigureAwait(true);
        await StoreBackupKeyAsync().ConfigureAwait(true);
        _menus.SetStatus(() => Text.Get("save_restored"));
    }

    // #region tutorial-save-policy
    /// <summary>Builds the explicit document schema independently of the current gameplay DTO.</summary>
    private SaveRepositoryOptions<DodgeProgress> CreateSaveOptions()
    {
        using var random = new RandomNumberGenerator { Seed = 0xD0D63 };
        return new SaveRepositoryOptions<DodgeProgress>
        {
            GameId = "dodge-the-creeps", CurrentVersion = DodgeProgress.CurrentVersion,
            Migrations = DodgeProgressMigration.CreateGraph(random.State), Validate = ValidateProgress,
            ModPolicies = _content.IsReady ? _content.Catalog.Packages.Where(static pack => pack.Manifest.Code is not null)
                .Select(static pack => new ModSavePolicy(pack.Manifest.Id, 1)).ToArray() : Array.Empty<ModSavePolicy>(),
            Content = new ContentSaveOptions
            {
                CurrentCatalog = () => _content.Catalog,
                EnumerateReferences = EnumerateProgressReferences,
                RecoverAsync = (document, _, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var data = document.Game.Data;
                    // Preparation owns this detached DOM. Missing-content policies never touch the running world.
                    var arena = (string?)data["ArenaId"] ?? "base:yard";
                    if (!_content.Catalog.Contains(new ContentReference("arenas", new ContentId(arena)))) data["ArenaId"] = "base:yard";
                    var enemies = data["Enemies"] as JArray ?? throw new InvalidOperationException("Enemies must be an array.");
                    foreach (var enemy in enemies.OfType<JObject>().ToArray())
                    {
                        var id = new ContentId((string?)enemy["EnemyId"] ?? string.Empty);
                        if (!_content.Catalog.Contains(new ContentReference("enemies", id))) { enemy.Remove(); continue; }
                        var definition = _content.Catalog.GetTable<Content.DodgeEnemyDefinition>("enemies").Get(id);
                        if (((int?)enemy["AnimationFrame"] ?? 0) >= (definition.Texture.Length > 0 ? 1 : 2))
                        {
                            enemy["AnimationFrame"] = 0; enemy["AnimationProgress"] = 0;
                        }
                    }
                    return Task.FromResult(document);
                }
            }
        };
    }
    // #endregion tutorial-save-policy

    /// <summary>Prepares on detached documents, asks for a decision, then commits once before activating gameplay.</summary>
    private async Task<DodgeProgress?> LoadContentSaveAsync()
    {
        var prepared = await _saves.PrepareLoadAsync(1, _lifetime.Token).ConfigureAwait(true);
        if (prepared.RequiresCommit)
        {
            var decision = await _ui.ShowDialogAsync(DodgeRoutes.Recover,
                () => Text.Get("recovery_question") + "\n\n" + FormatPreparedSave(prepared), _lifetime.Token).ConfigureAwait(true);
            if (!decision.Accepted || decision.Value != true) return null;
            var result = await _saves.CommitPreparedAsync(1, prepared, _lifetime.Token).ConfigureAwait(true);
            _latestBackup = result.BackupKey;
            await StoreBackupKeyAsync().ConfigureAwait(true);
        }
        _modState = prepared.ModState;
        return prepared.Data;
    }

    private Task StoreBackupKeyAsync() => _latestBackup is null
        ? _storage.DeleteAsync(_dataRoot + "/last-content-backup.json")
        : _storage.WriteAsync(_dataRoot + "/last-content-backup.json", _latestBackup);

    /// <summary>Displays structural conversion steps and content differences together before a write.</summary>
    private string FormatPreparedSave(PreparedSave<DodgeProgress> prepared) => string.Join("\n",
        prepared.MigrationPath.Select(step => Text.Get("migration_step", ("from", step.FromVersion), ("to", step.ToVersion),
            ("detail", step.FromVersion == 1 && step.ToVersion == 2 ? Text.Get("migration_sparse") :
                step.FromVersion == 3 && step.ToVersion == 2 ? Text.Get("migration_downgrade") : step.Description)))
            .Concat(prepared.Issues.Select(issue => FormatSaveIssue(prepared, issue))));

    /// <summary>Preserves localized recovery messages while showing stable author-defined IDs and exact source versions.</summary>
    private string FormatSaveIssue(PreparedSave<DodgeProgress> prepared, ContentSaveIssue issue)
    {
        var id = issue.PackId ?? string.Empty;
        var previous = prepared.SavedContent?.Packs.FirstOrDefault(pack => string.Equals(pack.Id, id, StringComparison.Ordinal))?.Version ?? "?";
        var current = _content.Catalog.Snapshot.Packs.FirstOrDefault(pack => string.Equals(pack.Id, id, StringComparison.Ordinal))?.Version ?? "?";
        return issue.Kind switch
        {
            ContentSaveIssueKind.MissingPack => Text.Get("issue_missing_pack", ("id", id), ("version", previous)),
            ContentSaveIssueKind.IncompatiblePack => Text.Get("issue_incompatible_pack", ("id", id), ("old", previous), ("current", current)),
            ContentSaveIssueKind.MissingContent => Text.Get("issue_missing_content", ("id", issue.Reference?.Id.ToString() ?? "?"), ("table", issue.Reference?.TableName ?? "?")),
            ContentSaveIssueKind.AdditionalPack => Text.Get("issue_added_pack", ("id", id)),
            ContentSaveIssueKind.LoadOrderChanged => Text.Get("issue_order"),
            ContentSaveIssueKind.MissingMetadata => Text.Get("issue_missing_metadata"),
            _ => issue.Detail
        };
    }

    /// <summary>Enumerates IDs after structural conversion, without constructing the current gameplay DTO.</summary>
    private static IEnumerable<ContentReference> EnumerateProgressReferences(SaveDocument document)
    {
        var data = document.Game.Data;
        yield return new ContentReference("arenas", new ContentId((string?)data["ArenaId"] ?? string.Empty));
        if (data["Enemies"] is not JArray enemies) throw new InvalidOperationException("Enemies must be an array.");
        foreach (var enemy in enemies)
            yield return new ContentReference("enemies", new ContentId((string?)enemy["EnemyId"] ?? string.Empty));
    }

    /// <summary>Validates current DTO values and resolved native presentation before persistence or activation.</summary>
    private void ValidateProgress(DodgeProgress save)
    {
        DodgeProgress.ValidateSnapshot(save);
        foreach (var enemy in save.Enemies)
        {
            var definition = _content.Catalog.GetTable<Content.DodgeEnemyDefinition>("enemies").Get(new ContentId(enemy.EnemyId));
            if (enemy.AnimationFrame >= (definition.Texture.Length > 0 ? 1 : 2))
                throw new InvalidOperationException("The saved enemy animation is incompatible with its current content: " + enemy.EnemyId);
        }
    }
}
