// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Pause;
using GFramework.Game.Abstractions.UI;
using Godot;

namespace DodgeTheCreeps.Advanced;

/// <summary>Native acceptance uses real nodes, button signals and input, with an isolated persistence namespace.</summary>
public partial class GameApplication
{
    private bool _smokeMode;
    private bool _smokeContinue;
    private string _smokeId = string.Empty;
    private string? _capturePath;
    private string _captureKind = "menu";
    private string? _contentSmokePhase;
    private string? _featureSmoke;
    private bool _featureContinuation;
    private string? _captureLanguage;

    /// <summary>Restricts acceptance data to one validated GUID rather than accessing normal player data.</summary>
    private void ConfigureSmoke(string[] arguments)
    {
        foreach (var argument in arguments)
        {
            if (TryConfigureFeatureSmoke(argument) || TryConfigureCapture(argument))
                continue;
            if (string.Equals(argument, "--content-smoke", StringComparison.Ordinal))
            {
                _smokeMode = true;
                _smokeId = _launch.InstanceId;
                _contentSmokePhase = "seed";
            }
            else if (argument.StartsWith("--content-smoke-continue=", StringComparison.Ordinal))
            {
                var continuationParts = argument["--content-smoke-continue=".Length..].Split(':');
                if (continuationParts.Length != 2 || !Guid.TryParseExact(continuationParts[0], "N", out _) ||
                    continuationParts[1] is not ("active" or "recover" or "broken" or "profile" or "done"))
                    throw new ArgumentException("Content continuation requires a GUID and active/recover/broken/profile/done phase.", nameof(arguments));
                _smokeMode = true;
                _smokeId = continuationParts[0];
                _contentSmokePhase = continuationParts[1];
            }
            else if (string.Equals(argument, "--smoke", StringComparison.Ordinal))
            {
                _smokeMode = true;
                _smokeId = _launch.InstanceId;
            }
            else if (argument.StartsWith("--smoke-continue=", StringComparison.Ordinal))
            {
                _smokeMode = true;
                _smokeContinue = true;
                _smokeId = argument[17..];
                if (!Guid.TryParseExact(_smokeId, "N", out _))
                    throw new ArgumentException("Smoke identifier must be a 32-digit GUID.", nameof(arguments));
            }
        }
        if (_smokeMode)
            _dataRoot = "user://dodge_advanced_smoke/" + _smokeId;
        else if (_capturePath is not null)
            _dataRoot = "user://dodge_advanced_capture/" + _launch.InstanceId;
    }

    /// <summary>Selects a supported rendered view independently of the native smoke scenario.</summary>
    private bool TryConfigureCapture(string argument)
    {
        const string languagePrefix = "--capture-language=";
        if (argument.StartsWith(languagePrefix, StringComparison.Ordinal))
        {
            _captureLanguage = argument[languagePrefix.Length..];
            return true;
        }
        foreach (var view in new[] { "menu", "content", "recovery", "settings", "game" })
        {
            var prefix = "--capture-" + view + "=";
            if (!argument.StartsWith(prefix, StringComparison.Ordinal))
                continue;
            _capturePath = argument[prefix.Length..];
            _captureKind = view;
            return true;
        }
        return false;
    }

