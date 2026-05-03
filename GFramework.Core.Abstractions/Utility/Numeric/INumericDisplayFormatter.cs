// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Utility.Numeric;

/// <summary>
/// 数值显示格式化器接口。
/// </summary>
public interface INumericDisplayFormatter
{
    /// <summary>
    /// 将数值格式化为展示字符串。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">待格式化的值。</param>
    /// <param name="options">格式化选项。</param>
    /// <returns>格式化后的字符串。</returns>
    string Format<T>(T value, NumericFormatOptions? options = null);
}