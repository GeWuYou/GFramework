// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;

namespace GFramework.Godot.UI;

/// <summary>Owns a Godot Control instance and preserves its authored input settings across activation changes.</summary>
/// <remarks>The UI service disposes this lease on the Godot thread. Scene scripts never need a page-provider interface.</remarks>
public sealed class GodotUiView : IUiView, IUiActionTarget
{
    private readonly Dictionary<Control, (Control.MouseFilterEnum Mouse, Control.FocusModeEnum Focus)> _input = new();
    private readonly Node.ProcessModeEnum _processMode;
    private bool _disposed;
    private bool _active;

    /// <summary>Creates an owned lease around a detached scene.</summary>
    /// <param name="control">Detached scene instance, transferred to this lease.</param>
    /// <param name="options">Registered route policy.</param>
    public GodotUiView(Control control, UiRouteOptions options)
    {
        Control = control ?? throw new ArgumentNullException(nameof(control));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _processMode = control.ProcessMode;
        control.Hide();
    }

    /// <summary>Gets the owned native view for mounting and native focus handling.</summary>
    public Control Control { get; }
    /// <summary>Gets the route policy shared with the service.</summary>
    public UiRouteOptions Options { get; }
    /// <summary>Gets or sets the host-owned mount, including any modal shield.</summary>
    internal Control? Mount { get; set; }
    /// <inheritdoc />
    public bool IsAlive => !_disposed && GodotObject.IsInstanceValid(Control) && !Control.IsQueuedForDeletion();
    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Control.ProcessMode = Options.ContinueProcessingWhenPaused ? Node.ProcessModeEnum.Always : _processMode;
        return Control is IGodotUiLifecycle lifecycle ? lifecycle.OpenAsync(cancellationToken) : Task.CompletedTask;
    }
    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) =>
        Control is IGodotUiLifecycle lifecycle ? lifecycle.CloseAsync(cancellationToken) : Task.CompletedTask;
    /// <inheritdoc />
    public void SetVisible(bool visible)
    {
        if (!IsAlive) return;
        Control.Visible = visible;
        if (Mount is not null && GodotObject.IsInstanceValid(Mount)) Mount.Visible = visible;
    }
    /// <inheritdoc />
    public void SetActive(bool active)
    {
        if (!IsAlive) return;
        var changed = _active != active;
        _active = active;
        if (active)
        {
            foreach (var pair in _input)
            {
                if (!GodotObject.IsInstanceValid(pair.Key)) continue;
                pair.Key.MouseFilter = pair.Value.Mouse;
                pair.Key.FocusMode = pair.Value.Focus;
            }
            _input.Clear();
            if (changed && Control is IGodotUiActivation activation) activation.OnActivated();
        }
        else
        {
            foreach (var item in EnumerateControls(Control))
            {
                if (!_input.ContainsKey(item)) _input[item] = (item.MouseFilter, item.FocusMode);
                item.MouseFilter = Control.MouseFilterEnum.Ignore;
                item.FocusMode = Control.FocusModeEnum.None;
                if (item.IsInsideTree()) item.ReleaseFocus();
            }
            if (changed && Control is IGodotUiActivation activation) activation.OnDeactivated();
        }
    }
    /// <inheritdoc />
    public Task<bool> HandleActionAsync(UiInputAction action, CancellationToken cancellationToken = default) =>
        Control is IUiActionTarget target ? target.HandleActionAsync(action, cancellationToken) : Task.FromResult(false);
    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        _input.Clear();
        if (GodotObject.IsInstanceValid(Control) && !Control.IsQueuedForDeletion()) Control.QueueFree();
        return ValueTask.CompletedTask;
    }
    /// <summary>Visits native controls without changing their authored processing flags.</summary>
    internal static IEnumerable<Control> EnumerateControls(Node node)
    {
        if (node is Control control) yield return control;
        foreach (var child in node.GetChildren())
            foreach (var descendant in EnumerateControls(child)) yield return descendant;
    }
}
