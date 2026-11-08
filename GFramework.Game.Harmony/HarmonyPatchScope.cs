// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Modding;

namespace GFramework.Game.Harmony;

/// <summary>Owns one mod's patches and removes only its owner during normal or failed-start cleanup.</summary>
internal sealed class HarmonyPatchScope : IModPatchService, IGameModServiceScope
{
    private readonly HarmonyEngine _engine;
    private readonly object _owner;
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private bool _closed;
    private bool _failed;

    /// <summary>Creates an owner scope without applying any patch.</summary>
    internal HarmonyPatchScope(HarmonyEngine engine, string ownerId)
    {
        OwnerId = ownerId;
        _engine = engine;
        _owner = engine.CreateOwner(ownerId);
    }
    /// <inheritdoc />
    public string OwnerId { get; }
    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return serviceType == typeof(IModPatchService) ? this : null;
    }
    /// <inheritdoc />
    public void PatchAll(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        lock (_gate)
        {
            RequireUsable();
            try { _engine.PatchAll(_owner, assembly); }
            catch { RollBackFailedApplication(); throw; }
        }
    }
    /// <inheritdoc />
    public MethodInfo? Patch(MethodBase original, ModPatchMethod? prefix = null, ModPatchMethod? postfix = null,
        ModPatchMethod? transpiler = null, ModPatchMethod? finalizer = null)
    {
        ArgumentNullException.ThrowIfNull(original);
        lock (_gate)
        {
            RequireUsable();
            try { return _engine.Patch(_owner, original, prefix, postfix, transpiler, finalizer); }
            catch { RollBackFailedApplication(); throw; }
        }
    }
    /// <inheritdoc />
    public void UnpatchAll()
    {
        lock (_gate)
        {
            if (!_closed) _engine.UnpatchAll(_owner, OwnerId);
        }
    }
    /// <inheritdoc />
    public IReadOnlyList<ModPatchDiagnostic> GetPatches()
    {
        lock (_gate)
        {
            return _engine.GetPatches(OwnerId);
        }
    }
    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_closed) return ValueTask.CompletedTask;
            _engine.UnpatchAll(_owner, OwnerId);
            _closed = true;
            return ValueTask.CompletedTask;
        }
    }
    /// <summary>Removes partial registration after a failing PatchAll or manual patch request.</summary>
    private void RollBackFailedApplication()
    {
        _failed = true;
        _engine.UnpatchAll(_owner, OwnerId);
    }
    /// <summary>Prevents patch calls after failure or scope disposal.</summary>
    private void RequireUsable()
    {
        ObjectDisposedException.ThrowIf(_closed, this);
        if (_failed) throw new InvalidOperationException("Patch application failed; repair the mod and restart.");
    }
}
