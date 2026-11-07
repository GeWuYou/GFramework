// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;

namespace GFramework.Godot.UI;

/// <summary>Connects unhandled native Cancel input to the UI service, leaving native control navigation intact.</summary>
/// <remarks>The host supplies its observed asynchronous action boundary; all action failures remain visible to that host.</remarks>
public partial class GodotUiInputBridge : Node
{
    /// <summary>Gets or sets the service belonging to this scene tree.</summary>
    public IUiService Service { get; set; } = null!;
    /// <summary>Gets or sets the native action name used for Back.</summary>
    public string CancelAction { get; set; } = "ui_cancel";
    /// <summary>Gets or sets the host's action boundary, which must observe the supplied task.</summary>
    public Action<Func<Task>> RunAction { get; set; } = null!;
    /// <summary>Gets or sets the observed fallback when no UI owns Cancel, for example opening a gameplay pause menu.</summary>
    public Func<Task>? UnhandledCancelAsync { get; set; }

    /// <summary>Keeps UI navigation available while gameplay is paused.</summary>
    public override void _Ready() => ProcessMode = ProcessModeEnum.Always;

    /// <summary>Dispatches one non-repeated Cancel after native GUI controls have had their turn.</summary>
    /// <param name="inputEvent">Native input event.</param>
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!inputEvent.IsActionPressed(CancelAction) || inputEvent.IsEcho() || Service.Snapshot.IsStopping)
            return;
        GetViewport().SetInputAsHandled();
        RunAction(async () =>
        {
            if (!await Service.DispatchActionAsync(UiInputAction.Cancel).ConfigureAwait(true) && UnhandledCancelAsync is not null)
                await UnhandledCancelAsync().ConfigureAwait(true);
        });
    }
}
