using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Coroutine.Extensions;

namespace GFramework.Core.Cqrs.Extensions;

/// <summary>
///     提供 CQRS 命令与协程集成的扩展方法。
///     这些扩展直接走架构上下文的内建 CQRS runtime，不依赖外部 Mediator 服务。
/// </summary>
public static class CqrsCoroutineExtensions
{
    /// <summary>
    ///     以协程方式发送无返回值 CQRS 命令并处理可能的异常。
    /// </summary>
    /// <typeparam name="TCommand">命令类型。</typeparam>
    /// <param name="contextAware">上下文感知对象，用于获取架构上下文。</param>
    /// <param name="command">要发送的命令对象。</param>
    /// <param name="onError">发生异常时的回调处理函数。</param>
    /// <returns>协程枚举器，用于协程执行。</returns>
    public static IEnumerator<IYieldInstruction> SendCommandCoroutine<TCommand>(
        this IContextAware contextAware,
        TCommand command,
        Action<Exception>? onError = null)
        where TCommand : IRequest<Unit>
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var task = contextAware.GetContext().SendAsync(command).AsTask();

        yield return task.AsCoroutineInstruction();

        if (!task.IsFaulted)
            yield break;

        if (onError != null)
            onError.Invoke(task.Exception!);
        else
            throw task.Exception!.InnerException ?? task.Exception;
    }
}
