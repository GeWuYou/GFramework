// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DodgeTheCreeps.Gameplay;
using GFramework.Core.Pause;
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
    private SceneRouter _scenes = null!;
    private UiService _ui = null!;
    private DodgeMenus _menus = null!;
    private DodgeWorld? _world;
    private Label? _startupStatus;
    private Task _startup = Task.CompletedTask;
    private bool _stopping;
    private bool _dirty;
    private string _dataRoot = "user://dodge_the_creeps";

    /// <summary>Gets the currently loaded preferences.</summary>
    internal DodgePreferences Preferences { get; private set; } = new();
    /// <summary>Gets the service used by views and native smoke acceptance.</summary>
    internal IUiService Ui => _ui;
    /// <summary>Gets the active gameplay node.</summary>
    internal DodgeWorld? World => _world;

    /// <summary>Creates host roots before initializing storage and showing the first menu.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += RequestQuit;
        var startupCanvas = new CanvasLayer { Name = "StartupStatus", Layer = 100 };
        AddChild(startupCanvas);
        _startupStatus = new Label
        {
            Text = "Loading...", Position = new Vector2(20, 24), Size = new Vector2(440, 200),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
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
            _saves = new SaveRepository<DodgeProgress>(_storage, new SaveConfiguration { SaveRoot = _dataRoot + "/saves" });
            if (await _storage.ExistsAsync(_dataRoot + "/preferences.json").ConfigureAwait(true))
                Preferences = await _storage.ReadAsync<DodgePreferences>(_dataRoot + "/preferences.json").ConfigureAwait(true);
            if (!float.IsFinite(Preferences.Volume) || Preferences.Volume is < 0 or > 1 || Preferences.BestScore < 0)
                throw new InvalidOperationException("The preferences file contains invalid values.");
            ApplyPreferences();
            var canContinue = await _saves.ExistsAsync(1, _lifetime.Token).ConfigureAwait(true);
            await _ui.ReplaceAsync(DodgeRoutes.Menu, canContinue, _lifetime.Token).ConfigureAwait(true);
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
        var saved = restore ? await _saves.LoadAsync(1, _lifetime.Token).ConfigureAwait(true) : null;
        _world?.StopRun();
        await _ui.ClearAsync(_lifetime.Token).ConfigureAwait(true);
        await _scenes.ReplaceAsync("play", cancellationToken: _lifetime.Token).ConfigureAwait(true);
        if (saved is null) _world!.StartRun();
        else _world!.RestoreRun(saved.Score, new Vector2(saved.X, saved.Y));
        _dirty = true;
        await _ui.ShowAsync(DodgeRoutes.Hud, _world!.Score, _lifetime.Token).ConfigureAwait(true);
    }
    // #endregion tutorial-start-game

    // #region tutorial-save-game
    /// <summary>Writes one explicit save slot, preserving the previous file on failure.</summary>
    internal async Task SaveGameAsync()
    {
        if (_world is null) return;
        var position = _world.PlayerPosition;
        await _saves.SaveAsync(1, new DodgeProgress { Score = _world.Score, X = position.X, Y = position.Y }, _lifetime.Token).ConfigureAwait(true);
        _dirty = false;
        _menus.SetStatus("Run saved. Continue restores your score and position.");
    }
    // #endregion tutorial-save-game

    /// <summary>Opens settings as an independent window with its own pause request.</summary>
    internal Task OpenSettingsAsync() => _ui.ShowAsync(DodgeRoutes.Settings, true, _lifetime.Token);

    // #region tutorial-save-settings
    /// <summary>Persists preferences through the same atomic native storage implementation.</summary>
    internal async Task SavePreferencesAsync(float volume, bool fullscreen)
    {
        var next = new DodgePreferences { Volume = volume, Fullscreen = fullscreen, BestScore = Preferences.BestScore };
        await _storage.WriteAsync(_dataRoot + "/preferences.json", next).ConfigureAwait(true);
        Preferences = next;
        ApplyPreferences();
        _menus.SetStatus("Settings saved.");
    }
    // #endregion tutorial-save-settings

    // #region tutorial-confirm-exit
    /// <summary>Confirms discarding unsaved gameplay with a typed dialog result.</summary>
    internal async Task ReturnToMenuAsync()
    {
        if (_dirty && _world is { IsRunning: true })
        {
            var decision = await _ui.ShowDialogAsync(DodgeRoutes.Confirm, "Leave this run without saving?", _lifetime.Token).ConfigureAwait(true);
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
            Preferences.BestScore = score;
            try { await _storage.WriteAsync(_dataRoot + "/preferences.json", Preferences).ConfigureAwait(true); }
            catch (Exception exception)
            {
                // A failed best-score write must leave the game's retry and exit actions available.
                GD.PushError(exception.ToString());
                await ShowGameOverAsync(score, "Best score could not be saved.").ConfigureAwait(true);
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
    /// <summary>Applies native preferences only after the storage operation has succeeded.</summary>
    private void ApplyPreferences()
    {
        AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(Math.Max(Preferences.Volume, 0.0001f)));
        if (!DisplayServer.GetName().Equals("headless", StringComparison.Ordinal))
            DisplayServer.WindowSetMode(Preferences.Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
    }
    /// <summary>Shows a concise error without hiding the current game.</summary>
    private void ReportError(Exception exception)
    {
        _menus.SetStatus("Operation failed: " + exception.Message);
        if (_startupStatus is not null && GodotObject.IsInstanceValid(_startupStatus))
            _startupStatus.Text = "Could not load game data.\n" + exception.Message;
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
            DisposeOwnedResource(_storage, errors);
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
            app._world.ScoreChanged += (_, score) => { app._menus.SetScore(score.Score); app._dirty = true; };
            app._world.RunEnded += (_, _) => app.RunAction(app.GameOverAsync);
            return SceneBehaviorFactory.Create(app._world, sceneKey);
        }
    }
}
