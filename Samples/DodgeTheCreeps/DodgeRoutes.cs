// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;

namespace DodgeTheCreeps;

/// <summary>Typed definitions shared by registration and business operations.</summary>
internal static class DodgeRoutes
{
    /// <summary>Main menu navigation page.</summary>
    internal static UiRoute<bool> Menu { get; } = new("menu", new UiRouteOptions { Layer = 10, AllowBack = false, BlocksWorldActionInput = true, BlocksWorldPointerInput = true });
    /// <summary>Gameplay HUD stays visible under menus without owning input.</summary>
    internal static UiRoute<int> Hud { get; } = new("hud", new UiRouteOptions { Kind = UiRouteKind.Window, Layer = 5, AllowBack = false, CapturedActions = UiInputActionMask.None });
    /// <summary>Gameplay pause window owns one pause request.</summary>
    internal static UiRoute<int> Pause { get; } = new("pause", Modal(20));
    /// <summary>Settings owns a separate pause request, including while opened over pause.</summary>
    internal static UiRoute<bool> Settings { get; } = new("settings", Modal(30));
    /// <summary>Game-over page carries the completed score.</summary>
    internal static UiRoute<int> GameOver { get; } = new("game-over", new UiRouteOptions { Layer = 10, AllowBack = false, BlocksWorldActionInput = true });
    /// <summary>Unsaved progress confirmation returns an explicit business choice.</summary>
    internal static UiDialogRoute<string, bool> Confirm { get; } = new("confirm", Modal(40));

    /// <summary>Declares input and pause behavior independently of visual ordering.</summary>
    private static UiRouteOptions Modal(int layer) => new()
    {
        Kind = UiRouteKind.Window, Layer = layer, Modal = true,
        BlocksWorldActionInput = true, BlocksWorldPointerInput = true,
        PauseWhileVisible = true, ContinueProcessingWhenPaused = true
    };
}
