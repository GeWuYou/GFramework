// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Numerics;
using GFramework.Core.Abstractions.Utility.Numeric;
using GFramework.Core.Utility.Numeric;

namespace GFramework.Core.Extensions;

/// <summary>
/// 数值显示扩展方法。
/// </summary>
public static class NumericDisplayExtensions
{
    /// <summary>
    /// 按指定选项将数值格式化为展示字符串。
    /// </summary>
    public static string ToDisplayString<T>(this T value, NumericFormatOptions? options = null) where T : INumber<T>
    {
        return NumericDisplay.Format(value, options);
    }

    /// <summary>
    /// 使用默认紧凑风格将数值格式化为展示字符串。
    /// </summary>
    public static string ToCompactString<T>(
        this T value,
        int maxDecimalPlaces = 1,
        IFormatProvider? formatProvider = null) where T : INumber<T>
    {
        return NumericDisplay.FormatCompact(value, maxDecimalPlaces, formatProvider);
    }
}