// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Notification;

/// <summary>
///     表示一次通知发布调用的执行上下文。
/// </summary>
/// <typeparam name="TNotification">通知类型。</typeparam>
/// <remarks>
///     该上下文把“当前通知”“已解析处理器集合”和“执行单个处理器”的入口收敛到同一对象中，
///     使发布策略只需决定遍历、排序或并发方式，而无需了解 dispatcher 的上下文注入细节。
/// </remarks>
public abstract class NotificationPublishContext<TNotification>
    where TNotification : INotification
{
    /// <summary>
    ///     初始化一次通知发布上下文。
    /// </summary>
    /// <param name="notification">当前通知。</param>
    /// <param name="handlers">当前发布调用已解析到的处理器集合。</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="notification" /> 或 <paramref name="handlers" /> 为 <see langword="null" />。
    /// </exception>
    protected NotificationPublishContext(TNotification notification, IReadOnlyList<object> handlers)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(handlers);

        Notification = notification;
        Handlers = handlers;
    }

    /// <summary>
    ///     获取当前要发布的通知。
    /// </summary>
    public TNotification Notification { get; }

    /// <summary>
    ///     获取当前发布调用已解析到的处理器集合。
    /// </summary>
    public IReadOnlyList<object> Handlers { get; }

    /// <summary>
    ///     执行单个通知处理器。
    /// </summary>
    /// <param name="handler">要执行的处理器实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示当前处理器执行完成的值任务。</returns>
    public abstract ValueTask InvokeHandlerAsync(object handler, CancellationToken cancellationToken);
}
