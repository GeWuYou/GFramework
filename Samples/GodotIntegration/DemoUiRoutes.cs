// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;

namespace GodotIntegration;

/// <summary>Defines titles and interaction policy once for the sample's typed routes.</summary>
internal static class DemoUiRoutes
{
    private static readonly UiRouteOptions ModalOptions = new()
    {
        Kind = UiRouteKind.Window,
        Layer = 30,
        Modal = true,
        BlocksWorldPointerInput = true,
        BlocksWorldActionInput = true,
        PauseWhileVisible = true,
        ContinueProcessingWhenPaused = true
    };

    /// <summary>Gets the non-dismissible main-menu page; the string argument is its visible title.</summary>
    internal static UiRoute<string> Menu { get; } = new("menu", new UiRouteOptions
    {
        Layer = 20,
        AllowBack = false,
        CapturedActions = UiInputActionMask.None,
        BlocksWorldPointerInput = true,
        BlocksWorldActionInput = true
    });

    /// <summary>Gets the pause window, which owns one pause request while visible.</summary>
    internal static UiRoute<string> Pause { get; } = new("pause", ModalOptions);

    /// <summary>Gets the settings window, whose pause request is independent of a lower pause window.</summary>
    internal static UiRoute<string> Settings { get; } = new("settings", ModalOptions);
}
