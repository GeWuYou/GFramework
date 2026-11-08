// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DodgeTheCreeps.Advanced.Core.Progress;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Data;
using Godot;
using Newtonsoft.Json.Linq;

namespace DodgeTheCreeps.Advanced;

/// <summary>Owns document persistence, recovery confirmation, backup metadata and the host's mod-state envelope.</summary>
internal sealed class SaveService : IRunStore
{
    private readonly IStorage _storage;
    private readonly Content.DodgeContent _content;
    private readonly IUiService _ui;
    private readonly DodgeLocalization _text;
    private readonly GamePresentationState _presentation;
    private readonly ModService _mods;
    private readonly string _dataRoot;
    private readonly CancellationToken _cancellationToken;
    private SaveRepository<DodgeProgress> _saves = null!;
    private string? _latestBackup;

    /// <summary>Creates the storage boundary; document options are built only after content initialization.</summary>
    internal SaveService(IStorage storage, Content.DodgeContent content, IUiService ui,
        GamePresentationState presentation, ModService mods, GameLaunchOptions launch, GameLifetime lifetime)
    {
        _storage = storage;
        _content = content;
        _ui = ui;
        _text = presentation.Text;
        _presentation = presentation;
        _mods = mods;
        _dataRoot = launch.DataRoot;
        _cancellationToken = lifetime.Token;
    }

    /// <summary>The document repository, exposed internally to native acceptance fixtures.</summary>
    internal SaveRepository<DodgeProgress> Repository => _saves;

    /// <summary>The last exact-byte backup entry; setting it is restricted to the game's host assembly.</summary>
    internal string? LatestBackup
    {
        get => _latestBackup; set => _latestBackup = value;
    }

    /// <summary>Creates the document repository and restores a backup pointer only while a save still exists.</summary>
    internal async Task InitializeAsync()
    {
        _saves = new SaveRepository<DodgeProgress>(_storage,
            new SaveConfiguration { SaveRoot = _dataRoot + "/saves" }, CreateSaveOptions());
        BeginNewRun();
        if (await _saves.ExistsAsync(1, _cancellationToken).ConfigureAwait(true) &&
            await _storage.ExistsAsync(_dataRoot + "/last-content-backup.json").ConfigureAwait(true))
            _latestBackup = await _storage.ReadAsync<string>(_dataRoot + "/last-content-backup.json").ConfigureAwait(true);
    }

    /// <summary>Creates a fresh isolated mod payload for a new run.</summary>
    public void BeginNewRun() => _mods.State = _saves.CreateModState();

