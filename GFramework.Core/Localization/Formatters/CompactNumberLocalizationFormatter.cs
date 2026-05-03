// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using GFramework.Core.Abstractions.Localization;
using GFramework.Core.Abstractions.Utility.Numeric;
using GFramework.Core.Utility.Numeric;

namespace GFramework.Core.Localization.Formatters;

/// <summary>
/// 紧凑数值格式化器。
/// 格式: {value:compact} 或 {value:compact:maxDecimals=2,trimZeros=false}
/// </summary>
public sealed class CompactNumberLocalizationFormatter : ILocalizationFormatter
{
    /// <summary>
    /// 获取格式化器的名称
    /// </summary>
    public string Name => "compact";


    /// <summary>
    /// 尝试将指定值按照紧凑数值格式进行格式化
    /// </summary>
    /// <param name="format">格式字符串，可包含以下选项：
    /// maxDecimals: 最大小数位数
    /// minDecimals: 最小小数位数  
    /// trimZeros: 是否去除尾随零
    /// grouping: 是否在阈值以下使用分组</param>
    /// <param name="value">要格式化的数值对象</param>
    /// <param name="provider">格式提供程序，用于区域性特定的格式设置</param>
    /// <param name="result">格式化后的字符串结果</param>
    /// <returns>如果格式化成功则返回true；如果格式字符串无效或格式化失败则返回false</returns>
    public bool TryFormat(string format, object value, IFormatProvider? provider, out string result)
    {
        result = string.Empty;

        if (!TryParseOptions(format, provider, out var options))
        {
            return false;
        }

        try
        {
            result = NumericDisplay.Format(value, options);
            return true;
        }
        catch (ArgumentNullException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试解析格式字符串中的选项参数
    /// </summary>
    /// <param name="format">格式字符串，包含以逗号分隔的键值对，如"maxDecimals=2,trimZeros=false"</param>
    /// <param name="provider">格式提供程序</param>
    /// <param name="options">解析成功的选项输出</param>
    /// <returns>如果所有选项都正确解析则返回true；如果有任何语法错误或无效值则返回false</returns>
    /// <remarks>
    /// 支持的选项包括：
    /// - maxDecimals: 最大小数位数，必须是有效整数
    /// - minDecimals: 最小小数位数，必须是有效整数
    /// - trimZeros: 是否去除尾随零，必须是有效布尔值
    /// - grouping: 是否在阈值以下使用分组，必须是有效布尔值
    /// 选项之间用逗号或分号分隔，格式为key=value
    /// </remarks>
    private static bool TryParseOptions(string format, IFormatProvider? provider, out NumericFormatOptions options)
    {
        options = new NumericFormatOptions
        {
            FormatProvider = provider
        };

        if (string.IsNullOrWhiteSpace(format))
        {
            return true;
        }

        var maxDecimalPlaces = options.MaxDecimalPlaces;
        var minDecimalPlaces = options.MinDecimalPlaces;
        var trimTrailingZeros = options.TrimTrailingZeros;
        var useGroupingBelowThreshold = options.UseGroupingBelowThreshold;

        foreach (var segment in format.Split([',', ';'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryParseSegment(segment, out var key, out var value))
            {
                return false;
            }

            if (!TryApplyOption(
                    key,
                    value,
                    ref maxDecimalPlaces,
                    ref minDecimalPlaces,
                    ref trimTrailingZeros,
                    ref useGroupingBelowThreshold))
            {
                return false;
            }
        }

        options = options with
        {
            MaxDecimalPlaces = maxDecimalPlaces,
            MinDecimalPlaces = minDecimalPlaces,
            TrimTrailingZeros = trimTrailingZeros,
            UseGroupingBelowThreshold = useGroupingBelowThreshold
        };

        return true;
    }

    /// <summary>
    /// 尝试解析格式字符串中的单个键值对片段。
    /// </summary>
    /// <param name="segment">包含键值对的字符串片段，格式应为"key=value"</param>
    /// <param name="key">解析得到的键名</param>
    /// <param name="value">解析得到的值</param>
    /// <returns>如果片段格式有效且成功解析则返回true；如果格式无效（如缺少分隔符、空键等）则返回false</returns>
    private static bool TryParseSegment(string segment, out string key, out string value)
    {
        var separatorIndex = segment.IndexOf('=');
        if (separatorIndex <= 0 || separatorIndex == segment.Length - 1)
        {
            key = string.Empty;
            value = string.Empty;
            return false;
        }

        key = segment[..separatorIndex].Trim();
        value = segment[(separatorIndex + 1)..].Trim();
        return true;
    }

    /// <summary>
    /// 尝试将解析得到的键值对应用到相应的选项变量中。
    /// </summary>
    /// <param name="key">选项名称</param>
    /// <param name="value">选项值的字符串表示</param>
    /// <param name="maxDecimalPlaces">最大小数位数的引用参数</param>
    /// <param name="minDecimalPlaces">最小小数位数的引用参数</param>
    /// <param name="trimTrailingZeros">是否去除尾随零的引用参数</param>
    /// <param name="useGroupingBelowThreshold">是否在阈值以下使用分组的引用参数</param>
    /// <returns>如果值成功解析或键名未知则返回true；如果键名已知但值解析失败则返回false</returns>
    private static bool TryApplyOption(
        string key,
        string value,
        ref int maxDecimalPlaces,
        ref int minDecimalPlaces,
        ref bool trimTrailingZeros,
        ref bool useGroupingBelowThreshold)
    {
        var formatProvider = CultureInfo.InvariantCulture;
        return key switch
        {
            "maxDecimals" => int.TryParse(value, NumberStyles.Integer, formatProvider, out maxDecimalPlaces),
            "minDecimals" => int.TryParse(value, NumberStyles.Integer, formatProvider, out minDecimalPlaces),
            "trimZeros" => bool.TryParse(value, out trimTrailingZeros),
            "grouping" => bool.TryParse(value, out useGroupingBelowThreshold),
            _ => true
        };
    }
}
