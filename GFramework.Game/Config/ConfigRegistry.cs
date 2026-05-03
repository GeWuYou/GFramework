// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     默认配置注册表实现。
///     该类型负责统一管理按名称注册的配置表，并在消费端提供类型安全的解析入口。
///     为了支持开发期热重载，注册行为采用覆盖策略而不是拒绝重复名称。
/// </summary>
public sealed class ConfigRegistry : IConfigRegistry
{
    private const string NameCannotBeNullOrWhiteSpaceMessage = "Table name cannot be null or whitespace.";

    private readonly ConcurrentDictionary<string, IConfigTable> _tables = new(StringComparer.Ordinal);

    /// <summary>
    ///     获取已注册的配置表数量。
    /// </summary>
    public int Count => _tables.Count;

    /// <summary>
    ///     获取所有已注册配置表的名称集合，按字典序排序。
    /// </summary>
    /// <returns>返回只读的配置表名称集合。</returns>
    public IReadOnlyCollection<string> GetTableNames()
    {
        return _tables.Keys.OrderBy(static key => key, StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    ///     注册一个配置表到注册表中。
    ///     如果同名的配置表已存在，则会覆盖原有注册以支持热重载。
    /// </summary>
    /// <typeparam name="TKey">配置表主键的类型，必须为非空类型。</typeparam>
    /// <typeparam name="TValue">配置表值的类型。</typeparam>
    /// <param name="name">配置表的注册名称，用于后续查找。</param>
    /// <param name="table">要注册的配置表实例。</param>
    /// <exception cref="ArgumentException">当 <paramref name="name" /> 为 null、空或仅包含空白字符时抛出。</exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="table" /> 为 null 时抛出。</exception>
    public void RegisterTable<TKey, TValue>(string name, IConfigTable<TKey, TValue> table)
        where TKey : notnull
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(NameCannotBeNullOrWhiteSpaceMessage, nameof(name));
        }

        ArgumentNullException.ThrowIfNull(table);

        _tables[name] = table;
    }

    /// <summary>
    ///     根据名称获取已注册的配置表，并进行类型验证。
    /// </summary>
    /// <typeparam name="TKey">期望的主键类型，必须为非空类型。</typeparam>
    /// <typeparam name="TValue">期望的值类型。</typeparam>
    /// <param name="name">要查找的配置表名称。</param>
    /// <returns>返回类型匹配的配置表实例。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="name" /> 为 null、空或仅包含空白字符时抛出。</exception>
    /// <exception cref="KeyNotFoundException">当指定名称的配置表不存在时抛出。</exception>
    /// <exception cref="InvalidOperationException">
    ///     当找到的配置表类型与请求的类型不匹配时抛出。
    /// </exception>
    public IConfigTable<TKey, TValue> GetTable<TKey, TValue>(string name)
        where TKey : notnull
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(NameCannotBeNullOrWhiteSpaceMessage, nameof(name));
        }

        if (!_tables.TryGetValue(name, out var table))
        {
            throw new KeyNotFoundException($"Config table '{name}' was not found.");
        }

        if (table is IConfigTable<TKey, TValue> typedTable)
        {
            return typedTable;
        }

        throw new InvalidOperationException(
            $"Config table '{name}' was registered as '{table.KeyType.Name} -> {table.ValueType.Name}', " +
            $"but the caller requested '{typeof(TKey).Name} -> {typeof(TValue).Name}'.");
    }

    /// <summary>
    ///     尝试根据名称获取配置表，操作失败时不会抛出异常。
    /// </summary>
    /// <typeparam name="TKey">期望的主键类型，必须为非空类型。</typeparam>
    /// <typeparam name="TValue">期望的值类型。</typeparam>
    /// <param name="name">要查找的配置表名称。</param>
    /// <param name="table">
    ///     输出参数，如果查找成功则返回类型匹配的配置表实例，否则为 null。
    /// </param>
    /// <returns>如果找到指定名称且类型匹配的配置表则返回 true，否则返回 false。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="name" /> 为 null、空或仅包含空白字符时抛出。</exception>
    public bool TryGetTable<TKey, TValue>(string name, out IConfigTable<TKey, TValue>? table)
        where TKey : notnull
    {
        table = default;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(NameCannotBeNullOrWhiteSpaceMessage, nameof(name));
        }

        if (!_tables.TryGetValue(name, out var rawTable))
        {
            return false;
        }

        if (rawTable is not IConfigTable<TKey, TValue> typedTable)
        {
            return false;
        }

        table = typedTable;
        return true;
    }

    /// <summary>
    ///     尝试根据名称获取原始配置表。
    /// </summary>
    /// <param name="name">要查找的配置表名称。</param>
    /// <param name="table">输出参数，如果查找成功则返回原始配置表实例，否则为 null。</param>
    /// <returns>如果找到指定名称的配置表则返回 true，否则返回 false。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="name" /> 为 null、空或仅包含空白字符时抛出。</exception>
    public bool TryGetTable(string name, out IConfigTable? table)
    {
        table = default;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(NameCannotBeNullOrWhiteSpaceMessage, nameof(name));
        }

        return _tables.TryGetValue(name, out table);
    }

    /// <summary>
    ///     检查指定名称的配置表是否已注册。
    /// </summary>
    /// <param name="name">要检查的配置表名称。</param>
    /// <returns>如果配置表已注册则返回 true，否则返回 false。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="name" /> 为 null、空或仅包含空白字符时抛出。</exception>
    public bool HasTable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(NameCannotBeNullOrWhiteSpaceMessage, nameof(name));
        }

        return _tables.ContainsKey(name);
    }

    /// <summary>
    ///     从注册表中移除指定名称的配置表。
    /// </summary>
    /// <param name="name">要移除的配置表名称。</param>
    /// <returns>如果配置表存在并被成功移除则返回 true，否则返回 false。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="name" /> 为 null、空或仅包含空白字符时抛出。</exception>
    public bool RemoveTable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(NameCannotBeNullOrWhiteSpaceMessage, nameof(name));
        }

        return _tables.TryRemove(name, out _);
    }

    /// <summary>
    ///     清空注册表中的所有配置表。
    /// </summary>
    public void Clear()
    {
        _tables.Clear();
    }
}