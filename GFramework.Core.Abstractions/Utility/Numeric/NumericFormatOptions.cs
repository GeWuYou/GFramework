// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Utility.Numeric;

/// <summary>
/// 数值格式化选项。
/// </summary>
public sealed record NumericFormatOptions
{
    /// <summary>
    /// 显示风格。
    /// </summary>
    public NumericDisplayStyle Style { get; init; } = NumericDisplayStyle.Compact;

    /// <summary>
    /// 最大保留小数位数。
    /// </summary>
    public int MaxDecimalPlaces { get; init; } = 1;

    /// <summary>
    /// 最少保留小数位数。
    /// </summary>
    public int MinDecimalPlaces { get; init; } = 0;

    /// <summary>
    /// 四舍五入策略。
    /// </summary>
    public MidpointRounding MidpointRounding { get; init; } = MidpointRounding.AwayFromZero;

    /// <summary>
    /// 是否裁剪小数末尾的 0。
    /// </summary>
    public bool TrimTrailingZeros { get; init; } = true;

    /// <summary>
    /// 小于缩写阈值时是否启用千分位分组。
    /// </summary>
    public bool UseGroupingBelowThreshold { get; init; }

    /// <summary>
    /// 进入缩写显示的阈值。
    /// </summary>
    public decimal CompactThreshold { get; init; } = 1000m;

    /// <summary>
    /// 格式提供者。
    /// </summary>
    public IFormatProvider? FormatProvider { get; init; }

    /// <summary>
    /// 自定义格式规则。
    /// </summary>
    public INumericFormatRule? Rule { get; init; }
}