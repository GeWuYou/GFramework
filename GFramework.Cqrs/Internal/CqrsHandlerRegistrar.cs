// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0


using System.Diagnostics.CodeAnalysis;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Internal;

/// <summary>
///     在架构初始化期间扫描并注册 CQRS 处理器。
///     运行时会优先尝试使用源码生成的程序集级注册器，以减少冷启动阶段的反射开销；
///     当目标程序集没有生成注册器，或注册器不可用时，再回退到运行时反射扫描。
/// </summary>
internal static class CqrsHandlerRegistrar
{
    /// <summary>
    ///     描述 generated invoker descriptor 在 registrar 预热阶段使用的 request/response 类型对键。
    /// </summary>
    /// <param name="RequestType">请求运行时类型。</param>
    /// <param name="ResponseType">响应运行时类型。</param>
    private readonly record struct InvokerDescriptorKey(Type RequestType, Type ResponseType);

    // 卸载安全的进程级缓存：程序集元数据只按弱键复用。
    // 若程序集来自 collectible AssemblyLoadContext，被回收后会重新分析，而不会被静态缓存永久钉住。
    private static readonly WeakKeyCache<Assembly, AssemblyRegistrationMetadata> AssemblyMetadataCache =
        new();

    // 卸载安全的进程级缓存：registry 类型的构造分析可跨容器复用，但不应阻止类型卸载。
    private static readonly WeakKeyCache<Type, RegistryActivationMetadata> RegistryActivationMetadataCache =
        new();

    // 卸载安全的进程级缓存：可加载类型列表只在程序集存活期间保留；
    // 若程序集卸载，后续重新加载后的首次注册会重新执行 GetTypes()/恢复逻辑。
    private static readonly WeakKeyCache<Assembly, IReadOnlyList<Type>> LoadableTypesCache =
        new();

    // 卸载安全的进程级缓存：同一 handler 类型跨容器重复注册时，
    // 复用已筛选且排序好的 supported handler interface 列表，避免重复执行 GetInterfaces()。
    private static readonly WeakKeyCache<Type, IReadOnlyList<Type>> SupportedHandlerInterfacesCache =
        new();

    /// <summary>
    ///     扫描指定程序集并注册所有 CQRS 请求/通知/流式处理器。
    /// </summary>
    /// <param name="services">目标容器。</param>
    /// <param name="assemblies">要扫描的程序集集合。</param>
    /// <param name="logger">日志记录器。</param>
    public static void RegisterHandlers(
        IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(logger);

        foreach (var assembly in assemblies
                     .Where(static assembly => assembly is not null)
                     .Distinct()
                     .OrderBy(GetAssemblySortKey, StringComparer.Ordinal))
        {
            var generatedRegistrationResult =
                TryRegisterGeneratedHandlers(services, assembly, logger);
            if (generatedRegistrationResult is { UsedGeneratedRegistry: true, RequiresReflectionFallback: false })
                continue;

            RegisterAssemblyHandlers(
                services,
                assembly,
                logger,
                generatedRegistrationResult.ReflectionFallbackMetadata);
        }
    }

    /// <summary>
    ///     直接激活并注册单个 generated registry，避免调用方为了只接入一个 benchmark registry
    ///     而额外扫描同一程序集里的其他 registry / handler。
    /// </summary>
    /// <param name="services">承载 generated registry 注册结果的目标容器。</param>
    /// <param name="registryType">要直接激活的 generated registry 类型。</param>
    /// <param name="logger">当前注册过程使用的日志记录器。</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="services" />、<paramref name="registryType" /> 或 <paramref name="logger" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="InvalidOperationException">指定 registry 类型不满足 generated registry 运行时契约。</exception>
    internal static void RegisterGeneratedRegistry(
        IServiceCollection services,
        Type registryType,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(registryType);
        ArgumentNullException.ThrowIfNull(logger);

        var assemblyName = GetAssemblySortKey(registryType.Assembly);
        if (!TryCreateGeneratedRegistry(registryType, assemblyName, logger, out var registry))
        {
            throw new InvalidOperationException(
                $"Unable to activate generated CQRS handler registry {registryType.FullName} in assembly {assemblyName}.");
        }

        RegisterGeneratedRegistries(services, [registry], assemblyName, logger);
    }

    /// <summary>Registers an explicitly created registry without assembly discovery or reflective activation.</summary>
    /// <param name="services">The composition root.</param>
    /// <param name="registry">A consumer-created generated registry instance.</param>
    /// <param name="logger">Registration diagnostics.</param>
    internal static void RegisterGeneratedRegistry(IServiceCollection services, ICqrsHandlerRegistry registry, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(logger);
        RegisterGeneratedRegistries(services, [registry], GetAssemblySortKey(registry.GetType().Assembly), logger);
    }

