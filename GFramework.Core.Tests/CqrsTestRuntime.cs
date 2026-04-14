using System.Reflection;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;

namespace GFramework.Core.Tests;

internal static class CqrsTestRuntime
{
    private static readonly MethodInfo RegisterHandlersMethod = typeof(ArchitectureContext).Assembly
                                                                    .GetType(
                                                                        "GFramework.Core.Cqrs.Internal.CqrsHandlerRegistrar",
                                                                        throwOnError: true)!
                                                                    .GetMethod(
                                                                        "RegisterHandlers",
                                                                        BindingFlags.Public | BindingFlags.NonPublic |
                                                                        BindingFlags.Static)!
                                                                ?? throw new InvalidOperationException(
                                                                    "Failed to locate CqrsHandlerRegistrar.RegisterHandlers.");

    public static void RegisterHandlers(MicrosoftDiContainer container, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(assemblies);

        var logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(CqrsTestRuntime));
        RegisterHandlersMethod.Invoke(
            null,
            [container, assemblies.Where(static assembly => assembly is not null).Distinct().ToArray(), logger]);
    }
}
