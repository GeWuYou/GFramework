// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Input;

/// <summary>
///     描述一个框架无关的动作绑定。
/// </summary>
/// <remarks>
///     该模型是运行时输入系统与宿主适配层之间的稳定交换格式。
///     宿主层负责把原生输入事件转成此描述，抽象层和默认运行时只根据这些字段做查询、冲突检测和持久化。
/// </remarks>
public sealed class InputBindingDescriptor
{
    /// <summary>
    ///     初始化一个动作绑定描述。
    /// </summary>
    /// <param name="deviceKind">设备族。</param>
    /// <param name="bindingKind">绑定类型。</param>
    /// <param name="code">宿主无关的物理码值。</param>
    /// <param name="displayName">用于设置界面展示的名称。</param>
    /// <param name="axisDirection">轴向方向；非轴向绑定时为 <see langword="null" />。</param>
    /// <exception cref="ArgumentException">当 <paramref name="code" /> 为空时抛出。</exception>
    public InputBindingDescriptor(
        InputDeviceKind deviceKind,
        InputBindingKind bindingKind,
        string code,
        string displayName,
        float? axisDirection = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Binding code cannot be null or whitespace.", nameof(code));
        }

        DeviceKind = deviceKind;
        BindingKind = bindingKind;
        Code = code;
        DisplayName = displayName ?? string.Empty;
        AxisDirection = axisDirection;
    }

    /// <summary>
    ///     获取设备族。
    /// </summary>
    public InputDeviceKind DeviceKind
    {
        get;
    }

    /// <summary>
    ///     获取绑定类型。
    /// </summary>
    public InputBindingKind BindingKind
    {
        get;
    }

    /// <summary>
    ///     获取宿主无关的物理码值。
    /// </summary>
    public string Code
    {
        get;
    }

    /// <summary>
    ///     获取用于展示的标签。
    /// </summary>
    public string DisplayName
    {
        get;
    }

    /// <summary>
    ///     获取轴向方向。
    /// </summary>
    public float? AxisDirection
    {
        get;
    }
}
