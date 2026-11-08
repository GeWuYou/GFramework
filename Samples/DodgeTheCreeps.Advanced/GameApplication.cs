// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DodgeTheCreeps.Advanced.Gameplay;
using DodgeTheCreeps.Advanced.Content;
using GFramework.Core.Pause;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Data;
using GFramework.Game.Scene;
using GFramework.Game.UI;
using GFramework.Godot.Storage;
using Godot;

namespace DodgeTheCreeps.Advanced;

/// <summary>
/// Coordinates user-visible flow through explicit run, settings, save and content services.
/// Native node ownership stays with AdvancedGameRoot; this object has no container or global context.
/// </summary>
public partial class GameApplication : IDisposable
{
    private readonly AdvancedGameRoot _host;
    private readonly GameLaunchOptions _launch;
    private readonly GameLifetime _lifetime;
    private readonly List<Task> _actions = [];
    private readonly SemaphoreSlim _navigation = new(1, 1);
    private readonly PauseStackManager _pause;
    private readonly GodotFileStorage _storage;
    private readonly SaveService _saveService;
    private readonly SettingsService _settings;
    private readonly ModService _mods;
    private readonly ContentSelectionService _selection;
    private readonly GameSession _session;
    private readonly RunUseCases _runs;
    private readonly WorldState _worldState;
    private readonly SceneRouter _scenes;
    private readonly UiService _ui;
    private readonly GamePresentationState _menus;
    private readonly IGameDispatcher _gameDispatcher;
    private Task _startup = Task.CompletedTask;
    private Task _acceptance = Task.CompletedTask;
    private Task? _stopTask;
    private bool _stopping;
    private string _dataRoot;

    /// <summary>Creates a flow coordinator from grouped, bounded runtime and feature dependencies.</summary>
    internal GameApplication(AdvancedGameRoot host, GameLaunchOptions launch, GameLifetime lifetime,
        GameRuntimeServices runtime, GameFeatureServices features)
    {
        _host = host;
        _launch = launch;
        _lifetime = lifetime;
        _dataRoot = launch.DataRoot;
        _pause = runtime.Pause;
        _storage = runtime.Storage;
        _scenes = runtime.Scenes;
        _ui = runtime.Ui;
        _gameDispatcher = runtime.Dispatcher;
        _saveService = features.Saves;
        _settings = features.Settings;
        _mods = features.Mods;
        _selection = features.Selection;
        _session = features.Runs.Session;
        _runs = features.Runs.UseCases;
        _worldState = features.Runs.World;
        _content = features.Content;
        _menus = features.Presentation;
        Text = features.Presentation.Text;
        ConfigureSmoke(launch.Arguments);
    }

    private DodgeWorld? _world
    {
        get => _worldState.World; set => _worldState.World = value;
    }
    private string _arenaId
    {
        get => _worldState.ArenaId; set => _worldState.ArenaId = value;
    }
    private DodgeContent _contentInstance = null!;
    private DodgeContent _content
    {
        get => _contentInstance;
        set
        {
            _contentInstance = value;
            _selection.SetContent(value);
        }
    }
    private SaveRepository<DodgeProgress> _saves => _saveService.Repository;
    private string? _latestBackup
    {
        get => _saveService.LatestBackup; set => _saveService.LatestBackup = value;
    }
    private ModSaveSession _modState => _mods.State;
    private int ScoreModifierCount => _mods.RegistrationCount;
    private bool _dirty
    {
        get => _session.HasChanges;
        set
        {
            if (value)
                _session.MarkChanged();
            else
                _session.MarkClean();
        }
    }

