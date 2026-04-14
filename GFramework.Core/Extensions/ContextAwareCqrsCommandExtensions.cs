using GFramework.Core.Abstractions.Cqrs.Command;
using GFramework.Core.Abstractions.Rule;

namespace GFramework.Core.Cqrs.Extensions;

/// <summary>
///     提供对 <see cref="IContextAware" /> 接口的 CQRS 命令扩展方法。
/// </summary>
public static class ContextAwareCqrsCommandExtensions
{
    /// <summary>
    ///     发送命令的同步版本（不推荐，仅用于兼容同步调用链）。
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型。</typeparam>
    /// <param name="contextAware">实现 <see cref="IContextAware" /> 接口的对象。</param>
    /// <param name="command">要发送的命令对象。</param>
    /// <returns>命令执行结果。</returns>
    public static TResponse SendCommand<TResponse>(this IContextAware contextAware, ICommand<TResponse> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        return contextAware.GetContext().SendCommand(command);
    }

    /// <summary>
    ///     异步发送命令并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型。</typeparam>
    /// <param name="contextAware">实现 <see cref="IContextAware" /> 接口的对象。</param>
    /// <param name="command">要发送的命令对象。</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作。</param>
    /// <returns>包含命令执行结果的 <see cref="ValueTask{TResult}" />。</returns>
    public static ValueTask<TResponse> SendCommandAsync<TResponse>(
        this IContextAware contextAware,
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        return contextAware.GetContext().SendCommandAsync(command, cancellationToken);
    }
}
