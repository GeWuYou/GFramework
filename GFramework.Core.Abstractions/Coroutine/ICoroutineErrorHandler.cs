using System;

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程错误处理器接口，定义协程异常的处理逻辑
/// </summary>
/// <remarks>
///     实现此接口可以自定义协程的异常处理行为
///     错误处理器可以决定异常是否需要传播，或者进行恢复操作
/// </remarks>
public interface ICoroutineErrorHandler
{
    /// <summary>
    ///     处理协程异常
    /// </summary>
    /// <param name="handle">发生异常的协程句柄</param>
    /// <param name="exception">发生的异常</param>
    /// <param name="context">协程上下文</param>
    void HandleException(ICoroutineHandle handle, Exception exception, CoroutineContext context);
}