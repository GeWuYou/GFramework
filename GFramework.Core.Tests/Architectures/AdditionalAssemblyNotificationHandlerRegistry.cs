// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     模拟由 source-generator 为扩展程序集生成的 CQRS handler registry。
/// </summary>
internal sealed class AdditionalAssemblyNotificationHandlerRegistry : ICqrsHandlerRegistry
{
    /// <summary>
    ///     将扩展程序集中的通知处理器映射写入服务集合。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="logger">日志记录器。</param>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="services" /> 或 <paramref name="logger" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddTransient<INotificationHandler<AdditionalAssemblyNotification>>(_ => CreateHandler());
        logger.Debug(
            $"Registered CQRS handler proxy for {typeof(INotificationHandler<AdditionalAssemblyNotification>).FullName}.");
    }

    /// <summary>
    ///     创建一个仅供显式程序集注册路径使用的动态通知处理器。
    /// </summary>
    /// <returns>用于记录通知触发次数的测试替身处理器。</returns>
    private static INotificationHandler<AdditionalAssemblyNotification> CreateHandler()
    {
        var handler = new Mock<INotificationHandler<AdditionalAssemblyNotification>>();
        handler
            .Setup(target => target.Handle(It.IsAny<AdditionalAssemblyNotification>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                AdditionalAssemblyNotificationHandlerState.RecordInvocation();
                return ValueTask.CompletedTask;
            });
        return handler.Object;
    }
}
