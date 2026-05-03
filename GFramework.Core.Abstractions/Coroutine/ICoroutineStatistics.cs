// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程统计信息接口
///     提供协程执行的性能统计数据
/// </summary>
public interface ICoroutineStatistics
{
    /// <summary>
    ///     获取总协程启动数量
    /// </summary>
    long TotalStarted { get; }

    /// <summary>
    ///     获取总协程完成数量
    /// </summary>
    long TotalCompleted { get; }

    /// <summary>
    ///     获取总协程失败数量
    /// </summary>
    long TotalFailed { get; }

    /// <summary>
    ///     获取当前活跃协程数量
    /// </summary>
    int ActiveCount { get; }

    /// <summary>
    ///     获取当前暂停协程数量
    /// </summary>
    int PausedCount { get; }

    /// <summary>
    ///     获取协程平均执行时间（毫秒）
    /// </summary>
    double AverageExecutionTimeMs { get; }

    /// <summary>
    ///     获取协程最大执行时间（毫秒）
    /// </summary>
    double MaxExecutionTimeMs { get; }

    /// <summary>
    ///     获取按优先级分组的协程数量
    /// </summary>
    /// <param name="priority">协程优先级</param>
    /// <returns>指定优先级的协程数量</returns>
    int GetCountByPriority(CoroutinePriority priority);

    /// <summary>
    ///     获取按标签分组的协程数量
    /// </summary>
    /// <param name="tag">协程标签</param>
    /// <returns>指定标签的协程数量</returns>
    int GetCountByTag(string tag);

    /// <summary>
    ///     重置统计数据
    /// </summary>
    void Reset();

    /// <summary>
    ///     生成统计报告
    /// </summary>
    /// <returns>格式化的统计报告字符串</returns>
    string GenerateReport();
}