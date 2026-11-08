// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Reflection;

namespace GFramework.Game.Harmony;

/// <summary>Mod-local patch ownership; the host removes this owner's patches after the mod stops.</summary>
/// <remarks>Only use this scope for patches that belong to the current mod. Patch mutation, inspection and disposal are synchronously serialized per scope.
/// Harmony still owns priority/before/after semantics, and arbitrary patch side effects cannot be rolled back.</remarks>
public interface IModPatchService
{
    /// <summary>Gets the stable owner ID used by HarmonyBefore and HarmonyAfter annotations.</summary>
    string OwnerId
    {
        get;
    }
    /// <summary>Applies annotations from an explicit assembly, avoiding caller-assembly inference.</summary>
    /// <param name="assembly">The mod assembly containing patch declarations.</param>
    void PatchAll(Assembly assembly);
    /// <summary>Applies one method patch with Harmony's native ordering declarations.</summary>
    /// <param name="original">Managed method to patch.</param>
    /// <param name="prefix">Optional prefix.</param>
    /// <param name="postfix">Optional postfix.</param>
    /// <param name="transpiler">Optional IL transformation.</param>
    /// <param name="finalizer">Optional exception finalizer.</param>
    /// <returns>Harmony's generated replacement method.</returns>
    MethodInfo? Patch(MethodBase original, ModPatchMethod? prefix = null, ModPatchMethod? postfix = null,
        ModPatchMethod? transpiler = null, ModPatchMethod? finalizer = null);
    /// <summary>Removes only this owner's patches; other mods and Harmony users remain installed.</summary>
    void UnpatchAll();
    /// <summary>Inspects this owner's registered methods and native ordering declarations.</summary>
    /// <returns>A snapshot suitable for logs and diagnostic interfaces.</returns>
    IReadOnlyList<ModPatchDiagnostic> GetPatches();
}
