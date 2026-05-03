// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     记录请求通过管道次数的测试行为。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
public sealed class TrackingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static int _invocationCount;

    /// <summary>
    ///     获取当前测试进程中该请求类型对应的行为触发次数。
    ///     该计数器是按泛型闭包共享的静态状态，测试需要在每次运行前显式重置。
    /// </summary>
    public static int InvocationCount
    {
        get => Volatile.Read(ref _invocationCount);
        set => Volatile.Write(ref _invocationCount, value);
    }

    /// <summary>
    ///     以线程安全方式记录一次行为执行，然后继续执行下一个处理器。
    /// </summary>
    /// <param name="message">当前请求消息。</param>
    /// <param name="next">下一个处理委托。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下游处理器的响应结果。</returns>
    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);
        return await next(message, cancellationToken).ConfigureAwait(false);
    }
}
