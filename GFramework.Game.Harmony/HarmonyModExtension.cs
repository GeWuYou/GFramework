// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using GFramework.Game.Abstractions.Modding;

namespace GFramework.Game.Harmony;

/// <summary>Opt-in adapter sharing one host Harmony library and giving each mod an independently owned patch scope.</summary>
/// <remarks>Add this extension to GameModHostOptions.Extensions. The base Game and Godot packages never load Harmony.
/// The engine lives in the Default load context; this adapter can remain in an engine-owned context without duplicating Harmony types.</remarks>
public sealed class HarmonyModExtension : IGameModExtension
{
    private readonly HarmonyEngine _engine;

    /// <summary>Creates an adapter around one pinned Default-context Harmony engine.</summary>
    /// <param name="harmonyAssemblyPath">Optional absolute host-owned 0Harmony.dll path. Otherwise Default name resolution
    /// and the application base directory are used; arbitrary mod folders are never searched.</param>
    public HarmonyModExtension(string? harmonyAssemblyPath = null)
    {
        _engine = new HarmonyEngine(harmonyAssemblyPath);
        SharedAssemblies = Array.AsReadOnly([typeof(HarmonyModExtension).Assembly, _engine.Assembly]);
    }
    /// <inheritdoc />
    public string Capability => "harmony";
    /// <inheritdoc />
    public IReadOnlyList<Assembly> SharedAssemblies
    {
        get;
    }
    /// <inheritdoc />
    public IGameModServiceScope CreateScope(IGameModContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new HarmonyPatchScope(_engine, GetOwnerId(context.GameId, context.Package.Id));
    }
    /// <summary>Builds a collision-free owner identity for mod ordering annotations and diagnostics.</summary>
    /// <param name="gameId">Stable game identity.</param>
    /// <param name="modId">Canonical package ID.</param>
    /// <returns>The owner ID used by this adapter.</returns>
    public static string GetOwnerId(string gameId, string modId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        return "gframework:" + gameId.Length.ToString(CultureInfo.InvariantCulture) + ":" + gameId + ":" + modId;
    }
}
