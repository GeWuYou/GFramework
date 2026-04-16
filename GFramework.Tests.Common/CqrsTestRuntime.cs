using System;
using System.Collections.Generic;
using System.Reflection;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Command;
using LegacyICqrsRuntime = GFramework.Core.Abstractions.Cqrs.ICqrsRuntime;

namespace GFramework.Tests.Common;

/// <summary>
///     为测试项目提供对 CQRS 处理器真实注册入口的受控访问。
/// </summary>
/// <remarks>
///     该测试基础设施位于独立模块中，避免多个测试项目复制同一份反射绑定与默认 runtime 接线逻辑。
///     测试应通过该入口驱动注册流程，而不是各自维护一份实现细节副本。
/// </remarks>
public static class CqrsTestRuntime
{
    private static readonly Assembly CqrsRuntimeAssembly = typeof(CommandBase<,>).Assembly;

    private static readonly Type CqrsHandlerRegistrarType = CqrsRuntimeAssembly
        .GetType(
            "GFramework.Cqrs.Internal.CqrsHandlerRegistrar",
            throwOnError: true)!;

    private static readonly MethodInfo RegisterHandlersMethod = CqrsHandlerRegistrarType
                                                                    .GetMethod(
                                                                        "RegisterHandlers",
                                                                        BindingFlags.Public | BindingFlags.NonPublic |
                                                                        BindingFlags.Static,
                                                                        binder: null,
                                                                        [
                                                                            typeof(IIocContainer),
                                                                            typeof(IEnumerable<Assembly>),
                                                                            typeof(ILogger)
                                                                        ],
                                                                        modifiers: null)
                                                                ?? throw new InvalidOperationException(
                                                                    "Failed to locate CqrsHandlerRegistrar.RegisterHandlers.");

    /// <summary>
    ///     为裸测试容器补齐默认 CQRS runtime seam。
    /// </summary>
    /// <param name="container">目标测试容器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="container" /> 为 <see langword="null" />。</exception>
    /// <remarks>
    ///     这使仅使用 <see cref="MicrosoftDiContainer" /> 的测试环境也能观察与生产路径一致的 runtime 行为，
    ///     而无需完整启动服务模块管理器。
    ///     该方法按服务类型执行幂等注册，只会补齐当前容器中尚未接线的 CQRS 基础设施。
    /// </remarks>
    public static void RegisterInfrastructure(MicrosoftDiContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        if (container.Get<ICqrsRuntime>() is null)
        {
            var runtimeLogger = LoggerFactoryResolver.Provider.CreateLogger("CqrsDispatcher");
            var runtime = CqrsRuntimeFactory.CreateRuntime(container, runtimeLogger);
            container.Register(runtime);
            container.Register<LegacyICqrsRuntime>((LegacyICqrsRuntime)runtime);
        }
        else if (container.Get<LegacyICqrsRuntime>() is null)
        {
            container.Register<LegacyICqrsRuntime>((LegacyICqrsRuntime)container.GetRequired<ICqrsRuntime>());
        }

        if (container.Get<ICqrsHandlerRegistrar>() is null)
        {
            var registrarLogger = LoggerFactoryResolver.Provider.CreateLogger("DefaultCqrsHandlerRegistrar");
            var registrar = CqrsRuntimeFactory.CreateHandlerRegistrar(container, registrarLogger);
            container.Register<ICqrsHandlerRegistrar>(registrar);
        }

        if (container.Get<ICqrsRegistrationService>() is null)
        {
            var registrationLogger = LoggerFactoryResolver.Provider.CreateLogger("DefaultCqrsRegistrationService");
            var registrar = container.GetRequired<ICqrsHandlerRegistrar>();
            var registrationService = CqrsRuntimeFactory.CreateRegistrationService(registrar, registrationLogger);
            container.Register<ICqrsRegistrationService>(registrationService);
        }
    }

    /// <summary>
    ///     通过与生产代码一致的注册入口扫描并注册指定程序集中的 CQRS 处理器。
    /// </summary>
    /// <param name="container">承载处理器映射的测试容器。</param>
    /// <param name="assemblies">要扫描的程序集集合。</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="container" /> 或 <paramref name="assemblies" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="TargetInvocationException">反射调用底层 CQRS 处理器注册入口失败时抛出。</exception>
    /// <remarks>
    ///     该入口会自动调用 <see cref="RegisterInfrastructure" />，因此测试通常无需预先手动接线 CQRS 基础设施。
    ///     程序集去重与空元素过滤由生产注册入口统一处理，避免测试辅助层复制相同筛选逻辑。
    /// </remarks>
    public static void RegisterHandlers(MicrosoftDiContainer container, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(assemblies);

        RegisterInfrastructure(container);

        var logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(CqrsTestRuntime));
        RegisterHandlersMethod.Invoke(
            null,
            [container, assemblies, logger]);
    }
}
