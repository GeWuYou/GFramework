using System.ComponentModel;
using GFramework.Core.Abstractions.Cqrs.Command;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Cqrs.Extensions;

namespace GFramework.Core.Extensions;

/// <summary>
///     提供对 <see cref="IContextAware" /> 接口的 CQRS 命令扩展方法。
///     该类型保留旧名称以兼容历史调用点；新代码应改用 <see cref="GFramework.Core.Cqrs.Extensions.ContextAwareCqrsCommandExtensions" />。
///     兼容层计划在未来的 major 版本中移除，因此不会继续承载新能力。
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete(
    "Use GFramework.Core.Cqrs.Extensions.ContextAwareCqrsCommandExtensions instead. This compatibility alias will be removed in a future major version.")]
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
        return ContextAwareCqrsCommandExtensions.SendCommand(contextAware, command);
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
        return ContextAwareCqrsCommandExtensions.SendCommandAsync(
            contextAware,
            command,
            cancellationToken);
    }
}
