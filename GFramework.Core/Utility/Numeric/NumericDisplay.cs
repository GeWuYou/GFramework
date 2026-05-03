// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Numerics;
using GFramework.Core.Abstractions.Utility.Numeric;

namespace GFramework.Core.Utility.Numeric;

/// <summary>
/// 数值显示静态入口。
/// </summary>
public static class NumericDisplay
{
    private static readonly NumericDisplayFormatter DefaultFormatter = new();

    /// <summary>
    /// 将数值格式化为展示字符串。
    /// </summary>
    public static string Format<T>(T value, NumericFormatOptions? options = null) where T : INumber<T>
    {
        return DefaultFormatter.Format(value, options);
    }

    /// <summary>
    /// 将运行时数值对象格式化为展示字符串。
    /// </summary>
    public static string Format(object value, NumericFormatOptions? options = null)
    {
        return DefaultFormatter.Format(value, options);
    }

    /// <summary>
    /// 使用默认紧凑风格格式化数值。
    /// </summary>
    public static string FormatCompact<T>(
        T value,
        int maxDecimalPlaces = 1,
        IFormatProvider? formatProvider = null) where T : INumber<T>
    {
        return Format(value, new NumericFormatOptions
        {
            MaxDecimalPlaces = maxDecimalPlaces,
            FormatProvider = formatProvider
        });
    }

    /// <summary>
    /// 使用默认紧凑风格格式化运行时数值对象。
    /// </summary>
    public static string FormatCompact(
        object value,
        int maxDecimalPlaces = 1,
        IFormatProvider? formatProvider = null)
    {
        return Format(value, new NumericFormatOptions
        {
            MaxDecimalPlaces = maxDecimalPlaces,
            FormatProvider = formatProvider
        });
    }
}