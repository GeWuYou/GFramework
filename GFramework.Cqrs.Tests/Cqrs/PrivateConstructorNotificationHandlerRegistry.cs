// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     模拟生成注册器使用私有无参构造器的场景，验证运行时仍可通过缓存工厂激活它。
/// </summary>
internal sealed class PrivateConstructorNotificationHandlerRegistry : ICqrsHandlerRegistry
{
    /// <summary>
    ///     初始化一个新的私有生成注册器实例。
    /// </summary>
    private PrivateConstructorNotificationHandlerRegistry()
    {
    }

    /// <summary>
    ///     将测试通知处理器注册到目标服务集合。
    /// </summary>
    /// <param name="services">承载处理器映射的服务集合。</param>
    /// <param name="logger">用于记录注册诊断的日志器。</param>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddTransient(
            typeof(INotificationHandler<GeneratedRegistryNotification>),
            typeof(GeneratedRegistryNotificationHandler));
        logger.Debug(
            $"Registered CQRS handler {typeof(GeneratedRegistryNotificationHandler).FullName} as {typeof(INotificationHandler<GeneratedRegistryNotification>).FullName}.");
    }
}
