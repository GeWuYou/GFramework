// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Data;
using GFramework.Godot.Scene;
using Godot;
using JsonSerializer = GFramework.Game.Serializer.JsonSerializer;

namespace GodotIntegration;

/// <summary>
/// Bounded native integration checks for the runnable sample. This runner exercises production actions,
/// native input and frame processing in a per-run data directory; it does not establish visual acceptance.
/// </summary>
public partial class DemoRoot
{
    /// <summary>Observes the smoke workflow and requests normal shutdown with an appropriate process exit code.</summary>
    private async void RunSmoke()
    {
        try
        {
            await _startupTask.ConfigureAwait(true);
            Require(_ready, "startup did not reach Ready");
            Require(Gameplay.Speed > 0, "generated YAML configuration was not loaded");
            Require(Ui.Snapshot.Views.Any(view => string.Equals(view.RouteKey, DemoUiRoutes.Menu.Key, StringComparison.Ordinal) && view.Visible), "initial menu missing");
            Require(_dataRootPath.StartsWith("user://runtime_smoke/", StringComparison.Ordinal), "smoke data is not isolated");

            await VerifySceneProcessingFlagsAsync().ConfigureAwait(true);
            if (_smokeContinue)
            {
                await VerifyPersistedProcessAsync().ConfigureAwait(true);
                GD.Print("GFRAMEWORK_SMOKE_CHECKS cross-process-preferences,binding,continue: PASS");
            }
            else
            {
                var world = await VerifyNewGameAndMovementAsync().ConfigureAwait(true);
                await VerifyNestedPauseAsync(world).ConfigureAwait(true);
                await VerifySaveAndContinueAsync(world).ConfigureAwait(true);
                GD.Print("GFRAMEWORK_SMOKE_DATA " + _smokeDataId);
                GD.Print("GFRAMEWORK_SMOKE_CHECKS config,input,scene,ui,pause,settings,save,coroutine: PASS");
            }
        }
        catch (Exception error)
        {
            _exitCode = 1;
            GD.PushError("GFRAMEWORK_SMOKE_FAILURE " + error);
        }
        finally
        {
            // This runner is not an ordinary action and is never part of the tasks joined by RequestQuit.
            RequestQuit();
        }
    }

    /// <summary>Accepts only a generated or exact N-format GUID below the smoke-only storage root.</summary>
    /// <param name="arguments">Godot user arguments after the command separator.</param>
    /// <param name="error">A malformed or ambiguous smoke invocation message.</param>
    /// <returns>Whether the arguments can be used without reaching the ordinary sample data.</returns>
    private bool TryConfigureSmoke(string[] arguments, out string error)
    {
        const string continuationPrefix = "--smoke-continue=";
        var continuation = arguments.Where(argument =>
            argument.StartsWith("--smoke-continue", StringComparison.Ordinal)).ToArray();
        var fresh = arguments.Contains("--smoke", StringComparer.Ordinal);
        error = string.Empty;
        if (!fresh && continuation.Length == 0)
            return true;
        _smoke = true;
        if ((fresh && continuation.Length != 0) || continuation.Length > 1)
        {
            error = "Choose one smoke mode.";
            return false;
        }

        if (fresh)
        {
            _smokeDataId = Guid.NewGuid().ToString("N");
        }
        else
        {
            var argument = continuation[0];
            if (!argument.StartsWith(continuationPrefix, StringComparison.Ordinal) ||
                argument.Length != continuationPrefix.Length + 32 ||
                !Guid.TryParseExact(argument[continuationPrefix.Length..], "N", out var identifier))
            {
                error = "--smoke-continue requires one exact N-format GUID.";
                return false;
            }
            _smokeDataId = identifier.ToString("N");
            _smokeContinue = true;
        }

        _dataRootPath = "user://runtime_smoke/" + _smokeDataId;
        return true;
    }

    /// <summary>Checks native scene pause/restoration against asymmetric authored processing settings.</summary>
    private async Task VerifySceneProcessingFlagsAsync()
    {
        var node = new Node { Name = "ProcessingProbe" };
        _sceneRoot.AddChild(node);
        node.SetProcess(false);
        node.SetPhysicsProcess(true);
        node.SetProcessInput(false);
        var scene = SceneBehaviorFactory.Create(node, "processing-probe");
        try
        {
            await scene.OnLoadAsync(null).ConfigureAwait(true);
            await scene.OnEnterAsync().ConfigureAwait(true);
            await scene.OnResumeAsync().ConfigureAwait(true);
            RequireProcessingFlags(node, process: false, physics: true, input: false);

            await scene.OnPauseAsync().ConfigureAwait(true);
            RequireProcessingFlags(node, process: false, physics: false, input: false);
            await scene.OnPauseAsync().ConfigureAwait(true);
            await scene.OnResumeAsync().ConfigureAwait(true);
            RequireProcessingFlags(node, process: false, physics: true, input: false);

            // Repeated resume must preserve a later authored state, rather than
            // replaying an already consumed snapshot or enabling all processing.
            node.SetProcess(true);
            node.SetPhysicsProcess(false);
            node.SetProcessInput(true);
            await scene.OnResumeAsync().ConfigureAwait(true);
            RequireProcessingFlags(node, process: true, physics: false, input: true);
            await scene.OnPauseAsync().ConfigureAwait(true);
            await scene.OnResumeAsync().ConfigureAwait(true);
            RequireProcessingFlags(node, process: true, physics: false, input: true);
        }
        finally
        {
            await scene.OnExitAsync().ConfigureAwait(true);
            await scene.OnUnloadAsync().ConfigureAwait(true);
            RemoveScene(scene);
            await WaitFramesAsync(2).ConfigureAwait(true);
            Require(!GodotObject.IsInstanceValid(node), "native processing probe was not released");
        }
        GD.Print("GFRAMEWORK_SMOKE_CHECKS native-scene-processing: PASS");
    }

