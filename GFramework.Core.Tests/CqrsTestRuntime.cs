using System.Reflection;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;

namespace GFramework.Core.Tests;

/// <summary>
///     为测试项目提供对 CQRS 处理器真实注册入口的受控访问。
/// </summary>
/// <remarks>
///     测试应通过该入口驱动注册流程，而不是直接反射调用注册器的私有辅助方法，
///     这样可以覆盖生产启动路径中的程序集去重、日志记录与容错恢复行为。
/// </remarks>
internal static class CqrsTestRuntime
{
    private static readonly Type CqrsHandlerRegistrarType = typeof(ArchitectureContext).Assembly
                                                                .GetType(
                                                                    "GFramework.Core.Cqrs.Internal.CqrsHandlerRegistrar",
                                                                    throwOnError: true)!
                                                            ?? throw new InvalidOperationException(
                                                                "Failed to locate CqrsHandlerRegistrar type.");

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
    ///     通过与生产代码一致的注册入口扫描并注册指定程序集中的 CQRS 处理器。
    /// </summary>
    /// <param name="container">承载处理器映射的测试容器。</param>
    /// <param name="assemblies">要扫描的程序集集合。</param>
    internal static void RegisterHandlers(MicrosoftDiContainer container, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(assemblies);

        var logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(CqrsTestRuntime));
        RegisterHandlersMethod.Invoke(
            null,
            [container, assemblies.Where(static assembly => assembly is not null).Distinct().ToArray(), logger]);
    }
}
