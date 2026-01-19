using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.Coroutine.Errors;

/// <summary>
///     默认协程错误处理器，提供标准的异常处理逻辑
/// </summary>
/// <remarks>
///     默认错误处理器会记录错误日志，并将异常标记为已处理
///     可以通过继承此类来自定义错误处理行为
/// </remarks>
public class DefaultErrorHandler : ICoroutineErrorHandler
{
    private readonly ILogger _logger;

    /// <summary>
    ///     初始化默认错误处理器的新实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public DefaultErrorHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     处理协程异常
    /// </summary>
    /// <param name="handle">发生异常的协程句柄</param>
    /// <param name="exception">发生的异常</param>
    /// <param name="context">协程上下文</param>
    public virtual void HandleException(ICoroutineHandle handle, Exception exception, CoroutineContext context)
    {
        // 记录错误日志
        _logger.Error($"Coroutine '{handle.Name}' failed: {exception.Message}", exception);

        // 标记异常为已处理，防止传播
        // 子类可以重写此行为
    }
}