    /// <summary>Loads the current run after document preparation and explicit recovery consent.</summary>
    public async Task<RunSnapshot?> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await LoadContentSaveAsync().ConfigureAwait(true);
    }

    /// <summary>Persists the board together with the active mod session.</summary>
    public Task SaveAsync(RunSnapshot snapshot, CancellationToken cancellationToken) =>
        _saves.SaveDocumentAsync(1, DodgeProgress.FromSnapshot(snapshot), _mods.State, cancellationToken);

    /// <summary>Invalidates backup recovery before deleting the ended run.</summary>
    public async Task RetireAsync(CancellationToken cancellationToken)
    {
        _latestBackup = null;
        await _saves.DeleteAsync(1, cancellationToken: cancellationToken).ConfigureAwait(true);
        await StoreBackupKeyAsync().ConfigureAwait(true);
    }

    internal async Task RestoreContentBackupAsync()
    {
        if (_latestBackup is null)
            return;
        var decision = await _ui.ShowDialogAsync(DodgeRoutes.Recover,
            () => _text.Get("restore_question", ("backup", _latestBackup ?? string.Empty)), _cancellationToken).ConfigureAwait(true);
        if (!decision.Accepted || !decision.Value)
            return;
        _latestBackup = await _saves.RestoreBackupAsync(1, _latestBackup, _cancellationToken).ConfigureAwait(true);
        await StoreBackupKeyAsync().ConfigureAwait(true);
        _presentation.SetStatus(() => _text.Get("save_restored"));
    }

    // #region tutorial-save-policy
    /// <summary>Builds the explicit document schema independently of the current gameplay DTO.</summary>
    private SaveRepositoryOptions<DodgeProgress> CreateSaveOptions()
    {
        using var random = new RandomNumberGenerator { Seed = 0xD0D63 };
        return new SaveRepositoryOptions<DodgeProgress>
        {
            GameId = "dodge-the-creeps-advanced",
            CurrentVersion = DodgeProgress.CurrentVersion,
            Migrations = DodgeProgressMigration.CreateGraph(random.State),
            Validate = ValidateProgress,
            ModPolicies = _content.IsReady ? [.. _content.Catalog.Packages.Where(static pack => pack.Manifest.Code is not null).Select(static pack => new ModSavePolicy(pack.Manifest.Id, 1))] : Array.Empty<ModSavePolicy>(),
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
                    if (!_content.Catalog.Contains(new ContentReference("arenas", new ContentId(arena))))
                        data["ArenaId"] = "base:yard";
                    var enemies = data["Enemies"] as JArray ?? throw new InvalidOperationException("Enemies must be an array.");
                    foreach (var enemy in enemies.OfType<JObject>().ToArray())
                    {
                        var id = new ContentId((string?)enemy["EnemyId"] ?? string.Empty);
                        if (!_content.Catalog.Contains(new ContentReference("enemies", id)))
                        {
                            enemy.Remove();
                            continue;
                        }
                        var definition = _content.Catalog.GetTable<Content.DodgeEnemyDefinition>("enemies").Get(id);
                        if (((int?)enemy["AnimationFrame"] ?? 0) >= (definition.Texture.Length > 0 ? 1 : 2))
                        {
                            enemy["AnimationFrame"] = 0;
                            enemy["AnimationProgress"] = 0;
                        }
                    }
                    return Task.FromResult(document);
                },
            },
        };
    }
    // #endregion tutorial-save-policy

    /// <summary>Prepares on detached documents, asks for a decision, then commits once before activating gameplay.</summary>
    private async Task<DodgeProgress?> LoadContentSaveAsync()
    {
        var prepared = await _saves.PrepareLoadAsync(1, _cancellationToken).ConfigureAwait(true);
        if (prepared.RequiresCommit)
        {
            var decision = await _ui.ShowDialogAsync(DodgeRoutes.Recover,
                () => _text.Get("recovery_question") + "\n\n" + FormatPreparedSave(prepared), _cancellationToken).ConfigureAwait(true);
            if (!decision.Accepted || !decision.Value)
                return null;
            var result = await _saves.CommitPreparedAsync(1, prepared, _cancellationToken).ConfigureAwait(true);
            _latestBackup = result.BackupKey;
            await StoreBackupKeyAsync().ConfigureAwait(true);
        }
        _mods.State = prepared.ModState;
        return prepared.Data;
    }

    internal Task StoreBackupKeyAsync() => _latestBackup is null
        ? _storage.DeleteAsync(_dataRoot + "/last-content-backup.json")
        : _storage.WriteAsync(_dataRoot + "/last-content-backup.json", _latestBackup);

    /// <summary>Displays structural conversion steps and content differences together before a write.</summary>
    internal string FormatPreparedSave(PreparedSave<DodgeProgress> prepared) => string.Join('\n',
        prepared.MigrationPath.Select(step => _text.Get("migration_step", ("from", step.FromVersion), ("to", step.ToVersion),
            ("detail", step.FromVersion == 1 && step.ToVersion == 2 ? _text.Get("migration_sparse") :
                step.FromVersion == 3 && step.ToVersion == 2 ? _text.Get("migration_downgrade") : step.Description)))
            .Concat(prepared.Issues.Select(issue => FormatSaveIssue(prepared, issue))));

    /// <summary>Preserves localized recovery messages while showing stable author-defined IDs and exact source versions.</summary>
    private string FormatSaveIssue(PreparedSave<DodgeProgress> prepared, ContentSaveIssue issue)
    {
        var id = issue.PackId ?? string.Empty;
        var previous = prepared.SavedContent?.Packs.FirstOrDefault(pack => string.Equals(pack.Id, id, StringComparison.Ordinal))?.Version ?? "?";
        var current = _content.Catalog.Snapshot.Packs.FirstOrDefault(pack => string.Equals(pack.Id, id, StringComparison.Ordinal))?.Version ?? "?";
        return issue.Kind switch
        {
            ContentSaveIssueKind.MissingPack => _text.Get("issue_missing_pack", ("id", id), ("version", previous)),
            ContentSaveIssueKind.IncompatiblePack => _text.Get("issue_incompatible_pack", ("id", id), ("old", previous), ("current", current)),
            ContentSaveIssueKind.MissingContent => _text.Get("issue_missing_content", ("id", issue.Reference?.Id.ToString() ?? "?"), ("table", issue.Reference?.TableName ?? "?")),
            ContentSaveIssueKind.AdditionalPack => _text.Get("issue_added_pack", ("id", id)),
            ContentSaveIssueKind.LoadOrderChanged => _text.Get("issue_order"),
            ContentSaveIssueKind.MissingMetadata => _text.Get("issue_missing_metadata"),
            _ => issue.Detail,
        };
    }

    /// <summary>Enumerates IDs after structural conversion, without constructing the current gameplay DTO.</summary>
    private static IEnumerable<ContentReference> EnumerateProgressReferences(SaveDocument document)
    {
        var data = document.Game.Data;
        yield return new ContentReference("arenas", new ContentId((string?)data["ArenaId"] ?? string.Empty));
        if (data["Enemies"] is not JArray enemies)
            throw new InvalidOperationException("Enemies must be an array.");
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
