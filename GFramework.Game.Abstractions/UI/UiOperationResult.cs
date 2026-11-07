// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>Reports an operation's outcome and the affected instance when applicable.</summary>
/// <param name="Status">Committed, refused, or unchanged outcome.</param>
/// <param name="Handle">Affected instance, if one exists.</param>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
public readonly record struct UiOperationResult(UiOperationStatus Status, UiInstanceHandle? Handle = null);
