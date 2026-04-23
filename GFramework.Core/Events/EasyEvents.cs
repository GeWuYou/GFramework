using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>
///     EasyEvents事件管理器类，用于全局事件的注册、获取和管理
///     提供了类型安全的事件系统，支持泛型事件的自动创建和检索
///     线程安全：所有公共方法都是线程安全的
/// </summary>
public class EasyEvents
{
    /// <summary>
    ///     全局单例事件管理器实例
    /// </summary>
    private static readonly EasyEvents MGlobalEvents = new();

    /// <summary>
    ///     存储事件类型与事件实例映射关系的字典（线程安全）
    /// </summary>
    private readonly ConcurrentDictionary<Type, IEvent> _mTypeEvents = new();

    /// <summary>
    ///     获取指定类型的全局事件实例
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口</typeparam>
    /// <returns>指定类型的事件实例，如果未注册则返回默认值</returns>
    public static T Get<T>() where T : IEvent
    {
        return MGlobalEvents.GetEvent<T>();
    }

    /// <summary>
    ///     注册指定类型的全局事件
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口且具有无参构造函数</typeparam>
    public static void Register<T>() where T : IEvent, new()
    {
        MGlobalEvents.AddEvent<T>();
    }

    /// <summary>
    ///     获取或添加指定类型的全局事件
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口且具有无参构造函数</typeparam>
    /// <returns>指定类型的事件实例</returns>
    public static T GetOrAdd<T>() where T : IEvent, new()
    {
        return MGlobalEvents.GetOrAddEvent<T>();
    }

    /// <summary>
    ///     添加指定类型的事件到事件字典中
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口且具有无参构造函数</typeparam>
    /// <exception cref="ArgumentException">当事件类型已存在时抛出。</exception>
    public void AddEvent<T>() where T : IEvent, new()
    {
        if (!_mTypeEvents.TryAdd(typeof(T), new T()))
        {
#pragma warning disable MA0015 // Preserve the public ArgumentException contract without inventing a fake parameter name.
            throw new ArgumentException($"Event type {typeof(T).Name} already registered.");
#pragma warning restore MA0015
        }
    }

    /// <summary>
    ///     获取指定类型的事件实例
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口</typeparam>
    /// <returns>指定类型的事件实例，如果不存在则返回默认值</returns>
    public T GetEvent<T>() where T : IEvent
    {
        return _mTypeEvents.TryGetValue(typeof(T), out var e) ? (T)e : default!;
    }

    /// <summary>
    ///     获取指定类型的事件实例，如果不存在则创建并添加到事件字典中
    /// </summary>
    /// <typeparam name="T">事件类型，必须实现IEasyEvent接口且具有无参构造函数</typeparam>
    /// <returns>指定类型的事件实例</returns>
    public T GetOrAddEvent<T>() where T : IEvent, new()
    {
        return (T)_mTypeEvents.GetOrAdd(typeof(T), _ => new T());
    }
}
