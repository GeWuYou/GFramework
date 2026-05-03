// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Notification;

/// <summary>
///     定义默认 CQRS runtime 的通知发布策略。
/// </summary>
/// <remarks>
///     <para>dispatcher 会先解析当前通知对应的处理器集合，再把本次发布上下文交给该抽象决定执行顺序。</para>
///     <para>实现应把 <see cref="NotificationPublishContext{TNotification}.Handlers" /> 视为当前发布调用的瞬时数据，
///     不要跨发布缓存处理器实例或假设它们已经脱离当前上下文。</para>
/// </remarks>
public interface INotificationPublisher
{
    /// <summary>
    ///     执行一次通知发布。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="context">当前发布调用的处理器集合与执行入口，不能为空。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示通知发布完成的值任务。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> 为 <see langword="null" />。</exception>
    ValueTask PublishAsync<TNotification>(
        NotificationPublishContext<TNotification> context,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
