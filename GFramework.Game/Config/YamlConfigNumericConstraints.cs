// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     表示标量节点上声明的数值范围与步进约束。
///     该类型只覆盖整数 / 浮点共享的关键字，避免字符串字段继续暴露不相关的成员。
/// </summary>
internal sealed class YamlConfigNumericConstraints
{
    /// <summary>
    ///     初始化数值约束模型。
    /// </summary>
    /// <param name="minimum">最小值约束。</param>
    /// <param name="maximum">最大值约束。</param>
    /// <param name="exclusiveMinimum">开区间最小值约束。</param>
    /// <param name="exclusiveMaximum">开区间最大值约束。</param>
    /// <param name="multipleOf">数值步进约束。</param>
    public YamlConfigNumericConstraints(
        double? minimum,
        double? maximum,
        double? exclusiveMinimum,
        double? exclusiveMaximum,
        double? multipleOf)
    {
        Minimum = minimum;
        Maximum = maximum;
        ExclusiveMinimum = exclusiveMinimum;
        ExclusiveMaximum = exclusiveMaximum;
        MultipleOf = multipleOf;
    }

    /// <summary>
    ///     获取最小值约束。
    /// </summary>
    public double? Minimum { get; }

    /// <summary>
    ///     获取最大值约束。
    /// </summary>
    public double? Maximum { get; }

    /// <summary>
    ///     获取开区间最小值约束。
    /// </summary>
    public double? ExclusiveMinimum { get; }

    /// <summary>
    ///     获取开区间最大值约束。
    /// </summary>
    public double? ExclusiveMaximum { get; }

    /// <summary>
    ///     获取数值步进约束。
    /// </summary>
    public double? MultipleOf { get; }
}
