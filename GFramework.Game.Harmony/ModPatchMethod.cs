// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Reflection;

namespace GFramework.Game.Harmony;

/// <summary>ABI-neutral declaration for a manual patch; Harmony-specific runtime objects never cross host load contexts.</summary>
public sealed class ModPatchMethod
{
    /// <summary>Creates a patch declaration for an exact managed method.</summary>
    /// <param name="method">Static patch method interpreted by the host's shared Harmony engine.</param>
    public ModPatchMethod(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);
        Method = method;
    }
    /// <summary>Gets the exact patch method.</summary>
    public MethodInfo Method { get; }
    /// <summary>Gets an optional priority override; null preserves Harmony's method annotation/default behavior.</summary>
    public int? Priority { get; init; }
    /// <summary>Gets owner IDs to run after this patch; an empty list preserves method annotations.</summary>
    public IReadOnlyList<string> Before { get; init; } = Array.Empty<string>();
    /// <summary>Gets owner IDs to run before this patch; an empty list preserves method annotations.</summary>
    public IReadOnlyList<string> After { get; init; } = Array.Empty<string>();
}
