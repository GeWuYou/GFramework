// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Extensions;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Extensions;

namespace GFramework.Godot.Coroutine;

/// <summary>
///     提供协程相关的扩展方法，用于简化协程的启动和管理。
/// </summary>
public static class ContextAwareCoroutineExtensions
{
    /// <summary>
    /// 发送无返回值请求并直接以协程方式运行
    /// </summary>
    /// <typeparam name="TCommand">请求类型，必须是无返回值请求</typeparam>
    /// <param name="contextAware">上下文感知对象，用于发送请求</param>
    /// <param name="command">要发送的请求对象</param>
    /// <param name="segment">协程运行的时间段，默认为 Process</param>
    /// <param name="tag">协程的标签，可用于标识或分组协程</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>返回协程的句柄，可用于后续操作（如停止协程）</returns>
    public static CoroutineHandle RunRequestCoroutine<TCommand>(
        this IContextAware contextAware,
        TCommand command,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
        where TCommand : IRequest<Unit>
    {
        return Timing.RunCoroutine(context => ContextAwareCqrsExtensions
            .SendAsync(contextAware, command, context.CancellationToken)
            .AsTask().ToCoroutineEnumerator(), segment, tag, cancellationToken);
    }

    /// <summary>
    /// 发送请求并直接以协程方式运行（带返回值）
    /// </summary>
    /// <typeparam name="TResponse">请求返回值的类型</typeparam>
    /// <param name="contextAware">上下文感知对象，用于发送请求</param>
    /// <param name="request">要发送的请求对象</param>
    /// <param name="segment">协程运行的时间段，默认为 Process</param>
    /// <param name="tag">协程的标签，可用于标识或分组协程</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>返回协程的句柄，可用于后续操作（如停止协程）</returns>
    public static CoroutineHandle RunRequestCoroutine<TResponse>(
        this IContextAware contextAware,
        IRequest<TResponse> request,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return Timing.RunCoroutine(context => ContextAwareCqrsExtensions
            .SendAsync(contextAware, request, context.CancellationToken)
            .AsTask().ToCoroutineEnumerator(), segment, tag, cancellationToken);
    }

    /// <summary>
    /// 发布通知并直接以协程方式运行
    /// </summary>
    /// <param name="contextAware">上下文感知对象，用于发布通知</param>
    /// <param name="notification">要发布的通知对象</param>
    /// <param name="segment">协程运行的时间段，默认为 Process</param>
    /// <param name="tag">协程的标签，可用于标识或分组协程</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>返回协程的句柄，可用于后续操作（如停止协程）</returns>
    public static CoroutineHandle RunPublishCoroutine(
        this IContextAware contextAware,
        INotification notification,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return Timing.RunCoroutine(context => ContextAwareCqrsExtensions
            .PublishAsync(contextAware, notification, context.CancellationToken)
            .AsTask().ToCoroutineEnumerator(), segment, tag, cancellationToken);
    }

    /// <summary>
    /// 发送流式请求并直接以协程方式运行（协程在完整枚举响应流后结束）
    /// </summary>
    /// <typeparam name="TResponse">流元素类型</typeparam>
    /// <param name="contextAware">上下文感知对象，用于发送流式请求</param>
    /// <param name="request">要发送的流式请求对象</param>
    /// <param name="segment">协程运行的时间段，默认为 Process</param>
    /// <param name="tag">协程的标签，可用于标识或分组协程</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>返回协程的句柄，可用于后续操作（如停止协程）</returns>
    /// <remarks>
    ///     流元素不逐个映射为协程帧；协程只等待整个响应流被消费完毕，需要逐元素处理时请自行迭代
    ///     <c>CreateStream</c> 返回的异步序列。
    /// </remarks>
    public static CoroutineHandle RunStreamCoroutine<TResponse>(
        this IContextAware contextAware,
        IStreamRequest<TResponse> request,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return Timing.RunCoroutine(context => EnumerateStreamAsync(contextAware, request, context.CancellationToken)
            .ToCoroutineEnumerator(), segment, tag, cancellationToken);
    }

    /// <summary>
    ///     把流式请求排空为单个 Task，供协程桥接；避免为每个流元素引入独立的 yield 指令。
    /// </summary>
    private static async Task EnumerateStreamAsync<TResponse>(
        IContextAware contextAware,
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        await foreach (var _ in contextAware.CreateStream(request, cancellationToken).ConfigureAwait(false))
        {
            // 仅为等待响应流消费完毕，不处理单个流元素。
        }
    }
}
