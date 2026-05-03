// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     用于验证“生成注册器 + reflection fallback”组合路径的私有嵌套处理器容器。
/// </summary>
internal sealed class ReflectionFallbackNotificationContainer
{
    /// <summary>
    ///     获取可被直接引用、适合通过 <see cref="Type" /> 元数据补扫的处理器类型。
    /// </summary>
    /// <returns>
    ///     可被生成注册器直接引用的 fallback 处理器类型，用于验证 runtime 会优先消费 <see cref="Type" /> 元数据。
    /// </returns>
    public static Type DirectFallbackHandlerType => typeof(DirectFallbackGeneratedRegistryNotificationHandler);

    /// <summary>
    ///     获取仅能通过反射补扫接入的私有嵌套处理器类型。
    /// </summary>
    public static Type ReflectionOnlyHandlerType => typeof(ReflectionOnlyGeneratedRegistryNotificationHandler);

    private sealed class DirectFallbackGeneratedRegistryNotificationHandler
        : INotificationHandler<GeneratedRegistryNotification>
    {
        /// <summary>
        ///     处理测试通知。
        /// </summary>
        /// <param name="notification">通知实例。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>已完成任务。</returns>
        public ValueTask Handle(GeneratedRegistryNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ReflectionOnlyGeneratedRegistryNotificationHandler
        : INotificationHandler<GeneratedRegistryNotification>
    {
        /// <summary>
        ///     处理测试通知。
        /// </summary>
        /// <param name="notification">通知实例。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>已完成任务。</returns>
        public ValueTask Handle(GeneratedRegistryNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
