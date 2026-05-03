// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     记录缓存 notification binding 复用场景下每次分发注入到 handler 的上下文与实例身份。
/// </summary>
internal sealed class DispatcherNotificationContextRefreshHandler
    : CqrsContextAwareHandlerBase,
        INotificationHandler<DispatcherNotificationContextRefreshNotification>
{
    private readonly int _instanceId = DispatcherNotificationContextRefreshState.AllocateHandlerInstanceId();

    /// <summary>
    ///     记录当前 handler 实例收到的上下文。
    /// </summary>
    /// <param name="notification">当前通知。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已完成任务。</returns>
    public ValueTask Handle(
        DispatcherNotificationContextRefreshNotification notification,
        CancellationToken cancellationToken)
    {
        DispatcherNotificationContextRefreshState.Record(notification.DispatchId, _instanceId, Context);
        return ValueTask.CompletedTask;
    }
}
