using System.ComponentModel;
using System.Reflection;
using GFramework.Core.Abstractions.Bases;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Rule;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Ioc;

/// <summary>
/// Microsoft.Extensions.DependencyInjection 适配器
/// 将 Microsoft DI 包装为 IIocContainer 接口实现
/// 提供线程安全的依赖注入容器功能
/// </summary>
/// <param name="serviceCollection">可选的IServiceCollection实例，默认创建新的ServiceCollection</param>
public class MicrosoftDiContainer(IServiceCollection? serviceCollection = null) : ContextAwareBase, IIocContainer
{
    #region Helper Methods

    /// <summary>
    /// 检查容器是否已冻结，如果已冻结则抛出异常
    /// 用于保护注册操作的安全性
    /// </summary>
    /// <exception cref="InvalidOperationException">当容器已冻结时抛出</exception>
    private void ThrowIfFrozen()
    {
        if (!_frozen) return;
        const string errorMsg = "MicrosoftDiContainer is frozen";
        _logger.Error(errorMsg);
        throw new InvalidOperationException(errorMsg);
    }

    #endregion

    /// <summary>
    ///     记录某个实例在未冻结查询中可见的服务类型分组信息。
    /// </summary>
    /// <param name="ServiceType">当前分组对应的服务类型。</param>
    /// <param name="Count">该服务类型下的描述符数量。</param>
    /// <param name="FirstIndex">该服务类型首次出现的位置，用于稳定打破并列。</param>
    private sealed record VisibleServiceTypeGroup(Type ServiceType, int Count, int FirstIndex);

    #region Fields

    /// <summary>
    /// 服务提供者，在容器冻结后构建，用于解析服务实例
    /// </summary>
    private IServiceProvider? _provider;

    /// <summary>
    /// 容器冻结状态标志，true表示容器已冻结不可修改
    /// </summary>
    private volatile bool _frozen;

    /// <summary>
    /// 读写锁，确保多线程环境下的线程安全操作
    /// </summary>
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    /// <summary>
    /// 已注册实例的集合，用于快速检查实例是否存在
    /// </summary>
    private readonly HashSet<object> _registeredInstances = [];

