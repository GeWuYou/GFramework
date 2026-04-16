using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
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
            var registryTypes = assembly
                .GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), inherit: false)
                .OfType<CqrsHandlerRegistryAttribute>()
                .Select(static attribute => attribute.RegistryType)
                .Where(static type => type is not null)
                .Distinct()
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
                .ToList();

            if (registryTypes.Count == 0)
                return GeneratedRegistrationResult.NoGeneratedRegistry();

            var registries = new List<ICqrsHandlerRegistry>(registryTypes.Count);
            foreach (var registryType in registryTypes)
            {
                if (!typeof(ICqrsHandlerRegistry).IsAssignableFrom(registryType))
                {
                    logger.Warn(
                        $"Ignoring generated CQRS handler registry {registryType.FullName} in assembly {assemblyName} because it does not implement {typeof(ICqrsHandlerRegistry).FullName}.");
                    return GeneratedRegistrationResult.NoGeneratedRegistry();
                }

                if (registryType.IsAbstract)
                {
                    logger.Warn(
                        $"Ignoring generated CQRS handler registry {registryType.FullName} in assembly {assemblyName} because it is abstract.");
                    return GeneratedRegistrationResult.NoGeneratedRegistry();
                }

                if (Activator.CreateInstance(registryType, nonPublic: true) is not ICqrsHandlerRegistry registry)
                {
                    logger.Warn(
                        $"Ignoring generated CQRS handler registry {registryType.FullName} in assembly {assemblyName} because it could not be instantiated.");
                    return GeneratedRegistrationResult.NoGeneratedRegistry();
                }

                registries.Add(registry);
            }

            foreach (var registry in registries)
            {
                logger.Debug(
                    $"Registering CQRS handlers for assembly {assemblyName} via generated registry {registry.GetType().FullName}.");
                registry.Register(services, logger);
            }

            var reflectionFallbackMetadata = GetReflectionFallbackMetadata(assembly, logger);
            if (reflectionFallbackMetadata is not null)
            {
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

            return GeneratedRegistrationResult.FullyHandled();
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
        foreach (var implementationType in GetCandidateHandlerTypes(assembly, logger, reflectionFallbackMetadata)
                     .Where(IsConcreteHandlerType))
        {
            var handlerInterfaces = implementationType
                .GetInterfaces()
                .Where(IsSupportedHandlerInterface)
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
                .ToList();

            if (handlerInterfaces.Count == 0)
                continue;

            foreach (var handlerInterface in handlerInterfaces)
            {
                if (IsHandlerMappingAlreadyRegistered(services, handlerInterface, implementationType))
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

        foreach (var typeName in fallbackAttributes
                     .SelectMany(static attribute => attribute.FallbackHandlerTypeNames)
                     .Where(static name => !string.IsNullOrWhiteSpace(name))
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(static name => name, StringComparer.Ordinal))
        {
            try
            {
                var type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
                if (type is null)
                {
                    logger.Warn(
                        $"Generated CQRS reflection fallback type {typeName} could not be resolved in assembly {assemblyName}. Skipping targeted fallback entry.");
                    continue;
                }

                resolvedTypes.Add(type);
            }
            catch (Exception exception)
            {
                logger.Warn(
                    $"Generated CQRS reflection fallback type {typeName} failed to load in assembly {assemblyName}: {exception.Message}");
            }
        }

        return new ReflectionFallbackMetadata(
            resolvedTypes
                .Distinct()
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
                .ToArray());
    }

    /// <summary>
    ///     安全获取程序集中的可加载类型，并在部分类型加载失败时保留其余处理器注册能力。
    /// </summary>
    private static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly, ILogger logger)
    {
        try
        {
            return assembly.GetTypes()
                .Where(static type => type is not null)
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
                .ToList();
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
    ///     判断同一 handler 映射是否已经由生成注册器或先前扫描步骤写入服务集合。
    /// </summary>
    private static bool IsHandlerMappingAlreadyRegistered(
        IServiceCollection services,
        Type handlerInterface,
        Type implementationType)
    {
        // 这里保持线性扫描，避免为常见的小到中等规模程序集长期维护额外索引。
        // 若未来大型服务集合出现热点，可在更高层批处理中引入 HashSet<(Type, Type)> 做 O(1) 去重。
        return services.Any(descriptor =>
            descriptor.ServiceType == handlerInterface &&
            descriptor.ImplementationType == implementationType);
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

    private sealed class ReflectionFallbackMetadata(IReadOnlyList<Type> types)
    {
        public IReadOnlyList<Type> Types { get; } = types ?? throw new ArgumentNullException(nameof(types));

        public bool HasExplicitTypes => Types.Count > 0;
    }
}
