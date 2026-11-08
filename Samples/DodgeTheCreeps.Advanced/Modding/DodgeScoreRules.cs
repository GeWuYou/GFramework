// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace DodgeTheCreeps.Modding;

/// <summary>Internal game implementation used to demonstrate method patching separately from the published mod API.</summary>
internal static class DodgeScoreRules
{
    /// <summary>Returns the game's ordinary score increment; the external Harmony example patches this exact method.</summary>
    /// <remarks>NoInlining keeps this demonstration target callable in Release exports; arbitrary game methods have no such guarantee.</remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int Calculate() => 1;
}