    /// <summary>Compares actual Godot processing flags without inferring them from business activity.</summary>
    private static void RequireProcessingFlags(Node node, bool process, bool physics, bool input)
    {
        Require(node.IsProcessing() == process, "scene process setting was not preserved");
        Require(node.IsPhysicsProcessing() == physics, "scene physics setting was not preserved");
        Require(node.IsProcessingInput() == input, "scene input setting was not preserved");
    }

    /// <summary>Confirms a fresh process loaded the prior run's settings and slot through normal startup and menu actions.</summary>
    private async Task VerifyPersistedProcessAsync()
    {
        var expected = FormattableString.Invariant($"key:{(int)Key.P}");
        Require(Preferences.Bindings is not null, "fresh process did not load persisted preferences");
        Require(_bindings.GetBindings("demo_pause").Bindings.Any(binding =>
            string.Equals(binding.Code, expected, StringComparison.Ordinal)), "fresh process did not restore the P binding");
        await ClickAsync("Continue slot 1").ConfigureAwait(true);
        Require(Scenes.Current?.Original is DemoWorld, "fresh process could not continue the saved slot");
        Require(Save.Version == 1 && float.IsFinite(Save.X) && Save.X > 480,
            "fresh process loaded default or invalid gameplay state");
        Require(!GetTree().Paused && Ui.Snapshot.Views.Count(view => view.Options.Kind == UiRouteKind.Window) == 0, "continued world inherited a stale pause owner");
    }

    /// <summary>Creates the native world and checks movement plus the running node-owned coroutine.</summary>
    /// <returns>The world instance later used to verify pause and lifetime behavior.</returns>
    private async Task<DemoWorld> VerifyNewGameAndMovementAsync()
    {
        await ClickAsync("New game").ConfigureAwait(true);
        Require(Scenes.Current?.Original is DemoWorld, "new game did not create the native world");
        var world = (DemoWorld)Scenes.Current!.Original;
        Require(world.IsInsideTree(), "world is not attached to the scene root");
        Require(Ui.Snapshot.Views.Count == 0, "main menu remained on the page stack");

        var originalX = Save.X;
        InjectKey(Key.D, pressed: true);
        await WaitSecondsAsync(0.2).ConfigureAwait(true);
        InjectKey(Key.D, pressed: false);
        Require(Save.X > originalX, "native movement input did not reach the world");
        await WaitSecondsAsync(1.1).ConfigureAwait(true);
        Require(world.ActiveSeconds > 0, "node-owned coroutine did not advance");
        return world;
    }

    /// <summary>Checks native pause behavior and the independently held settings/pause modal tokens.</summary>
    /// <param name="world">Current scene and its observable heartbeat counter.</param>
    private async Task VerifyNestedPauseAsync(DemoWorld world)
    {
        InjectKey(Key.Escape, pressed: true);
        InjectKey(Key.Escape, pressed: false);
        await AwaitActionAsync().ConfigureAwait(true);
        Require(GetTree().Paused && Ui.Snapshot.Views.Count(view => view.Options.Kind == UiRouteKind.Window) == 1, "pause input did not pause the scene tree");
        var pausedSeconds = world.ActiveSeconds;
        var pausedX = Save.X;
        InjectKey(Key.D, pressed: true);
        await WaitSecondsAsync(1.1).ConfigureAwait(true);
        InjectKey(Key.D, pressed: false);
        Require(world.ActiveSeconds == pausedSeconds && Save.X == pausedX, "world or coroutine advanced while paused");

        await ClickAsync("Settings").ConfigureAwait(true);
        Require(GetTree().Paused && Ui.Snapshot.Views.Count(view => view.Options.Kind == UiRouteKind.Window) == 2, "nested settings did not retain both pause owners");
        await VerifyPreferencesAsync().ConfigureAwait(true);
        await ClickAsync("Back").ConfigureAwait(true);
        Require(GetTree().Paused && Ui.Snapshot.Views.Count(view => view.Options.Kind == UiRouteKind.Window) == 1, "closing settings resumed the lower pause owner");
    }

