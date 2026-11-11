// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Runtime;
using GFramework.Godot.UI;
using Godot;

namespace GodotIntegration.GettingStarted;

/// <summary>Native lifetime owner for the minimal adoption scene, with observed startup, actions and shutdown.</summary>
/// <remarks>Async continuations retain the owning Godot context because they access native nodes.</remarks>
public partial class QuickStartRoot : Control
{
    /// <summary>Gets or sets the page scene retained by this host.</summary>
    [Export] public PackedScene PageScene { get; set; } = null!;

    /// <summary>Gets or sets the typed confirmation scene retained by this host.</summary>
    [Export] public PackedScene ConfirmScene { get; set; } = null!;

    /// <summary>Gets or sets the resources registered during explicit startup.</summary>
    [Export] public Godot.Collections.Array<QuickStartItem> Items { get; set; } = [];

    /// <summary>The shared page definition used by registration and navigation.</summary>
    internal static readonly UiRoute<int> PageRoute = new("quickstart_page");

    /// <summary>The shared dialog definition and native modal policy.</summary>
    internal static readonly UiDialogRoute<string, bool> ConfirmRoute = new("quickstart_confirm",
        new UiRouteOptions { Kind = UiRouteKind.Window, Modal = true, Layer = 10 });

    /// <summary>Gets the borrowed native UI host until architecture shutdown completes.</summary>
    internal GodotUiRoot UiRoot { get; private set; } = null!;

    /// <summary>Gets whether the observed user action is still running.</summary>
    internal bool IsBusy => !_action.IsCompleted;

    private readonly CancellationTokenSource _consentCancellation = new();
    private readonly HashSet<Task> _responses = [];
    private Task _startup = Task.CompletedTask;
    private Task _action = Task.CompletedTask;
    private Task? _shutdown;
    private QuickStartArchitecture? _architecture;
    private IUiService? _ui;
    private bool _closing;
    private bool _smoke;
    private bool _smokeStarted;
    private int _exitCode;

    /// <inheritdoc />
    public override void _Ready()
    {
        GetTree().AutoAcceptQuit = false;
        _smoke = OS.GetCmdlineUserArgs().Contains("--quickstart-smoke", StringComparer.Ordinal);
        UiRoot = new GodotUiRoot { Name = "Ui" };
        var dispatcher = new SynchronizationContextGameDispatcher(SynchronizationContext.Current
            ?? throw new InvalidOperationException("Create the host on the Godot thread."));
        _architecture = new QuickStartArchitecture(this, dispatcher);
        _startup = ObserveAsync(() => _architecture.InitializeAsync(_consentCancellation.Token));
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        // Begin self-checks outside the architecture startup callback, which cannot reenter its own shutdown.
        if (!_smoke || _smokeStarted || !_startup.IsCompleted)
            return;
        _smokeStarted = true;
        _ = ObserveSmokeAsync();
    }

    private async Task ObserveSmokeAsync()
    {
        if (_exitCode == 0)
            await ObserveAsync(() => QuickStartSmoke.RunAsync(this)).ConfigureAwait(true);
        await RequestQuitAsync().ConfigureAwait(true);
    }

    /// <summary>Attaches native mounting after resource validation, then opens the configured page.</summary>
    /// <param name="ui">The fully registered UI service.</param>
    internal async Task AttachUiAsync(IUiService ui)
    {
        _ui = ui;
        AddChild(UiRoot);
        await ui.ReplaceAsync(PageRoute, 0, _consentCancellation.Token).ConfigureAwait(true);
    }

    /// <summary>Admits one observed opening action while allowing dialog replies to complete independently.</summary>
    internal void RequestConfirmation()
    {
        if (_closing || !_action.IsCompleted)
            return;
        _action = ObserveAsync(async () =>
        {
            var result = await _ui!.ShowDialogAsync(ConfirmRoute, "Use the registered resource?", _consentCancellation.Token).ConfigureAwait(true);
            GetNode<Label>("Status").Text = result.Accepted && result.Value ? "Confirmed" : "Cancelled";
        });
    }

    /// <summary>Tracks a dialog response without occupying its opener's action boundary.</summary>
    /// <param name="response">The typed completion or dismissal operation.</param>
    internal void RunResponse(Func<Task> response)
    {
        if (_closing)
            return;
        var task = ObserveAsync(response);
        _responses.Add(task);
        _ = RemoveResponseAsync(task);
    }

    private async Task RemoveResponseAsync(Task task)
    {
        await task.ConfigureAwait(true);
        _responses.Remove(task);
    }

    /// <summary>Stops admission, cancels pending consent and joins admitted work before UI service shutdown.</summary>
    internal async Task StopActionsAsync()
    {
        _closing = true;
        await _consentCancellation.CancelAsync().ConfigureAwait(true);
        await _action.ConfigureAwait(true);
        await Task.WhenAll([.. _responses]).ConfigureAwait(true);
    }

    /// <summary>Reclaims native mounting after business stop and waits for deferred deletion.</summary>
    internal async Task DetachUiAsync()
    {
        if (!GodotObject.IsInstanceValid(UiRoot))
            return;
        if (UiRoot.IsInsideTree())
        {
            UiRoot.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        else
            UiRoot.Free();
    }

    /// <summary>Shares the asynchronous close operation and quits after architecture and native cleanup.</summary>
    internal Task RequestQuitAsync() => _shutdown ??= QuitAsync();

    private async Task QuitAsync()
    {
        await _startup.ConfigureAwait(true);
        await ObserveAsync(async () =>
        {
            if (_architecture is not null)
                await _architecture.DestroyAsync().ConfigureAwait(true);
        }).ConfigureAwait(true);
        _consentCancellation.Dispose();
        if (_smoke && _exitCode == 0)
            GD.Print("GFRAMEWORK_QUICKSTART_RESULT PASS");
        GetTree().Quit(_exitCode);
    }

    private async Task ObserveAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(true);
        }
        catch (OperationCanceledException error) when (error.CancellationToken.IsCancellationRequested) { }
        catch (Exception error)
        {
            _exitCode = 1;
            GD.PushError(error.ToString());
            GetNode<Label>("Status").Text = error.Message;
        }
    }

    /// <inheritdoc />
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            _ = RequestQuitAsync();
    }
}
