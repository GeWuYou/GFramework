using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Tests.Shared;

/// <summary>
///     为测试项目提供对 CQRS 处理器真实注册入口的受控访问。
/// </summary>
/// <remarks>
///     该文件以共享源码的方式同时编译进多个测试项目，确保反射绑定签名、默认 runtime 接线和注册入口行为始终保持一致，
///     避免测试副本在独立演化后产生隐藏分歧。
/// </remarks>
internal static class CqrsTestRuntime
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
    ///     这使仅使用 <see cref="MicrosoftDiContainer" /> 的测试环境也能观察与生产路径一致的 runtime 行为，
    ///     而无需完整启动服务模块管理器。
    /// </summary>
    /// <param name="container">目标测试容器。</param>
    internal static void RegisterInfrastructure(MicrosoftDiContainer container)
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
    internal static void RegisterHandlers(MicrosoftDiContainer container, params Assembly[] assemblies)
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
