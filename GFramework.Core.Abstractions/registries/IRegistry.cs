using GFramework.Core.Abstractions.bases;

namespace GFramework.Core.Abstractions.registries;

/// <summary>
///     表示一个通用的注册表接口，用于根据键类型T获取值类型TR的对象
/// </summary>
/// <typeparam name="T">注册表中用作键的类型</typeparam>
/// <typeparam name="Tr">注册表中存储的值的类型</typeparam>
public interface IRegistry<T, Tr>
{
    /// <summary>
    ///     获取注册表中所有的键
    /// </summary>
    IEnumerable<T> Keys { get; }

    /// <summary>
    ///     获取注册表中项的数量
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     根据指定的键获取对应的值
    /// </summary>
    /// <param name="key">用于查找值的键</param>
    /// <returns>与指定键关联的值</returns>
    Tr Get(T key);

    /// <summary>
    ///     检查注册表是否包含指定的键
    /// </summary>
    /// <param name="key">要检查的键</param>
    /// <returns>如果注册表包含具有指定键的元素，则为true；否则为false</returns>
    bool Contains(T key);

    /// <summary>
    ///     添加一个键值对到注册表中
    /// </summary>
    /// <param name="mapping">要添加的键值对映射对象</param>
    IRegistry<T, Tr> Registry(IKeyValue<T, Tr> mapping);

    /// <summary>
    ///     添加一个键值对到注册表中
    /// </summary>
    /// <param name="key">要添加的键</param>
    /// <param name="value">要添加的值</param>
    IRegistry<T, Tr> Registry(T key, Tr value);

    /// <summary>
    ///     从注册表中移除指定键的项
    /// </summary>
    /// <param name="key">要移除的键</param>
    /// <returns>如果成功移除则返回true，否则返回false</returns>
    bool Unregister(T key);

    /// <summary>
    ///     获取注册表中所有的键值对
    /// </summary>
    /// <returns>包含所有注册键值对的只读字典</returns>
    IReadOnlyDictionary<T, Tr> GetAll();


    /// <summary>
    ///     获取注册表中所有的值
    /// </summary>
    /// <returns>包含所有注册值的只读集合</returns>
    IReadOnlyCollection<Tr> Values();
}