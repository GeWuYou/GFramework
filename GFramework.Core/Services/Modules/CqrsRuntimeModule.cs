// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Notification;
using LegacyICqrsRuntime = GFramework.Core.Abstractions.Cqrs.ICqrsRuntime;

namespace GFramework.Core.Services.Modules;

/// <summary>
///     CQRS runtime 模块，用于把默认请求分发器与处理器注册器接入架构容器。
///     该模块在架构初始化早期完成注册，保证用户初始化阶段即可使用 CQRS 入口与 handler 自动接入能力。
/// </summary>
public sealed class CqrsRuntimeModule : IServiceModule
{
    /// <summary>
    ///     获取模块名称。
    /// </summary>
    public string ModuleName => nameof(CqrsRuntimeModule);

    /// <summary>
    ///     获取模块优先级。
    ///     CQRS runtime 需要先于架构默认 handler 扫描路径可用，因此放在基础总线模块之后、用户初始化之前注册。
    /// </summary>
    public int Priority => 15;

    /// <summary>
    ///     获取模块启用状态，默认启用。
    /// </summary>
    public bool IsEnabled => true;

    /// <summary>
    ///     注册默认 CQRS runtime seam 实现。
    ///     该入口会同时补齐旧命名空间下的 <c>ICqrsRuntime</c> 兼容别名，
    ///     并保证新旧服务类型都解析到同一个 runtime 实例。
    /// </summary>
    /// <param name="container">目标依赖注入容器。</param>
    public void Register(IIocContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var dispatcherLogger = LoggerFactoryResolver.Provider.CreateLogger("CqrsDispatcher");
        var registrarLogger = LoggerFactoryResolver.Provider.CreateLogger("DefaultCqrsHandlerRegistrar");
        var registrationLogger = LoggerFactoryResolver.Provider.CreateLogger("DefaultCqrsRegistrationService");
        var notificationPublisher = container.Get<INotificationPublisher>();
        var runtime = CqrsRuntimeFactory.CreateRuntime(container, dispatcherLogger, notificationPublisher);
        var registrar = CqrsRuntimeFactory.CreateHandlerRegistrar(container, registrarLogger);

        container.Register(runtime);
        RegisterLegacyRuntimeAlias(container, runtime);
        container.Register<ICqrsHandlerRegistrar>(registrar);
        container.Register<ICqrsRegistrationService>(
            CqrsRuntimeFactory.CreateRegistrationService(registrar, registrationLogger));
    }

    /// <summary>
    ///     为旧命名空间下的 CQRS runtime 契约注册兼容别名。
    /// </summary>
    /// <param name="container">承载运行时实例的依赖注入容器。</param>
    /// <param name="runtime">当前已创建的新 CQRS runtime 实例。</param>
    /// <remarks>
    ///     旧接口仍作为兼容入口保留，因此这里明确把别名注册收敛到单独 helper，
    ///     便于后续独立评估 alias 收口，而不混入 runtime 主体行为。
    /// </remarks>
    private static void RegisterLegacyRuntimeAlias(IIocContainer container, ICqrsRuntime runtime)
    {
        container.Register<LegacyICqrsRuntime>((LegacyICqrsRuntime)runtime);
    }

    /// <summary>
    ///     初始化模块。
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    ///     异步销毁模块。
    /// </summary>
    /// <returns>已完成的值任务。</returns>
    public ValueTask DestroyAsync()
    {
        return ValueTask.CompletedTask;
    }
}
