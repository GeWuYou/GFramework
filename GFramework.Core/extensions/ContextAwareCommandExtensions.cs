using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.extensions;

/// <summary>
///     提供对 IContextAware 接口的命令执行扩展方法
/// </summary>
public static class ContextAwareCommandExtensions
{
    /// <summary>
    ///     [Mediator] 发送命令的同步版本（不推荐,仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令对象</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static TResponse SendCommand<TResponse>(this IContextAware contextAware,
        Mediator.ICommand<TResponse> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendCommand(command);
    }

    /// <summary>
    ///     发送一个带返回结果的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static TResult SendCommand<TResult>(this IContextAware contextAware,
        Abstractions.command.ICommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendCommand(command);
    }

    /// <summary>
    ///     发送一个无返回结果的命令
    /// </summary>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static void SendCommand(this IContextAware contextAware, Abstractions.command.ICommand command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        context.SendCommand(command);
    }

    /// <summary>
    ///     [Mediator] 异步发送命令并返回结果
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令对象</param>
    /// <param name="cancellationToken">取消令牌,用于取消操作</param>
    /// <returns>包含命令执行结果的ValueTask</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static ValueTask<TResponse> SendCommandAsync<TResponse>(this IContextAware contextAware,
        Mediator.ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendCommandAsync(command, cancellationToken);
    }

    /// <summary>
    ///     发送并异步执行一个无返回值的命令
    /// </summary>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static async Task SendCommandAsync(this IContextAware contextAware, IAsyncCommand command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        await context.SendCommandAsync(command);
    }

    /// <summary>
    ///     发送并异步执行一个带返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static async Task<TResult> SendCommandAsync<TResult>(this IContextAware contextAware,
        IAsyncCommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return await context.SendCommandAsync(command);
    }
}