// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Input;

/// <summary>
///     描述一个逻辑绑定使用的物理输入类型。
/// </summary>
public enum InputBindingKind
{
    /// <summary>
    ///     未指定。
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     键盘按键。
    /// </summary>
    Key = 1,

    /// <summary>
    ///     鼠标按钮。
    /// </summary>
    MouseButton = 2,

    /// <summary>
    ///     手柄按钮。
    /// </summary>
    GamepadButton = 3,

    /// <summary>
    ///     手柄轴向。
    /// </summary>
    GamepadAxis = 4,
}
