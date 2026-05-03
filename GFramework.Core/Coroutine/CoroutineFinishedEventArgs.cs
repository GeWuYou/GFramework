// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine;

/// <summary>
///     表示协程结束事件的数据。
///     该类型统一描述协程完成、取消或失败后的最终结果。
/// </summary>
public sealed class CoroutineFinishedEventArgs : EventArgs
{
    /// <summary>
    ///     初始化 <see cref="CoroutineFinishedEventArgs" /> 的新实例。
    /// </summary>
    /// <param name="handle">已结束的协程句柄。</param>
    /// <param name="completionStatus">协程最终结果。</param>
    /// <param name="exception">若协程以失败结束，则为对应异常；否则为 <see langword="null" />。</param>
    public CoroutineFinishedEventArgs(
        CoroutineHandle handle,
        CoroutineCompletionStatus completionStatus,
        Exception? exception)
    {
        Handle = handle;
        CompletionStatus = completionStatus;
        Exception = exception;
    }

    /// <summary>
    ///     获取已结束的协程句柄。
    /// </summary>
    public CoroutineHandle Handle { get; }

    /// <summary>
    ///     获取协程最终结果。
    /// </summary>
    public CoroutineCompletionStatus CompletionStatus { get; }

    /// <summary>
    ///     获取协程失败时对应的异常对象。
    ///     对于完成或取消结果，该值为 <see langword="null" />。
    /// </summary>
    public Exception? Exception { get; }
}
