// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DodgeTheCreeps.Gameplay;
using DodgeTheCreeps.Content;
using GFramework.Core.Pause;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Data;
using GFramework.Game.Runtime;
using GFramework.Game.Scene;
using GFramework.Game.UI;
using GFramework.Godot.Pause;
using GFramework.Godot.Scene;
using GFramework.Godot.Storage;
using GFramework.Godot.UI;
using Godot;
using JsonSerializer = GFramework.Game.Serializer.JsonSerializer;

namespace DodgeTheCreeps;

/// <summary>Composes ordinary framework services and native gameplay, observing asynchronous actions until shutdown.</summary>
public partial class DodgeApp : Node, ISceneRoot
{
    private readonly List<Task> _actions = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly PauseStackManager _pause = new();
    private GodotFileStorage _storage = null!;
    private SaveRepository<DodgeProgress> _saves = null!;
    private DodgeContent _content = null!;
    private string _arenaId = "base:yard";
    private string? _latestBackup;
    private SceneRouter _scenes = null!;
    private UiService _ui = null!;
    private DodgeMenus _menus = null!;
    private DodgeWorld? _world;
    private Label? _startupStatus;
    private Font? _startupFont;
    private Task _startup = Task.CompletedTask;
    private bool _stopping;
    private bool _dirty;
    private string _dataRoot = "user://dodge_the_creeps";

    /// <summary>Gets the currently loaded preferences.</summary>
    internal DodgePreferences Preferences { get; private set; } = new();
    /// <summary>Owns framework translations and live view bindings for the current native host.</summary>
    internal DodgeLocalization Text { get; } = new();
    /// <summary>Gets the service used by views and native smoke acceptance.</summary>
    internal IUiService Ui => _ui;
    /// <summary>Gets the active gameplay node.</summary>
    internal DodgeWorld? World => _world;
    /// <summary>Gets content selection and its immutable active session.</summary>
    internal DodgeContent Content => _content;
    /// <summary>Gets whether this process has a complete usable content catalog.</summary>
    internal bool CanStart => _content is { IsReady: true };
    /// <summary>Gets the most recent original save backup available to restore.</summary>
    internal bool HasBackup => _latestBackup is not null;

    /// <summary>Creates host roots before initializing storage and showing the first menu.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Text.Initialize();
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += RequestQuit;
        var startupCanvas = new CanvasLayer { Name = "StartupStatus", Layer = 100 };
        AddChild(startupCanvas);
        _startupStatus = new Label
        {
            Text = Text.Get("loading"), Position = new Vector2(20, 24), Size = new Vector2(440, 200),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _startupFont = DodgeLocalization.CreateChineseFont();
        _startupStatus.AddThemeFontOverride("font", _startupFont);
        startupCanvas.AddChild(_startupStatus);
        ConfigureInput();
        var arguments = OS.GetCmdlineUserArgs();
        ConfigureSmoke(arguments);
        // #region tutorial-services
        var root = new GodotUiRoot { Name = "Ui" };
        AddChild(root);
        var context = SynchronizationContext.Current ?? throw new InvalidOperationException("Godot synchronization context is unavailable.");
        var dispatcher = new SynchronizationContextGameDispatcher(context);
        _pause.RegisterHandler(new GodotPauseHandler(GetTree()));
        _scenes = new SceneRouter(new WorldFactory(this), this, dispatcher);
        var registry = new UiRegistry();
        _menus = new DodgeMenus(this);
        _menus.Register(registry);
        _ui = new UiService(registry, root, dispatcher, _pause);
        var input = new GodotUiInputBridge { Service = _ui, RunAction = RunAction };
        input.UnhandledCancelAsync = () => _world is { IsRunning: true }
            ? _ui.ShowAsync(DodgeRoutes.Pause, _world.Score, _lifetime.Token) : Task.CompletedTask;
        AddChild(input);
        _startup = StartAsync();
        // #endregion tutorial-services
    }

