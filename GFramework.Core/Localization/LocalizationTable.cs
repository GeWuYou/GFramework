// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Localization;

namespace GFramework.Core.Localization;

/// <summary>
/// 本地化表实现
/// </summary>
public class LocalizationTable : ILocalizationTable
{
    /// <summary>
    /// 存储原始本地化数据的字典
    /// </summary>
    private readonly Dictionary<string, string> _data;

    /// <summary>
    /// 存储覆盖数据的字典，优先级高于原始数据
    /// </summary>
    private readonly Dictionary<string, string> _overrides;

    /// <summary>
    /// 初始化本地化表
    /// </summary>
    /// <param name="name">表名</param>
    /// <param name="language">语言代码</param>
    /// <param name="data">数据字典</param>
    /// <param name="fallback">回退表</param>
    public LocalizationTable(
        string name,
        string language,
        IReadOnlyDictionary<string, string> data,
        ILocalizationTable? fallback = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Language = language ?? throw new ArgumentNullException(nameof(language));
        _data = new Dictionary<string, string>(data, StringComparer.Ordinal);
        _overrides = new Dictionary<string, string>(StringComparer.Ordinal);
        Fallback = fallback;
    }

    /// <summary>
    /// 获取本地化表的名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取语言代码
    /// </summary>
    public string Language { get; }

    /// <summary>
    /// 获取回退表，当当前表找不到键时用于查找
    /// </summary>
    public ILocalizationTable? Fallback { get; }

    /// <summary>
    /// 获取指定键的原始文本内容
    /// </summary>
    /// <param name="key">要查找的本地化键</param>
    /// <returns>找到的本地化文本值</returns>
    /// <exception cref="ArgumentNullException">当 key 为 null 时抛出</exception>
    /// <exception cref="LocalizationKeyNotFoundException">当键在表中不存在且无回退表时抛出</exception>
    public string GetRawText(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        // 优先使用覆盖数据
        if (_overrides.TryGetValue(key, out var overrideValue))
        {
            return overrideValue;
        }

        // 然后使用原始数据
        if (_data.TryGetValue(key, out var value))
        {
            return value;
        }

        // 最后尝试回退表
        if (Fallback is { } fb && fb.ContainsKey(key))
        {
            return fb.GetRawText(key);
        }

        throw new LocalizationKeyNotFoundException(Name, key);
    }

    /// <summary>
    /// 检查是否包含指定的键
    /// </summary>
    /// <param name="key">要检查的本地化键</param>
    /// <returns>如果存在则返回 true，否则返回 false</returns>
    public bool ContainsKey(string key)
    {
        return _overrides.ContainsKey(key)
               || _data.ContainsKey(key)
               || (Fallback is { } fb && fb.ContainsKey(key));
    }

    /// <summary>
    /// 获取所有可用的本地化键集合
    /// </summary>
    /// <returns>包含所有键的可枚举集合</returns>
    public IEnumerable<string> GetKeys()
    {
        var keys = new HashSet<string>(_data.Keys, StringComparer.Ordinal);
        keys.UnionWith(_overrides.Keys);

        if (Fallback != null)
        {
            keys.UnionWith(Fallback.GetKeys());
        }

        return keys;
    }

    /// <summary>
    /// 合并覆盖数据到当前表
    /// </summary>
    /// <param name="overrides">要合并的覆盖数据字典</param>
    /// <exception cref="ArgumentNullException">当 overrides 为 null 时抛出</exception>
    public void Merge(IReadOnlyDictionary<string, string> overrides)
    {
        if (overrides == null)
        {
            throw new ArgumentNullException(nameof(overrides));
        }

        foreach (var (key, value) in overrides)
        {
            _overrides[key] = value;
        }
    }
}
