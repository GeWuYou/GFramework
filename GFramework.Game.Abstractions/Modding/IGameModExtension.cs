// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace GFramework.Game.Abstractions.Modding;

/// <summary>An optional host adapter that contributes shared contract assemblies and per-mod services.</summary>
/// <remarks>The host owns this adapter; each created scope is disposed before the native host detaches.</remarks>
public interface IGameModExtension
{
    /// <summary>Gets the canonical capability name accepted by code manifests.</summary>
    string Capability
    {
        get;
    }
    /// <summary>Gets assemblies that must share host type identity instead of loading private mod copies.</summary>
    IReadOnlyList<Assembly> SharedAssemblies
    {
        get;
    }
    /// <summary>Creates one scope; constructors must not patch or otherwise start the mod.</summary>
    /// <param name="context">The approved mod context.</param>
    /// <returns>A scope whose services are visible only to this mod.</returns>
    IGameModServiceScope CreateScope(IGameModContext context);
}
