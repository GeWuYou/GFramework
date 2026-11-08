// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Input;
using GFramework.Game.Input;

namespace GFramework.Godot.Input;

/// <summary>
///     提供基于 Godot `InputMap` 的输入绑定存储实现。
/// </summary>
/// <remarks>
///     该类型把 Godot 原生 `InputEvent` / `InputMap` 适配到 `GFramework.Game.Abstractions.Input` 契约。
///     项目可以直接用它做重绑定、动作快照导出导入，以及“当前活跃设备”识别。
/// </remarks>
public sealed class GodotInputBindingStore : IInputBindingStore, IInputDeviceTracker
{
    private readonly IGodotInputMapBackend _backend;
    private readonly InputBindingStore _state;
    private readonly InputDeviceTracker _deviceTracker;

    /// <summary>
    ///     初始化一个基于全局 `InputMap` 的输入绑定存储。
    /// </summary>
    public GodotInputBindingStore()
        : this(new GodotInputMapBackend())
    {
    }

    /// <summary>
    ///     初始化一个可测试的输入绑定存储。
    /// </summary>
    /// <param name="backend">要使用的 `InputMap` 后端。</param>
    internal GodotInputBindingStore(IGodotInputMapBackend backend)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _state = new InputBindingStore(CaptureSnapshotFromBackend());
        _deviceTracker = new InputDeviceTracker();
    }

    /// <inheritdoc />
    public InputDeviceContext CurrentDevice => _deviceTracker.CurrentDevice;

    /// <inheritdoc />
    public InputActionBinding GetBindings(string actionName)
    {
        ReloadFromBackend();
        return _state.GetBindings(actionName);
    }

    /// <inheritdoc />
    public InputBindingSnapshot ExportSnapshot()
    {
        ReloadFromBackend();
        return _state.ExportSnapshot();
    }

    /// <inheritdoc />
    public void ImportSnapshot(InputBindingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        ReloadFromBackend();
        var snapshotActionNames = snapshot.Actions
            .Select(static action => action.ActionName)
            .ToHashSet(StringComparer.Ordinal);
        var removedActionNames = _state.ExportSnapshot().Actions
            .Select(static action => action.ActionName)
            .Where(actionName => !snapshotActionNames.Contains(actionName))
            .ToArray();

        foreach (var actionName in removedActionNames)
        {
            _backend.SetBindings(actionName, []);
        }

        foreach (var action in snapshot.Actions)
        {
            ApplyActionBindings(action);
        }

        ReloadFromBackend();
    }

    /// <inheritdoc />
    public void SetPrimaryBinding(string actionName, InputBindingDescriptor binding, bool swapIfTaken = true)
    {
        ReloadFromBackend();
        _state.SetPrimaryBinding(actionName, binding, swapIfTaken);
        ApplySnapshot(_state.ExportSnapshot());
    }

    /// <inheritdoc />
    public void ResetAction(string actionName)
    {
        _backend.ResetAction(actionName);
        ReloadFromBackend();
    }

    /// <inheritdoc />
    public void ResetAll()
    {
        _backend.ResetAll();
        ReloadFromBackend();
    }

    /// <summary>
    ///     使用 Godot 原生输入事件更新当前活跃设备上下文。
    /// </summary>
    /// <param name="inputEvent">原生输入事件。</param>
    public void UpdateDeviceFromInput(InputEvent inputEvent)
    {
        var context = GodotInputBindingCodec.GetDeviceContext(inputEvent);
        _deviceTracker.Update(context);
    }

    private void ApplyActionBindings(InputActionBinding actionBinding)
    {
        _backend.SetBindings(actionBinding.ActionName, actionBinding.Bindings);
    }

    private void ApplySnapshot(InputBindingSnapshot snapshot)
    {
        foreach (var actionBinding in snapshot.Actions)
        {
            ApplyActionBindings(actionBinding);
        }
    }

    private InputBindingSnapshot CaptureSnapshotFromBackend()
    {
        var actions = _backend.GetActionNames()
            .Select(CreateActionBinding)
            .ToArray();

        return new InputBindingSnapshot(actions);
    }

    private InputActionBinding CreateActionBinding(string actionName)
    {
        return new InputActionBinding(actionName, [.. _backend.GetBindings(actionName)]);
    }

    private void ReloadFromBackend()
    {
        _state.ImportSnapshot(CaptureSnapshotFromBackend());
    }
}