    /// <summary>
    /// 日志记录器，用于记录容器操作日志
    /// </summary>
    private readonly ILogger _logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(MicrosoftDiContainer));

    #endregion

    #region Register

    /// <summary>
    /// 注册单例服务实例
    /// 确保同一类型只能注册一次，避免重复注册
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="instance">要注册的实例对象</param>
    /// <exception cref="InvalidOperationException">当容器已冻结或类型已被注册时抛出</exception>
    public void RegisterSingleton<T>(T instance)
    {
        var type = typeof(T);
        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();

            // 检查是否已注册该类型，防止重复注册
            if (GetServicesUnsafe.Any(s => s.ServiceType == type))
            {
                var errorMsg = $"Singleton already registered for type: {type.Name}";
                _logger.Error(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            GetServicesUnsafe.AddSingleton(type, instance!);
            _registeredInstances.Add(instance!);
            _logger.Debug($"Singleton registered: {type.Name}");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 注册单例服务，指定服务类型和实现类型
    /// 直接使用底层DI容器注册类型映射关系
    /// </summary>
    /// <typeparam name="TService">服务接口或基类类型</typeparam>
    /// <typeparam name="TImpl">具体的实现类型</typeparam>
    public void RegisterSingleton<TService, TImpl>()
        where TImpl : class, TService
        where TService : class
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();
            GetServicesUnsafe.AddSingleton<TService, TImpl>();
            _logger.Debug($"Singleton registered: {typeof(TService).Name}");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 注册瞬态服务，指定服务类型和实现类型
    /// 每次解析时都会创建新的实例
    /// </summary>
    /// <typeparam name="TService">服务接口或基类类型</typeparam>
    /// <typeparam name="TImpl">具体的实现类型</typeparam>
    public void RegisterTransient<TService, TImpl>()
        where TImpl : class, TService
        where TService : class
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();
            GetServicesUnsafe.AddTransient<TService, TImpl>();
            _logger.Debug($"Transient registered: {typeof(TService).Name}");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 注册作用域服务，指定服务类型和实现类型
    /// 在同一作用域内共享实例，不同作用域使用不同实例
    /// </summary>
    /// <typeparam name="TService">服务接口或基类类型</typeparam>
    /// <typeparam name="TImpl">具体的实现类型</typeparam>
    public void RegisterScoped<TService, TImpl>()
        where TImpl : class, TService
        where TService : class
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();
            GetServicesUnsafe.AddScoped<TService, TImpl>();
            _logger.Debug($"Scoped registered: {typeof(TService).Name}");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }


    /// <summary>
    /// 注册多个实例到其所有接口和具体类型
    /// 实现一个实例支持多种接口类型的解析
    /// </summary>
    /// <param name="instance">要注册的对象实例</param>
    /// <exception cref="InvalidOperationException">当容器已冻结时抛出</exception>
    public void RegisterPlurality(object instance)
    {
        var concreteType = instance.GetType();
        var interfaces = concreteType.GetInterfaces();

        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();

            // 注册具体类型映射
            GetServicesUnsafe.AddSingleton(concreteType, instance);

            // 注册所有接口类型映射（指向同一实例）
            foreach (var interfaceType in interfaces)
            {
                GetServicesUnsafe.AddSingleton(interfaceType, _ => instance);
            }

            _registeredInstances.Add(instance);
            _logger.Debug($"Plurality registered: {concreteType.Name} with {interfaces.Length} interfaces");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 注册多个实例到其所有接口
    /// 实现一个实例支持多种接口类型的解析
    /// </summary>
    public void RegisterPlurality<T>() where T : class
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();

            var concreteType = typeof(T);
            var interfaces = concreteType.GetInterfaces();

            // 注册具体类型
            GetServicesUnsafe.AddSingleton<T>();

            // 注册所有接口（指向同一个实例）
            foreach (var interfaceType in interfaces)
            {
                GetServicesUnsafe.AddSingleton(interfaceType, sp => sp.GetRequiredService<T>());
            }

            _logger.Debug($"Type registered: {concreteType.Name} with {interfaces.Length} interfaces");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 注册系统实例
    /// 通过RegisterPlurality方法注册ISystem类型实例
    /// </summary>
    /// <param name="system">要注册的系统实例</param>
    public void RegisterSystem(ISystem system)
    {
        RegisterPlurality(system);
    }

    /// <summary>
    /// 注册指定泛型类型的服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="instance">要注册的实例对象</param>
    /// <exception cref="InvalidOperationException">当容器已冻结时抛出</exception>
    public void Register<T>(T instance)
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();
            GetServicesUnsafe.AddSingleton(typeof(T), instance!);
            _registeredInstances.Add(instance!);
            _logger.Debug($"Registered: {typeof(T).Name}");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 注册指定类型的服务实例
    /// </summary>
    /// <param name="type">服务类型</param>
    /// <param name="instance">要注册的实例对象</param>
    /// <exception cref="InvalidOperationException">当容器已冻结时抛出</exception>
    public void Register(Type type, object instance)
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();
            GetServicesUnsafe.AddSingleton(type, instance);
            _registeredInstances.Add(instance);
            _logger.Debug($"Registered: {type.Name}");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 注册工厂方法来创建服务实例
    /// 通过委托函数动态创建服务实例，支持依赖注入
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <param name="factory">创建服务实例的工厂委托函数，接收IServiceProvider参数</param>
    public void RegisterFactory<TService>(
        Func<IServiceProvider, TService> factory) where TService : class
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();
            GetServicesUnsafe.AddSingleton(factory);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }


    /// <summary>
    ///     注册 CQRS 请求管道行为。
    ///     同时支持开放泛型行为类型和已闭合的具体行为类型，
    ///     以兼容通用行为和针对单一请求的专用行为两种注册方式。
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型</typeparam>
    public void RegisterCqrsPipelineBehavior<TBehavior>() where TBehavior : class
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();

            var behaviorType = typeof(TBehavior);

            if (behaviorType.IsGenericTypeDefinition)
            {
                GetServicesUnsafe.AddSingleton(typeof(IPipelineBehavior<,>), behaviorType);
            }
            else
            {
                var pipelineInterfaces = behaviorType
                    .GetInterfaces()
                    .Where(type => type.IsGenericType &&
                                   type.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                    .ToList();

                if (pipelineInterfaces.Count == 0)
                {
                    var errorMessage = $"{behaviorType.Name} does not implement IPipelineBehavior<,>";
                    _logger.Error(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                // 为每个已闭合的管道接口建立显式映射，支持针对特定请求/响应的专用行为。
                foreach (var pipelineInterface in pipelineInterfaces)
                {
                    GetServicesUnsafe.AddSingleton(pipelineInterface, behaviorType);
                }
            }

            _logger.Debug($"CQRS pipeline behavior registered: {behaviorType.Name}");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     注册 CQRS 请求管道行为。
    ///     该成员保留旧名称以兼容历史调用点，内部行为与 <see cref="RegisterCqrsPipelineBehavior{TBehavior}" /> 一致。
    ///     新代码不应继续依赖该别名；兼容层计划在未来的 major 版本中移除。
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Use RegisterCqrsPipelineBehavior<TBehavior>() instead. This compatibility alias will be removed in a future major version.")]
    public void RegisterMediatorBehavior<TBehavior>() where TBehavior : class
    {
        RegisterCqrsPipelineBehavior<TBehavior>();
    }

    /// <summary>
    ///     从指定程序集显式注册 CQRS 处理器。
    /// </summary>
    /// <param name="assembly">包含 CQRS 处理器或生成注册器的程序集。</param>
    /// <exception cref="ArgumentNullException"><paramref name="assembly" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">容器已冻结，无法继续注册 CQRS 处理器。</exception>
    public void RegisterCqrsHandlersFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        RegisterCqrsHandlersFromAssemblies([assembly]);
    }

    /// <summary>
    ///     从多个程序集显式注册 CQRS 处理器。
    ///     同一程序集只会被接入一次，避免默认启动路径与扩展模块重复注册相同 handlers。
    /// </summary>
    /// <param name="assemblies">要接入的程序集集合。</param>
    /// <exception cref="ArgumentNullException"><paramref name="assemblies" /> 为 <see langword="null" />。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="assemblies" /> 中存在 <see langword="null" /> 元素。</exception>
    /// <exception cref="InvalidOperationException">容器已冻结，无法继续注册 CQRS 处理器。</exception>
    public void RegisterCqrsHandlersFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        var assemblyArray = assemblies.ToArray();
        foreach (var assembly in assemblyArray)
        {
            ArgumentNullException.ThrowIfNull(assembly);
        }

        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();
            ResolveCqrsRegistrationService().RegisterHandlers(assemblyArray);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     配置服务
    /// </summary>
    /// <param name="configurator">服务配置委托</param>
    public void ExecuteServicesHook(Action<IServiceCollection>? configurator = null)
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfFrozen();
            configurator?.Invoke(GetServicesUnsafe);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    #endregion

    #region Get

    /// <summary>
    ///     获取当前容器中已注册的 CQRS 程序集注册协调器。
    ///     该方法仅供容器内部在注册阶段使用，因此直接读取服务描述符中的实例绑定，
    ///     避免在容器未冻结前依赖完整的服务提供者构建流程。
    /// </summary>
    /// <returns>已注册的 CQRS 程序集注册协调器实例。</returns>
    /// <exception cref="InvalidOperationException">未找到可用的 CQRS 程序集注册协调器实例时抛出。</exception>
    private ICqrsRegistrationService ResolveCqrsRegistrationService()
    {
        var descriptor = GetServicesUnsafe.LastOrDefault(static service =>
            service.ServiceType == typeof(ICqrsRegistrationService));

        if (descriptor?.ImplementationInstance is ICqrsRegistrationService registrationService)
            return registrationService;

        const string errorMessage =
            "ICqrsRegistrationService not registered. Ensure the CQRS runtime module has been installed before registering handlers.";
        _logger.Error(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    /// <summary>
    /// 获取指定泛型类型的服务实例
    /// 返回第一个匹配的注册实例，如果不存在则返回null
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例或null</returns>
    public T? Get<T>() where T : class
    {
        _lock.EnterReadLock();
        try
        {
            if (_provider == null)
            {
                // 如果容器未冻结，从服务集合中查找已注册的实例
                var serviceType = typeof(T);
                var descriptor = GetServicesUnsafe.FirstOrDefault(s =>
                    s.ServiceType == serviceType || serviceType.IsAssignableFrom(s.ServiceType));

                if (descriptor?.ImplementationInstance is T instance)
                {
                    return instance;
                }

                // 在未冻结状态下无法调用工厂方法或创建实例，返回null
                return null;
            }

            var result = _provider!.GetService<T>();
            _logger.Debug(result != null
                ? $"Retrieved instance: {typeof(T).Name}"
                : $"No instance found for type: {typeof(T).Name}");
            return result;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取指定类型的服务实例
    /// 返回第一个匹配的注册实例，如果不存在则返回null
    /// </summary>
    /// <param name="type">服务类型</param>
    /// <returns>服务实例或null</returns>
    public object? Get(Type type)
    {
        _lock.EnterReadLock();
        try
        {
            if (_provider == null)
            {
                // 如果容器未冻结，从服务集合中查找已注册的实例
                var descriptor =
                    GetServicesUnsafe.FirstOrDefault(s =>
                        s.ServiceType == type || type.IsAssignableFrom(s.ServiceType));

                return descriptor?.ImplementationInstance;
            }

            var result = _provider!.GetService(type);
            _logger.Debug(result != null
                ? $"Retrieved instance: {type.Name}"
                : $"No instance found for type: {type.Name}");
            return result;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取指定泛型类型的必需服务实例
    /// 必须存在且唯一，否则抛出异常
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>唯一的服务实例</returns>
    /// <exception cref="InvalidOperationException">当实例不存在或多于一个时抛出</exception>
    public T GetRequired<T>() where T : class
    {
        var list = GetAll<T>();

        switch (list.Count)
        {
            case 0:
                var notFoundMsg = $"No instance registered for {typeof(T).Name}";
                _logger.Error(notFoundMsg);
                throw new InvalidOperationException(notFoundMsg);

            case 1:
                _logger.Debug($"Retrieved required instance: {typeof(T).Name}");
                return list[0];

            default:
                var multipleMsg = $"Multiple instances registered for {typeof(T).Name}";
                _logger.Error(multipleMsg);
                throw new InvalidOperationException(multipleMsg);
        }
    }

    /// <summary>
    /// 获取指定类型的必需服务实例
    /// 必须存在且唯一，否则抛出异常
    /// </summary>
    /// <param name="type">服务类型</param>
    /// <returns>唯一的服务实例</returns>
    /// <exception cref="InvalidOperationException">当实例不存在或多于一个时抛出</exception>
    public object GetRequired(Type type)
    {
        var list = GetAll(type);

        switch (list.Count)
        {
            case 0:
                var notFoundMsg = $"No instance registered for {type.Name}";
                _logger.Error(notFoundMsg);
                throw new InvalidOperationException(notFoundMsg);

            case 1:
                _logger.Debug($"Retrieved required instance: {type.Name}");
                return list[0];

            default:
                var multipleMsg = $"Multiple instances registered for {type.Name}";
                _logger.Error(multipleMsg);
                throw new InvalidOperationException(multipleMsg);
        }
    }

    /// <summary>
    /// 获取指定泛型类型的所有服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>只读的服务实例列表</returns>
    public IReadOnlyList<T> GetAll<T>() where T : class
    {
        _lock.EnterReadLock();
        try
        {
            if (_provider == null)
            {
                return CollectRegisteredImplementationInstances(typeof(T)).Cast<T>().ToList();
            }

            var services = _provider!.GetServices<T>().ToList();
            _logger.Debug($"Retrieved {services.Count} instances of {typeof(T).Name}");
            return services;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取指定类型的所有服务实例
    /// </summary>
    /// <param name="type">服务类型</param>
    /// <returns>只读的服务实例列表</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type" /> 为 <see langword="null" /> 时抛出</exception>
    public IReadOnlyList<object> GetAll(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        _lock.EnterReadLock();
        try
        {
            if (_provider == null)
            {
                return CollectRegisteredImplementationInstances(type);
            }

            var services = _provider!.GetServices(type).ToList();
            _logger.Debug($"Retrieved {services.Count} instances of {type.Name}");
            return services.Where(o => o != null).Cast<object>().ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     在容器未冻结时，从服务描述符中收集当前可直接观察到的实例绑定。
    /// </summary>
    /// <param name="requestedServiceType">调用方请求的服务类型。</param>
    /// <returns>按当前未冻结语义可见的实例列表。</returns>
    /// <remarks>
    ///     该方法只读取 <see cref="ServiceDescriptor.ImplementationInstance" />，因为未冻结路径不会主动执行工厂方法，
    ///     也不会提前构造 <see cref="ServiceDescriptor.ImplementationType" />。
    ///     若同一实例同时经由多个可赋值的 <see cref="ServiceDescriptor.ServiceType" /> 暴露，
    ///     这里会把它视为兼容别名并只保留一个规范服务类型对应的结果；
    ///     但同一 <see cref="ServiceDescriptor.ServiceType" /> 的重复显式注册仍会完整保留，以维持注册顺序和多次注册语义。
    /// </remarks>
    private List<object> CollectRegisteredImplementationInstances(Type requestedServiceType)
    {
        ArgumentNullException.ThrowIfNull(requestedServiceType);

        var matchingDescriptors = GetServicesUnsafe
            .Where(descriptor =>
                descriptor.ServiceType == requestedServiceType ||
                requestedServiceType.IsAssignableFrom(descriptor.ServiceType))
            .ToList();

        if (matchingDescriptors.Count == 0)
            return [];

        var preferredServiceTypes = BuildPreferredVisibleServiceTypes(matchingDescriptors, requestedServiceType);
        var result = new List<object>();
        foreach (var descriptor in matchingDescriptors)
        {
            if (descriptor.ImplementationInstance is { } instance)
            {
                if (preferredServiceTypes.TryGetValue(instance, out var preferredServiceType) &&
                    preferredServiceType == descriptor.ServiceType)
                {
                    result.Add(instance);
                }
            }
            else if (descriptor.ImplementationFactory != null)
            {
                // 在未冻结状态下无法调用工厂方法，跳过。
            }
            else if (descriptor.ImplementationType != null)
            {
                // 在未冻结状态下无法创建实例，跳过。
            }
        }

        return result;
    }

    /// <summary>
    ///     为每个可见实例选择一个规范服务类型，避免同一实例因兼容别名重复出现在未冻结查询结果中。
    /// </summary>
    /// <param name="matchingDescriptors">已按请求类型过滤过的服务描述符集合。</param>
    /// <param name="requestedServiceType">调用方请求的服务类型。</param>
    /// <returns>实例到其规范服务类型的映射。</returns>
    private static Dictionary<object, Type> BuildPreferredVisibleServiceTypes(
        IReadOnlyList<ServiceDescriptor> matchingDescriptors,
        Type requestedServiceType)
    {
        var preferredServiceTypes = new Dictionary<object, Type>(ReferenceEqualityComparer.Instance);
        foreach (var instanceGroup in matchingDescriptors
                     .Where(static descriptor => descriptor.ImplementationInstance is not null)
                     .GroupBy(static descriptor => descriptor.ImplementationInstance!,
                         ReferenceEqualityComparer.Instance))
        {
            preferredServiceTypes.Add(
                instanceGroup.Key,
                SelectPreferredVisibleServiceType(instanceGroup, requestedServiceType));
        }

        return preferredServiceTypes;
    }

    /// <summary>
    ///     在“同一实例被多个服务类型暴露”的场景下，选择未冻结查询结果应保留的规范服务类型。
    /// </summary>
    /// <param name="descriptorsForInstance">引用同一实例的服务描述符。</param>
    /// <param name="requestedServiceType">调用方请求的服务类型。</param>
    /// <returns>应在结果中保留的服务类型。</returns>
    private static Type SelectPreferredVisibleServiceType(
        IEnumerable<ServiceDescriptor> descriptorsForInstance,
        Type requestedServiceType)
    {
        var serviceTypeGroups = descriptorsForInstance
            .GroupBy(static descriptor => descriptor.ServiceType)
            .Select((group, index) => new VisibleServiceTypeGroup(group.Key, group.Count(), index))
            .ToList();

        // 若调用方请求的正是其中一个服务类型，优先保留它，使未冻结行为尽量贴近冻结后的精确服务解析口径。
        var requestedGroup = serviceTypeGroups.FirstOrDefault(group => group.ServiceType == requestedServiceType);
        if (requestedGroup is not null)
            return requestedGroup.ServiceType;

        // 否则优先保留“同一服务类型下注册次数最多”的那组，避免显式多次注册被较宽泛的别名折叠掉。
        return serviceTypeGroups
            .OrderByDescending(static group => group.Count)
            .ThenBy(static group => group.FirstIndex)
            .First()
            .ServiceType;
    }

    /// <summary>
    /// 获取并排序指定泛型类型的所有服务实例
    /// 主要用于系统调度场景
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="comparison">比较委托，用于定义排序规则</param>
    /// <returns>排序后的只读服务实例列表</returns>
    public IReadOnlyList<T> GetAllSorted<T>(Comparison<T> comparison) where T : class
    {
        var list = GetAll<T>().ToList();
        list.Sort(comparison);
        return list;
    }

    /// <summary>
    /// 获取指定类型的所有实例，并按优先级排序
    /// 实现 IPrioritized 接口的服务将按优先级排序（数值越小优先级越高）
    /// 未实现 IPrioritized 的服务将使用默认优先级 0
    /// </summary>
    /// <typeparam name="T">期望获取的实例类型</typeparam>
    /// <returns>按优先级排序后的实例列表</returns>
    public IReadOnlyList<T> GetAllByPriority<T>() where T : class
    {
        var services = GetAll<T>();
        return SortByPriority(services);
    }

    /// <summary>
    /// 获取指定类型的所有实例，并按优先级排序
    /// 实现 IPrioritized 接口的服务将按优先级排序（数值越小优先级越高）
    /// 未实现 IPrioritized 的服务将使用默认优先级 0
    /// </summary>
    /// <param name="type">期望获取的实例类型</param>
    /// <returns>按优先级排序后的实例列表</returns>
    public IReadOnlyList<object> GetAllByPriority(Type type)
    {
        var services = GetAll(type);
        return SortByPriority(services);
    }

    /// <summary>
    /// 按优先级排序服务列表
    /// 实现 IPrioritized 接口的服务按 Priority 属性排序（升序）
    /// 未实现接口的服务使用默认优先级 0
    /// 相同优先级保持原有注册顺序（稳定排序）
    /// </summary>
    private static IReadOnlyList<T> SortByPriority<T>(IReadOnlyList<T> services) where T : class
    {
        if (services.Count <= 1)
            return services;

        // 使用 OrderBy 确保稳定排序（相同优先级保持原有顺序）
        return services
            .Select((service, index) => new { Service = service, Index = index })
            .OrderBy(x =>
            {
                var priority = x.Service is IPrioritized p ? p.Priority : 0;
                return (priority, x.Index); // 先按优先级，再按索引
            })
            .Select(x => x.Service)
            .ToList();
    }

    #endregion

    #region Utility

    /// <summary>
    /// 检查容器中是否包含指定泛型类型的实例
    /// 根据容器状态选择不同的检查策略
    /// </summary>
    /// <typeparam name="T">要检查的类型</typeparam>
    /// <returns>true表示包含该类型实例，false表示不包含</returns>
    public bool Contains<T>() where T : class
    {
        _lock.EnterReadLock();
        try
        {
            if (_provider == null)
                return GetServicesUnsafe.Any(s => s.ServiceType == typeof(T));

            return _provider.GetService<T>() != null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 判断容器中是否包含某个具体的实例对象
    /// 通过已注册实例集合进行快速查找
    /// </summary>
    /// <param name="instance">要检查的实例对象</param>
    /// <returns>true表示包含该实例，false表示不包含</returns>
    public bool ContainsInstance(object instance)
    {
        _lock.EnterReadLock();
        try
        {
            return _registeredInstances.Contains(instance);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 清空容器中的所有实例和服务注册
    /// 只有在容器未冻结状态下才能执行清空操作
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            // 冻结的容器不允许清空操作
            if (_frozen)
            {
                _logger.Warn("Cannot clear frozen container");
                return;
            }

            GetServicesUnsafe.Clear();
            _registeredInstances.Clear();
            _provider = null;
            _logger.Info("Container cleared");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 冻结容器并构建ServiceProvider
    /// 冻结后容器变为只读状态，不能再注册新服务
    /// </summary>
    public void Freeze()
    {
        _lock.EnterWriteLock();
        try
        {
            // 防止重复冻结
            if (_frozen)
            {
                _logger.Warn("Container already frozen");
                return;
            }

            _provider = GetServicesUnsafe.BuildServiceProvider();
            _frozen = true;
            _logger.Info("IOC Container frozen - ServiceProvider built");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     获取底层的服务集合
    ///     提供对内部IServiceCollection的访问权限，用于高级配置和自定义操作
    /// </summary>
    /// <returns>底层的IServiceCollection实例</returns>
    public IServiceCollection GetServicesUnsafe { get; } = serviceCollection ?? new ServiceCollection();

    /// <summary>
    ///     创建一个新的服务作用域
    ///     作用域内的 Scoped 服务将共享同一实例
    /// </summary>
    /// <returns>服务作用域实例</returns>
    /// <exception cref="InvalidOperationException">当容器未冻结时抛出</exception>
    public IServiceScope CreateScope()
    {
        _lock.EnterReadLock();
        try
        {
            // 在锁内检查，避免竞态条件
            if (!_frozen || _provider == null)
            {
                const string errorMsg = "Cannot create scope before container is frozen";
                _logger.Error(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            var scope = _provider.CreateScope();
            _logger.Debug("Service scope created");
            return scope;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    #endregion
}
