// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>Describes a successfully evaluated operation, separate from failures and cancellation.</summary>
public enum UiOperationStatus
{
    /// <summary>The requested state change was committed.</summary>
    Applied,
    /// <summary>A guard refused the request without changing state.</summary>
    Rejected,
    /// <summary>The requested state already exists or the target is no longer open.</summary>
    NoOp,
}
