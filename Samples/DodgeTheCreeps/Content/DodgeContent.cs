// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Content;
using GFramework.Game.Modding;
using GFramework.Godot.Content;
using Godot;
using FileAccess = Godot.FileAccess;

namespace DodgeTheCreeps.Content;

/// <summary>
/// Composes the engine-neutral content session with trusted native assets and player-owned
/// next-start settings. Its catalog is published only after all native resources validate.
/// A bad optional selection leaves the management shell available without starting gameplay.
/// </summary>
internal sealed class DodgeContent : IDisposable
{
    private readonly IStorage _storage;
    private readonly string _settingsKey;
    private readonly string _physicalRoot;
    private readonly SemaphoreSlim _selectionGate = new(1, 1);
    private readonly Dictionary<string, ImageTexture> _textures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AudioStream> _audio = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PackedScene> _scenes = new(StringComparer.Ordinal);
    private readonly List<ContentPack> _packages = [];
    private readonly List<string> _discoveryErrors = [];
    private readonly GodotContentPackMounter _native;
    private DodgeContentPreferences _preferences = new();
    private string _activeSelection = string.Empty;
    private ContentCatalog? _catalog;
    private GameModHost? _mods;
    private readonly GameModHostOptions? _modOptions;

    /// <summary>Creates the session on the Godot thread, before any native content resources load.</summary>
    internal DodgeContent(IStorage storage, string dataRoot, GameModHostOptions? modOptions = null)
    {
        _storage = storage;
        _modOptions = modOptions;
        _settingsKey = dataRoot + "/content.json";
        _physicalRoot = ProjectSettings.GlobalizePath(dataRoot + "/content");
        _native = new GodotContentPackMounter(static (pack, digest) =>
            string.Equals(pack.PackId, "moon", StringComparison.Ordinal) &&
            string.Equals(digest, OfficialPackTrust.MoonSha256, StringComparison.Ordinal));
    }

    /// <summary>Gets whether a complete content session can start gameplay.</summary>
    internal bool IsReady => _catalog is not null;
    /// <summary>Gets the complete active catalog; management can exist before this property becomes available.</summary>
    internal ContentCatalog Catalog => _catalog ?? throw new InvalidOperationException("Repair the content selection and restart the game.");
    /// <summary>Gets discovered packages, including disabled ones.</summary>
    internal IReadOnlyList<ContentPack> Packages => _packages;
    /// <summary>Gets the next-start preferences without changing the active catalog.</summary>
    internal ContentPackProfile RequestedProfile => _preferences.Profile;
    /// <summary>Gets the most recent content-startup failure.</summary>
    internal string? Error
    {
        get; private set;
    }
    /// <summary>Gets package discovery errors for the repair screen.</summary>
    internal IReadOnlyList<string> DiscoveryErrors => _discoveryErrors;
    /// <summary>Gets whether the next-start selection differs from this process's selection.</summary>
    internal bool PendingRestart => _catalog is null || !string.Equals(_activeSelection, SelectionKey(_preferences.Profile), StringComparison.Ordinal);
    /// <summary>Gets the currently selected available arena.</summary>
    internal string ArenaId => _preferences.ArenaId;
    /// <summary>Gets the physical directory into which players install data mod folders.</summary>
    internal string InstalledDirectory => Path.Combine(_physicalRoot, "installed");

    /// <summary>Checks exact installed bytes against the player's last explicit execution approval.</summary>
    internal bool IsCodeApproved(ContentPackStamp stamp) => _preferences.CodeApprovals.Contains(stamp);

