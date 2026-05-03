// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Notification;

/// <summary>
///     通过内部回调桥接 dispatcher 执行逻辑的通知发布上下文。
/// </summary>
/// <typeparam name="TNotification">通知类型。</typeparam>
/// <typeparam name="TState">执行单个处理器所需的内部状态类型。</typeparam>
internal sealed class DelegatingNotificationPublishContext<TNotification, TState> : NotificationPublishContext<TNotification>
    where TNotification : INotification
{
    private readonly NotificationHandlerExecutor<TNotification, TState> _handlerExecutor;
    private readonly TState _state;

    /// <summary>
    ///     初始化一个委托驱动的通知发布上下文。
    /// </summary>
    /// <param name="notification">当前通知。</param>
    /// <param name="handlers">当前发布调用已解析到的处理器集合。</param>
    /// <param name="state">执行处理器时需要的内部状态。</param>
    /// <param name="handlerExecutor">执行单个处理器时调用的内部回调。</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="handlerExecutor" /> 为 <see langword="null" />。
    /// </exception>
    internal DelegatingNotificationPublishContext(
        TNotification notification,
        IReadOnlyList<object> handlers,
        TState state,
        NotificationHandlerExecutor<TNotification, TState> handlerExecutor)
        : base(notification, handlers)
    {
        ArgumentNullException.ThrowIfNull(handlerExecutor);

        _state = state;
        _handlerExecutor = handlerExecutor;
    }

    /// <summary>
    ///     通过默认 dispatcher 提供的内部回调执行单个处理器。
    /// </summary>
    /// <param name="handler">要执行的处理器实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示当前处理器执行完成的值任务。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler" /> 为 <see langword="null" />。</exception>
    public override ValueTask InvokeHandlerAsync(object handler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return _handlerExecutor(handler, Notification, _state, cancellationToken);
    }
}
