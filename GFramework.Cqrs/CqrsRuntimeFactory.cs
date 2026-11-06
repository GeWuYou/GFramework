// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Internal;
using GFramework.Cqrs.Notification;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Cqrs;

/// <summary>
///     提供 CQRS runtime 默认实现的跨程序集创建入口。
/// </summary>
/// <remarks>
///     该工厂基于标准 <see cref="IServiceProvider" /> / <see cref="IServiceCollection" /> 工作，
///     使 CQRS runtime 可以脱离 Architecture 独立安装；架构集成入口见
///     <c>GFramework.Cqrs.CqrsRuntimeModule</c> 与 <c>GFramework.Cqrs.Extensions</c>。
/// </remarks>
public static class CqrsRuntimeFactory
{
    /// <summary>
    ///     创建默认 CQRS runtime 分发器，并从 provider 解析 runtime 诊断日志器。
    /// </summary>
    /// <param name="provider">承载 handler、behavior 与 notification publisher 注册的服务提供者。</param>
    /// <param name="notificationPublisher">
    ///     可选的通知发布策略；若为 <see langword="null" />，runtime 会在发布时优先尝试解析 provider 中已声明的
    ///     <see cref="INotificationPublisher" />，否则再回退到默认顺序发布器。
    /// </param>
    /// <returns>默认 CQRS runtime。</returns>
    /// <remarks>
    ///     runtime 诊断日志器通过 <c>GetService&lt;ILogger&lt;CqrsDispatcher&gt;&gt;</c> 从 provider 解析；
    ///     宿主未注册日志基础设施时回退到空实现，保持 runtime 可独立安装。
    ///     若调用方未显式传入 notification publisher，runtime 会在真正发布通知时优先复用 provider 里声明的
    ///     <see cref="INotificationPublisher" />；若仍未声明，则回退到默认顺序发布器。
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="provider" /> 为 <see langword="null" />。
    /// </exception>
    public static ICqrsRuntime CreateRuntime(
        IServiceProvider provider,
        INotificationPublisher? notificationPublisher = null)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var logger = provider.GetService<ILogger<CqrsDispatcher>>()
                     ?? NullLogger<CqrsDispatcher>.Instance;
        return new CqrsDispatcher(
            provider,
            logger,
            notificationPublisher);
    }

    /// <summary>
    ///     创建默认 CQRS runtime 分发器。
    /// </summary>
    /// <param name="provider">承载 handler、behavior 与 notification publisher 注册的服务提供者。</param>
    /// <param name="logger">用于 runtime 诊断的日志器。</param>
    /// <returns>默认 CQRS runtime。</returns>
    /// <remarks>
    ///     若调用方未显式传入 notification publisher，runtime 会在真正发布通知时优先复用 provider 里声明的
    ///     <see cref="INotificationPublisher" />；若仍未声明，则回退到默认顺序发布器。
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="provider" /> 或 <paramref name="logger" /> 为 <see langword="null" />。
    /// </exception>
    public static ICqrsRuntime CreateRuntime(IServiceProvider provider, ILogger logger)
    {
        return CreateRuntime(provider, logger, notificationPublisher: null);
    }

    /// <summary>
    ///     创建默认 CQRS runtime 分发器，并允许调用方指定通知发布策略。
    /// </summary>
    /// <param name="provider">承载 handler、behavior 与 notification publisher 注册的服务提供者。</param>
    /// <param name="logger">用于 runtime 诊断的日志器。</param>
    /// <param name="notificationPublisher">
    ///     可选的通知发布策略；若为 <see langword="null" />，runtime 会在发布时优先尝试解析 provider 中已声明的
    ///     <see cref="INotificationPublisher" />，否则再回退到默认顺序发布器。
    /// </param>
    /// <returns>默认 CQRS runtime。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="provider" /> 或 <paramref name="logger" /> 为 <see langword="null" />。
    /// </exception>
    public static ICqrsRuntime CreateRuntime(
        IServiceProvider provider,
        ILogger logger,
        INotificationPublisher? notificationPublisher)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(logger);

        return new CqrsDispatcher(
            provider,
            logger,
            notificationPublisher);
    }

    /// <summary>
    ///     创建默认 CQRS 处理器注册器。
    /// </summary>
    /// <param name="services">承载 handler 注册结果的标准服务集合；注册发生在 provider 构建之前。</param>
    /// <param name="logger">用于注册阶段诊断的日志器。</param>
    /// <returns>默认 CQRS handler registrar。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="services" /> 或 <paramref name="logger" /> 为 <see langword="null" />。
    /// </exception>
    public static ICqrsHandlerRegistrar CreateHandlerRegistrar(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        return new DefaultCqrsHandlerRegistrar(services, logger);
    }

    /// <summary>
    ///     创建默认的 CQRS 程序集注册协调器。
    /// </summary>
    /// <param name="registrar">底层 handler 注册器。</param>
    /// <param name="logger">用于注册阶段诊断的日志器。</param>
    /// <returns>默认 CQRS 程序集注册协调器。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="registrar" /> 或 <paramref name="logger" /> 为 <see langword="null" />。
    /// </exception>
    public static ICqrsRegistrationService CreateRegistrationService(ICqrsHandlerRegistrar registrar, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(logger);

        return new DefaultCqrsRegistrationService(registrar, logger);
    }
}
