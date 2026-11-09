// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Runtime;
using Godot;

using DodgeTheCreeps.Advanced.Composition;

namespace DodgeTheCreeps.Advanced;

/// <summary>
/// Godot's composition and lifetime owner. It starts one architecture, owns native
/// children and awaits shutdown; gameplay, settings and document rules live in services.
/// </summary>
public partial class AdvancedGameRoot : Node, ISceneRoot
{
    private AdvancedGameArchitecture? _architecture;
    private bool _quitting;
    private GameApplication? _application;
    private bool _postStartupStarted;

    /// <summary>Captures the host context and begins the explicit asynchronous application lifetime.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += RequestQuit;
        var context = SynchronizationContext.Current ??
            throw new InvalidOperationException("Godot synchronization context is unavailable.");
        _architecture = new AdvancedGameArchitecture(this, GameLaunchOptions.Parse(OS.GetCmdlineUserArgs()),
            new SynchronizationContextGameDispatcher(context));
        _ = StartAsync();
    }

    /// <summary>Receives the already composed application from the only container-aware boundary.</summary>
    internal void AttachApplication(GameApplication application) => _application = application;

    /// <summary>Starts optional native acceptance outside the startup callback's execution context.</summary>
    public override void _Process(double delta)
    {
        if (_postStartupStarted || _architecture is not { IsReady: true } || _application is not { IsStarted: true })
            return;
        _postStartupStarted = true;
        _application.BeginPostStartupActions();
    }

    private async Task StartAsync()
    {
        try
        {
            await _architecture!.InitializeAsync().ConfigureAwait(true);
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            System.Environment.ExitCode = 1;
            var status = new Label { Name = "StartupError", Text = error.Message, Size = new Vector2(440, 240), Position = new Vector2(20, 24), AutowrapMode = TextServer.AutowrapMode.WordSmart };
            AddChild(status);
            if (OS.GetCmdlineUserArgs().Length > 0)
                RequestQuit();
        }
    }

    /// <summary>Stops the owned architecture once and quits only after native views and services have drained.</summary>
    internal async void RequestQuit()
    {
        if (_quitting)
            return;
        _quitting = true;
        var exit = System.Environment.ExitCode;
        try
        {
            if (_architecture is not null)
                await _architecture.DestroyAsync().ConfigureAwait(true);
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            exit = 1;
        }
        GetWindow().CloseRequested -= RequestQuit;
        GetTree().Quit(exit);
    }

    /// <summary>Attaches the node owned by SceneRouter.</summary>
    public void AddScene(ISceneBehavior scene) => AddChild((Node)scene.Original);

    /// <summary>Detaches only a scene actually owned by this root.</summary>
    public void RemoveScene(ISceneBehavior scene)
    {
        var node = (Node)scene.Original;
        if (GodotObject.IsInstanceValid(node) && node.GetParent() == this)
            RemoveChild(node);
    }

    /// <summary>Lets deferred node deletion and native audio release finish before provider-owned resources dispose.</summary>
    internal async Task DrainNativeViewsAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var audioDrain = Math.Clamp(AudioServer.GetTimeToNextMix() * 2, 0.12, 0.5);
        await ToSignal(GetTree().CreateTimer(audioDrain, processAlways: true, processInPhysics: false, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
    }
}
