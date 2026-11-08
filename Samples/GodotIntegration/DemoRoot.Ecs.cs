// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using GFramework.Ecs.Arch.Abstractions;
using GFramework.Ecs.Arch.Components;
using Godot;
using Microsoft.Extensions.DependencyInjection;

namespace GodotIntegration;

/// <summary>The sample's host-owned physics loop and native ECS acceptance checks.</summary>
public partial class DemoRoot
{
    private IArchRuntime? _ecs;
    private DemoEcsSystem? _ecsSystem;
    private World? _ecsWorld;
    private Entity _ecsEntity;

    /// <summary>Captures the initialized runtime once at the composition boundary.</summary>
    private void BindEcs()
    {
        _ecs = _architecture!.Services.GetRequiredService<IArchRuntime>();
        _ecsSystem = (DemoEcsSystem)_architecture.Services.GetServices<global::Arch.System.ISystem<float>>().Single();
        _ecsWorld = _architecture.Services.GetRequiredService<World>();
        _ecsEntity = _ecsWorld.Create(new Position(0, 0), new Velocity(1, 0));
    }

    /// <summary>Godot retains the physics loop and pause policy; ECS only receives admitted frame steps.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_ready && !_stopping && !GetTree().Paused)
        {
            try
            {
                _ecs!.Update((float)delta);
            }
            catch (Exception error)
            {
                _exitCode = 1;
                ReportError(error);
                RequestQuit();
            }
        }
    }

    /// <summary>Observes actual physics frames and the native system's complete phase sequence.</summary>
    private async Task VerifyEcsFramesAsync()
    {
        for (var index = 0; index < 3; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        var statistics = _ecs!.GetStatistics()!;
        Require(statistics.CompletedFrames > 0 && statistics.FailedFrames == 0, "native ECS frame statistics");
        Require(_ecsSystem!.BeforeCalls == _ecsSystem.AfterCalls && _ecsSystem.AfterCalls > 0, "native ECS three phases");
        Require(_ecsWorld!.Get<Position>(_ecsEntity).X > 0, "native ECS movement");
        var previousPause = GetTree().Paused;
        var completedFrames = statistics.CompletedFrames;
        try
        {
            GetTree().Paused = true;
            for (var index = 0; index < 3; index++)
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Require(_ecs.GetStatistics()!.CompletedFrames == completedFrames, "paused host advanced ECS");
        }
        finally
        {
            GetTree().Paused = previousPause;
        }
        GD.Print("GFRAMEWORK_ECS_FRAMES_PASS");
    }

    /// <summary>Checks stop and final disposal using retained observations after the provider closes.</summary>
    private void VerifyEcsShutdown()
    {
        if (_ecsSystem is null)
            return;
        Require(_ecsSystem.Stopped && _ecsSystem.DisposeCalls == 1, "ECS shutdown ownership");
        var frames = _ecs!.GetStatistics()!.CompletedFrames;
        _ecs.Update(1);
        Require(_ecs.GetStatistics()!.CompletedFrames == frames, "stopped ECS admitted a frame");
        if (_smoke)
            GD.Print("GFRAMEWORK_ECS_SHUTDOWN_PASS");
    }
}
