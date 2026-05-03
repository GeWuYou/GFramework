// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Pool;

/// <summary>
///     对象池统计信息
/// </summary>
public class PoolStatistics
{
    /// <summary>
    ///     池中当前可用对象数量
    /// </summary>
    public int AvailableCount { get; init; }

    /// <summary>
    ///     当前活跃（已获取但未释放）的对象数量
    /// </summary>
    public int ActiveCount { get; init; }

    /// <summary>
    ///     池的最大容量限制，0 表示无限制
    /// </summary>
    public int MaxCapacity { get; init; }

    /// <summary>
    ///     累计创建的对象总数
    /// </summary>
    public int TotalCreated { get; init; }

    /// <summary>
    ///     累计获取对象的次数
    /// </summary>
    public int TotalAcquired { get; init; }

    /// <summary>
    ///     累计释放对象的次数
    /// </summary>
    public int TotalReleased { get; init; }

    /// <summary>
    ///     累计销毁的对象数量（超过容量限制被销毁的对象）
    /// </summary>
    public int TotalDestroyed { get; init; }
}
