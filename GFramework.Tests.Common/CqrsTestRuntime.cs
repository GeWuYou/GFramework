using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

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
    private static readonly Type CqrsHandlerRegistrarType = typeof(ArchitectureContext).Assembly
        .GetType(
            "GFramework.Core.Cqrs.Internal.CqrsHandlerRegistrar",
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

    private static readonly Type CqrsDispatcherType = typeof(ArchitectureContext).Assembly
        .GetType(
            "GFramework.Core.Cqrs.Internal.CqrsDispatcher",
            throwOnError: true)!;

    private static readonly ConstructorInfo CqrsDispatcherConstructor = CqrsDispatcherType.GetConstructor(
                                                                            BindingFlags.Instance |
                                                                            BindingFlags.Public |
                                                                            BindingFlags.NonPublic,
                                                                            binder: null,
                                                                            [
                                                                                typeof(IIocContainer),
                                                                                typeof(ILogger)
                                                                            ],
                                                                            modifiers: null)
                                                                        ?? throw new InvalidOperationException(
                                                                            "Failed to locate CqrsDispatcher constructor.");

    private static readonly Type DefaultCqrsHandlerRegistrarType = typeof(ArchitectureContext).Assembly
        .GetType(
            "GFramework.Core.Cqrs.Internal.DefaultCqrsHandlerRegistrar",
            throwOnError: true)!;

    private static readonly ConstructorInfo DefaultCqrsHandlerRegistrarConstructor =
        DefaultCqrsHandlerRegistrarType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [
                typeof(IIocContainer),
                typeof(ILogger)
            ],
            modifiers: null)
        ?? throw new InvalidOperationException(
            "Failed to locate DefaultCqrsHandlerRegistrar constructor.");

    /// <summary>
    ///     为裸测试容器补齐默认 CQRS runtime seam。
    /// </summary>
    /// <param name="container">目标测试容器。</param>
    /// <remarks>
    ///     这使仅使用 <see cref="MicrosoftDiContainer" /> 的测试环境也能观察与生产路径一致的 runtime 行为，
    ///     而无需完整启动服务模块管理器。
    /// </remarks>
    public static void RegisterInfrastructure(MicrosoftDiContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var runtimeLogger = LoggerFactoryResolver.Provider.CreateLogger("CqrsDispatcher");
        var registrarLogger = LoggerFactoryResolver.Provider.CreateLogger(nameof(CqrsTestRuntime));
        var runtime = (ICqrsRuntime)CqrsDispatcherConstructor.Invoke([container, runtimeLogger]);
        var registrar =
            (ICqrsHandlerRegistrar)DefaultCqrsHandlerRegistrarConstructor.Invoke([container, registrarLogger]);

        container.Register<ICqrsRuntime>(runtime);
        container.Register<ICqrsHandlerRegistrar>(registrar);
    }

    /// <summary>
    ///     通过与生产代码一致的注册入口扫描并注册指定程序集中的 CQRS 处理器。
    /// </summary>
    /// <param name="container">承载处理器映射的测试容器。</param>
    /// <param name="assemblies">要扫描的程序集集合。</param>
    public static void RegisterHandlers(MicrosoftDiContainer container, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(assemblies);

        RegisterInfrastructure(container);

        var logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(CqrsTestRuntime));
        RegisterHandlersMethod.Invoke(
            null,
            [container, assemblies.Where(static assembly => assembly is not null).Distinct().ToArray(), logger]);
    }
}
