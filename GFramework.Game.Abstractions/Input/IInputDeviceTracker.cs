// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Input;

/// <summary>
///     定义当前活跃输入设备上下文的查询入口。
/// </summary>
public interface IInputDeviceTracker
{
    /// <summary>
    ///     获取当前输入设备上下文。
    /// </summary>
    InputDeviceContext CurrentDevice
    {
        get;
    }
}