    /// <summary>The host's last fully committed settings, never an editable menu draft.</summary>
    internal DodgePreferences Preferences => _settings.Preferences;
    /// <summary>The explicit native owner used for bounded language subscriptions.</summary>
    internal AdvancedGameRoot Host => _host;
    /// <summary>The application's localized text boundary.</summary>
    internal DodgeLocalization Text
    {
        get;
    }
    /// <summary>The UI service supplied by the composition root.</summary>
    internal IUiService Ui => _ui;
    /// <summary>The current board, used only by presentation and native acceptance.</summary>
    internal DodgeWorld? World => _world;
    /// <summary>The current validated content authority.</summary>
    internal DodgeContent Content => _content;
    /// <summary>Whether starting a board is allowed by the content session.</summary>
    internal bool CanStart => _content.IsReady;
    /// <summary>Whether backup recovery is permitted independently of a newly started retry.</summary>
    internal bool HasBackup => CanRestoreSave && _latestBackup is not null;
    /// <summary>Whether the persistent checkpoint is safe to continue, including after a failed death-save deletion.</summary>
    internal bool CanRestoreSave => !_session.IsRetired && !_session.IsCheckpointRetired;

    /// <summary>Initializes feature state after composition and content startup have completed.</summary>
    internal Task InitializeAsync()
    {
        _menus.RunEnded += OnRunEnded;
        _startup = StartAsync();
        return _startup;
    }

    /// <summary>Whether initial feature loading and the first menu completed successfully.</summary>
    internal bool IsStarted
    {
        get; private set;
    }

    /// <summary>Starts optional acceptance only from a fresh engine callback after architecture readiness.</summary>
    internal void BeginPostStartupActions()
    {
        if (_capturePath is not null)
            _acceptance = _gameDispatcher.InvokeAsync(() => ObserveAcceptanceAsync(CaptureMenuAsync));
        else if (_smokeMode)
            _acceptance = _gameDispatcher.InvokeAsync(() => ObserveAcceptanceAsync(RunSmokeAsync));
    }

    /// <summary>Observes isolated native acceptance separately from user actions so shutdown cannot await itself.</summary>
    private async Task ObserveAcceptanceAsync(Func<Task> run)
    {
        try
        {
            await run().ConfigureAwait(true);
        }
        catch (Exception error)
        {
            ReportError(error);
            System.Environment.ExitCode = 1;
            RequestQuit();
        }
    }

    /// <summary>Captures the ended board before queuing its retirement, preserving identity across later navigation.</summary>
    private void OnRunEnded(object? sender, EventArgs eventArgs)
    {
        var endedWorld = _world;
        if (endedWorld is not null)
            RunAction(() => GameOverAsync(endedWorld));
    }

    /// <summary>Loads persistent feature state on the Godot thread before publishing the first usable menu.</summary>
    private async Task StartAsync()
    {
        try
        {
            // #region tutorial-load-data
            await _saveService.InitializeAsync().ConfigureAwait(true);
            await _settings.LoadAsync().ConfigureAwait(true);
            if (_captureLanguage is not null)
                Text.SetLanguage(_captureLanguage);
            var canContinue = await _saves.ExistsAsync(1, _lifetime.Token).ConfigureAwait(true);
            await _ui.ReplaceAsync(DodgeRoutes.Menu, canContinue, _lifetime.Token).ConfigureAwait(true);
            if (!CanStart)
                _menus.SetStatus(() => Text.Get("content_start_failed", ("error", _content.Error ?? string.Empty)));
            // #endregion tutorial-load-data
            IsStarted = true;
        }
        catch (OperationCanceledException) when (_stopping) { }
        catch (Exception exception)
        {
            ReportError(exception);
            throw;
        }
    }

    // #region tutorial-actions
    /// <summary>Observes every native signal task and retains it for shutdown.</summary>
    /// <param name="action">Business operation called from a signal.</param>
    internal void RunAction(Func<Task> action)
    {
        if (!_stopping)
            _actions.Add(ObserveAsync(action));
    }

