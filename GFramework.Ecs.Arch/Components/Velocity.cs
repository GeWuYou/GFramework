// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace GFramework.Ecs.Arch.Components;

/// <summary>
///     速度结构体，用于表示二维空间中实体的瞬时速度向量
///     包含X轴和Y轴的速度分量，通常用于物理计算和运动系统
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Velocity(float x, float y)
{
    /// <summary>
    ///     X轴速度分量，单位为距离单位/秒
    /// </summary>
    public float X { get; set; } = x;

    /// <summary>
    ///     Y轴速度分量，单位为距离单位/秒
    /// </summary>
    public float Y { get; set; } = y;
}