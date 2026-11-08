// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Arch.Core;
using Arch.System;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Ecs.Arch.Components;

namespace GodotIntegration;

/// <summary>A native Arch system whose frame phases and stop are exercised by the Godot host.</summary>
/// <param name="world">The provider-owned World; this system does not dispose it.</param>
internal sealed class DemoEcsSystem(World world) : BaseSystem<World, float>(world), IDestroyable
{
    private QueryDescription _query = new QueryDescription().WithAll<Position, Velocity>();
    /// <summary>The number of native pre-update callbacks.</summary>
    public int BeforeCalls
    {
        get; private set;
    }
    /// <summary>The number of native post-update callbacks.</summary>
    public int AfterCalls
    {
        get; private set;
    }
    /// <summary>Whether business stop ran before native host detachment.</summary>
    public bool Stopped
    {
        get; private set;
    }
    /// <summary>Whether final resource disposal ran once.</summary>
    public int DisposeCalls
    {
        get; private set;
    }
    /// <inheritdoc />
    public override void BeforeUpdate(in float t) => BeforeCalls++;
    /// <inheritdoc />
    public override void Update(in float t)
    {
        var delta = t;
        World.Query(in _query, (ref Position position, ref Velocity velocity) =>
        {
            position.X += velocity.X * delta;
            position.Y += velocity.Y * delta;
        });
    }
    /// <inheritdoc />
    public override void AfterUpdate(in float t) => AfterCalls++;
    /// <inheritdoc />
    public void Destroy() => Stopped = true;
    /// <inheritdoc />
    public override void Dispose() => DisposeCalls++;
}
