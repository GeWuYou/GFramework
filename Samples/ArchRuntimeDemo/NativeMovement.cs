// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Arch.Core;
using Arch.System;
using GFramework.Ecs.Arch.Components;

namespace ArchRuntimeDemo;

/// <summary>A plain Arch system; no framework inheritance or service locator is required.</summary>
/// <param name="world">The caller-owned entity world.</param>
internal sealed class NativeMovement(World world) : BaseSystem<World, float>(world)
{
    private QueryDescription _query = new QueryDescription().WithAll<Position, Velocity>();
    /// <summary>Observed pre-update invocations.</summary>
    public int BeforeCalls
    {
        get; private set;
    }
    /// <summary>Observed post-update invocations.</summary>
    public int AfterCalls
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
}