    /// <summary>
    ///     优先使用程序集级源码生成注册器完成 CQRS 映射注册。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="assembly">当前要处理的程序集。</param>
    /// <param name="logger">日志记录器。</param>
    /// <returns>生成注册器的使用结果。</returns>
    private static GeneratedRegistrationResult TryRegisterGeneratedHandlers(
        IServiceCollection services,
        Assembly assembly,
        ILogger logger)
    {
        var assemblyName = GetAssemblySortKey(assembly);

        try
        {
            var assemblyMetadata = AssemblyMetadataCache.GetOrAdd(
                assembly,
                logger,
                static (key, state) => AnalyzeAssemblyRegistrationMetadata(key, state));
            var registryTypes = assemblyMetadata.RegistryTypes;

            if (registryTypes.Count == 0)
                return GeneratedRegistrationResult.NoGeneratedRegistry();

            if (!TryCreateGeneratedRegistries(registryTypes, assemblyName, logger, out var registries))
                return GeneratedRegistrationResult.NoGeneratedRegistry();

            RegisterGeneratedRegistries(services, registries, assemblyName, logger);
            return BuildGeneratedRegistrationResult(
                assemblyMetadata.ReflectionFallbackMetadata,
                assemblyName,
                logger);
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Generated CQRS handler registry discovery failed for assembly {AssemblyName}. Falling back to reflection scan.", assemblyName);
            }
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Failed to use generated CQRS handler registry for assembly {AssemblyName}: {ErrorMessage}", assemblyName, exception.Message);
            }
            return GeneratedRegistrationResult.NoGeneratedRegistry();
        }
    }

    /// <summary>
    ///     注册单个程序集里的所有 CQRS 处理器映射。
    /// </summary>
    private static void RegisterAssemblyHandlers(
        IServiceCollection services,
        Assembly assembly,
        ILogger logger,
        ReflectionFallbackMetadata? reflectionFallbackMetadata)
    {
        var registeredMappings = CreateRegisteredHandlerMappings(services);
        foreach (var implementationType in GetCandidateHandlerTypes(assembly, logger, reflectionFallbackMetadata)
                     .Where(IsConcreteHandlerType))
        {
            var handlerInterfaces = GetSupportedHandlerInterfaces(implementationType);

            if (handlerInterfaces.Count == 0)
                continue;

            foreach (var handlerInterface in handlerInterfaces)
            {
                if (!registeredMappings.Add(new HandlerMapping(handlerInterface, implementationType)))
                {
                    if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
                    {
                        logger.LogDebug("Skipping duplicate CQRS handler {ImplementationType} as {HandlerInterface}.", implementationType.FullName, handlerInterface.FullName);
                    }
                    continue;
                }

                // Request/notification handlers receive context injection before every dispatch.
                // Transient registration avoids sharing mutable Context across concurrent requests.
                services.AddTransient(handlerInterface, implementationType);
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
                {
                    logger.LogDebug("Registered CQRS handler {ImplementationType} as {HandlerInterface}.", implementationType.FullName, handlerInterface.FullName);
                }
            }
        }
    }

    /// <summary>
    ///     激活当前程序集声明的所有 generated registry；若任一 registry 不满足运行时契约，则整批回退到反射扫描。
    /// </summary>
    /// <param name="registryTypes">程序集声明的 generated registry 类型列表。</param>
    /// <param name="assemblyName">用于诊断的程序集稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    /// <param name="registries">成功激活后的 registry 实例。</param>
    /// <returns>当全部 registry 都可安全激活时返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    private static bool TryCreateGeneratedRegistries(
        IReadOnlyList<Type> registryTypes,
        string assemblyName,
        ILogger logger,
        out IReadOnlyList<ICqrsHandlerRegistry> registries)
    {
        var activatedRegistries = new List<ICqrsHandlerRegistry>(registryTypes.Count);
        foreach (var registryType in registryTypes)
        {
            if (!TryCreateGeneratedRegistry(registryType, assemblyName, logger, out var registry))
            {
                registries = [];
                return false;
            }

            activatedRegistries.Add(registry);
        }

        registries = activatedRegistries;
        return true;
    }

    /// <summary>
    ///     激活单个 generated registry，并在契约不满足时输出与原先完全一致的回退诊断。
    /// </summary>
    /// <param name="registryType">要分析的 generated registry 类型。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    /// <param name="registry">激活成功后的 registry 实例。</param>
    /// <returns>当 registry 可安全使用时返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    private static bool TryCreateGeneratedRegistry(
        Type registryType,
        string assemblyName,
        ILogger logger,
        [NotNullWhen(true)] out ICqrsHandlerRegistry? registry)
    {
        var activationMetadata = RegistryActivationMetadataCache.GetOrAdd(
            registryType,
            AnalyzeRegistryActivation);

        if (!activationMetadata.ImplementsRegistryContract)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Ignoring generated CQRS handler registry {RegistryType} in assembly {AssemblyName} because it does not implement {RegistryContract}.", registryType.FullName, assemblyName, typeof(ICqrsHandlerRegistry).FullName);
            }
            registry = null;
            return false;
        }

        if (activationMetadata.IsAbstract)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Ignoring generated CQRS handler registry {RegistryType} in assembly {AssemblyName} because it is abstract.", registryType.FullName, assemblyName);
            }
            registry = null;
            return false;
        }

        if (activationMetadata.Factory is null)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Ignoring generated CQRS handler registry {RegistryType} in assembly {AssemblyName} because it does not expose an accessible parameterless constructor.", registryType.FullName, assemblyName);
            }
            registry = null;
            return false;
        }

        registry = activationMetadata.Factory();
        return true;
    }

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
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                logger.LogDebug("Registering CQRS handlers for assembly {AssemblyName} via generated registry {RegistryType}.", assemblyName, registry.GetType().FullName);
            }
            registry.Register(services, logger);
            RegisterGeneratedRequestInvokerProvider(services, registry, assemblyName, logger);
            RegisterGeneratedStreamInvokerProvider(services, registry, assemblyName, logger);
        }
    }

    /// <summary>
    ///     当 generated registry 同时提供 request invoker 元数据时，把该 provider 注册到当前容器中。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="registry">当前已激活的 generated registry。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    /// <remarks>
    ///     provider 作为 registry 的附加能力注册到容器后，dispatcher 才能在首次请求分发时优先消费编译期生成的 invoker 元数据。
    ///     若 registry 不实现该契约，则保持现有纯反射 request binding 创建语义。
    /// </remarks>
    private static void RegisterGeneratedRequestInvokerProvider(
        IServiceCollection services,
        ICqrsHandlerRegistry registry,
        string assemblyName,
        ILogger logger)
    {
        if (registry is not ICqrsRequestInvokerProvider provider)
            return;

        RegisterGeneratedRequestInvokerDescriptors(provider, assemblyName, logger);
        services.AddSingleton(provider);
        if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
        {
            logger.LogDebug("Registered CQRS request invoker provider {ProviderType} for assembly {AssemblyName}.", provider.GetType().FullName, assemblyName);
        }
    }

    /// <summary>
    ///     读取 generated request invoker provider 中当前可见的描述符，并写入 dispatcher 的进程级弱缓存。
    /// </summary>
    /// <param name="provider">当前已激活的 request invoker provider。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    /// <remarks>
    ///     运行时当前只要求 provider 暴露可枚举的描述符集合，而不是在 dispatcher 首次命中时再回调容器。
    ///     这样 request dispatch binding 的静态缓存创建仍然只依赖类型键，不需要依赖具体容器实例。
    /// </remarks>
    private static void RegisterGeneratedRequestInvokerDescriptors(
        ICqrsRequestInvokerProvider provider,
        string assemblyName,
        ILogger logger)
    {
        if (provider is not IEnumeratesCqrsRequestInvokerDescriptors descriptorSource)
            return;

        var descriptors = ReadGeneratedRequestDescriptors(descriptorSource, assemblyName, logger);
        if (descriptors == null)
            return;

        var registeredKeys = new HashSet<InvokerDescriptorKey>();
        foreach (var descriptorEntry in descriptors)
        {
            if (descriptorEntry is null)
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    logger.LogWarning("Ignoring null generated CQRS request invoker descriptor entry from provider {ProviderType} in assembly {AssemblyName}.", provider.GetType().FullName, assemblyName);
                }
                continue;
            }

            var descriptorKey = new InvokerDescriptorKey(
                descriptorEntry.RequestType,
                descriptorEntry.ResponseType);

            if (!TryValidateEnumeratedRequestInvokerDescriptor(provider, descriptorEntry, assemblyName, logger))
                continue;

            if (!registeredKeys.Add(descriptorKey))
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    logger.LogWarning("Ignoring duplicate generated CQRS request invoker descriptor for {RequestType} -> {ResponseType} from provider {ProviderType} in assembly {AssemblyName}.", descriptorEntry.RequestType.FullName, descriptorEntry.ResponseType.FullName, provider.GetType().FullName, assemblyName);
                }
                continue;
            }

            CqrsDispatcher.RegisterGeneratedRequestInvokerDescriptor(
                descriptorEntry.RequestType,
                descriptorEntry.ResponseType,
                descriptorEntry.Descriptor);
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                logger.LogDebug("Registered generated CQRS request invoker descriptor for {RequestType} -> {ResponseType} from assembly {AssemblyName}.", descriptorEntry.RequestType.FullName, descriptorEntry.ResponseType.FullName, assemblyName);
            }
        }
    }

    /// <summary>
    ///     当 generated registry 同时提供 stream invoker 元数据时，把该 provider 注册到当前容器中。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="registry">当前已激活的 generated registry。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    /// <remarks>
    ///     provider 作为 registry 的附加能力注册到容器后，dispatcher 才能在首次建流时优先消费编译期生成的 invoker 元数据。
    ///     若 registry 不实现该契约，则保持现有纯反射 stream binding 创建语义。
    /// </remarks>
    private static void RegisterGeneratedStreamInvokerProvider(
        IServiceCollection services,
        ICqrsHandlerRegistry registry,
        string assemblyName,
        ILogger logger)
    {
        if (registry is not ICqrsStreamInvokerProvider provider)
            return;

        RegisterGeneratedStreamInvokerDescriptors(provider, assemblyName, logger);
        services.AddSingleton(provider);
        if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
        {
            logger.LogDebug("Registered CQRS stream invoker provider {ProviderType} for assembly {AssemblyName}.", provider.GetType().FullName, assemblyName);
        }
    }

    /// <summary>
    ///     读取 generated stream invoker provider 中当前可见的描述符，并写入 dispatcher 的进程级弱缓存。
    /// </summary>
    /// <param name="provider">当前已激活的 stream invoker provider。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    /// <remarks>
    ///     运行时当前只要求 provider 暴露可枚举的描述符集合，而不是在 dispatcher 首次命中时再回调容器。
    ///     这样 stream dispatch binding 的静态缓存创建仍然只依赖类型键，不需要依赖具体容器实例。
    /// </remarks>
    private static void RegisterGeneratedStreamInvokerDescriptors(
        ICqrsStreamInvokerProvider provider,
        string assemblyName,
        ILogger logger)
    {
        if (provider is not IEnumeratesCqrsStreamInvokerDescriptors descriptorSource)
            return;

        var descriptors = ReadGeneratedStreamDescriptors(descriptorSource, assemblyName, logger);
        if (descriptors == null)
            return;

        var registeredKeys = new HashSet<InvokerDescriptorKey>();
        foreach (var descriptorEntry in descriptors)
        {
            if (descriptorEntry is null)
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    logger.LogWarning("Ignoring null generated CQRS stream invoker descriptor entry from provider {ProviderType} in assembly {AssemblyName}.", provider.GetType().FullName, assemblyName);
                }
                continue;
            }

            var descriptorKey = new InvokerDescriptorKey(
                descriptorEntry.RequestType,
                descriptorEntry.ResponseType);

            if (!TryValidateEnumeratedStreamInvokerDescriptor(provider, descriptorEntry, assemblyName, logger))
                continue;

            if (!registeredKeys.Add(descriptorKey))
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    logger.LogWarning("Ignoring duplicate generated CQRS stream invoker descriptor for {RequestType} -> {ResponseType} from provider {ProviderType} in assembly {AssemblyName}.", descriptorEntry.RequestType.FullName, descriptorEntry.ResponseType.FullName, provider.GetType().FullName, assemblyName);
                }
                continue;
            }

            CqrsDispatcher.RegisterGeneratedStreamInvokerDescriptor(
                descriptorEntry.RequestType,
                descriptorEntry.ResponseType,
                descriptorEntry.Descriptor);
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                logger.LogDebug("Registered generated CQRS stream invoker descriptor for {RequestType} -> {ResponseType} from assembly {AssemblyName}.", descriptorEntry.RequestType.FullName, descriptorEntry.ResponseType.FullName, assemblyName);
            }
        }
    }

    /// <summary>读取生成描述符，并将枚举失败或空结果保留为明确的反射回退诊断。</summary>
    /// <param name="descriptorSource">已经确认支持枚举的 provider。</param>
    /// <param name="assemblyName">当前程序集名称。</param>
    /// <param name="logger">保留既有日志字段和级别的记录器。</param>
    /// <returns>可见描述符；不能安全枚举时返回 null。</returns>
    private static IReadOnlyList<CqrsRequestInvokerDescriptorEntry>? ReadGeneratedRequestDescriptors(
        IEnumeratesCqrsRequestInvokerDescriptors descriptorSource, string assemblyName, ILogger logger)
    {
        IReadOnlyList<CqrsRequestInvokerDescriptorEntry>? descriptors;
        try
        {
            descriptors = descriptorSource.GetDescriptors();
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Failed to enumerate generated CQRS request invoker descriptors from provider {ProviderType} in assembly {AssemblyName}. Falling back to runtime reflection for request invokers: {ErrorMessage}", descriptorSource.GetType().FullName, assemblyName, exception.Message);
            }
            return null;
        }

        if (descriptors is null)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Ignoring generated CQRS request invoker descriptors from provider {ProviderType} in assembly {AssemblyName} because GetDescriptors() returned null.", descriptorSource.GetType().FullName, assemblyName);
            }
            return null;
        }

        return descriptors;
    }

    /// <summary>读取生成描述符，并将枚举失败或空结果保留为明确的反射回退诊断。</summary>
    /// <param name="descriptorSource">已经确认支持枚举的 provider。</param>
    /// <param name="assemblyName">当前程序集名称。</param>
    /// <param name="logger">保留既有日志字段和级别的记录器。</param>
    /// <returns>可见描述符；不能安全枚举时返回 null。</returns>
    private static IReadOnlyList<CqrsStreamInvokerDescriptorEntry>? ReadGeneratedStreamDescriptors(
        IEnumeratesCqrsStreamInvokerDescriptors descriptorSource, string assemblyName, ILogger logger)
    {
        IReadOnlyList<CqrsStreamInvokerDescriptorEntry>? descriptors;
        try
        {
            descriptors = descriptorSource.GetDescriptors();
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Failed to enumerate generated CQRS stream invoker descriptors from provider {ProviderType} in assembly {AssemblyName}. Falling back to runtime reflection for stream invokers: {ErrorMessage}", descriptorSource.GetType().FullName, assemblyName, exception.Message);
            }
            return null;
        }

        if (descriptors is null)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Ignoring generated CQRS stream invoker descriptors from provider {ProviderType} in assembly {AssemblyName} because GetDescriptors() returned null.", descriptorSource.GetType().FullName, assemblyName);
            }
            return null;
        }

        return descriptors;
    }

    /// <summary>
    ///     校验 request descriptor 枚举项是否与 provider 的显式查询结果保持一致。
    /// </summary>
    /// <param name="provider">当前正在预热的 request invoker provider。</param>
    /// <param name="descriptorEntry">当前枚举到的描述符条目。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    /// <returns>当该枚举项可安全写入 dispatcher 缓存时返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    private static bool TryValidateEnumeratedRequestInvokerDescriptor(
        ICqrsRequestInvokerProvider provider,
        CqrsRequestInvokerDescriptorEntry descriptorEntry,
        string assemblyName,
        ILogger logger)
    {
        try
        {
            if (!provider.TryGetDescriptor(
                    descriptorEntry.RequestType,
                    descriptorEntry.ResponseType,
                    out var resolvedDescriptor) ||
                resolvedDescriptor is null)
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    logger.LogWarning("Ignoring generated CQRS request invoker descriptor for {RequestType} -> {ResponseType} from provider {ProviderType} in assembly {AssemblyName} because TryGetDescriptor did not return a matching descriptor.", descriptorEntry.RequestType.FullName, descriptorEntry.ResponseType.FullName, provider.GetType().FullName, assemblyName);
                }
                return false;
            }

            if (!resolvedDescriptor.InvokerMethod.Equals(descriptorEntry.Descriptor.InvokerMethod) ||
                resolvedDescriptor.HandlerType != descriptorEntry.Descriptor.HandlerType)
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    logger.LogWarning("Ignoring generated CQRS request invoker descriptor for {RequestType} -> {ResponseType} from provider {ProviderType} in assembly {AssemblyName} because the enumerated descriptor does not match TryGetDescriptor.", descriptorEntry.RequestType.FullName, descriptorEntry.ResponseType.FullName, provider.GetType().FullName, assemblyName);
                }
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Ignoring generated CQRS request invoker descriptor for {RequestType} -> {ResponseType} from provider {ProviderType} in assembly {AssemblyName} because TryGetDescriptor threw: {ErrorMessage}", descriptorEntry.RequestType.FullName, descriptorEntry.ResponseType.FullName, provider.GetType().FullName, assemblyName, exception.Message);
            }
            return false;
        }
    }

    /// <summary>
    ///     校验 stream descriptor 枚举项是否与 provider 的显式查询结果保持一致。
    /// </summary>
    /// <param name="provider">当前正在预热的 stream invoker provider。</param>
    /// <param name="descriptorEntry">当前枚举到的描述符条目。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    /// <returns>当该枚举项可安全写入 dispatcher 缓存时返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    private static bool TryValidateEnumeratedStreamInvokerDescriptor(
        ICqrsStreamInvokerProvider provider,
        CqrsStreamInvokerDescriptorEntry descriptorEntry,
        string assemblyName,
        ILogger logger)
    {
        try
        {
            if (!provider.TryGetDescriptor(
                    descriptorEntry.RequestType,
                    descriptorEntry.ResponseType,
                    out var resolvedDescriptor) ||
                resolvedDescriptor is null)
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    logger.LogWarning("Ignoring generated CQRS stream invoker descriptor for {RequestType} -> {ResponseType} from provider {ProviderType} in assembly {AssemblyName} because TryGetDescriptor did not return a matching descriptor.", descriptorEntry.RequestType.FullName, descriptorEntry.ResponseType.FullName, provider.GetType().FullName, assemblyName);
                }
                return false;
            }

            if (!resolvedDescriptor.InvokerMethod.Equals(descriptorEntry.Descriptor.InvokerMethod) ||
                resolvedDescriptor.HandlerType != descriptorEntry.Descriptor.HandlerType)
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    logger.LogWarning("Ignoring generated CQRS stream invoker descriptor for {RequestType} -> {ResponseType} from provider {ProviderType} in assembly {AssemblyName} because the enumerated descriptor does not match TryGetDescriptor.", descriptorEntry.RequestType.FullName, descriptorEntry.ResponseType.FullName, provider.GetType().FullName, assemblyName);
                }
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Ignoring generated CQRS stream invoker descriptor for {RequestType} -> {ResponseType} from provider {ProviderType} in assembly {AssemblyName} because TryGetDescriptor threw: {ErrorMessage}", descriptorEntry.RequestType.FullName, descriptorEntry.ResponseType.FullName, provider.GetType().FullName, assemblyName, exception.Message);
            }
            return false;
        }
    }

    /// <summary>
    ///     将 generated registry 的 fallback 元数据转换为统一的注册结果，并记录下一阶段是定向补扫还是整程序集扫描。
    /// </summary>
    /// <param name="reflectionFallbackMetadata">生成注册器声明的反射补扫元数据。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    /// <returns>描述 generated registry 是否已完全处理当前程序集的结果对象。</returns>
    private static GeneratedRegistrationResult BuildGeneratedRegistrationResult(
        ReflectionFallbackMetadata? reflectionFallbackMetadata,
        string assemblyName,
        ILogger logger)
    {
        if (reflectionFallbackMetadata is null)
            return GeneratedRegistrationResult.FullyHandled();

        if (reflectionFallbackMetadata.HasExplicitTypes)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                logger.LogDebug("Generated CQRS registry for assembly {AssemblyName} requested targeted reflection fallback for {HandlerCount} unsupported handler type(s).", assemblyName, reflectionFallbackMetadata.Types.Count);
            }
        }
        else
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                logger.LogDebug("Generated CQRS registry for assembly {AssemblyName} requested full reflection fallback for unsupported handlers.", assemblyName);
            }
        }

        return GeneratedRegistrationResult.WithReflectionFallback(reflectionFallbackMetadata);
    }

    /// <summary>
    ///     获取指定实现类型上所有受支持的 CQRS handler 接口，并缓存筛选与排序结果。
    /// </summary>
    /// <param name="implementationType">要分析的处理器实现类型。</param>
    /// <returns>当前实现类型声明的受支持 handler 接口列表。</returns>
    private static IReadOnlyList<Type> GetSupportedHandlerInterfaces(Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);

        return SupportedHandlerInterfacesCache.GetOrAdd(
            implementationType,
            static key => [.. key
                .GetInterfaces()
                .Where(IsSupportedHandlerInterface)
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)]);
    }

    /// <summary>
    ///     根据当前服务集合创建已注册 handler 映射的快速索引，避免 reflection fallback 路径重复线性扫描服务描述符。
    /// </summary>
    /// <param name="services">当前容器的服务描述符集合。</param>
    /// <returns>已存在的 handler 映射集合。</returns>
    private static HashSet<HandlerMapping> CreateRegisteredHandlerMappings(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return [.. services
            .Where(static descriptor => descriptor.ImplementationType is not null)
            .Select(static descriptor => new HandlerMapping(descriptor.ServiceType, descriptor.ImplementationType!))];
    }

    /// <summary>
    ///     根据生成器提供的 fallback 清单或整程序集扫描结果，获取本轮要注册的候选处理器类型。
    /// </summary>
    private static IReadOnlyList<Type> GetCandidateHandlerTypes(
        Assembly assembly,
        ILogger logger,
        ReflectionFallbackMetadata? reflectionFallbackMetadata)
    {
        return reflectionFallbackMetadata is { HasExplicitTypes: true }
            ? reflectionFallbackMetadata.Types
            : GetLoadableTypes(assembly, logger);
    }

    /// <summary>
    ///     获取生成注册器要求运行时继续补充反射扫描的 handler 元数据。
    /// </summary>
    private static ReflectionFallbackMetadata? GetReflectionFallbackMetadata(
        Assembly assembly,
        ILogger logger)
    {
        var assemblyName = GetAssemblySortKey(assembly);
        var fallbackAttributes = assembly
            .GetCustomAttributes(typeof(CqrsReflectionFallbackAttribute), inherit: false)
            .OfType<CqrsReflectionFallbackAttribute>()
            .ToList();

        if (fallbackAttributes.Count == 0)
            return null;

        var resolvedTypes = new List<Type>();
        AppendDirectFallbackTypes(fallbackAttributes, resolvedTypes, assemblyName, logger);
        AppendNamedFallbackTypes(assembly, fallbackAttributes, resolvedTypes, assemblyName, logger);

        return new ReflectionFallbackMetadata(
            [.. resolvedTypes
                .Distinct()
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)]);
    }

    /// <summary>
    ///     追加 attribute 里直接携带的 fallback 类型，并过滤掉跨程序集误声明的条目。
    /// </summary>
    /// <param name="fallbackAttributes">当前程序集上的 fallback attribute 集合。</param>
    /// <param name="resolvedTypes">待补充的已解析类型集合。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    private static void AppendDirectFallbackTypes(
        IReadOnlyList<CqrsReflectionFallbackAttribute> fallbackAttributes,
        List<Type> resolvedTypes,
        string assemblyName,
        ILogger logger)
    {
        foreach (var fallbackType in fallbackAttributes
                     .SelectMany(static attribute => attribute.FallbackHandlerTypes)
                     .Where(static type => type is not null)
                     .Distinct()
                     .OrderBy(GetTypeSortKey, StringComparer.Ordinal))
        {
            if (!string.Equals(
                    GetAssemblySortKey(fallbackType.Assembly),
                    assemblyName,
                    StringComparison.Ordinal))
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    logger.LogWarning("Generated CQRS reflection fallback type {FallbackType} was declared on assembly {AssemblyName} but belongs to assembly {ActualAssembly}. Skipping mismatched fallback entry.", fallbackType.FullName, assemblyName, GetAssemblySortKey(fallbackType.Assembly));
                }
                continue;
            }

            resolvedTypes.Add(fallbackType);
        }
    }

    /// <summary>
    ///     追加 attribute 里以类型名声明的 fallback 条目，并保留逐项失败的诊断能力。
    /// </summary>
    /// <param name="assembly">当前待解析的程序集。</param>
    /// <param name="fallbackAttributes">当前程序集上的 fallback attribute 集合。</param>
    /// <param name="resolvedTypes">待补充的已解析类型集合。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="logger">日志记录器。</param>
    private static void AppendNamedFallbackTypes(
        Assembly assembly,
        IReadOnlyList<CqrsReflectionFallbackAttribute> fallbackAttributes,
        ICollection<Type> resolvedTypes,
        string assemblyName,
        ILogger logger)
    {
        foreach (var typeName in fallbackAttributes
                     .SelectMany(static attribute => attribute.FallbackHandlerTypeNames)
                     .Where(static name => !string.IsNullOrWhiteSpace(name))
                     .Distinct(StringComparer.Ordinal)
                     .Order(StringComparer.Ordinal))
        {
            TryAppendNamedFallbackType(assembly, resolvedTypes, assemblyName, typeName, logger);
        }
    }

    /// <summary>
    ///     解析并追加单个按名称声明的 fallback 类型，同时保留“找不到”与“加载异常”两类不同日志语义。
    /// </summary>
    /// <param name="assembly">当前待解析的程序集。</param>
    /// <param name="resolvedTypes">待补充的已解析类型集合。</param>
    /// <param name="assemblyName">当前程序集的稳定名称。</param>
    /// <param name="typeName">要解析的完整类型名。</param>
    /// <param name="logger">日志记录器。</param>
    private static void TryAppendNamedFallbackType(
        Assembly assembly,
        ICollection<Type> resolvedTypes,
        string assemblyName,
        string typeName,
        ILogger logger)
    {
        try
        {
            var type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (type is null)
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    logger.LogWarning("Generated CQRS reflection fallback type {TypeName} could not be resolved in assembly {AssemblyName}. Skipping targeted fallback entry.", typeName, assemblyName);
                }
                return;
            }

            resolvedTypes.Add(type);
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Generated CQRS reflection fallback type {TypeName} failed to load in assembly {AssemblyName}: {ErrorMessage}", typeName, assemblyName, exception.Message);
            }
        }
    }

    /// <summary>
    ///     安全获取程序集中的可加载类型，并在部分类型加载失败时保留其余处理器注册能力。
    /// </summary>
    private static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly, ILogger logger)
    {
        return LoadableTypesCache.GetOrAdd(
            assembly,
            logger,
            static (key, state) => LoadAndSortTypes(key, state));
    }

    /// <summary>
    ///     分析并缓存指定程序集上的 generated-registry 与 fallback 元数据。
    /// </summary>
    private static AssemblyRegistrationMetadata AnalyzeAssemblyRegistrationMetadata(
        Assembly assembly,
        ILogger logger)
    {
        var registryTypes = assembly
            .GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), inherit: false)
            .OfType<CqrsHandlerRegistryAttribute>()
            .Select(static attribute => attribute.RegistryType)
            .Where(static type => type is not null)
            .Distinct()
            .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
            .ToArray();

        var reflectionFallbackMetadata = GetReflectionFallbackMetadata(assembly, logger);
        return new AssemblyRegistrationMetadata(registryTypes, reflectionFallbackMetadata);
    }

    /// <summary>
    ///     分析并缓存 registry 类型的可激活性，避免每次注册都重复检查接口实现与构造函数。
    /// </summary>
    private static RegistryActivationMetadata AnalyzeRegistryActivation(Type registryType)
    {
        var implementsRegistryContract = typeof(ICqrsHandlerRegistry).IsAssignableFrom(registryType);
        if (!implementsRegistryContract)
            return new RegistryActivationMetadata(implementsRegistryContract: false, registryType.IsAbstract, factory: null);

        if (registryType.IsAbstract)
            return new RegistryActivationMetadata(implementsRegistryContract: true, isAbstract: true, factory: null);

        var constructor = registryType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        return constructor is null
            ? new RegistryActivationMetadata(implementsRegistryContract: true, isAbstract: false, factory: null)
            : new RegistryActivationMetadata(
implementsRegistryContract: true,
isAbstract: false,
                CreateRegistryFactory(constructor));
    }

    /// <summary>Activates metadata only for explicitly selected assembly/type registration paths.</summary>
    /// <param name="constructor">The validated runtime-discovered constructor.</param>
    /// <returns>A cached factory; generated consumers pass an instance and never enter this path.</returns>
    private static Func<ICqrsHandlerRegistry> CreateRegistryFactory(ConstructorInfo constructor) =>
        () => (ICqrsHandlerRegistry)constructor.Invoke(parameters: null);

    /// <summary>
    ///     首次命中未生成 registry 的程序集时加载并排序全部可扫描类型，后续复用缓存结果。
    /// </summary>
    private static IReadOnlyList<Type> LoadAndSortTypes(Assembly assembly, ILogger logger)
    {
        try
        {
            return [.. assembly.GetTypes()
                .Where(static type => type is not null)
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)];
        }
        catch (ReflectionTypeLoadException exception)
        {
            return RecoverLoadableTypes(assembly, exception, logger);
        }
    }

    /// <summary>
    ///     记录部分类型加载失败，并返回仍然可用的类型集合。
    /// </summary>
    private static IReadOnlyList<Type> RecoverLoadableTypes(
        Assembly assembly,
        ReflectionTypeLoadException exception,
        ILogger logger)
    {
        var assemblyName = GetAssemblySortKey(assembly);
        if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
        {
            logger.LogWarning("CQRS handler scan partially failed for assembly {AssemblyName}. Continuing with loadable types.", assemblyName);
        }

        foreach (var loaderException in exception.LoaderExceptions.Where(static ex => ex is not null))
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning("Failed to load one or more types while scanning {AssemblyName}: {ErrorMessage}", assemblyName, loaderException!.Message);
            }
        }

        return [.. exception.Types
            .Where(static type => type is not null)
            .Cast<Type>()
            .OrderBy(GetTypeSortKey, StringComparer.Ordinal)];
    }

    /// <summary>
    ///     判断指定类型是否可作为可实例化处理器。
    /// </summary>
    private static bool IsConcreteHandlerType(Type type)
    {
        return type is { IsAbstract: false, IsInterface: false } && !type.ContainsGenericParameters;
    }

    /// <summary>
    ///     判断接口是否为当前运行时支持的 CQRS 处理器接口。
    /// </summary>
    private static bool IsSupportedHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(IRequestHandler<,>) ||
               definition == typeof(INotificationHandler<>) ||
               definition == typeof(IStreamRequestHandler<,>);
    }

    /// <summary>
    ///     生成程序集排序键，保证跨运行环境的处理器注册顺序稳定。
    /// </summary>
    private static string GetAssemblySortKey(Assembly assembly)
    {
        return assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString();
    }

    /// <summary>
    ///     生成类型排序键，保证同一程序集内的处理器与接口映射顺序稳定。
    /// </summary>
    private static string GetTypeSortKey(Type type)
    {
        return type.FullName ?? type.Name;
    }

    private readonly record struct HandlerMapping(Type ServiceType, Type ImplementationType);

    private readonly record struct GeneratedRegistrationResult(
        bool UsedGeneratedRegistry,
        bool RequiresReflectionFallback,
        ReflectionFallbackMetadata? ReflectionFallbackMetadata)
    {
        public static GeneratedRegistrationResult NoGeneratedRegistry()
        {
            return new GeneratedRegistrationResult(
                UsedGeneratedRegistry: false,
                RequiresReflectionFallback: false,
                ReflectionFallbackMetadata: null);
        }

        public static GeneratedRegistrationResult FullyHandled()
        {
            return new GeneratedRegistrationResult(
                UsedGeneratedRegistry: true,
                RequiresReflectionFallback: false,
                ReflectionFallbackMetadata: null);
        }

        public static GeneratedRegistrationResult WithReflectionFallback(
            ReflectionFallbackMetadata reflectionFallbackMetadata)
        {
            ArgumentNullException.ThrowIfNull(reflectionFallbackMetadata);

            return new GeneratedRegistrationResult(
                UsedGeneratedRegistry: true,
                RequiresReflectionFallback: true,
                ReflectionFallbackMetadata: reflectionFallbackMetadata);
        }
    }

    /// <summary>
    ///     描述某个程序集在生成注册器之后仍需运行时补扫的 handler 元数据。
    /// </summary>
    /// <remarks>
    ///     该对象把“是否存在精确 fallback 类型列表”与“是否只能回退到整程序集扫描”收敛为同一份内部状态，
    ///     供注册流水线后续阶段统一判断。
    /// </remarks>
    private sealed class ReflectionFallbackMetadata(IReadOnlyList<Type> types)
    {
        /// <summary>
        ///     获取需要通过运行时反射补充注册的 handler 类型集合。
        /// </summary>
        public IReadOnlyList<Type> Types { get; } = types ?? throw new ArgumentNullException(nameof(types));

        /// <summary>
        ///     获取当前是否持有精确的 fallback 类型清单。
        /// </summary>
        public bool HasExplicitTypes => Types.Count > 0;
    }

    /// <summary>
    ///     描述单个程序集在注册阶段提取到的 generated registry 与 reflection fallback 元数据。
    /// </summary>
    private sealed class AssemblyRegistrationMetadata(
        IReadOnlyList<Type> registryTypes,
        ReflectionFallbackMetadata? reflectionFallbackMetadata)
    {
        /// <summary>
        ///     获取程序集上声明的 generated registry 类型集合。
        /// </summary>
        public IReadOnlyList<Type> RegistryTypes
        {
            get;
        } =
            registryTypes ?? throw new ArgumentNullException(nameof(registryTypes));

        /// <summary>
        ///     获取该程序集是否还要求运行时补充 reflection fallback。
        /// </summary>
        public ReflectionFallbackMetadata? ReflectionFallbackMetadata { get; } = reflectionFallbackMetadata;
    }

    /// <summary>
    ///     缓存 generated registry 激活所需的类型判定结果与工厂委托。
    /// </summary>
    /// <remarks>
    ///     该缓存把“是否实现契约”“是否为抽象类型”“是否已构建激活委托”封装为不可变快照，
    ///     避免对同一 registry 类型重复执行反射分析。
    /// </remarks>
    private sealed class RegistryActivationMetadata(
        bool implementsRegistryContract,
        bool isAbstract,
        Func<ICqrsHandlerRegistry>? factory)
    {
        /// <summary>
        ///     获取目标类型是否实现了 <see cref="ICqrsHandlerRegistry" />。
        /// </summary>
        public bool ImplementsRegistryContract { get; } = implementsRegistryContract;

        /// <summary>
        ///     获取目标类型是否为抽象类型。
        /// </summary>
        public bool IsAbstract { get; } = isAbstract;

        /// <summary>
        ///     获取可用于实例化 registry 的工厂委托。
        /// </summary>
        public Func<ICqrsHandlerRegistry>? Factory { get; } = factory;
    }
}
