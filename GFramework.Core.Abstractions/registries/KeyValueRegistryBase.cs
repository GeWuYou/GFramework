using GFramework.Core.Abstractions.bases;
using GFramework.Game.Abstractions.registry;

namespace GFramework.Core.Abstractions.registries;

/// <summary>
///     基于Dictionary的通用键值注册表基类
///     提供基于字典的键值对注册、查询和管理功能
/// </summary>
/// <typeparam name="TKey">键的类型</typeparam>
/// <typeparam name="TValue">值的类型</typeparam>
public abstract class KeyValueRegistryBase<TKey, TValue>
    : IRegistry<TKey, TValue>
{
    /// <summary>
    ///     存储键值对映射关系的字典
    /// </summary>
    protected readonly IDictionary<TKey, TValue> Map;

    /// <summary>
    ///     初始化KeyValueRegistryBase的新实例
    /// </summary>
    /// <param name="comparer">用于比较键的相等性的比较器，如果为null则使用默认比较器</param>
    protected KeyValueRegistryBase(IEqualityComparer<TKey>? comparer = null)
    {
        // 使用指定的比较器或默认比较器创建字典
        Map = new Dictionary<TKey, TValue>(comparer ?? EqualityComparer<TKey>.Default);
    }

    /// <summary>
    ///     根据指定的键获取对应的值
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <returns>与键关联的值</returns>
    /// <exception cref="KeyNotFoundException">当键不存在时抛出异常</exception>
    public virtual TValue Get(TKey key)
    {
        return Map.TryGetValue(key, out var value)
            ? value
            : throw new KeyNotFoundException($"{GetType().Name}: key not registered: {key}");
    }

    /// <summary>
    ///     判断是否包含指定的键
    /// </summary>
    /// <param name="key">要检查的键</param>
    /// <returns>如果包含该键返回true，否则返回false</returns>
    public virtual bool Contains(TKey key)
    {
        return Map.ContainsKey(key);
    }

    /// <summary>
    ///     注册键值对到注册表中
    /// </summary>
    /// <param name="key">要注册的键</param>
    /// <param name="value">要注册的值</param>
    /// <returns>当前注册表实例，支持链式调用</returns>
    public virtual IRegistry<TKey, TValue> Registry(TKey key, TValue value)
    {
        Map.Add(key, value);
        return this;
    }

    /// <summary>
    ///     注册键值对映射对象到注册表中
    /// </summary>
    /// <param name="mapping">包含键值对的映射对象</param>
    /// <returns>当前注册表实例，支持链式调用</returns>
    public virtual IRegistry<TKey, TValue> Registry(IKeyValue<TKey, TValue> mapping)
    {
        return Registry(mapping.Key, mapping.Value);
    }
}