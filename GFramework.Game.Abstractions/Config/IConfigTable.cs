using GFramework.Core.Abstractions.Utility;

namespace GFramework.Game.Abstractions.Config;

/// <summary>
///     定义配置表的非泛型公共契约，用于在注册表中保存异构配置表实例。
///     该接口只暴露运行时发现和诊断所需的元数据，不提供具体类型访问能力。
/// </summary>
public interface IConfigTable : IUtility
{
    /// <summary>
    ///     获取配置表主键类型。
    /// </summary>
    Type KeyType { get; }

    /// <summary>
    ///     获取配置项值类型。
    /// </summary>
    Type ValueType { get; }

    /// <summary>
    ///     获取当前配置表中的条目数量。
    /// </summary>
    int Count { get; }
}

/// <summary>
///     定义强类型只读配置表契约。
///     运行时配置表应通过主键执行只读查询，而不是暴露可变集合接口，
///     以保持配置数据在加载完成后的稳定性和可预测性。
/// </summary>
/// <typeparam name="TKey">配置表主键类型。</typeparam>
/// <typeparam name="TValue">配置项值类型。</typeparam>
public interface IConfigTable<TKey, TValue> : IConfigTable
    where TKey : notnull
{
    /// <summary>
    ///     获取指定主键的配置项。
    /// </summary>
    /// <param name="key">配置项主键。</param>
    /// <returns>找到的配置项。</returns>
    /// <exception cref="KeyNotFoundException">当主键不存在时抛出。</exception>
    TValue Get(TKey key);

    /// <summary>
    ///     尝试获取指定主键的配置项。
    /// </summary>
    /// <param name="key">配置项主键。</param>
    /// <param name="value">找到的配置项；未找到时返回默认值。</param>
    /// <returns>找到配置项时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    bool TryGet(TKey key, out TValue? value);

    /// <summary>
    ///     检查指定主键是否存在。
    /// </summary>
    /// <param name="key">配置项主键。</param>
    /// <returns>主键存在时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    bool ContainsKey(TKey key);

    /// <summary>
    ///     获取配置表中的所有配置项快照。
    /// </summary>
    /// <returns>只读配置项集合。</returns>
    IReadOnlyCollection<TValue> All();
}