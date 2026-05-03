// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Numerics;
using GFramework.Core.Abstractions.Utility.Numeric;

namespace GFramework.Core.Utility.Numeric;

/// <summary>
/// 默认数值显示格式化器。
/// </summary>
public sealed class NumericDisplayFormatter : INumericDisplayFormatter
{
    private readonly INumericFormatRule _defaultRule;

    /// <summary>
    /// 初始化默认数值显示格式化器。
    /// </summary>
    public NumericDisplayFormatter()
        : this(NumericSuffixFormatRule.InternationalCompact)
    {
    }

    /// <summary>
    /// 初始化数值显示格式化器。
    /// </summary>
    /// <param name="defaultRule">默认规则。</param>
    public NumericDisplayFormatter(INumericFormatRule defaultRule)
    {
        _defaultRule = defaultRule ?? throw new ArgumentNullException(nameof(defaultRule));
    }

    /// <inheritdoc/>
    public string Format<T>(T value, NumericFormatOptions? options = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var resolvedOptions = NormalizeOptions(options);
        var rule = ResolveRule(resolvedOptions);

        if (rule.TryFormat(value, resolvedOptions, out var result))
        {
            return result;
        }

        return FormatFallback(value!, resolvedOptions.FormatProvider);
    }

    /// <summary>
    /// 将运行时数值对象格式化为展示字符串。
    /// </summary>
    /// <param name="value">待格式化的数值对象。</param>
    /// <param name="options">格式化选项。</param>
    /// <returns>格式化后的字符串。</returns>
    public string Format(object value, NumericFormatOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value switch
        {
            byte byteValue => Format(byteValue, options),
            sbyte sbyteValue => Format(sbyteValue, options),
            short shortValue => Format(shortValue, options),
            ushort ushortValue => Format(ushortValue, options),
            int intValue => Format(intValue, options),
            uint uintValue => Format(uintValue, options),
            long longValue => Format(longValue, options),
            ulong ulongValue => Format(ulongValue, options),
            nint nativeIntValue => Format(nativeIntValue, options),
            nuint nativeUIntValue => Format(nativeUIntValue, options),
            float floatValue => Format(floatValue, options),
            double doubleValue => Format(doubleValue, options),
            decimal decimalValue => Format(decimalValue, options),
            BigInteger bigIntegerValue => Format(bigIntegerValue, options),
            _ => FormatFallback(value, options?.FormatProvider)
        };
    }

    internal static NumericFormatOptions NormalizeOptions(NumericFormatOptions? options)
    {
        var resolved = options ?? new NumericFormatOptions();

        if (resolved.MaxDecimalPlaces < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                resolved.MaxDecimalPlaces,
                "MaxDecimalPlaces 不能小于 0。");
        }

        if (resolved.MinDecimalPlaces < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                resolved.MinDecimalPlaces,
                "MinDecimalPlaces 不能小于 0。");
        }

        if (resolved.MinDecimalPlaces > resolved.MaxDecimalPlaces)
        {
            throw new ArgumentException("MinDecimalPlaces 不能大于 MaxDecimalPlaces。", nameof(options));
        }

        if (resolved.CompactThreshold <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                resolved.CompactThreshold,
                "CompactThreshold 必须大于 0。");
        }

        return resolved;
    }

    private INumericFormatRule ResolveRule(NumericFormatOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Rule is not null)
        {
            return options.Rule;
        }

        return options.Style switch
        {
            NumericDisplayStyle.Compact => _defaultRule,
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Style, "不支持的数值显示风格。")
        };
    }

    private static string FormatFallback(object value, IFormatProvider? provider)
    {
        return value switch
        {
            IFormattable formattable => formattable.ToString(null, provider ?? CultureInfo.CurrentCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}