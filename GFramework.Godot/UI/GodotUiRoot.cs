// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using GFramework.Game.Abstractions.UI;

namespace GFramework.Godot.UI;

/// <summary>Default native UI host. Visual layers, modal shields and focus are mounted beneath this node.</summary>
/// <remarks>Attach it before constructing UiService. Stop the service before freeing this root.</remarks>
public partial class GodotUiRoot : Node, IUiHost
{
    private readonly Dictionary<int, CanvasLayer> _layers = [];
    private readonly Dictionary<IUiView, Control> _mounts = [];

    /// <inheritdoc />
    public void Attach(IUiView view, int layer, long order)
    {
        if (view is not GodotUiView native)
            throw new ArgumentException("GodotUiRoot requires a GodotUiView lease.", nameof(view));
        if (_mounts.TryGetValue(view, out var existing))
        {
            // Singleton reuse updates native child order as well as the service's logical input order.
            existing.GetParent().MoveChild(existing, -1);
            return;
        }
        if (!_layers.TryGetValue(layer, out var canvas))
        {
            canvas = new CanvasLayer { Name = "UiLayer" + layer.ToString(CultureInfo.InvariantCulture), Layer = layer };
            AddChild(canvas);
            _layers.Add(layer, canvas);
        }
        var mount = new Control { Name = "View" + order.ToString(CultureInfo.InvariantCulture), MouseFilter = Control.MouseFilterEnum.Ignore, ProcessMode = ProcessModeEnum.Always };
        mount.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        mount.Hide();
        native.Mount = mount;
        _mounts.Add(view, mount);
        canvas.AddChild(mount);
        if (native.Options.Modal)
        {
            var shield = new ColorRect { Name = "ModalShield", Color = new Color(0, 0, 0, 0.55f), MouseFilter = Control.MouseFilterEnum.Stop };
            shield.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            mount.AddChild(shield);
        }
        mount.AddChild(native.Control);
    }
    /// <inheritdoc />
    public void Detach(IUiView view)
    {
        if (!_mounts.Remove(view, out var mount))
            return;
        if (view is GodotUiView owned)
            owned.Mount = null;
        if (view is GodotUiView native && GodotObject.IsInstanceValid(native.Control) && native.Control.GetParent() == mount)
            mount.RemoveChild(native.Control);
        if (GodotObject.IsInstanceValid(mount) && !mount.IsQueuedForDeletion())
            mount.QueueFree();
    }
    /// <inheritdoc />
    public object? CaptureFocus() => GetViewport().GuiGetFocusOwner();
    /// <inheritdoc />
    public void Focus(IUiView view)
    {
        if (view is not GodotUiView native || !native.IsAlive)
            return;
        var target = GodotUiView.EnumerateControls(native.Control).FirstOrDefault(control =>
            control.FocusMode == Control.FocusModeEnum.All && control.IsVisibleInTree() && control is not BaseButton { Disabled: true });
        target?.GrabFocus();
    }
    /// <inheritdoc />
    public void RestoreFocus(object? bookmark)
    {
        if (bookmark is Control control && GodotObject.IsInstanceValid(control) && control.IsInsideTree() && control.IsVisibleInTree() && control.FocusMode != Control.FocusModeEnum.None)
            control.GrabFocus();
    }
}
