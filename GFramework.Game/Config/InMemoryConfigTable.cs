using System.Collections.ObjectModel;
using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     基于内存字典的只读配置表实现。
///     该实现用于 Runtime MVP 阶段，为加载器和注册表提供稳定的只读查询对象。
/// </summary>
/// <typeparam name="TKey">配置表主键类型。</typeparam>
/// <typeparam name="TValue">配置项值类型。</typeparam>
public sealed class InMemoryConfigTable<TKey, TValue> : IConfigTable<TKey, TValue>
    where TKey : notnull
{
    private readonly IReadOnlyCollection<TValue> _allValues;
    private readonly IReadOnlyDictionary<TKey, TValue> _entries;

    /// <summary>
    ///     使用配置项序列和主键选择器创建内存配置表。
    /// </summary>
    /// <param name="values">配置项序列。</param>
    /// <param name="keySelector">用于提取主键的委托。</param>
    /// <param name="comparer">可选的主键比较器。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="values" /> 或 <paramref name="keySelector" /> 为空时抛出。</exception>
    /// <exception cref="InvalidOperationException">当配置项主键重复时抛出。</exception>
    public InMemoryConfigTable(
        IEnumerable<TValue> values,
        Func<TValue, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(keySelector);

        var dictionary = new Dictionary<TKey, TValue>(comparer);
        var allValues = new List<TValue>();

        foreach (var value in values)
        {
            var key = keySelector(value);

            // 配置表必须在加载期拒绝重复主键，否则运行期查询结果将不可预测。
            if (!dictionary.TryAdd(key, value))
            {
                throw new InvalidOperationException(
                    $"Duplicate config key '{key}' was detected for table value type '{typeof(TValue).Name}'.");
            }

            allValues.Add(value);
        }

        _entries = new ReadOnlyDictionary<TKey, TValue>(dictionary);
        _allValues = new ReadOnlyCollection<TValue>(allValues);
    }

    /// <summary>
    ///     获取配置表的主键类型。
    /// </summary>
    public Type KeyType => typeof(TKey);

    /// <summary>
    ///     获取配置表的值类型。
    /// </summary>
    public Type ValueType => typeof(TValue);

    /// <summary>
    ///     获取配置表中配置项的数量。
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    ///     根据主键获取配置项的值。
    /// </summary>
    /// <param name="key">要查找的配置项主键。</param>
    /// <returns>返回对应主键的配置项值。</returns>
    /// <exception cref="KeyNotFoundException">当指定主键的配置项不存在时抛出。</exception>
    public TValue Get(TKey key)
    {
        if (!_entries.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException(
                $"Config key '{key}' was not found in table '{typeof(TValue).Name}'.");
        }

        return value;
    }

    /// <summary>
    ///     尝试根据主键获取配置项的值，操作失败时不会抛出异常。
    /// </summary>
    /// <param name="key">要查找的配置项主键。</param>
    /// <param name="value">
    ///     输出参数，如果查找成功则返回对应的配置项值，否则为默认值。
    /// </param>
    /// <returns>如果找到指定主键的配置项则返回 true，否则返回 false。</returns>
    public bool TryGet(TKey key, out TValue? value)
    {
        return _entries.TryGetValue(key, out value);
    }

    /// <summary>
    ///     检查指定主键的配置项是否存在于配置表中。
    /// </summary>
    /// <param name="key">要检查的配置项主键。</param>
    /// <returns>如果配置项已存在则返回 true，否则返回 false。</returns>
    public bool ContainsKey(TKey key)
    {
        return _entries.ContainsKey(key);
    }

    /// <summary>
    ///     获取配置表中所有配置项的集合。
    /// </summary>
    /// <returns>返回所有配置项值的只读集合。</returns>
    public IReadOnlyCollection<TValue> All()
    {
        return _allValues;
    }
}