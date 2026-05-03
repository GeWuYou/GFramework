// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Localization;

/// <summary>
/// 本地化表接口
/// </summary>
public interface ILocalizationTable
{
    /// <summary>
    /// 表名
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 语言代码
    /// </summary>
    string Language { get; }

    /// <summary>
    /// 回退表（当前表中找不到键时使用）
    /// </summary>
    ILocalizationTable? Fallback { get; }

    /// <summary>
    /// 获取原始文本（不进行格式化）
    /// </summary>
    /// <param name="key">键名</param>
    /// <returns>原始文本</returns>
    string GetRawText(string key);

    /// <summary>
    /// 检查是否包含指定键
    /// </summary>
    /// <param name="key">键名</param>
    /// <returns>是否包含</returns>
    bool ContainsKey(string key);

    /// <summary>
    /// 获取所有键
    /// </summary>
    /// <returns>键集合</returns>
    IEnumerable<string> GetKeys();

    /// <summary>
    /// 合并覆盖数据
    /// </summary>
    /// <param name="overrides">覆盖数据</param>
    void Merge(IReadOnlyDictionary<string, string> overrides);
}