    /// <summary>Persists a slot, unloads the world across native frames, and restores the same saved state.</summary>
    /// <param name="world">Original world whose release and coroutine termination must be observed.</param>
    private async Task VerifySaveAndContinueAsync(DemoWorld world)
    {
        await ClickAsync("Save slot 1").ConfigureAwait(true);
        var loaded = await _saves.LoadAsync(1).ConfigureAwait(true);
        Require(!ReferenceEquals(loaded, Save), "slot load reused the in-memory DTO");
        Require(loaded.X == Save.X && loaded.Y == Save.Y && loaded.Collected == Save.Collected,
            "slot reload did not preserve gameplay state");
        await ClickAsync("Return without saving").ConfigureAwait(true);
        Require(Scenes.Current is null && !GetTree().Paused && Ui.Snapshot.Views.Count(view => view.Options.Kind == UiRouteKind.Window) == 0,
            "return to menu left a scene or pause owner active");
        Require(!GodotObject.IsInstanceValid(world), "QueueFree did not release the old world after frame boundaries");
        var stoppedSeconds = world.ActiveSeconds;
        await WaitSecondsAsync(1.1).ConfigureAwait(true);
        Require(world.ActiveSeconds == stoppedSeconds, "node-owned coroutine continued after its owner left the tree");

        await ClickAsync("Continue slot 1").ConfigureAwait(true);
        Require(Scenes.Current?.Original is DemoWorld && Save.X == loaded.X && Save.Y == loaded.Y,
            "continue did not restore the saved world");
    }

    /// <summary>Exercises actual rebind UI and reloads the persisted section using a fresh repository instance.</summary>
    private async Task VerifyPreferencesAsync()
    {
        await ClickAsync("Rebind pause: press a key").ConfigureAwait(true);
        InjectKey(Key.P, pressed: true);
        InjectKey(Key.P, pressed: false);
        await WaitFramesAsync(2).ConfigureAwait(true);
        Require(!_awaitingBinding, "native key input did not finish rebinding");
        await ClickAsync("Save settings").ConfigureAwait(true);

        var repository = new UnifiedSettingsDataRepository(_storage ?? throw new InvalidOperationException("Storage was not initialized."), new JsonSerializer(),
            new DataRepositoryOptions { BasePath = _dataRootPath, EnableEvents = false });
        repository.RegisterDataType(_settingsLocation, typeof(DemoPreferences));
        var preferences = await repository.LoadAsync<DemoPreferences>(_settingsLocation).ConfigureAwait(true);
        Require(preferences.Bindings is not null, "persisted binding snapshot missing");
        _bindings.ResetAction("demo_pause");
        _bindings.ImportSnapshot(preferences.Bindings!);
        var expected = FormattableString.Invariant($"key:{(int)Key.P}");
        Require(_bindings.GetBindings("demo_pause").Bindings.Any(binding =>
            string.Equals(binding.Code, expected, StringComparison.Ordinal)), "binding snapshot did not restore native InputMap");
    }

    /// <summary>Emits a real Godot button signal and joins the production action it accepted.</summary>
    /// <param name="text">Exact visible label in the topmost sample view.</param>
    private async Task ClickAsync(string text)
    {
        var target = Ui.Snapshot.Views.Last(view => view.Visible && view.Active);
        var panel = EnumerateNodes(_uiRoot).OfType<DemoPanel>()
            .Single(candidate => candidate.InstanceHandle == target.Handle);
        var button = panel.GetChildren().OfType<VBoxContainer>().Single()
            .GetChildren().OfType<Button>().Single(candidate => string.Equals(candidate.Text, text, StringComparison.Ordinal));
        button.EmitSignal(Button.SignalName.Pressed);
        await AwaitActionAsync().ConfigureAwait(true);
    }

    /// <summary>Traverses the default native host without depending on its mounting implementation.</summary>
    /// <param name="node">Current native branch.</param>
    /// <returns>The branch and all descendants.</returns>
    private static IEnumerable<Node> EnumerateNodes(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren())
        {
            foreach (var descendant in EnumerateNodes(child))
                yield return descendant;
        }
    }

    /// <summary>Joins the latest production operation, then crosses native frame boundaries for deferred frees.</summary>
    private async Task AwaitActionAsync()
    {
        await _actionTask.ConfigureAwait(true);
        await WaitFramesAsync(2).ConfigureAwait(true);
        Require(!_busy, "accepted action did not finish");
    }

    /// <summary>Injects a native key event through Godot's normal input path.</summary>
    private static void InjectKey(Key key, bool pressed)
    {
        using var input = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed };
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
    }

    /// <summary>Waits even while the gameplay scene is paused.</summary>
    private async Task WaitSecondsAsync(double seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds, processAlways: true), SceneTreeTimer.SignalName.Timeout);
    }

    /// <summary>Waits for native frames without blocking the engine thread.</summary>
    private async Task WaitFramesAsync(int count)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    /// <summary>Fails the native smoke run with an actionable assertion message.</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
