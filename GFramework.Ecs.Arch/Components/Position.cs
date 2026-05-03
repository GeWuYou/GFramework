// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace GFramework.Ecs.Arch.Components;

/// <summary>
/// 位置组件，用于表示实体在二维空间中的坐标位置。
/// </summary>
/// <param name="x">X轴坐标值</param>
/// <param name="y">Y轴坐标值</param>
[StructLayout(LayoutKind.Sequential)]
public struct Position(float x, float y)
{
    /// <summary>
    /// 获取X轴坐标值。
    /// </summary>
    public float X { get; set; } = x;

    /// <summary>
    /// 获取Y轴坐标值。
    /// </summary>
    public float Y { get; set; } = y;
}