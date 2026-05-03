// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Ecs.Arch.Abstractions;

/// <summary>
///     Arch ECS 配置选项
/// </summary>
public sealed class ArchOptions
{
    /// <summary>
    ///     World 初始容量
    /// </summary>
    public int WorldCapacity { get; set; } = 1000;

    /// <summary>
    ///     是否启用统计信息
    /// </summary>
    public bool EnableStatistics { get; set; }

    /// <summary>
    ///     模块优先级
    /// </summary>
    public int Priority { get; set; } = 50;
}