    /// <summary>Persists explicit trust without changing the running process's active content.</summary>
    internal async Task ApproveCodeAsync(string id, CancellationToken cancellationToken)
    {
        var package = _packages.Single(pack => string.Equals(pack.Manifest.Id, id, StringComparison.Ordinal));
        if (package.Manifest.Code is null)
            throw new InvalidOperationException("This is not a code package.");
        await _selectionGate.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            var stamp = new ContentPackStamp(id, package.Manifest.Version, package.Fingerprint);
            var next = new DodgeContentPreferences
            {
                Profile = _preferences.Profile,
                ArenaId = _preferences.ArenaId,
                CodeApprovals = [.. _preferences.CodeApprovals.Where(item => !string.Equals(item.Id, id, StringComparison.Ordinal)), stamp],
            };
            await _storage.WriteAsync(_settingsKey, next).ConfigureAwait(true);
            _preferences = next;
        }
        finally { _selectionGate.Release(); }
    }

    /// <summary>Joins mod cleanup before the application releases native resources.</summary>
    internal Task StopModsAsync() => _mods?.StopAsync() ?? Task.CompletedTask;

    /// <summary>Copies game-owned fixture data, discovers packages, and prepares one immutable active session.</summary>
    internal async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_physicalRoot);
        var baseDirectory = Path.Combine(_physicalRoot, "base");
        var schemaDirectory = Path.Combine(_physicalRoot, "schemas");
        CopyShippedDirectory("res://content_sources/base", baseDirectory);
        CopyShippedDirectory("res://content-schemas", schemaDirectory);
        SeedOptionalExamples();

        await LoadPreferencesAsync().ConfigureAwait(true);
        _activeSelection = SelectionKey(_preferences.Profile);

        await DiscoverDirectoryAsync(baseDirectory, cancellationToken).ConfigureAwait(true);
        foreach (var directory in Directory.GetDirectories(InstalledDirectory).Order(StringComparer.Ordinal))
            await DiscoverDirectoryAsync(directory, cancellationToken).ConfigureAwait(true);
        if (Error is not null)
            return;

        try
        {
            var plan = Resolve(_preferences.Profile);
            _mods = await GameModHost.PrepareAsync(plan, _modOptions ?? new GameModHostOptions
            {
                GameId = "dodge-the-creeps",
                CodeApiVersion = "1.0.0",
                TrustPolicy = IsCodeApproved,
            }, cancellationToken).ConfigureAwait(true);
            var loader = new ContentCatalogLoader()
                .RegisterTable<DodgeEnemyDefinition>("enemies", Path.Combine(schemaDirectory, "enemies.schema.json"), static value => value.Id)
                .RegisterTable<DodgeArenaDefinition>("arenas", Path.Combine(schemaDirectory, "arenas.schema.json"), static value => value.Id);
            var catalog = await loader.LoadAsync(plan, _mods.Admission, cancellationToken).ConfigureAwait(true);
            ValidateRequiredRows(catalog);
            var native = plan.Packages.SelectMany(pack => pack.Manifest.ResourcePacks.Select(path =>
                new GodotContentPackDescriptor(pack.Manifest.Id, pack.RootPath, path))).ToArray();
            _native.MountAll(native);
            PrepareResources(catalog);
            if (!catalog.Contains(new ContentReference("arenas", new ContentId(_preferences.ArenaId))))
                _preferences.ArenaId = "base:yard";
            _catalog = catalog;
            await _mods.StartAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            GD.PushError(exception.ToString());
            _catalog = null;
            if (_mods is not null)
                await _mods.StopAsync(CancellationToken.None).ConfigureAwait(true);
            Error = exception.Message + (_native.RequiresRestart ? " Restart is required after repairing the selection." : string.Empty);
        }
    }

    /// <summary>Reads the next-start selection while retaining unreadable settings for the explicit repair action.</summary>
    private async Task LoadPreferencesAsync()
    {
        try
        {
            if (await _storage.ExistsAsync(_settingsKey).ConfigureAwait(true))
            {
                var candidate = await _storage.ReadAsync<DodgeContentPreferences>(_settingsKey).ConfigureAwait(true);
                ValidatePreferences(candidate);
                _preferences = candidate;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Only the explicit Reset selection action replaces unreadable player preferences.
            Error = "Content settings could not be read: " + exception.Message;
        }
    }

    /// <summary>Rejects catalog removals that would prevent gameplay or the repair screen from being constructed.</summary>
    /// <param name="catalog">The complete unpublished candidate.</param>
    private static void ValidateRequiredRows(ContentCatalog catalog)
    {
        foreach (var reference in new[]
        {
            new ContentReference("arenas", new ContentId("base:yard")),
            new ContentReference("enemies", new ContentId("base:walker")),
        })
        {
            if (!catalog.Contains(reference))
            {
                throw new InvalidOperationException($"Required sample content is missing: {reference}. Disable the removing package and restart.");
            }
        }
    }

    /// <summary>Returns the dependency-constrained order for the player's saved next-start selection.</summary>
    internal string DescribeRequestedOrder()
    {
        try
        {
            return string.Join(" > ", Resolve(_preferences.Profile).Packages.Select(static pack => pack.Manifest.Id));
        }
        catch (Exception exception) { return exception.Message; }
    }

    /// <summary>Shows which package supplies the final field after applying all patches.</summary>
    internal string DescribeWalkerSpeed()
    {
        if (!IsReady)
            return "Content is unavailable until the next successful start.";
        var enemy = Catalog.GetTable<DodgeEnemyDefinition>("enemies").Get(new ContentId("base:walker"));
        var source = Catalog.GetFieldSource(new ContentReference("enemies", new ContentId("base:walker")), "speedMin");
        return string.Create(CultureInfo.InvariantCulture, $"Walker speed {enemy.SpeedMin:0}-{enemy.SpeedMax:0}; source: {source?.PackId ?? "base"}");
    }

    /// <summary>Persists an enable choice for the next process without mutating the active session.</summary>
    internal Task ToggleAsync(string id, CancellationToken cancellationToken) => ChangeProfileAsync(profile =>
    {
        var enabled = profile.EnabledPackages.ToList();
        if (!enabled.Remove(id))
            enabled.Add(id);
        return new ContentPackProfile { EnabledPackages = enabled, LoadOrder = [.. PreferredIds(profile)] };
    }, cancellationToken);

    /// <summary>Persists player priority; the resolver still constrains it by dependencies.</summary>
    internal Task MoveAsync(string id, int direction, CancellationToken cancellationToken) => ChangeProfileAsync(profile =>
    {
        var order = PreferredIds(profile).ToList();
        var index = order.IndexOf(id);
        var destination = Math.Clamp(index + direction, 0, order.Count - 1);
        if (index >= 0 && index != destination)
        {
            order.RemoveAt(index);
            order.Insert(destination, id);
        }
        return new ContentPackProfile { EnabledPackages = profile.EnabledPackages, LoadOrder = order };
    }, cancellationToken);

    /// <summary>Allows the management shell to replace a broken profile with an explicit base-only choice.</summary>
    internal Task ResetAsync(CancellationToken cancellationToken) => ChangeProfileAsync(static _ => new ContentPackProfile(), cancellationToken);

    /// <summary>Selects another already-active arena; package activation remains a restart-only operation.</summary>
    internal async Task CycleArenaAsync(CancellationToken cancellationToken)
    {
        var arenas = Catalog.GetTable<DodgeArenaDefinition>("arenas").All().OrderBy(static arena => arena.Id, StringComparer.Ordinal).ToArray();
        var index = Array.FindIndex(arenas, arena => string.Equals(arena.Id, _preferences.ArenaId, StringComparison.Ordinal));
        var next = arenas[(index + 1) % arenas.Length].Id;
        var preferences = new DodgeContentPreferences { Profile = _preferences.Profile, ArenaId = next, CodeApprovals = _preferences.CodeApprovals };
        cancellationToken.ThrowIfCancellationRequested();
        await _storage.WriteAsync(_settingsKey, preferences).ConfigureAwait(true);
        _preferences = preferences;
    }

    /// <summary>Gets a prepared, owned image referenced by game content.</summary>
    internal Texture2D? Texture(string reference) => string.IsNullOrEmpty(reference) ? null : _textures[reference];
    /// <summary>Gets an optional prepared stream referenced by arena data.</summary>
    internal AudioStream? Music(string reference) => string.IsNullOrEmpty(reference) ? null : _audio[reference];
    /// <summary>Creates an official decorative scene; only admitted trusted PCK paths reach ResourceLoader.</summary>
    internal Node? CreateOfficialScene(string reference) => string.IsNullOrEmpty(reference) ? null : _scenes[reference].Instantiate();

    /// <summary>Releases native media after gameplay nodes have stopped and left the tree.</summary>
    public void Dispose()
    {
        foreach (var texture in _textures.Values)
            texture.Dispose();
        foreach (var stream in _audio.Values)
            stream.Dispose();
        foreach (var scene in _scenes.Values)
            scene.Dispose();
        _selectionGate.Dispose();
    }

    private static string SelectionKey(ContentPackProfile profile) =>
        string.Join(',', profile.EnabledPackages.Order(StringComparer.Ordinal)) + "|" + string.Join(',', profile.LoadOrder);

    private IEnumerable<string> PreferredIds(ContentPackProfile profile) => profile.LoadOrder
        .Concat(_packages.Select(static pack => pack.Manifest.Id).Where(static id => !string.Equals(id, "base", StringComparison.Ordinal)))
        .Distinct(StringComparer.Ordinal);

    private ContentLoadPlan Resolve(ContentPackProfile profile) => ContentPackResolver.Resolve(_packages, profile,
        new ContentHostCompatibility { GameId = "dodge-the-creeps", GameVersion = "1.0.0", ContentApiVersion = "1.0.0", CodeApiVersion = "1.0.0", BasePackId = "base" });

    private async Task ChangeProfileAsync(Func<ContentPackProfile, ContentPackProfile> change, CancellationToken cancellationToken)
    {
        await _selectionGate.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            var profile = change(_preferences.Profile);
            // A broken selection must remain editable one package at a time. Its effective
            // order/error is shown by management, while this process keeps its original session.
            var next = new DodgeContentPreferences { Profile = profile, ArenaId = _preferences.ArenaId, CodeApprovals = _preferences.CodeApprovals };
            await _storage.WriteAsync(_settingsKey, next).ConfigureAwait(true);
            _preferences = next;
        }
        finally { _selectionGate.Release(); }
    }

    private async Task DiscoverDirectoryAsync(string directory, CancellationToken cancellationToken)
    {
        try
        {
            _packages.AddRange(await new ContentPackDiscovery().DiscoverAsync([directory], cancellationToken).ConfigureAwait(true));
        }
        catch (Exception exception) when (exception is ContentLoadException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            _discoveryErrors.Add(Path.GetFileName(directory) + ": " + exception.Message);
        }
    }

    private static void ValidatePreferences(DodgeContentPreferences preferences)
    {
        if (preferences.Profile is null || preferences.Profile.EnabledPackages is null || preferences.Profile.LoadOrder is null ||
            preferences.Profile.EnabledPackages.Any(static id => !ContentId.IsCanonicalSegment(id)) ||
            preferences.Profile.LoadOrder.Any(static id => !ContentId.IsCanonicalSegment(id)) || preferences.CodeApprovals is null ||
            preferences.CodeApprovals.Any(static stamp => stamp is null || !ContentId.IsCanonicalSegment(stamp.Id) || string.IsNullOrWhiteSpace(stamp.Fingerprint)))
            throw new InvalidOperationException("Content settings require valid activation and order lists.");
        _ = new ContentId(preferences.ArenaId);
    }

    private void PrepareResources(ContentCatalog catalog)
    {
        foreach (var enemy in catalog.GetTable<DodgeEnemyDefinition>("enemies").All())
        {
            if (enemy.SpeedMax < enemy.SpeedMin)
                throw new InvalidOperationException("Enemy speed range is invalid: " + enemy.Id);
            if (enemy.Texture.Length > 0 && !_textures.ContainsKey(enemy.Texture))
            {
                var (pack, path) = ResolveMediaReference(enemy.Texture);
                _textures.Add(enemy.Texture, new GodotLooseContentAssets(pack.RootPath).LoadTexture(path));
            }
        }
        foreach (var arena in catalog.GetTable<DodgeArenaDefinition>("arenas").All())
        {
            if (arena.Music.Length > 0 && !_audio.ContainsKey(arena.Music))
            {
                var (pack, path) = ResolveMediaReference(arena.Music);
                _audio.Add(arena.Music, new GodotLooseContentAssets(pack.RootPath).LoadAudio(path));
            }
            if (arena.OfficialScene.Length > 0 && !_scenes.ContainsKey(arena.OfficialScene))
            {
                var (pack, path) = ResolveMediaReference(arena.OfficialScene);
                var resourcePath = _native.GetResourcePath(pack.Manifest.Id, path);
                if (!string.Equals(Path.GetExtension(path), ".tscn", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Official arena scenes must use .tscn: " + arena.Id);
                _scenes.Add(arena.OfficialScene, GD.Load<PackedScene>(resourcePath)
                    ?? throw new InvalidOperationException("Official arena scene could not be loaded: " + arena.Id));
            }
        }
    }

    private (ContentPack Pack, string Path) ResolveMediaReference(string reference)
    {
        var separator = reference.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0 || separator == reference.Length - 1)
            throw new InvalidOperationException("Media references require packageId:relative/path.");
        var id = reference[..separator];
        // Only enabled packages may supply media, even if disabled directories are present on disk.
        var pack = Resolve(_preferences.Profile).Packages.SingleOrDefault(pack => string.Equals(pack.Manifest.Id, id, StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Media package is not enabled: " + id);
        return (pack, reference[(separator + 1)..]);
    }

    private void SeedOptionalExamples()
    {
        Directory.CreateDirectory(InstalledDirectory);
        foreach (var id in new[] { "moon", "coral", "slow", "fast", "code-bonus", "code-double" })
        {
            // A not-yet-built code example must remain installable later; successful installs alone get a marker.
            var marker = Path.Combine(_physicalRoot, "example-installed-" + id);
            if (File.Exists(marker))
                continue;
            if (id.StartsWith("code-", StringComparison.Ordinal) &&
                !DirAccess.GetFilesAt("res://content_sources/" + id).Any(static file => file.EndsWith(".dll.source", StringComparison.Ordinal)))
                continue;
            var destination = Path.Combine(InstalledDirectory, id);
            if (!Directory.Exists(destination))
                CopyShippedDirectory("res://content_sources/" + id, destination);
            File.WriteAllText(marker, "1");
        }
    }

    private static void CopyShippedDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in DirAccess.GetFilesAt(source))
        {
            // Imported editor metadata is not part of the portable loose-content format.
            if (file.EndsWith(".import", StringComparison.Ordinal))
                continue;
            var bytes = FileAccess.GetFileAsBytes(source + "/" + file);
            if (FileAccess.GetOpenError() != Godot.Error.Ok)
                throw new IOException("Cannot read shipped content: " + source + "/" + file);
            // The sample ships raw media with a .source suffix so exports retain original
            // bytes rather than replacing them with editor-imported Godot resources.
            var targetName = file.EndsWith(".source", StringComparison.Ordinal) ? file[..^".source".Length] : file;
            File.WriteAllBytes(Path.Combine(destination, targetName), bytes);
        }
        foreach (var directory in DirAccess.GetDirectoriesAt(source))
            CopyShippedDirectory(source + "/" + directory, Path.Combine(destination, directory));
    }
}
