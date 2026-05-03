// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Internal;
using GFramework.Cqrs.Notification;

namespace GFramework.Cqrs;

/// <summary>
///     提供 CQRS runtime 默认实现的跨程序集创建入口。
/// </summary>
/// <remarks>
///     <see cref="GFramework.Core" /> 需要在不暴露内部实现细节的前提下接入默认 CQRS runtime，
///     因此通过该工厂返回抽象接口，而不是直接公开内部 dispatcher / registrar 类型。
/// </remarks>
public static class CqrsRuntimeFactory
{
    /// <summary>
    ///     创建默认 CQRS runtime 分发器。
    /// </summary>
    /// <param name="container">目标依赖注入容器。</param>
    /// <param name="logger">用于 runtime 诊断的日志器。</param>
    /// <returns>默认 CQRS runtime。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="container" /> 或 <paramref name="logger" /> 为 <see langword="null" />。
    /// </exception>
    public static ICqrsRuntime CreateRuntime(IIocContainer container, ILogger logger)
    {
        return CreateRuntime(container, logger, notificationPublisher: null);
    }

    /// <summary>
    ///     创建默认 CQRS runtime 分发器，并允许调用方指定通知发布策略。
    /// </summary>
    /// <param name="container">目标依赖注入容器。</param>
    /// <param name="logger">用于 runtime 诊断的日志器。</param>
    /// <param name="notificationPublisher">可选的通知发布策略；若为 <see langword="null" /> 则使用默认顺序发布器。</param>
    /// <returns>默认 CQRS runtime。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="container" /> 或 <paramref name="logger" /> 为 <see langword="null" />。
    /// </exception>
    public static ICqrsRuntime CreateRuntime(
        IIocContainer container,
        ILogger logger,
        INotificationPublisher? notificationPublisher)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(logger);

        return new CqrsDispatcher(
            container,
            logger,
            notificationPublisher ?? new SequentialNotificationPublisher());
    }

    /// <summary>
    ///     创建默认 CQRS 处理器注册器。
    /// </summary>
    /// <param name="container">目标依赖注入容器。</param>
    /// <param name="logger">用于注册阶段诊断的日志器。</param>
    /// <returns>默认 CQRS handler registrar。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="container" /> 或 <paramref name="logger" /> 为 <see langword="null" />。
    /// </exception>
    public static ICqrsHandlerRegistrar CreateHandlerRegistrar(IIocContainer container, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(logger);

        return new DefaultCqrsHandlerRegistrar(container, logger);
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
