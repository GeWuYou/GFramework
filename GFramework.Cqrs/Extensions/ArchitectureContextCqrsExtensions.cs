// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Extensions;

/// <summary>
///     提供 <see cref="IArchitectureContext" /> 的 CQRS 分发扩展方法。
///     这是 Architecture 与 CQRS runtime 之间的明确集成边界：入口先解析当前架构容器中的
///     <see cref="ICqrsRuntime" />，再把架构上下文绑定进分发调用，供 <c>IContextAware</c> 处理器注入。
/// </summary>
/// <remarks>
///     只提供异步入口；同步包装会阻塞调用线程，不再作为推荐调用面维护。
///     上下文包装实例按架构上下文弱缓存复用，不引入全局强引用。
/// </remarks>
public static class ArchitectureContextCqrsExtensions
{
    // 同一架构上下文的分发包装复用同一实例；ConditionalWeakTable 保证架构销毁后包装随之可回收，
    // 不把包装生命周期强加给宿主。
    private static readonly ConditionalWeakTable<IArchitectureContext, ArchitectureCqrsDispatchContext>
        DispatchContextCache = [];

    /// <summary>
    ///     发送请求并返回响应。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="context">当前架构上下文。</param>
    /// <param name="request">要发送的请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>请求响应。</returns>
    /// <exception cref="InvalidOperationException">
    ///     当前架构未安装 CQRS runtime；请先通过 <see cref="CqrsRuntimeModule" /> 或组合根扩展安装。
    /// </exception>
    public static ValueTask<TResponse> SendRequestAsync<TResponse>(
        this IArchitectureContext context,
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        return ResolveRuntime(context).SendAsync(request, cancellationToken, BindContext(context));
    }

    /// <summary>
    ///     发布通知到所有已注册处理器。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="context">当前架构上下文。</param>
    /// <param name="notification">要发布的通知。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示通知分发完成的值任务。</returns>
    public static ValueTask PublishAsync<TNotification>(
        this IArchitectureContext context,
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(notification);

        return ResolveRuntime(context).PublishAsync(notification, cancellationToken, BindContext(context));
    }

    /// <summary>
    ///     创建流式请求的异步响应序列。
    /// </summary>
    /// <typeparam name="TResponse">流元素类型。</typeparam>
    /// <param name="context">当前架构上下文。</param>
    /// <param name="request">流式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按需生成的异步响应序列。</returns>
    public static IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        this IArchitectureContext context,
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        return ResolveRuntime(context).CreateStream(request, cancellationToken, BindContext(context));
    }

    /// <summary>
    ///     发送无返回值请求的便捷入口。
    /// </summary>
    /// <typeparam name="TCommand">请求类型。</typeparam>
    /// <param name="context">当前架构上下文。</param>
    /// <param name="command">要发送的请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示分发完成的值任务。</returns>
    public static async ValueTask SendAsync<TCommand>(
        this IArchitectureContext context,
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : IRequest<Unit>
    {
        ArgumentNullException.ThrowIfNull(command);

        await context.SendRequestAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     发送带返回值请求的便捷入口。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="context">当前架构上下文。</param>
    /// <param name="request">要发送的请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>包含响应结果的值任务。</returns>
    public static ValueTask<TResponse> SendAsync<TResponse>(
        this IArchitectureContext context,
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return context.SendRequestAsync(request, cancellationToken);
    }

    /// <summary>
    ///     从当前架构容器解析 CQRS runtime 单例。
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     容器中未注册 <see cref="ICqrsRuntime" />；请先安装 <see cref="CqrsRuntimeModule" />。
    /// </exception>
    private static ICqrsRuntime ResolveRuntime(IArchitectureContext context)
    {
        return context.GetService<ICqrsRuntime>();
    }

    /// <summary>
    ///     获取当前架构上下文对应的分发包装；同上下文复用同一实例。
    /// </summary>
    private static ArchitectureCqrsDispatchContext BindContext(IArchitectureContext context)
    {
        return DispatchContextCache.GetValue(
            context,
            static architectureContext => new ArchitectureCqrsDispatchContext(architectureContext));
    }
}
