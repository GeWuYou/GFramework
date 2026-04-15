using System.ComponentModel;
using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Cqrs.Extensions;

namespace GFramework.Core.Extensions;

/// <summary>
///     提供对 <see cref="IContextAware" /> 接口的 CQRS 统一接口扩展方法。
///     该类型保留旧名称以兼容历史调用点；新代码应改用 <see cref="GFramework.Core.Cqrs.Extensions.ContextAwareCqrsExtensions" />。
///     兼容层计划在未来的 major 版本中移除，因此不会继续承载新能力。
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete(
    "Use GFramework.Core.Cqrs.Extensions.ContextAwareCqrsExtensions instead. This compatibility alias will be removed in a future major version.")]
public static class ContextAwareMediatorExtensions
{
    /// <summary>
    ///     发送请求（统一处理 Command/Query）
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="request">要发送的请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>请求结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 request 为 null 时抛出</exception>
    public static ValueTask<TResponse> SendRequestAsync<TResponse>(this IContextAware contextAware,
        IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        return ContextAwareCqrsExtensions.SendRequestAsync(
            contextAware,
            request,
            cancellationToken);
    }

    /// <summary>
    ///     发送请求（同步版本,不推荐）
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="request">要发送的请求</param>
    /// <returns>请求结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 request 为 null 时抛出</exception>
    public static TResponse SendRequest<TResponse>(this IContextAware contextAware,
        IRequest<TResponse> request)
    {
        return ContextAwareCqrsExtensions.SendRequest(contextAware, request);
    }

    /// <summary>
    ///     发布通知（一对多事件）
    /// </summary>
    /// <typeparam name="TNotification">通知类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="notification">要发布的通知</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 notification 为 null 时抛出</exception>
    public static ValueTask PublishAsync<TNotification>(this IContextAware contextAware,
        TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        return ContextAwareCqrsExtensions.PublishAsync(
            contextAware,
            notification,
            cancellationToken);
    }

    /// <summary>
    ///     创建流式请求（用于大数据集）
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="request">流式请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步响应流</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 request 为 null 时抛出</exception>
    public static IAsyncEnumerable<TResponse> CreateStream<TResponse>(this IContextAware contextAware,
        IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        return ContextAwareCqrsExtensions.CreateStream(
            contextAware,
            request,
            cancellationToken);
    }

    /// <summary>
    ///     发送命令（无返回值）
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static ValueTask SendAsync<TCommand>(this IContextAware contextAware, TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : IRequest<Unit>
    {
        return ContextAwareCqrsExtensions.SendAsync(
            contextAware,
            command,
            cancellationToken);
    }

    /// <summary>
    ///     发送命令（有返回值）
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static ValueTask<TResponse> SendAsync<TResponse>(this IContextAware contextAware,
        IRequest<TResponse> command, CancellationToken cancellationToken = default)
    {
        return ContextAwareCqrsExtensions.SendAsync(
            contextAware,
            command,
            cancellationToken);
    }
}