    /// <summary>Reports recoverable operation failures on the visible status line.</summary>
    private async Task ObserveAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (_stopping) { }
        catch (Exception exception) { ReportError(exception); }
        _actions.RemoveAll(static task => task.IsCompleted);
    }
    // #endregion tutorial-actions

    // #region tutorial-start-game
    /// <summary>Begins a fresh or restored run through the scene service.</summary>
    internal async Task StartGameAsync(bool restore)
    {
        await _navigation.WaitAsync(_lifetime.Token).ConfigureAwait(true);
        try
        {
            if (_stopping)
                return;
            if (!await _runs.StartAsync(restore, CanStart, _content.ArenaId, _lifetime.Token).ConfigureAwait(true))
                return;
            if (!_stopping)
                await _ui.ShowAsync(DodgeRoutes.Hud, _world!.Score, _lifetime.Token).ConfigureAwait(true);

        }
        finally { _navigation.Release(); }
    }

    /// <summary>Commits a board through the use case, then publishes success feedback.</summary>
    internal async Task SaveGameAsync()
    {
        if (_world is not { IsRunning: true })
            return;
        await _runs.SaveAsync(_lifetime.Token).ConfigureAwait(true);
        _menus.SetStatus(() => Text.Get("run_saved"));
    }

    /// <summary>Opens the settings draft on the Godot thread; shutdown cancels the UI request.</summary>
    /// <returns>Completion of settings-view activation, before any draft is accepted or persisted.</returns>
    internal Task OpenSettingsAsync() => _ui.ShowAsync(DodgeRoutes.Settings, args: true, _lifetime.Token);

    // #region tutorial-confirm-exit
    /// <summary>Confirms discarding unsaved gameplay with a typed dialog result.</summary>
    internal async Task ReturnToMenuAsync()
    {
        await _navigation.WaitAsync(_lifetime.Token).ConfigureAwait(true);
        try
        {
            if (_stopping)
                return;
            if (_session.NeedsDiscardConfirmation(_world is { IsRunning: true },
                    _world?.PlayerPosition.X ?? 0, _world?.PlayerPosition.Y ?? 0))
            {
                var decision = await _ui.ShowDialogAsync(DodgeRoutes.Confirm, () => Text.Get("leave_question"), _lifetime.Token).ConfigureAwait(true);
                if (!decision.Accepted || !decision.Value || _stopping)
                    return;
            }
            await _runs.LeaveAsync(_lifetime.Token).ConfigureAwait(true);
            await _ui.ReplaceAsync(DodgeRoutes.Menu, await _saves.ExistsAsync(1, _lifetime.Token).ConfigureAwait(true), _lifetime.Token).ConfigureAwait(true);

        }
        finally { _navigation.Release(); }
    }
    // #endregion tutorial-confirm-exit

    /// <summary>Moves a completed score to the game-over page and persists the best score.</summary>
    private async Task GameOverAsync(DodgeWorld endedWorld)
    {
        await _navigation.WaitAsync(_lifetime.Token).ConfigureAwait(true);
        try
        {
            if (_stopping || !ReferenceEquals(_world, endedWorld))
                return;
            if (_world is null)
                return;
            _dirty = false;
            var score = _world.Score;
            var status = await RetireRunSaveAsync().ConfigureAwait(true);
            if (score > Preferences.BestScore)
            {
                try
                {
                    await SaveBestScoreAsync(score).ConfigureAwait(true);
                }
                catch (Exception exception)
                {
                    // A failed best-score write must leave the game's retry and exit actions available.
                    GD.PushError(exception.ToString());
                    var scoreError = Text.Get("best_save_failed", ("error", exception.Message));
                    status = status is null ? scoreError : status + "\n" + scoreError;
                }
            }
            await ShowGameOverAsync(score, status).ConfigureAwait(true);

        }
        finally { _navigation.Release(); }
    }

    // #region tutorial-run-save-retirement
    /// <summary>Retires the single run slot and its restore shortcut before offering retry or menu navigation.</summary>
    private async Task<string?> RetireRunSaveAsync()
    {
        try
        {
            // Death is terminal. Shutdown waits for this cleanup instead of cancelling
            // its admission and leaving an ended run available on the next launch.
            await _runs.RetireAsync(global::System.Threading.CancellationToken.None).ConfigureAwait(true);
            return null;
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            return Text.Get("save_clear_failed", ("error", exception.Message));
        }
    }

    // #endregion tutorial-run-save-retirement

    /// <summary>Shows the completed run even when persisting its best score has failed.</summary>
    private async Task ShowGameOverAsync(int score, string? status)
    {
        await _ui.ClearAsync(_lifetime.Token).ConfigureAwait(true);
        await _ui.ReplaceAsync(DodgeRoutes.GameOver, score, _lifetime.Token).ConfigureAwait(true);
        if (status is not null)
            _menus.SetStatus(status);
    }

    /// <summary>Registers missing input actions without changing user-authored bindings.</summary>
    internal static void ConfigureInput()
    {
        AddAction("dodge_left", Key.A, Key.Left);
        AddAction("dodge_right", Key.D, Key.Right);
        AddAction("dodge_up", Key.W, Key.Up);
        AddAction("dodge_down", Key.S, Key.Down);
    }
    /// <summary>Initializes missing project actions, leaving user-authored actions intact.</summary>
    private static void AddAction(string name, params Key[] keys)
    {
        if (InputMap.HasAction(name))
            return;
        InputMap.AddAction(name);
        foreach (var key in keys)
            InputMap.ActionAddEvent(name, new InputEventKey { PhysicalKeycode = key });
    }
    /// <summary>Shows a concise error without hiding the current game.</summary>
    private void ReportError(Exception exception)
    {
        _menus.SetStatus(() => Text.Get("operation_failed", ("error", exception.Message)));
        GD.PushError(exception.ToString());
    }

    // #region tutorial-shutdown
    /// <summary>Stops UI admission, cancels waiting dialogs and joins accepted actions before freeing native roots.</summary>
    internal void RequestQuit() => _host.RequestQuit();

    /// <summary>Stops actions and native feature services once, retaining every error until all cleanup was attempted.</summary>
    internal Task StopAsync() => _stopTask ??= StopCoreAsync();

    /// <summary>Cancels admission, joins accepted work and attempts every native cleanup phase before reporting failures.</summary>
    private async Task StopCoreAsync()
    {
        if (_stopping)
            return;
        _stopping = true;
        var errors = new List<Exception>();
        try
        {
            _lifetime.Cancel();
        }
        catch (Exception exception) { errors.Add(exception); }
        _world?.StopRun();
        await CaptureStopAsync(_ui.StopAsync, errors).ConfigureAwait(true);
        await CaptureStopAsync(() => Task.WhenAll([.. _actions]), errors).ConfigureAwait(true);
        await CaptureStopAsync(() => _acceptance, errors).ConfigureAwait(true);
        if (!_startup.IsCompleted)
            await CaptureStopAsync(() => _startup, errors).ConfigureAwait(true);
        await CaptureStopAsync(_scenes.StopAsync, errors).ConfigureAwait(true);
        await CaptureStopAsync(_content.StopModsAsync, errors).ConfigureAwait(true);
        await CaptureStopAsync(() => _pause.DisposeAsync().AsTask(), errors).ConfigureAwait(true);
        _menus.RunEnded -= OnRunEnded;
        if (errors.Count > 0)
            throw new AggregateException("Game shutdown failed after cleanup.", errors);
    }

    /// <summary>Releases only this coordinator's subscriptions; injected services retain their own owners.</summary>
    public void Dispose()
    {
        _menus.RunEnded -= OnRunEnded;
        _navigation.Dispose();
    }

    // These small native helpers are restricted to the acceptance partials; application use cases use ports.
    private SceneTree GetTree() => _host.GetTree();
    private Window GetWindow() => _host.GetWindow();
    private Viewport GetViewport() => _host.GetViewport();
    private void AddChild(Node node) => _host.AddChild(node);
    private void RemoveChild(Node node) => _host.RemoveChild(node);
    private SignalAwaiter ToSignal(GodotObject source, StringName signal) => _host.ToSignal(source, signal);

    /// <summary>Collects one cleanup failure without skipping subsequent owners during shutdown.</summary>
    private static async Task CaptureStopAsync(Func<Task> stop, List<Exception> errors)
    {
        try
        {
            await stop().ConfigureAwait(true);
        }
        catch (Exception exception) { errors.Add(exception); }
    }

}
