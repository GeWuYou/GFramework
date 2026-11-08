// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Ecs.Arch.Abstractions;

/// <summary>
///     Arch ECS 配置选项
/// </summary>
public sealed class ArchOptions
{
    /// <summary>
    ///     World 初始 archetype 容量
    /// </summary>
    public int WorldCapacity { get; set; } = 1000;

    /// <summary>
    ///     是否启用宿主线程采集的帧和顶层系统统计；默认关闭
    /// </summary>
    public bool EnableStatistics
    {
        get; set;
    }

    /// <summary>
    ///     Architecture 中运行时的启动顺序；系统排序由 IPrioritized 决定
    /// </summary>
    public int Priority { get; set; } = 50;
}
