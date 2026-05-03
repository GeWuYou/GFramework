// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     由模拟的源码生成注册器显式注册的通知处理器。
/// </summary>
internal sealed class GeneratedRegistryNotificationHandler : INotificationHandler<GeneratedRegistryNotification>
{
    /// <summary>
    ///     处理生成注册器测试中的通知。
    /// </summary>
    /// <param name="notification">通知实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已完成任务。</returns>
    public ValueTask Handle(GeneratedRegistryNotification notification, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
