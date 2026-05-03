// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Localization;

/// <summary>
/// 本地化格式化器接口
/// </summary>
public interface ILocalizationFormatter
{
    /// <summary>
    /// 格式化器名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 尝试格式化值
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="value">要格式化的值</param>
    /// <param name="provider">格式提供者</param>
    /// <param name="result">格式化结果</param>
    /// <returns>是否成功格式化</returns>
    bool TryFormat(string format, object value, IFormatProvider? provider, out string result);
}