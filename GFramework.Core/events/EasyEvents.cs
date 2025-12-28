using GFramework.Core.Abstractions.events;

namespace GFramework.Core.events;

/// <summary>
///     EasyEvents事件管理器类，用于全局事件的注册、获取和管理
///     提供了类型安全的事件系统，支持泛型事件的自动创建和检索
/// </summary>
public class EasyEvents
{
    /// <summary>
    ///     全局单例事件管理器实例
    /// </summary>
    private static readonly EasyEvents MGlobalEvents = new();

    /// <summary>
    ///     存储事件类型与事件实例映射关系的字典
    /// </summary>
    private readonly Dictionary<Type, IEasyEvent> _mTypeEvents = new();

    /// <summary>
    ///     获取指定类型的全局事件实例
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口</typeparam>
    /// <returns>指定类型的事件实例，如果未注册则返回默认值</returns>
    public static T Get<T>() where T : IEasyEvent
    {
        return MGlobalEvents.GetEvent<T>();
    }

    /// <summary>
    ///     注册指定类型的全局事件
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口且具有无参构造函数</typeparam>
    public static void Register<T>() where T : IEasyEvent, new()
    {
        MGlobalEvents.AddEvent<T>();
    }

    /// <summary>
    ///     添加指定类型的事件到事件字典中
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口且具有无参构造函数</typeparam>
    public void AddEvent<T>() where T : IEasyEvent, new()
    {
        _mTypeEvents.Add(typeof(T), new T());
    }

    /// <summary>
    ///     获取指定类型的事件实例
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口</typeparam>
    /// <returns>指定类型的事件实例，如果不存在则返回默认值</returns>
    public T GetEvent<T>() where T : IEasyEvent
    {
        return _mTypeEvents.TryGetValue(typeof(T), out var e) ? (T)e : default;
    }

    /// <summary>
    ///     获取指定类型的事件实例，如果不存在则创建并添加到事件字典中
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口且具有无参构造函数</typeparam>
    /// <returns>指定类型的事件实例</returns>
    public T GetOrAddEvent<T>() where T : IEasyEvent, new()
    {
        var eType = typeof(T);
        // 尝试从字典中获取事件实例
        if (_mTypeEvents.TryGetValue(eType, out var e)) return (T)e;

        // 如果不存在则创建新实例并添加到字典中
        var t = new T();
        _mTypeEvents.Add(eType, t);
        return t;
    }
}