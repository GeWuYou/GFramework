// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     记录流式请求通过管道次数的测试行为。
/// </summary>
/// <typeparam name="TRequest">流式请求类型。</typeparam>
/// <typeparam name="TResponse">流式响应元素类型。</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0018",
    Justification = "Static tracking state is intentionally isolated per closed stream request/response type in this fixture.")]
public sealed class TrackingStreamPipelineBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    private static int _invocationCount;

    /// <summary>
    ///     获取当前测试进程中该流式请求类型对应的行为触发次数。
    ///     该计数器是按泛型闭包共享的静态状态，测试需要在每次运行前显式重置。
    /// </summary>
    public static int InvocationCount
    {
        get => Volatile.Read(ref _invocationCount);
        set => Volatile.Write(ref _invocationCount, value);
    }

    /// <summary>
    ///     以线程安全方式记录一次行为执行，然后继续执行下一个处理阶段。
    /// </summary>
    /// <param name="message">当前流式请求消息。</param>
    /// <param name="next">下一个处理阶段。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下游处理阶段返回的异步流。</returns>
    public IAsyncEnumerable<TResponse> Handle(
        TRequest message,
        StreamMessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);
        return next(message, cancellationToken);
    }
}
