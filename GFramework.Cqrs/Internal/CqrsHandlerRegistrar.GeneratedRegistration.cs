// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Internal;

/// <summary>Registers generated handlers once per composition root across all public entrypoints.</summary>
internal static partial class CqrsHandlerRegistrar
{
    /// <summary>Records successful generated registration within one composition root.</summary>
    /// <param name="RegistryType">The generated registry identity shared by every entrypoint.</param>
    private sealed record RegisteredRegistry(Type RegistryType);

    /// <summary>
    ///     调用所有已激活的 generated registry 完成 CQRS handler 注册，并保留稳定的调试日志顺序。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="registries">已通过契约校验的 registry 实例。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    private static void RegisterGeneratedRegistries(
        IServiceCollection services,
        IReadOnlyList<ICqrsHandlerRegistry> registries,
        string assemblyName,
        ILogger logger)
    {
        foreach (var registry in registries)
        {
            var registryType = registry.GetType();
            // Identity belongs to this service collection, shared by direct and assembly entrypoints.
            // Do not skip assembly fallback: its metadata is evaluated after this method returns.
            if (services.Any(item => !item.IsKeyedService &&
                                     item.ServiceType == typeof(RegisteredRegistry) &&
                                     item.ImplementationInstance is RegisteredRegistry registered &&
                                     registered.RegistryType == registryType))
                continue;

            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                logger.LogDebug("Registering CQRS handlers for assembly {AssemblyName} via generated registry {RegistryType}.", assemblyName, registry.GetType().FullName);
            }
            registry.Register(services, logger);
            RegisterGeneratedRequestInvokerProvider(services, registry, assemblyName, logger);
            RegisterGeneratedStreamInvokerProvider(services, registry, assemblyName, logger);
            services.AddSingleton(new RegisteredRegistry(registryType));
        }
    }

}
