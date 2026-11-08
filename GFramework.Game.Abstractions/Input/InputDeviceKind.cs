// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Input;

/// <summary>
///     描述框架级输入设备族。
/// </summary>
/// <remarks>
///     该枚举用于跨宿主共享“当前输入来自哪一类设备”的语义。
///     它故意避免暴露 Godot、Unity 或平台 SDK 的原生事件类型，确保上层业务只依赖稳定的设备族判断。
/// </remarks>
public enum InputDeviceKind
{
    /// <summary>
    ///     未识别或尚未产生任何输入。
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     键盘与鼠标输入。
    /// </summary>
    KeyboardMouse = 1,

    /// <summary>
    ///     游戏手柄输入。
    /// </summary>
    Gamepad = 2,

    /// <summary>
    ///     触摸输入。
    /// </summary>
    Touch = 3,
}
