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

        if (_instances.TryGetValue(type, out var list) && list.Count > 0)
        {
            throw new InvalidOperationException(
                $"Singleton already registered for type: {type.Name}");
        }

        _instances[type] = new List<object> { instance! };
    }

    /// <summary>
    /// 注册多个接口实现，并将其实例同时绑定到其所有匹配的接口上
    /// </summary>
    /// <typeparam name="T">基类型或接口类型</typeparam>
    /// <param name="instance">具体的实例对象</param>
    public void RegisterPlurality<T>(T instance)
    {
        var concreteType = instance!.GetType();
        Register(concreteType, instance);
        // 获取实例类型实现的所有接口，并筛选出可以赋值给T类型的接口
        var interfaces = concreteType.GetInterfaces()
            .Where(typeof(T).IsAssignableFrom);
        foreach (var itf in interfaces)
        {
            Register(itf, instance);
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
    /// 注册实例（自动支持多实例）
    /// </summary>
    /// <typeparam name="T">要注册的实例的类型</typeparam>
    /// <param name="instance">要注册的实例</param>
    public void Register<T>(T instance)
    {
        Register(typeof(T), instance!);
    }

    /// <summary>
    /// 按指定类型注册实例
    /// （常用于 interface / base type）
    /// </summary>
    /// <param name="type">要注册的目标类型</param>
    /// <param name="instance">要注册的实例</param>
    public void Register(Type type, object instance)
    {
        if (!_instances.TryGetValue(type, out var list))
        {
            list = new List<object>();
            _instances[type] = list;
        }

        // 防止重复注册同一个实例
        if (!list.Contains(instance))
        {
            list.Add(instance);
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
        var type = typeof(T);

        // 尝试从实例字典中获取指定类型的实例列表
        if (_instances.TryGetValue(type, out var list) && list.Count > 0)
        {
            return list[0] as T;
        }

        return null;
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
        var type = typeof(T);

        if (_instances.TryGetValue(type, out var list))
        {
            return list.Cast<T>().ToList();
        }

        return Array.Empty<T>();
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
    /// 是否已注册指定类型
    /// </summary>
    /// <typeparam name="T">要检查是否注册的类型</typeparam>
    /// <returns>若该类型已被注册则返回 true，否则返回 false</returns>
    public bool Contains<T>()
    {
        return _instances.ContainsKey(typeof(T));
    }

    /// <summary>
    /// 清空容器
    /// </summary>
    public void Clear()
    {
        _instances.Clear();
    }

    #endregion
}