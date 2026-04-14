using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Cqrs.Command;

namespace GFramework.Core.Extensions;

/// <summary>
///     提供对 IContextAware 接口的 CQRS 命令扩展方法。
/// </summary>
public static class ContextAwareMediatorCommandExtensions
{
    /// <summary>
    ///     发送命令的同步版本（不推荐,仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令对象</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static TResponse SendCommand<TResponse>(this IContextAware contextAware,
        ICommand<TResponse> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendCommand(command);
    }

    /// <summary>
    ///     异步发送命令并返回结果
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令对象</param>
    /// <param name="cancellationToken">取消令牌,用于取消操作</param>
    /// <returns>包含命令执行结果的ValueTask</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static ValueTask<TResponse> SendCommandAsync<TResponse>(this IContextAware contextAware,
        ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendCommandAsync(command, cancellationToken);
    }
}
