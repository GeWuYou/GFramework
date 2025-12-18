using GFramework.Core.system;

namespace GFramework.Core.ioc;

/// <summary>
///     IOC容器类，用于管理对象的注册和获取
/// </summary>
public class IocContainer
{
    /// <summary>
    /// 核心存储结构：
    /// 一个 Type 对应 0~N 个实例
    /// </summary>
    private readonly Dictionary<Type, List<object>> _instances = new();

    #region Lock

    /// <summary>
    /// 读写锁对象，用于控制多线程环境下对共享资源的访问
    /// 使用ReaderWriterLockSlim提供高效的读写锁定机制
    /// 配置为不支持递归锁，避免死锁风险
    /// </summary>
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    #endregion

    #region Flag

    /// <summary>
    /// 冻结标志位，用于标识对象是否已被冻结
    /// true表示对象已冻结，不可修改；false表示对象可正常修改
    /// </summary>
    private bool _frozen;

    #endregion


    #region Register

    /// <summary>
    /// 注册单例（强语义）
    /// 一个类型只允许一个实例
    /// </summary>
    /// <typeparam name="T">要注册为单例的类型</typeparam>
    /// <param name="instance">要注册的单例实例</param>
    /// <exception cref="InvalidOperationException">当该类型已经注册过单例时抛出异常</exception>
    public void RegisterSingleton<T>(T instance)
    {
        var type = typeof(T);

        _lock.EnterWriteLock();
        try
        {
            if (_instances.TryGetValue(type, out var list) && list.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Singleton already registered for type: {type.Name}");
            }

            _instances[type] = [instance!];
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }


    /// <summary>
    /// 注册一个实例及其所有可赋值的接口类型到容器中
    /// </summary>
    /// <typeparam name="T">实例的类型</typeparam>
    /// <param name="instance">要注册的实例对象，不能为null</param>
    public void RegisterPlurality<T>(T instance)
    {
        var concreteType = instance!.GetType();
        // 获取实例类型直接实现的所有接口，并筛选出可以赋值给T类型的接口
        var interfaces = concreteType.GetInterfaces()
            .Where(typeof(T).IsAssignableFrom);

        _lock.EnterWriteLock();
        try
        {
            // 注册具体类型
            RegisterInternal(concreteType, instance);

            // 注册所有匹配的接口类型
            foreach (var itf in interfaces)
            {
                RegisterInternal(itf, instance);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 在内部字典中注册指定类型和实例的映射关系
    /// </summary>
    /// <param name="type">要注册的类型</param>
    /// <param name="instance">要注册的实例</param>
    private void RegisterInternal(Type type, object instance)
    {
        if (_frozen)
            throw new InvalidOperationException("IocContainer is frozen");
        // 如果该类型还没有对应的实例列表，则创建一个新的列表
        if (!_instances.TryGetValue(type, out var list))
        {
            list = [];
            _instances[type] = list;
        }

        // 避免重复添加相同的实例
        if (!list.Contains(instance))
        {
            list.Add(instance);
        }
    }

    /// <summary>
    /// 注册系统实例，将其绑定到其所有实现的接口上
    /// </summary>
    /// <param name="system">系统实例对象</param>
    public void RegisterSystem(ISystem system)
    {
        RegisterPlurality(system);
    }


    /// <summary>
    /// 注册指定类型的实例到容器中
    /// </summary>
    /// <typeparam name="T">要注册的实例类型</typeparam>
    /// <param name="instance">要注册的实例对象，不能为null</param>
    public void Register<T>(T instance)
    {
        // 获取写锁以确保线程安全
        _lock.EnterWriteLock();
        try
        {
            RegisterInternal(typeof(T), instance!);
        }
        finally
        {
            // 释放写锁
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 注册指定类型的实例到容器中
    /// </summary>
    /// <param name="type">要注册的实例类型</param>
    /// <param name="instance">要注册的实例对象</param>
    public void Register(Type type, object instance)
    {
        // 获取写锁以确保线程安全
        _lock.EnterWriteLock();
        try
        {
            RegisterInternal(type, instance);
        }
        finally
        {
            // 释放写锁
            _lock.ExitWriteLock();
        }
    }

    #endregion

    #region Get

    /// <summary>
    /// 获取单个实例（通常用于具体类型）
    /// 如果存在多个，只返回第一个
    /// </summary>
    /// <typeparam name="T">期望获取的实例类型</typeparam>
    /// <returns>找到的第一个实例；如果未找到则返回 null</returns>
    public T? Get<T>() where T : class
    {
        _lock.EnterReadLock();
        try
        {
            if (_instances.TryGetValue(typeof(T), out var list) && list.Count > 0)
            {
                return list[0] as T;
            }

            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }


    /// <summary>
    /// 获取指定类型的必需实例
    /// </summary>
    /// <typeparam name="T">期望获取的实例类型</typeparam>
    /// <returns>找到的唯一实例</returns>
    /// <exception cref="InvalidOperationException">当没有注册实例或注册了多个实例时抛出</exception>
    public T GetRequired<T>() where T : class
    {
        var list = GetAll<T>();

        // 根据实例数量进行判断和处理
        return list.Count switch
        {
            0 => throw new InvalidOperationException($"No instance registered for {typeof(T).Name}"),
            > 1 => throw new InvalidOperationException($"Multiple instances registered for {typeof(T).Name}"),
            _ => list[0]
        };
    }


    /// <summary>
    /// 获取指定类型的所有实例（接口 / 抽象类推荐使用）
    /// </summary>
    /// <typeparam name="T">期望获取的实例类型</typeparam>
    /// <returns>所有符合条件的实例列表；如果没有则返回空数组</returns>
    public IReadOnlyList<T> GetAll<T>() where T : class
    {
        _lock.EnterReadLock();
        try
        {
            return _instances.TryGetValue(typeof(T), out var list)
                ? list.Cast<T>().ToList() // 快照
                : Array.Empty<T>();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }


    /// <summary>
    /// 获取并排序（系统调度专用）
    /// </summary>
    /// <typeparam name="T">期望获取的实例类型</typeparam>
    /// <param name="comparison">比较器委托，定义排序规则</param>
    /// <returns>按指定方式排序后的实例列表</returns>
    public IReadOnlyList<T> GetAllSorted<T>(Comparison<T> comparison)
        where T : class
    {
        var list = GetAll<T>().ToList();
        list.Sort(comparison);
        return list;
    }

    #endregion

    #region Utility

    /// <summary>
    /// 检查容器中是否包含指定类型的实例
    /// </summary>
    /// <typeparam name="T">要检查的类型</typeparam>
    /// <returns>如果容器中包含指定类型的实例则返回true，否则返回false</returns>
    public bool Contains<T>()
    {
        _lock.EnterReadLock();
        try
        {
            return _instances.ContainsKey(typeof(T));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 清空容器中的所有实例
    /// </summary>
    public void Clear()
    {
        // 获取写锁以确保线程安全的清空操作
        _lock.EnterWriteLock();
        try
        {
            _instances.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 冻结容器，防止后续修改
    /// </summary>
    public void Freeze()
    {
        // 获取写锁以确保线程安全的状态修改
        _lock.EnterWriteLock();
        try
        {
            _frozen = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    #endregion
}