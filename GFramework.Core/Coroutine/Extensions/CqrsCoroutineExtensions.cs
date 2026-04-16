using System.Runtime.ExceptionServices;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.Rule;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Coroutine.Extensions;

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
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="contextAware" /> 或 <paramref name="command" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    /// <exception cref="TaskCanceledException">
    ///     当底层命令调度被取消且未提供 <paramref name="onError" /> 时抛出。
    /// </exception>
    /// <exception cref="Exception">
    ///     当底层命令调度失败且未提供 <paramref name="onError" /> 时，抛出底层原始异常。
    /// </exception>
    /// <remarks>
    ///     当底层命令调度失败时，该扩展会把底层异常解包后传给 <paramref name="onError" />，
    ///     在取消时则统一暴露 <see cref="TaskCanceledException" />，避免成功、失败与取消三种完成状态被混淆。
    /// </remarks>
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

        if (task.IsCanceled)
        {
            // 取消态与成功态区分：协程层统一映射为 TaskCanceledException。
            var canceledException = new TaskCanceledException(task);
            if (onError != null)
            {
                onError.Invoke(canceledException);
                yield break;
            }

            // 保留原始抛出栈，避免调试时丢失异常来源。
            ExceptionDispatchInfo.Capture(canceledException).Throw();
        }

        if (!task.IsFaulted)
            yield break;
        // 优先解包业务异常，避免直接暴露 AggregateException。
        var exception = task.Exception!.InnerException ?? task.Exception;
        if (onError != null)
            onError.Invoke(exception);
        else
            // 继续保留原始栈信息。
            ExceptionDispatchInfo.Capture(exception).Throw();
    }
}
