// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Coroutine;

/// <summary>
///     表示协程异常事件的数据。
///     该类型用于把失败协程的句柄与实际异常一起传递给订阅者。
/// </summary>
public sealed class CoroutineExceptionEventArgs : EventArgs
{
    /// <summary>
    ///     初始化 <see cref="CoroutineExceptionEventArgs" /> 的新实例。
    /// </summary>
    /// <param name="handle">发生异常的协程句柄。</param>
    /// <param name="exception">协程执行过程中抛出的异常。</param>
    /// <exception cref="ArgumentNullException"><paramref name="exception" /> 为 <see langword="null" />。</exception>
    public CoroutineExceptionEventArgs(CoroutineHandle handle, Exception exception)
    {
        Handle = handle;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>
    ///     获取发生异常的协程句柄。
    /// </summary>
    public CoroutineHandle Handle { get; }

    /// <summary>
    ///     获取协程执行过程中抛出的异常。
    /// </summary>
    public Exception Exception { get; }
}
