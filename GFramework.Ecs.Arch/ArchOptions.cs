// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Ecs.Arch;

/// <summary>
///     Arch ECS 模块配置选项
/// </summary>
public sealed class ArchOptions
{
    /// <summary>
    ///     World 初始容量（默认：1000）
    /// </summary>
    public int WorldCapacity { get; set; } = 1000;

    /// <summary>
    ///     是否启用统计信息（默认：false）
    /// </summary>
    public bool EnableStatistics { get; set; } = false;

    /// <summary>
    ///     模块优先级（默认：50）
    /// </summary>
    public int Priority { get; set; } = 50;
}