// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Events;

/// <summary>
///     事件统计信息接口
///     提供事件系统的性能统计数据
/// </summary>
public interface IEventStatistics
{
    /// <summary>
    ///     获取总事件发布数量
    /// </summary>
    long TotalPublished { get; }

    /// <summary>
    ///     获取总事件处理数量（监听器调用次数）
    /// </summary>
    long TotalHandled { get; }

    /// <summary>
    ///     获取总事件处理失败数量
    /// </summary>
    long TotalFailed { get; }

    /// <summary>
    ///     获取当前活跃的事件类型数量
    /// </summary>
    int ActiveEventTypes { get; }

    /// <summary>
    ///     获取当前活跃的监听器总数
    /// </summary>
    int ActiveListeners { get; }

    /// <summary>
    ///     获取指定事件类型的发布次数
    /// </summary>
    /// <param name="eventType">事件类型名称</param>
    /// <returns>发布次数</returns>
    long GetPublishCount(string eventType);

    /// <summary>
    ///     获取指定事件类型的监听器数量
    /// </summary>
    /// <param name="eventType">事件类型名称</param>
    /// <returns>监听器数量</returns>
    int GetListenerCount(string eventType);

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