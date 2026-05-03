// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Extensions;

/// <summary>
///     数值扩展方法
/// </summary>
public static class NumericExtensions
{
    /// <summary>
    ///     检查值是否在指定范围内
    /// </summary>
    /// <typeparam name="T">实现 IComparable 的类型</typeparam>
    /// <param name="value">要检查的值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <param name="inclusive">是否包含边界值，默认为 true</param>
    /// <returns>如果值在范围内则返回 true，否则返回 false</returns>
    /// <exception cref="ArgumentNullException">当 value、min 或 max 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当 min 大于 max 时抛出</exception>
    /// <example>
    /// <code>
    /// var value = 50;
    /// var inRange = value.Between(0, 100); // 返回 true
    /// var inRangeExclusive = value.Between(50, 100, inclusive: false); // 返回 false
    /// </code>
    /// </example>
    public static bool Between<T>(this T value, T min, T max, bool inclusive = true) where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(min);
        ArgumentNullException.ThrowIfNull(max);

        if (min.CompareTo(max) > 0)
        {
            throw new ArgumentException($"最小值 ({min}) 不能大于最大值 ({max})", nameof(min));
        }

        if (inclusive)
        {
            return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
        }

        return value.CompareTo(min) > 0 && value.CompareTo(max) < 0;
    }

    /// <summary>
    ///     在两个值之间进行线性插值
    /// </summary>
    /// <param name="from">起始值</param>
    /// <param name="to">目标值</param>
    /// <param name="t">插值参数（0 到 1 之间）</param>
    /// <returns>插值结果</returns>
    /// <example>
    /// <code>
    /// var result = 0f.Lerp(100f, 0.5f); // 返回 50
    /// </code>
    /// </example>
    public static float Lerp(this float from, float to, float t)
    {
        return from + (to - from) * t;
    }

    /// <summary>
    ///     计算值在两个值之间的插值参数
    /// </summary>
    /// <param name="value">当前值</param>
    /// <param name="from">起始值</param>
    /// <param name="to">目标值</param>
    /// <returns>插值参数（通常在 0 到 1 之间）</returns>
    /// <exception cref="DivideByZeroException">当 from 等于 to 时抛出</exception>
    /// <example>
    /// <code>
    /// var t = 50f.InverseLerp(0f, 100f); // 返回 0.5
    /// </code>
    /// </example>
    public static float InverseLerp(this float value, float from, float to)
    {
        if (Math.Abs(to - from) < float.Epsilon)
        {
            throw new DivideByZeroException("起始值和目标值不能相等");
        }

        return (value - from) / (to - from);
    }
}
