// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Arch.Core;
using Arch.System;

namespace ArchRuntimeDemo;

/// <summary>Empty native work isolates the optional instrumentation cost for local measurements.</summary>
/// <param name="world">The benchmark-owned World.</param>
internal sealed class EmptySystem(World world) : BaseSystem<World, float>(world)
{
    /// <inheritdoc />
    public override void Update(in float t)
    {
    }
}
