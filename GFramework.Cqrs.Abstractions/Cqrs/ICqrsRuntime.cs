// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Abstractions.Cqrs;

/// <summary>
///     定义 CQRS runtime 的标准服务解析入口。
///     该抽象把请求分发、通知发布与流式处理从具体实现和宿主中解耦，
///     使 CQRS runtime 可独立于 Architecture 安装到任意标准 DI 容器。
/// </summary>
/// <remarks>
///     <para>推荐调用入口：通过标准 DI 解析 <see cref="ICqrsRuntime" /> 单例后直接调用。
///     需要向 <c>IContextAware</c> 处理器注入宿主上下文时，才在可选的 context 参数
///     传入宿主提供的 <see cref="ICqrsContext" />。</para>
///     <para>实现者与 pipeline 行为不应依赖调用方的 <see cref="SynchronizationContext" />，
///     并应优先在内部异步链路上使用 <c>ConfigureAwait(false)</c>。</para>
/// </remarks>
public interface ICqrsRuntime
{
    /// <summary>
    ///     发送请求并返回响应。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">要分发的请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <param name="context">
    ///     可选的宿主分发上下文；仅在处理链包含需要上下文注入的 <c>IContextAware</c> 对象时必需。
    /// </param>
    /// <returns>请求响应。</returns>
    /// <exception cref="System.ArgumentNullException">
    ///     <paramref name="request" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    ///     未找到对应请求处理器，或处理链中的 <c>IContextAware</c> 对象需要宿主上下文但
    ///      未提供该能力。
    /// </exception>
    ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default,
        ICqrsContext? context = null);

    /// <summary>
    ///     发布通知到所有已注册处理器。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="notification">要发布的通知。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <param name="context">
    ///     可选的宿主分发上下文；仅在处理链包含需要上下文注入的 <c>IContextAware</c> 对象时必需。
    /// </param>
    /// <returns>表示通知分发完成的值任务。</returns>
    /// <exception cref="System.ArgumentNullException">
    ///     <paramref name="notification" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    ///     已解析到的通知处理器需要宿主上下文注入，但  未提供该能力。
    /// </exception>
    /// <remarks>
    ///     默认实现允许零处理器场景静默完成；只有在处理器注入前置条件不满足时才会抛出异常。
    /// </remarks>
    ValueTask PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default,
        ICqrsContext? context = null)
        where TNotification : INotification;

    /// <summary>
    ///     创建流式请求的异步响应序列。
    /// </summary>
    /// <typeparam name="TResponse">流元素类型。</typeparam>
    /// <param name="request">流式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <param name="context">
    ///     可选的宿主分发上下文；仅在处理链包含需要上下文注入的 <c>IContextAware</c> 对象时必需。
    /// </param>
    /// <returns>按需生成的异步响应序列。</returns>
    /// <exception cref="System.ArgumentNullException">
    ///     <paramref name="request" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    ///     未找到对应流式处理器，或处理链中的 <c>IContextAware</c> 对象需要宿主上下文但
    ///      未提供该能力。
    /// </exception>
    /// <remarks>
    ///     返回的异步序列在枚举前已完成处理器解析与上下文注入；
    ///     调用方应把传入的  视为整个枚举生命周期内的必需依赖。
    /// </remarks>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default,
        ICqrsContext? context = null);
}
