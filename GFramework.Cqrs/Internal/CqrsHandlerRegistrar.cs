// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

namespace GFramework.Cqrs.Internal;

/// <summary>
///     在架构初始化期间扫描并注册 CQRS 处理器。
///     运行时会优先尝试使用源码生成的程序集级注册器，以减少冷启动阶段的反射开销；
///     当目标程序集没有生成注册器，或注册器不可用时，再回退到运行时反射扫描。
/// </summary>
internal static class CqrsHandlerRegistrar
{
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
    /// <param name="container">目标容器。</param>
    /// <param name="assemblies">要扫描的程序集集合。</param>
    /// <param name="logger">日志记录器。</param>
    public static void RegisterHandlers(
        IIocContainer container,
        IEnumerable<Assembly> assemblies,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(logger);

        foreach (var assembly in assemblies
                     .Where(static assembly => assembly is not null)
                     .Distinct()
                     .OrderBy(GetAssemblySortKey, StringComparer.Ordinal))
        {
            var generatedRegistrationResult =
                TryRegisterGeneratedHandlers(container.GetServicesUnsafe, assembly, logger);
            if (generatedRegistrationResult is { UsedGeneratedRegistry: true, RequiresReflectionFallback: false })
                continue;

            RegisterAssemblyHandlers(
                container.GetServicesUnsafe,
                assembly,
                logger,
                generatedRegistrationResult.ReflectionFallbackMetadata);
        }
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
            logger.Warn(
                $"Generated CQRS handler registry discovery failed for assembly {assemblyName}. Falling back to reflection scan.");
            logger.Warn(
                $"Failed to use generated CQRS handler registry for assembly {assemblyName}: {exception.Message}");
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
                    logger.Debug(
                        $"Skipping duplicate CQRS handler {implementationType.FullName} as {handlerInterface.FullName}.");
                    continue;
                }

