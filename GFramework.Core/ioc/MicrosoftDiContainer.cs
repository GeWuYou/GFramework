using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.system;
using GFramework.Core.logging;
using GFramework.Core.rule;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.ioc;

/// <summary>
/// Microsoft.Extensions.DependencyInjection 适配器
/// 将 Microsoft DI 包装为 IIocContainer 接口实现
/// 提供线程安全的依赖注入容器功能
/// </summary>
/// <param name="serviceCollection">可选的IServiceCollection实例，默认创建新的ServiceCollection</param>
public class MicrosoftDiContainer(IServiceCollection? serviceCollection = null) : ContextAwareBase, IIocContainer
{
    #region Context Ready

    /// <summary>
    /// 上下文准备就绪时的回调方法
    /// 初始化日志记录器实例
    /// </summary>
    protected override void OnContextReady()
    {
        _logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(MicrosoftDiContainer));
    }

    #endregion

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
    private ILogger _logger = null!;

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
            if (Services.Any(s => s.ServiceType == type))
            {
                var errorMsg = $"Singleton already registered for type: {type.Name}";
                _logger.Error(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            Services.AddSingleton(type, instance!);
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
        where TImpl : class, TService where TService : class
    {
        Services.AddSingleton<TService, TImpl>();
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
            Services.AddSingleton(concreteType, instance);

            // 注册所有接口类型映射（指向同一实例）
            foreach (var interfaceType in interfaces)
            {
                Services.AddSingleton(interfaceType, _ => instance);
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
            Services.AddSingleton(typeof(T), instance!);
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
            Services.AddSingleton(type, instance);
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
    public void RegisterFactory<TService>(Func<IServiceProvider, TService> factory)
    {
        ThrowIfFrozen();
        Services.AddSingleton(factory);
    }

    #endregion

    #region Get

    /// <summary>
    /// 获取指定泛型类型的服务实例
    /// 返回第一个匹配的注册实例，如果不存在则返回null
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例或null</returns>
    /// <exception cref="InvalidOperationException">当容器未冻结时抛出</exception>
    public T? Get<T>() where T : class
    {
        EnsureProvider();
        _lock.EnterReadLock();
        try
        {
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
    /// <exception cref="InvalidOperationException">当容器未冻结时抛出</exception>
    public object? Get(Type type)
    {
        EnsureProvider();
        _lock.EnterReadLock();
        try
        {
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
    /// <exception cref="InvalidOperationException">当容器未冻结时抛出</exception>
    public IReadOnlyList<T> GetAll<T>() where T : class
    {
        EnsureProvider();
        _lock.EnterReadLock();
        try
        {
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
    /// <exception cref="InvalidOperationException">当容器未冻结时抛出</exception>
    public IReadOnlyList<object> GetAll(Type type)
    {
        EnsureProvider();
        _lock.EnterReadLock();
        try
        {
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
        if (_provider == null)
            return Services.Any(s => s.ServiceType == typeof(T));

        _lock.EnterReadLock();
        try
        {
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

            Services.Clear();
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

            _provider = Services.BuildServiceProvider();
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
    public IServiceCollection Services { get; } = serviceCollection ?? new ServiceCollection();

    #endregion

    #region Helper Methods

    /// <summary>
    /// 检查容器是否已冻结，如果已冻结则抛出异常
    /// 用于保护注册操作的安全性
    /// </summary>
    /// <exception cref="InvalidOperationException">当容器已冻结时抛出</exception>
    private void ThrowIfFrozen()
    {
        if (_frozen)
        {
            const string errorMsg = "MicrosoftDiContainer is frozen";
            _logger.Error(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }
    }

    /// <summary>
    /// 确保ServiceProvider已构建，如果未构建则抛出异常
    /// 用于保护获取服务操作的安全性
    /// </summary>
    /// <exception cref="InvalidOperationException">当ServiceProvider未构建时抛出</exception>
    private void EnsureProvider()
    {
        if (_provider == null)
        {
            throw new InvalidOperationException(
                "Container has not been frozen yet. Call Freeze() before retrieving services.");
        }
    }

    #endregion
}