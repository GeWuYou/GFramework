// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Input;

/// <summary>
///     描述当前活跃输入设备上下文。
/// </summary>
/// <remarks>
///     初始化一个输入设备上下文。
/// </remarks>
/// <param name="deviceKind">当前设备族。</param>
/// <param name="deviceIndex">设备索引；未知时为 <see langword="null" />。</param>
/// <param name="deviceName">宿主归一化后的设备名称。</param>
public sealed class InputDeviceContext(
    InputDeviceKind deviceKind,
    int? deviceIndex = null,
    string? deviceName = null)
{

    /// <summary>
    ///     获取当前设备族。
    /// </summary>
    public InputDeviceKind DeviceKind { get; } = deviceKind;

    /// <summary>
    ///     获取当前设备索引。
    /// </summary>
    public int? DeviceIndex { get; } = deviceIndex;

    /// <summary>
    ///     获取宿主归一化后的设备名称。
    /// </summary>
    public string DeviceName { get; } = deviceName ?? string.Empty;
}
