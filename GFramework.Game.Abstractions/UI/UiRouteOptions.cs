// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pause;

namespace GFramework.Game.Abstractions.UI;

/// <summary>
/// Declares a route's presentation and interaction policy once, independently of its native view.
/// Higher layers appear above lower layers; newer instances win within a layer. Visual order does not imply modality.
/// </summary>
public sealed record UiRouteOptions
{
    /// <summary>Gets whether the route is a page or an independent window.</summary>
    public UiRouteKind Kind { get; init; } = UiRouteKind.Page;
    /// <summary>Gets the visual layer; higher values appear above lower values.</summary>
    public int Layer
    {
        get; init;
    }
    /// <summary>Gets whether lower visible views lose interaction while this view is open.</summary>
    public bool Modal
    {
        get; init;
    }
    /// <summary>Gets whether visible instances prevent gameplay pointer input.</summary>
    public bool BlocksWorldPointerInput
    {
        get; init;
    }
    /// <summary>Gets whether visible instances prevent gameplay action input.</summary>
    public bool BlocksWorldActionInput
    {
        get; init;
    }
    /// <summary>Gets semantic actions captured by this route. Cancel defaults to normal back behavior.</summary>
    public UiInputActionMask CapturedActions { get; init; } = UiInputActionMask.Cancel;
    /// <summary>Gets whether visibility owns a pause request, released when hidden or closed.</summary>
    public bool PauseWhileVisible
    {
        get; init;
    }
    /// <summary>Gets the group affected by the optional pause request.</summary>
    public PauseGroup PauseGroup { get; init; } = PauseGroup.Global;
    /// <summary>Gets whether the native view should continue processing while gameplay is paused.</summary>
    public bool ContinueProcessingWhenPaused { get; init; } = true;
    /// <summary>Gets whether a window route may have multiple instances. Page pushes always create history entries.</summary>
    public bool AllowMultiple
    {
        get; init;
    }
    /// <summary>Gets whether the default back operation may close this view.</summary>
    public bool AllowBack { get; init; } = true;
}
