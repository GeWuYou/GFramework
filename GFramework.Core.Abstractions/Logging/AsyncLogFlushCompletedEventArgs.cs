// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Logging;

/// <summary>
///     表示异步日志刷新完成事件的数据。
///     该类型用于告知订阅者本次刷新是否在超时时间内成功完成。
/// </summary>
public sealed class AsyncLogFlushCompletedEventArgs : EventArgs
{
    /// <summary>
    ///     初始化 <see cref="AsyncLogFlushCompletedEventArgs" /> 的新实例。
    /// </summary>
    /// <param name="success">
    ///     刷新是否成功完成。
    ///     为 <see langword="true" /> 表示所有待处理日志都已在超时前落地；
    ///     为 <see langword="false" /> 表示刷新超时或输出器已不可用。
    /// </param>
    public AsyncLogFlushCompletedEventArgs(bool success)
    {
        Success = success;
    }

    /// <summary>
    ///     获取刷新是否成功完成。
    /// </summary>
    public bool Success { get; }
}
