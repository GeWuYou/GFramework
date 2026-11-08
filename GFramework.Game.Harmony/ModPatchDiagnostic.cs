// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace GFramework.Game.Harmony;

/// <summary>Describes an installed patch without claiming to detect semantic conflicts.</summary>
/// <param name="Target">Declaring type and original method.</param>
/// <param name="PatchMethod">Declaring type and patch method.</param>
/// <param name="OwnerId">Harmony owner ID.</param>
/// <param name="Kind">Prefix, Postfix, Transpiler or Finalizer.</param>
/// <param name="Priority">Harmony priority.</param>
/// <param name="Before">Owners requested after this patch.</param>
/// <param name="After">Owners requested before this patch.</param>
public sealed record ModPatchDiagnostic(string Target, string PatchMethod, string OwnerId, string Kind,
    int Priority, IReadOnlyList<string> Before, IReadOnlyList<string> After);
