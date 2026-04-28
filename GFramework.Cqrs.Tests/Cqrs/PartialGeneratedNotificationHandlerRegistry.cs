using System;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     模拟局部生成注册器场景中，仅注册“可由生成代码直接引用”的那部分 handlers。
/// </summary>
internal sealed class PartialGeneratedNotificationHandlerRegistry : ICqrsHandlerRegistry
{
    /// <summary>
    ///     将生成路径可见的通知处理器注册到目标服务集合。
    /// </summary>
    /// <param name="services">承载处理器映射的服务集合。</param>
    /// <param name="logger">用于记录注册诊断的日志器。</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="services" /> 或 <paramref name="logger" /> 为 <see langword="null" />。
    /// </exception>
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
