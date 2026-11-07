// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>Owns native mounting, ordering, and focus while the service owns view lifetimes.</summary>
public interface IUiHost
{
    /// <summary>
    /// Mounts a hidden view, or reorders a view already attached to this host without remounting its lifecycle.
    /// Higher layers and then higher order values are in front. Singleton-window reuse calls this method again.
    /// </summary>
    /// <param name="view">View to mount or reorder.</param>
    /// <param name="layer">Visual layer independent of input and pause policy.</param>
    /// <param name="order">Stable order within the layer.</param>
    void Attach(IUiView view, int layer, long order);
    /// <summary>Detaches a view without disposing its lease.</summary>
    /// <param name="view">Mounted or partially mounted view.</param>
    void Detach(IUiView view);
    /// <summary>Captures an opaque focus bookmark for restoration after a modal closes.</summary>
    /// <returns>Bookmark owned by the host, or null if no focus exists.</returns>
    object? CaptureFocus();
    /// <summary>Moves focus to a newly active view using the host's native navigation rules.</summary>
    /// <param name="view">View receiving focus.</param>
    void Focus(IUiView view);
    /// <summary>Restores a previously captured bookmark if its native target remains valid.</summary>
    /// <param name="bookmark">Previously captured bookmark.</param>
    void RestoreFocus(object? bookmark);
}
