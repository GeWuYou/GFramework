// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Abstractions.Cqrs;

/// <summary>
///     定义架构上下文使用的 CQRS runtime seam。
///     该抽象把请求分发、通知发布与流式处理从具体实现中解耦，
///     使 CQRS runtime 契约可独立归属到 <c>GFramework.Cqrs.Abstractions</c>。
/// </summary>
public interface ICqrsRuntime
{
    /// <summary>
    ///     发送请求并返回响应。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="context">当前 CQRS 分发上下文。</param>
    /// <param name="request">要分发的请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>请求响应。</returns>
    /// <exception cref="System.ArgumentNullException">
    ///     <paramref name="context" /> 或 <paramref name="request" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    ///     当前上下文无法满足运行时要求，例如未找到对应请求处理器，或请求处理链中的
    ///     <c>IContextAware</c> 对象需要 <c>IArchitectureContext</c> 但当前 <paramref name="context" /> 不提供该能力。
    /// </exception>
    /// <remarks>
    ///     该契约允许调用方传入任意 <see cref="ICqrsContext" />，
    ///     但默认运行时在需要向处理器或行为注入框架上下文时，仍要求该上下文同时实现 <c>IArchitectureContext</c>。
    ///     为了兼容 legacy 同步入口，<c>ArchitectureContext</c>、<c>QueryExecutor</c> 与 <c>CommandExecutor</c>
    ///     可能会在后台线程上同步等待该异步结果；实现者与 pipeline 行为不应依赖调用方的
    ///     <see cref="SynchronizationContext" />，并应优先在内部异步链路上使用 <c>ConfigureAwait(false)</c>。
    /// </remarks>
    ValueTask<TResponse> SendAsync<TResponse>(
        ICqrsContext context,
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     发布通知到所有已注册处理器。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="context">当前 CQRS 分发上下文。</param>
    /// <param name="notification">要发布的通知。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示通知分发完成的值任务。</returns>
    /// <exception cref="System.ArgumentNullException">
    ///     <paramref name="context" /> 或 <paramref name="notification" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    ///     已解析到的通知处理器需要框架级上下文注入，但当前 <paramref name="context" /> 不提供
    ///     <c>IArchitectureContext</c> 能力。
    /// </exception>
    /// <remarks>
    ///     默认实现允许零处理器场景静默完成；只有在处理器注入前置条件不满足时才会抛出异常。
    /// </remarks>
    ValueTask PublishAsync<TNotification>(
        ICqrsContext context,
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    ///     创建流式请求的异步响应序列。
    /// </summary>
    /// <typeparam name="TResponse">流元素类型。</typeparam>
    /// <param name="context">当前 CQRS 分发上下文。</param>
    /// <param name="request">流式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按需生成的异步响应序列。</returns>
    /// <exception cref="System.ArgumentNullException">
    ///     <paramref name="context" /> 或 <paramref name="request" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    ///     当前上下文无法满足运行时要求，例如未找到对应流式处理器，或流式处理链中的
    ///     <c>IContextAware</c> 对象需要 <c>IArchitectureContext</c> 但当前 <paramref name="context" /> 不提供该能力。
    /// </exception>
    /// <remarks>
    ///     返回的异步序列在枚举前通常已完成处理器解析与上下文注入，
    ///     因此调用方应把 <paramref name="context" /> 视为整个枚举生命周期内的必需依赖。
    /// </remarks>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        ICqrsContext context,
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
