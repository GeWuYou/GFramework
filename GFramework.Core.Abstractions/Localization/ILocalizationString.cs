// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Localization;

/// <summary>
/// 本地化字符串接口（支持变量和格式化）
/// </summary>
public interface ILocalizationString
{
    /// <summary>
    /// 表名
    /// </summary>
    string Table { get; }

    /// <summary>
    /// 键名
    /// </summary>
    string Key { get; }

    /// <summary>
    /// 添加变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="value">变量值</param>
    /// <returns>当前实例（支持链式调用）</returns>
    ILocalizationString WithVariable(string name, object value);

    /// <summary>
    /// 批量添加变量
    /// </summary>
    /// <param name="variables">变量数组</param>
    /// <returns>当前实例（支持链式调用）</returns>
    ILocalizationString WithVariables(params (string name, object value)[] variables);

    /// <summary>
    /// 格式化并返回最终文本
    /// </summary>
    /// <returns>格式化后的文本</returns>
    string Format();

    /// <summary>
    /// 获取原始文本（不进行格式化）
    /// </summary>
    /// <returns>原始文本</returns>
    string GetRaw();

    /// <summary>
    /// 检查键是否存在
    /// </summary>
    /// <returns>是否存在</returns>
    bool Exists();
}