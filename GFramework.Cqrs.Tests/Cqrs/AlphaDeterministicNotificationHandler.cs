// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     名称排序上应先于 Zeta 处理器执行的通知处理器。
/// </summary>
internal sealed class AlphaDeterministicNotificationHandler : INotificationHandler<DeterministicOrderNotification>
{
    /// <summary>
    ///     记录当前处理器已执行。
    /// </summary>
    /// <param name="notification">通知实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已完成任务。</returns>
    public ValueTask Handle(DeterministicOrderNotification notification, CancellationToken cancellationToken)
    {
        DeterministicNotificationHandlerState.InvocationOrder.Add(nameof(AlphaDeterministicNotificationHandler));
        return ValueTask.CompletedTask;
    }
}
