// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Notification;

/// <summary>
///     表示默认 dispatcher 执行单个通知处理器时使用的内部回调。
/// </summary>
/// <typeparam name="TNotification">通知类型。</typeparam>
/// <typeparam name="TState">执行当前处理器所需的内部状态类型。</typeparam>
/// <param name="handler">要执行的处理器实例。</param>
/// <param name="notification">当前通知。</param>
/// <param name="state">当前处理器执行所需的内部状态。</param>
/// <param name="cancellationToken">取消令牌。</param>
/// <returns>表示当前处理器执行完成的值任务。</returns>
internal delegate ValueTask NotificationHandlerExecutor<TNotification, in TState>(
    object handler,
    TNotification notification,
    TState state,
    CancellationToken cancellationToken)
    where TNotification : INotification;