    /// <summary>Loads persistent data without overwriting unreadable files with defaults.</summary>
    private async Task StartAsync()
    {
        try
        {
            // #region tutorial-load-data
            _storage = new GodotFileStorage(new JsonSerializer());
            _content = new DodgeContent(_storage, _dataRoot);
            await _content.InitializeAsync(_lifetime.Token).ConfigureAwait(true);
            _saves = new SaveRepository<DodgeProgress>(_storage, new SaveConfiguration { SaveRoot = _dataRoot + "/saves" },
                CreateContentSaveOptions());
            _saves.RegisterMigration(new DodgeProgressMigration());
            if (await _storage.ExistsAsync(_dataRoot + "/last-content-backup.json").ConfigureAwait(true))
                _latestBackup = await _storage.ReadAsync<string>(_dataRoot + "/last-content-backup.json").ConfigureAwait(true);
            if (await _storage.ExistsAsync(_dataRoot + "/preferences.json").ConfigureAwait(true))
                Preferences = await _storage.ReadAsync<DodgePreferences>(_dataRoot + "/preferences.json").ConfigureAwait(true);
            ApplyPreferences();
            if (_captureLanguage is not null) Text.SetLanguage(_captureLanguage);
            var canContinue = await _saves.ExistsAsync(1, _lifetime.Token).ConfigureAwait(true);
            await _ui.ReplaceAsync(DodgeRoutes.Menu, canContinue, _lifetime.Token).ConfigureAwait(true);
            if (!CanStart) _menus.SetStatus(() => Text.Get("content_start_failed", ("error", _content.Error ?? string.Empty)));
            _startupStatus!.GetParent().QueueFree();
            _startupStatus = null;
            // #endregion tutorial-load-data
            if (_capturePath is not null)
            {
                await CaptureMenuAsync().ConfigureAwait(true);
                return;
            }
            if (_smokeMode) await RunSmokeAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (_stopping) { }
        catch (Exception exception)
        {
            ReportError(exception);
            if (_smokeMode) FinishSmoke(false);
        }
    }

    // #region tutorial-actions
    /// <summary>Observes every native signal task and retains it for shutdown.</summary>
    /// <param name="action">Business operation called from a signal.</param>
    internal void RunAction(Func<Task> action)
    {
        if (!_stopping) _actions.Add(ObserveAsync(action));
    }

    /// <summary>Reports recoverable operation failures on the visible status line.</summary>
    private async Task ObserveAsync(Func<Task> action)
    {
        try { await action().ConfigureAwait(true); }
        catch (OperationCanceledException) when (_stopping) { }
        catch (Exception exception) { ReportError(exception); }
        _actions.RemoveAll(static task => task.IsCompleted);
    }
    // #endregion tutorial-actions

    // #region tutorial-start-game
    /// <summary>Begins a fresh or restored run through the scene service.</summary>
    internal async Task StartGameAsync(bool restore)
    {
        if (!CanStart) throw new InvalidOperationException("Repair content selection and restart before playing.");
        var saved = restore ? await LoadContentSaveAsync().ConfigureAwait(true) : null;
        if (restore && saved is null) return;
        _arenaId = saved?.ArenaId ?? _content.ArenaId;
        _world?.StopRun();
        await _ui.ClearAsync(_lifetime.Token).ConfigureAwait(true);
        await _scenes.ReplaceAsync("play", cancellationToken: _lifetime.Token).ConfigureAwait(true);
        if (saved is null) _world!.StartRun();
        else _world!.RestoreProgress(saved);
        _dirty = true;
        await _ui.ShowAsync(DodgeRoutes.Hud, _world!.Score, _lifetime.Token).ConfigureAwait(true);
        if (saved?.LegacyLayoutReset == true)
            _menus.SetStatus(() => Text.Get("legacy_layout_reset"));
    }
    // #endregion tutorial-start-game

    // #region tutorial-save-game
    /// <summary>Writes one explicit save slot, preserving the previous file on failure.</summary>
    internal async Task SaveGameAsync()
    {
        if (_world is null) return;
        await _saves.SaveAsync(1, _world.CaptureProgress(), _lifetime.Token).ConfigureAwait(true);
        _dirty = false;
        _menus.SetStatus(() => Text.Get("run_saved"));
    }
    // #endregion tutorial-save-game

    /// <summary>Opens settings as an independent window with its own pause request.</summary>
    internal Task OpenSettingsAsync() => _ui.ShowAsync(DodgeRoutes.Settings, true, _lifetime.Token);

    // #region tutorial-confirm-exit
    /// <summary>Confirms discarding unsaved gameplay with a typed dialog result.</summary>
    internal async Task ReturnToMenuAsync()
    {
        if (_dirty && _world is { IsRunning: true })
        {
            var decision = await _ui.ShowDialogAsync(DodgeRoutes.Confirm, () => Text.Get("leave_question"), _lifetime.Token).ConfigureAwait(true);
            if (!decision.Accepted || decision.Value != true || _stopping) return;
        }
        _dirty = false;
        _world?.StopRun();
        await _ui.ClearAsync(_lifetime.Token).ConfigureAwait(true);
        await _scenes.ClearAsync(_lifetime.Token).ConfigureAwait(true);
        _world = null;
        await _ui.ReplaceAsync(DodgeRoutes.Menu, await _saves.ExistsAsync(1, _lifetime.Token).ConfigureAwait(true), _lifetime.Token).ConfigureAwait(true);
    }
    // #endregion tutorial-confirm-exit

    /// <summary>Moves a completed score to the game-over page and persists the best score.</summary>
    private async Task GameOverAsync()
    {
        if (_world is null) return;
        _dirty = false;
        var score = _world.Score;
        if (score > Preferences.BestScore)
        {
            try { await SaveBestScoreAsync(score).ConfigureAwait(true); }
            catch (Exception exception)
            {
                // A failed best-score write must leave the game's retry and exit actions available.
                GD.PushError(exception.ToString());
                await ShowGameOverAsync(score, Text.Get("best_save_failed", ("error", exception.Message))).ConfigureAwait(true);
                return;
            }
        }
        await ShowGameOverAsync(score, null).ConfigureAwait(true);
    }

    /// <summary>Shows the completed run even when persisting its best score has failed.</summary>
    private async Task ShowGameOverAsync(int score, string? status)
    {
        await _ui.ClearAsync(_lifetime.Token).ConfigureAwait(true);
        await _ui.ReplaceAsync(DodgeRoutes.GameOver, score, _lifetime.Token).ConfigureAwait(true);
        if (status is not null) _menus.SetStatus(status);
    }

    /// <summary>Attaches a prepared scene on the host thread.</summary>
    public void AddScene(ISceneBehavior scene) => AddChild((Node)scene.Original);
    /// <summary>Detaches a scene while its behavior owns queued native release.</summary>
    public void RemoveScene(ISceneBehavior scene)
    {
        var node = (Node)scene.Original;
        if (GodotObject.IsInstanceValid(node) && node.GetParent() == this) RemoveChild(node);
    }

    /// <summary>Registers gameplay controls separately from native UI navigation actions.</summary>
    private static void ConfigureInput()
    {
        AddAction("dodge_left", Key.A, Key.Left);
        AddAction("dodge_right", Key.D, Key.Right);
        AddAction("dodge_up", Key.W, Key.Up);
        AddAction("dodge_down", Key.S, Key.Down);
    }
    /// <summary>Initializes missing project actions, leaving user-authored actions intact.</summary>
    private static void AddAction(string name, params Key[] keys)
    {
        if (InputMap.HasAction(name)) return;
        InputMap.AddAction(name);
        foreach (var key in keys) InputMap.ActionAddEvent(name, new InputEventKey { PhysicalKeycode = key });
    }
    /// <summary>Shows a concise error without hiding the current game.</summary>
    private void ReportError(Exception exception)
    {
        _menus.SetStatus(() => Text.Get("operation_failed", ("error", exception.Message)));
        if (_startupStatus is not null && GodotObject.IsInstanceValid(_startupStatus))
            _startupStatus.Text = Text.Get("data_load_failed", ("error", exception.Message));
        GD.PushError(exception.ToString());
    }

    // #region tutorial-shutdown
    /// <summary>Stops UI admission, cancels waiting dialogs and joins accepted actions before freeing native roots.</summary>
    internal async void RequestQuit()
    {
        if (_stopping) return;
        _stopping = true;
        var exit = System.Environment.ExitCode;
        var errors = new List<Exception>();
        try
        {
            try { _lifetime.Cancel(); }
            catch (Exception exception) { errors.Add(exception); }
            _world?.StopRun();
            await CaptureStopAsync(_ui.StopAsync, errors).ConfigureAwait(true);
            await CaptureStopAsync(() => Task.WhenAll(_actions.ToArray()), errors).ConfigureAwait(true);
            await CaptureStopAsync(() => _startup, errors).ConfigureAwait(true);
            await CaptureStopAsync(_scenes.StopAsync, errors).ConfigureAwait(true);
            await CaptureStopAsync(() => _pause.DisposeAsync().AsTask(), errors).ConfigureAwait(true);
            // QueueFree is frame-deferred. Let owned views leave the tree before disposing their shared theme.
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            // Native audio playback retires on the mixer thread, independently of fast headless process frames.
            var audioDrain = Math.Clamp(AudioServer.GetTimeToNextMix() * 2, 0.12, 0.5);
            await ToSignal(GetTree().CreateTimer(audioDrain, true, false, true), SceneTreeTimer.SignalName.Timeout);
        }
        catch (Exception exception) { errors.Add(exception); }
        finally
        {
            DisposeOwnedResource(_menus, errors);
            DisposeOwnedResource(_startupFont, errors);
            DisposeOwnedResource(Text, errors);
            DisposeOwnedResource(_content, errors);
            DisposeOwnedResource(_storage, errors);
            DisposeOwnedResource(_preferencesGate, errors);
            DisposeOwnedResource(_lifetime, errors);
            if (errors.Count > 0)
            {
                GD.PushError(new AggregateException("Game shutdown failed after cleanup.", errors).ToString());
                exit = 1;
            }
            GetTree().Quit(exit);
        }
    }

    /// <summary>Attempts every owned stop action even when an earlier callback reports an error.</summary>
    private static async Task CaptureStopAsync(Func<Task> stop, List<Exception> errors)
    {
        try { await stop().ConfigureAwait(true); }
        catch (Exception exception) { errors.Add(exception); }
    }

    /// <summary>Releases one owned resource without skipping the remaining resources.</summary>
    private static void DisposeOwnedResource(IDisposable? resource, List<Exception> errors)
    {
        try { resource?.Dispose(); }
        catch (Exception exception) { errors.Add(exception); }
    }
    // #endregion tutorial-shutdown

    /// <summary>Creates each gameplay instance without architecture or generated context access.</summary>
    private sealed class WorldFactory(DodgeApp app) : ISceneFactory
    {
        /// <inheritdoc />
        public ISceneBehavior Create(string sceneKey)
        {
            if (!string.Equals(sceneKey, "play", StringComparison.Ordinal)) throw new InvalidOperationException("Unknown scene: " + sceneKey);
            app._world = new DodgeWorld { Name = "World" };
            app._world.ConfigureContent(app._content, app._content.Catalog.GetTable<DodgeArenaDefinition>("arenas").Get(new ContentId(app._arenaId)));
            app._world.ScoreChanged += (_, score) => { app._menus.SetScore(score.Score); app._dirty = true; };
            app._world.RunEnded += (_, _) => app.RunAction(app.GameOverAsync);
            return SceneBehaviorFactory.Create(app._world, sceneKey);
        }
    }
}
