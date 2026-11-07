// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using Godot;

namespace GodotIntegration;

/// <summary>
/// A project-owned view provides input and pause intent to the standard UI behavior.
/// Views keep processing during pause so nested settings can close normally.
/// </summary>
public partial class DemoPanel : PanelContainer, IUiInteractionProfileProvider, IUiActionHandler
{
    /// <summary>Gets or sets the composition root before this view is added.</summary>
    internal DemoRoot Host { get; init; } = null!;

    /// <summary>Gets or sets whether this panel is a gameplay modal rather than the main menu.</summary>
    internal bool PausesGame { get; init; }

    /// <inheritdoc />
    public UiInteractionProfile GetUiInteractionProfile(UiLayer layer) => new()
    {
        CapturedActions = PausesGame ? UiInputActionMask.Cancel : UiInputActionMask.None,
        BlocksWorldActionInput = true,
        BlocksWorldPointerInput = true,
        PauseMode = PausesGame ? UiPauseMode.WhileVisible : UiPauseMode.None,
        ContinueProcessingWhenPaused = true,
        PauseReason = "sample-modal"
    };

    /// <inheritdoc />
    public bool TryHandleUiAction(UiInputAction action)
    {
        if (!PausesGame || action != UiInputAction.Cancel)
            return false;
        Host.RequestCloseModal();
        return true;
    }
}
