// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Utility.Numeric;

/// <summary>
/// 数值显示规则接口。
/// </summary>
public interface INumericFormatRule
{
    /// <summary>
    /// 规则名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 尝试按当前规则格式化数值。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">待格式化的值。</param>
    /// <param name="options">格式化选项。</param>
    /// <param name="result">输出结果。</param>
    /// <returns>格式化是否成功。</returns>
    bool TryFormat<T>(T value, NumericFormatOptions options, out string result);
}