    /// <summary>Runs representative agent adoption scenarios in the actual engine.</summary>
    private async Task RunSmokeAsync()
    {
        try
        {
            if (await RunFeatureSmokeAsync().ConfigureAwait(true))
                return;
            if (_contentSmokePhase is not null)
            {
                await RunContentSmokeAsync().ConfigureAwait(true);
                return;
            }
            if (_smokeContinue)
            {
                Require(!await _saves.ExistsAsync(1, _lifetime.Token).ConfigureAwait(true) &&
                    ((Button)FindNative("Continue", "Button")).Disabled && !HasBackup,
                    "An ended run became loadable again after restarting.");
                Require(Preferences.BestScore >= 37 && Math.Abs(Preferences.Volume - 0.35f) < 0.01,
                    "Retiring the run removed persistent score or settings.");
                FinishSmoke(passed: true);
                return;
            }
            Click("Start");
            await UntilAsync(() => _world is { IsRunning: true } && HasView("hud")).ConfigureAwait(true);
            var before = _world!.PlayerPosition;
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Pressed = true });
            for (var frame = 0; frame < 15; frame++)
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Pressed = false });
            Require(_world.PlayerPosition.X > before.X, "Native keyboard movement failed.");
            Click("Pause");
            await UntilAsync(() => HasView("pause") && GetTree().Paused).ConfigureAwait(true);
            var frozen = _world.PlayerPosition;
            var score = _world.Score;
            await ToSignal(GetTree().CreateTimer(0.12), SceneTreeTimer.SignalName.Timeout);
            Require(_world.PlayerPosition == frozen && _world.Score == score, "Gameplay advanced during pause.");
            await CheckNestedSettingsAsync().ConfigureAwait(true);
            _world.RestoreRun(37, new Vector2(140, 500));
            Click("Save");
            await UntilAsync(() => !_dirty).ConfigureAwait(true);
            Click("Resume");
            await UntilAsync(() => !HasView("pause") && !GetTree().Paused).ConfigureAwait(true);
            await CheckConfirmationAndGameOverAsync().ConfigureAwait(true);
            GD.Print("DODGE_SMOKE_DATA " + _smokeId);
            FinishSmoke(passed: true);
        }
        catch (Exception exception)
        {
            GD.PrintErr("DODGE_SMOKE_FAILURE " + exception);
            FinishSmoke(passed: false);
        }
    }

    /// <summary>Parses bounded acceptance identities without permitting arbitrary player-data paths.</summary>
    private bool TryConfigureFeatureSmoke(string argument)
    {
        foreach (var feature in new[] { "movement", "persistence", "localization", "settings", "modding", "review" })
        {
            var initial = "--" + feature + "-smoke";
            var continuation = initial + "-continue=";
            if (!string.Equals(argument, initial, StringComparison.Ordinal) && !argument.StartsWith(continuation, StringComparison.Ordinal))
                continue;
            _featureSmoke = feature;
            _featureContinuation = argument.StartsWith(continuation, StringComparison.Ordinal);
            _smokeId = _featureContinuation ? argument[continuation.Length..] : _launch.InstanceId;
            if (!Guid.TryParseExact(_smokeId, "N", out _))
                throw new ArgumentException("Acceptance identity must be a GUID.", nameof(argument));
            _smokeMode = true;
            return true;
        }
        return false;
    }

    /// <summary>Runs one feature-specific acceptance entrypoint while retaining normal shutdown handling.</summary>
    private async Task<bool> RunFeatureSmokeAsync()
    {
        switch (_featureSmoke)
        {
            case "modding":
                await RunModdingSmokeAsync(_featureContinuation).ConfigureAwait(true);
                break;
            case "movement":
                await RunMovementSmokeAsync().ConfigureAwait(true);
                break;
            case "persistence":
                await RunPersistenceSmokeAsync(_featureContinuation).ConfigureAwait(true);
                break;
            case "localization":
                await RunLocalizationSmokeAsync(_featureContinuation).ConfigureAwait(true);
                break;
            case "settings":
                await RunSettingsSmokeAsync(_featureContinuation).ConfigureAwait(true);
                break;
            case "review":
                await RunHistoricalReviewSmokeAsync().ConfigureAwait(true);
                break;
            default:
                return false;
        }
        return true;
    }

    /// <summary>Exercises dialog results, saved-run restore and collision loss.</summary>
    private async Task CheckConfirmationAndGameOverAsync()
    {
        // Start a fresh unsaved run to exercise a result-bearing confirmation with real signals.
        await StartGameAsync(restore: false).ConfigureAwait(true);
        Click("Pause");
        await UntilAsync(() => HasView("pause")).ConfigureAwait(true);
        Click("MainMenu");
        await UntilAsync(() => HasView("confirm")).ConfigureAwait(true);
        Click("KeepPlaying");
        await UntilAsync(() => !HasView("confirm") && HasView("pause") && _actions.TrueForAll(static task => task.IsCompleted)).ConfigureAwait(true);
        Click("MainMenu");
        await UntilAsync(() => HasView("confirm")).ConfigureAwait(true);
        Click("Leave");
        await UntilAsync(() => HasView("menu") && !GetTree().Paused).ConfigureAwait(true);
        Click("Continue");
        await UntilAsync(() => _world is { IsRunning: true } && HasView("hud")).ConfigureAwait(true);
        Require(_world!.Score >= 37, "Explicit save did not restore the expected score.");
        _latestBackup = "backups/death-probe";
        await _storage.WriteAsync(_dataRoot + "/saves/slot_1/" + _latestBackup, _world.CaptureProgress()).ConfigureAwait(true);
        await _saveService.StoreBackupKeyAsync().ConfigureAwait(true);
        await ToSignal(GetTree().CreateTimer(1.8), SceneTreeTimer.SignalName.Timeout);
        foreach (var enemy in GetTree().GetNodesInGroup("dodge_creeps"))
            if (enemy is Node2D creep)
                creep.Position = _world.PlayerPosition;
        await UntilAsync(() => HasView("game-over")).ConfigureAwait(true);
        Require(!await _saves.ExistsAsync(1, _lifetime.Token).ConfigureAwait(true) && !HasBackup &&
            !await _storage.ExistsAsync(_dataRoot + "/last-content-backup.json").ConfigureAwait(true),
            "Death did not retire the run slot and its restore shortcut.");
        Click("MainMenu");
        await UntilAsync(() => HasView("menu")).ConfigureAwait(true);
        Require(((Button)FindNative("Continue", "Button")).Disabled, "An ended run can still be continued from the menu.");
    }

    /// <summary>Exercises nested pause ownership and persisted settings through native controls.</summary>
    private async Task CheckNestedSettingsAsync()
    {
        Click("Settings");
        await UntilAsync(() => HasView("settings") && _pause.GetPauseDepth(PauseGroup.Global) == 2).ConfigureAwait(true);
        var volume = (HSlider)FindNative("Volume", "HSlider");
        volume.Value = 0.35;
        Click("SaveSettings");
        await UntilAsync(() => Math.Abs(Preferences.Volume - 0.35f) < 0.01).ConfigureAwait(true);
        Click("Back");
        await UntilAsync(() => !HasView("settings") && _pause.GetPauseDepth(PauseGroup.Global) == 1).ConfigureAwait(true);
        Require(GetTree().Paused, "Closing nested settings released the lower menu's pause.");
    }

    /// <summary>Captures a rendered menu as a local acceptance artifact when requested.</summary>
    private async Task CaptureMenuAsync()
    {
        if (_capturePath is null)
            return;
        using var frozen = string.Equals(_captureKind, "game", StringComparison.Ordinal)
            ? _pause.PauseScope("Game presentation capture") : null;
        if (string.Equals(_captureKind, "content", StringComparison.Ordinal))
            await OpenContentAsync().ConfigureAwait(true);
        else if (string.Equals(_captureKind, "settings", StringComparison.Ordinal))
            await OpenSettingsAsync().ConfigureAwait(true);
        else if (string.Equals(_captureKind, "game", StringComparison.Ordinal))
        {
            await StartGameAsync(restore: false).ConfigureAwait(true);
            PressMovementDirection(new Vector2(1, -1));
            _world!._PhysicsProcess(0);
            ReleaseMovementActions();
        }
        else if (string.Equals(_captureKind, "recovery", StringComparison.Ordinal))
        {
            RunAction(() => _ui.ShowDialogAsync(DodgeRoutes.Recover,
                () => Text.Get("recovery_question") + "\n\n" +
                    Text.Get("issue_missing_pack", ("id", "moon"), ("version", "1.0.0")) + "\n" +
                    Text.Get("issue_missing_content", ("id", "moon:crater"), ("table", "arenas")) + "\n" +
                    Text.Get("issue_order") + "\n\n" +
                    Text.Get("issue_missing_pack", ("id", "coral"), ("version", "1.0.0")),
                _lifetime.Token));
            await UntilAsync(() => HasView("recover")).ConfigureAwait(true);
        }
        // Locale shaping, native output sizing and newly opened scroll containers settle
        // over frame boundaries; capture only after the complete layout has rendered.
        for (var frame = 0; frame < 3; frame++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        // The viewport texture is borrowed; GetImage creates a new caller-owned native image.
        // Dispose it before completing acceptance so shutdown does not depend on a later GC pass.
        using var image = GetViewport().GetTexture().GetImage();
        var error = image.SavePng(_capturePath);
        if (error != Error.Ok)
            throw new InvalidOperationException("Menu capture failed: " + error);
        GD.Print("DODGE_CAPTURE " + _capturePath);
        RequestQuit();
    }

    /// <summary>Finds a visible action, avoiding stale controls queued for deletion.</summary>
    private Node FindNative(string name, string type) => GetTree().Root.FindChildren(name, type, recursive: true, owned: false)
        .FirstOrDefault(node => node is CanvasItem item && item.IsVisibleInTree() && !item.IsQueuedForDeletion())
        ?? throw new InvalidOperationException("Visible native control not found: " + name);
    /// <summary>Emits an actual native button signal.</summary>
    private void Click(string name)
    {
        var button = (Button)FindNative(name, "Button");
        Require(!button.Disabled, "Attempted a disabled action: " + name);
        button.EmitSignal(BaseButton.SignalName.Pressed);
    }
    /// <summary>Checks committed UI state without accessing runtime internals.</summary>
    private bool HasView(string key) => _ui.Snapshot.Views.Any(view => string.Equals(view.RouteKey, key, StringComparison.Ordinal) && view.Visible);
    /// <summary>Waits a bounded number of real engine frames for a public observable condition.</summary>
    private async Task UntilAsync(Func<bool> condition)
    {
        for (var frame = 0; frame < 600; frame++)
        {
            if (condition())
                return;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        throw new TimeoutException("Native acceptance condition did not become true.");
    }
    /// <summary>Reports one assertion with its game operation context.</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
    /// <summary>Publishes a machine-readable result and performs normal asynchronous shutdown.</summary>
    private void FinishSmoke(bool passed)
    {
        GD.Print("DODGE_SMOKE_RESULT " + (passed ? "PASS" : "FAIL"));
        if (!passed)
            System.Environment.ExitCode = 1;
        RequestQuit();
    }
}