                // Request/notification handlers receive context injection before every dispatch.
                // Transient registration avoids sharing mutable Context across concurrent requests.
                services.AddTransient(handlerInterface, implementationType);
                logger.Debug(
                    $"Registered CQRS handler {implementationType.FullName} as {handlerInterface.FullName}.");
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
                registries = Array.Empty<ICqrsHandlerRegistry>();
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
            logger.Warn(
                $"Ignoring generated CQRS handler registry {registryType.FullName} in assembly {assemblyName} because it does not implement {typeof(ICqrsHandlerRegistry).FullName}.");
            registry = null;
            return false;
        }

        if (activationMetadata.IsAbstract)
        {
            logger.Warn(
                $"Ignoring generated CQRS handler registry {registryType.FullName} in assembly {assemblyName} because it is abstract.");
            registry = null;
            return false;
        }

        if (activationMetadata.Factory is null)
        {
            logger.Warn(
                $"Ignoring generated CQRS handler registry {registryType.FullName} in assembly {assemblyName} because it does not expose an accessible parameterless constructor.");
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
            logger.Debug(
                $"Registering CQRS handlers for assembly {assemblyName} via generated registry {registry.GetType().FullName}.");
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
        services.AddSingleton(typeof(ICqrsRequestInvokerProvider), provider);
        logger.Debug(
            $"Registered CQRS request invoker provider {provider.GetType().FullName} for assembly {assemblyName}.");
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

        foreach (var descriptorEntry in descriptorSource.GetDescriptors())
        {
            CqrsDispatcher.RegisterGeneratedRequestInvokerDescriptor(
                descriptorEntry.RequestType,
                descriptorEntry.ResponseType,
                descriptorEntry.Descriptor);
            logger.Debug(
                $"Registered generated CQRS request invoker descriptor for {descriptorEntry.RequestType.FullName} -> {descriptorEntry.ResponseType.FullName} from assembly {assemblyName}.");
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
        services.AddSingleton(typeof(ICqrsStreamInvokerProvider), provider);
        logger.Debug(
            $"Registered CQRS stream invoker provider {provider.GetType().FullName} for assembly {assemblyName}.");
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

        foreach (var descriptorEntry in descriptorSource.GetDescriptors())
        {
            CqrsDispatcher.RegisterGeneratedStreamInvokerDescriptor(
                descriptorEntry.RequestType,
                descriptorEntry.ResponseType,
                descriptorEntry.Descriptor);
            logger.Debug(
                $"Registered generated CQRS stream invoker descriptor for {descriptorEntry.RequestType.FullName} -> {descriptorEntry.ResponseType.FullName} from assembly {assemblyName}.");
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
            logger.Debug(
                $"Generated CQRS registry for assembly {assemblyName} requested targeted reflection fallback for {reflectionFallbackMetadata.Types.Count} unsupported handler type(s).");
        }
        else
        {
            logger.Debug(
                $"Generated CQRS registry for assembly {assemblyName} requested full reflection fallback for unsupported handlers.");
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
            static key => key
                .GetInterfaces()
                .Where(IsSupportedHandlerInterface)
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
                .ToArray());
    }

    /// <summary>
    ///     根据当前服务集合创建已注册 handler 映射的快速索引，避免 reflection fallback 路径重复线性扫描服务描述符。
    /// </summary>
    /// <param name="services">当前容器的服务描述符集合。</param>
    /// <returns>已存在的 handler 映射集合。</returns>
    private static HashSet<HandlerMapping> CreateRegisteredHandlerMappings(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .Where(static descriptor => descriptor.ImplementationType is not null)
            .Select(static descriptor => new HandlerMapping(descriptor.ServiceType, descriptor.ImplementationType!))
            .ToHashSet();
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
            resolvedTypes
                .Distinct()
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
                .ToArray());
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
        ICollection<Type> resolvedTypes,
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
                logger.Warn(
                    $"Generated CQRS reflection fallback type {fallbackType.FullName} was declared on assembly {assemblyName} but belongs to assembly {GetAssemblySortKey(fallbackType.Assembly)}. Skipping mismatched fallback entry.");
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
                     .OrderBy(static name => name, StringComparer.Ordinal))
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
                logger.Warn(
                    $"Generated CQRS reflection fallback type {typeName} could not be resolved in assembly {assemblyName}. Skipping targeted fallback entry.");
                return;
            }

            resolvedTypes.Add(type);
        }
        catch (Exception exception)
        {
            logger.Warn(
                $"Generated CQRS reflection fallback type {typeName} failed to load in assembly {assemblyName}: {exception.Message}");
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
            return new RegistryActivationMetadata(false, registryType.IsAbstract, null);

        if (registryType.IsAbstract)
            return new RegistryActivationMetadata(true, true, null);

        var constructor = registryType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        return constructor is null
            ? new RegistryActivationMetadata(true, false, null)
            : new RegistryActivationMetadata(
                true,
                false,
                CreateRegistryFactory(registryType, constructor));
    }

    /// <summary>
    ///     为生成注册器创建可复用的激活工厂，优先使用一次性编译的动态方法，
    ///     避免后续每次命中缓存时仍走 <see cref="ConstructorInfo" /> 的反射激活路径。
    /// </summary>
    /// <param name="registryType">生成注册器类型。</param>
    /// <param name="constructor">已解析的无参构造函数。</param>
    /// <returns>可直接实例化注册器的工厂委托。</returns>
    private static Func<ICqrsHandlerRegistry> CreateRegistryFactory(
        Type registryType,
        ConstructorInfo constructor)
    {
        ArgumentNullException.ThrowIfNull(registryType);
        ArgumentNullException.ThrowIfNull(constructor);

        try
        {
            // 生成器产物通常是稳定的无参 registry；这里把构造反射收敛为一次性 IL 工厂，
            // 这样同一 registry 类型在多个容器间复用缓存时不会重复付出 ConstructorInfo.Invoke 成本。
            var dynamicMethod = new DynamicMethod(
                $"Create_{registryType.Name}_CqrsHandlerRegistry",
                typeof(ICqrsHandlerRegistry),
                Type.EmptyTypes,
                registryType.Module,
                skipVisibility: true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructor);

            if (registryType.IsValueType)
            {
                il.Emit(OpCodes.Box, registryType);
            }

            il.Emit(OpCodes.Castclass, typeof(ICqrsHandlerRegistry));
            il.Emit(OpCodes.Ret);

            return (Func<ICqrsHandlerRegistry>)dynamicMethod.CreateDelegate(typeof(Func<ICqrsHandlerRegistry>));
        }
        catch
        {
            // 某些受限运行环境若不允许动态方法，仍保留原有的反射激活语义，避免阻塞 generated registry 路径。
            return () => (ICqrsHandlerRegistry)constructor.Invoke(null);
        }
    }

    /// <summary>
    ///     首次命中未生成 registry 的程序集时加载并排序全部可扫描类型，后续复用缓存结果。
    /// </summary>
    private static IReadOnlyList<Type> LoadAndSortTypes(Assembly assembly, ILogger logger)
    {
        try
        {
            return assembly.GetTypes()
                .Where(static type => type is not null)
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
                .ToArray();
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
        logger.Warn(
            $"CQRS handler scan partially failed for assembly {assemblyName}. Continuing with loadable types.");

        foreach (var loaderException in exception.LoaderExceptions.Where(static ex => ex is not null))
        {
            logger.Warn(
                $"Failed to load one or more types while scanning {assemblyName}: {loaderException!.Message}");
        }

        return exception.Types
            .Where(static type => type is not null)
            .Cast<Type>()
            .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
            .ToList();
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
        public IReadOnlyList<Type> RegistryTypes { get; } =
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
