// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>A stable copy of committed UI state, safe to inspect without accessing native objects.</summary>
/// <param name="Views">Views in mounting order, copied when state changes.</param>
/// <param name="IsBusy">Whether a queued operation is currently executing.</param>
/// <param name="IsStopping">Whether admission is closed.</param>
public sealed record UiSnapshot(IReadOnlyList<UiViewSnapshot> Views, bool IsBusy, bool IsStopping)
{
    /// <summary>Gets whether visible UI blocks gameplay pointer input.</summary>
    public bool BlocksWorldPointerInput => Views.Any(static view => view.Visible && view.Options.BlocksWorldPointerInput);
    /// <summary>Gets whether visible UI blocks gameplay action input.</summary>
    public bool BlocksWorldActionInput => Views.Any(static view => view.Visible && view.Options.BlocksWorldActionInput);
}
