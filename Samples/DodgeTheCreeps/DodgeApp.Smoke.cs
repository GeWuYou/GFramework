// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Pause;
using GFramework.Game.Abstractions.UI;
using Godot;

namespace DodgeTheCreeps;

/// <summary>Native acceptance uses real nodes, button signals and input, with an isolated persistence namespace.</summary>
public partial class DodgeApp
{
    private bool _smokeMode;
    private bool _smokeContinue;
    private string _smokeId = string.Empty;
    private string? _capturePath;

    /// <summary>Restricts acceptance data to one validated GUID rather than accessing normal player data.</summary>
    private void ConfigureSmoke(string[] arguments)
    {
        foreach (var argument in arguments)
        {
            if (string.Equals(argument, "--smoke", StringComparison.Ordinal))
            {
                _smokeMode = true;
                _smokeId = Guid.NewGuid().ToString("N");
            }
            else if (argument.StartsWith("--smoke-continue=", StringComparison.Ordinal))
            {
                _smokeMode = true;
                _smokeContinue = true;
                _smokeId = argument[17..];
                if (!Guid.TryParseExact(_smokeId, "N", out _)) throw new ArgumentException("Smoke identifier must be a 32-digit GUID.", nameof(arguments));
            }
            else if (argument.StartsWith("--capture-menu=", StringComparison.Ordinal)) _capturePath = argument[15..];
        }
        if (_smokeMode) _dataRoot = "user://dodge_smoke/" + _smokeId;
    }

    /// <summary>Runs representative agent adoption scenarios in the actual engine.</summary>
    private async Task RunSmokeAsync()
    {
        try
        {
            if (_smokeContinue)
            {
                Click("Continue");
                await UntilAsync(() => _world is { IsRunning: true } && HasView("hud")).ConfigureAwait(true);
                Require(_world!.Score >= 37 && Math.Abs(Preferences.Volume - 0.35f) < 0.01, "Cross-process run/preferences restore failed.");
                FinishSmoke(true);
                return;
            }
            Click("Start");
            await UntilAsync(() => _world is { IsRunning: true } && HasView("hud")).ConfigureAwait(true);
            var before = _world!.PlayerPosition;
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Pressed = true });
            for (var frame = 0; frame < 15; frame++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
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
            FinishSmoke(true);
        }
        catch (Exception exception)
        {
            GD.PrintErr("DODGE_SMOKE_FAILURE " + exception);
            FinishSmoke(false);
        }
    }

    /// <summary>Exercises dialog results, saved-run restore and collision loss.</summary>
    private async Task CheckConfirmationAndGameOverAsync()
    {
        // Start a fresh unsaved run to exercise a result-bearing confirmation with real signals.
        await StartGameAsync(false).ConfigureAwait(true);
        Click("Pause");
        await UntilAsync(() => HasView("pause")).ConfigureAwait(true);
        Click("MainMenu");
        await UntilAsync(() => HasView("confirm")).ConfigureAwait(true);
        Click("KeepPlaying");
        await UntilAsync(() => !HasView("confirm") && HasView("pause") && _actions.All(static task => task.IsCompleted)).ConfigureAwait(true);
        Click("MainMenu");
        await UntilAsync(() => HasView("confirm")).ConfigureAwait(true);
        Click("Leave");
        await UntilAsync(() => HasView("menu") && !GetTree().Paused).ConfigureAwait(true);
        Click("Continue");
        await UntilAsync(() => _world is { IsRunning: true } && HasView("hud")).ConfigureAwait(true);
        Require(_world!.Score >= 37, "Explicit save did not restore the expected score.");
        await ToSignal(GetTree().CreateTimer(1.8), SceneTreeTimer.SignalName.Timeout);
        foreach (var enemy in GetTree().GetNodesInGroup("dodge_creeps"))
            if (enemy is Node2D creep) creep.Position = _world.PlayerPosition;
        await UntilAsync(() => HasView("game-over")).ConfigureAwait(true);
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
        if (_capturePath is null) return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var error = GetViewport().GetTexture().GetImage().SavePng(_capturePath);
        if (error != Error.Ok) throw new InvalidOperationException("Menu capture failed: " + error);
        GD.Print("DODGE_CAPTURE " + _capturePath);
        RequestQuit();
    }

    /// <summary>Finds a visible action, avoiding stale controls queued for deletion.</summary>
    private Node FindNative(string name, string type) => GetTree().Root.FindChildren(name, type, true, false)
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
            if (condition()) return;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        throw new TimeoutException("Native acceptance condition did not become true.");
    }
    /// <summary>Reports one assertion with its game operation context.</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    /// <summary>Publishes a machine-readable result and performs normal asynchronous shutdown.</summary>
    private void FinishSmoke(bool passed)
    {
        GD.Print("DODGE_SMOKE_RESULT " + (passed ? "PASS" : "FAIL"));
        if (!passed) System.Environment.ExitCode = 1;
        RequestQuit();
    }
}
