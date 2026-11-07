// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>Optional admission and leave policy, evaluated before any destructive state change.</summary>
public interface IUiGuard
{
    /// <summary>Checks whether opening the route is allowed.</summary>
    /// <param name="routeKey">Stable route key.</param>
    /// <param name="cancellationToken">Operation cancellation.</param>
    /// <returns>Whether to continue opening.</returns>
    Task<bool> CanOpenAsync(string routeKey, CancellationToken cancellationToken = default);
    /// <summary>Checks whether closing the current instance is allowed.</summary>
    /// <param name="view">Current committed instance snapshot.</param>
    /// <param name="cancellationToken">Operation cancellation.</param>
    /// <returns>Whether to continue closing.</returns>
    Task<bool> CanCloseAsync(UiViewSnapshot view, CancellationToken cancellationToken = default);
}
