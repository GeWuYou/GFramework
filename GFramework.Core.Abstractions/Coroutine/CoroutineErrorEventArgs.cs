using System;

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程错误事件参数类，包含协程错误的详细信息
/// </summary>
public class CoroutineErrorEventArgs : EventArgs
{
    /// <summary>
    ///     初始化协程错误事件参数的新实例
    /// </summary>
    /// <param name="handle">发生异常的协程句柄</param>
    /// <param name="exception">发生的异常</param>
    /// <param name="context">协程上下文</param>
    public CoroutineErrorEventArgs(
        ICoroutineHandle handle,
        Exception exception,
        CoroutineContext context)
    {
        Handle = handle;
        Exception = exception;
        Context = context;
        Handled = false;
    }

    /// <summary>
    ///     获取发生异常的协程句柄
    /// </summary>
    public ICoroutineHandle Handle { get; }

    /// <summary>
    ///     获取发生的异常
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    ///     获取协程上下文
    /// </summary>
    public CoroutineContext Context { get; }

    /// <summary>
    ///     获取或设置异常是否已被处理
    /// </summary>
    /// <remarks>
    ///     设置为true可以阻止异常的进一步传播
    /// </remarks>
    public bool Handled { get; set; }
}