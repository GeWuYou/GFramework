// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     表示一个数组节点上声明的元素数量、去重与 contains 匹配计数约束。
///     该模型与标量约束拆分保存，避免数组节点继续共享不适用的标量字段。
/// </summary>
internal sealed class YamlConfigArrayConstraints
{
    /// <summary>
    ///     初始化数组约束模型。
    /// </summary>
    /// <param name="minItems">最小元素数量约束。</param>
    /// <param name="maxItems">最大元素数量约束。</param>
    /// <param name="uniqueItems">是否要求数组元素唯一。</param>
    /// <param name="containsConstraints">数组 contains 约束；未声明时为空。</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="minItems"/> 或 <paramref name="maxItems"/> 为负数时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="minItems"/> 大于 <paramref name="maxItems"/> 时抛出。</exception>
    public YamlConfigArrayConstraints(
        int? minItems,
        int? maxItems,
        bool uniqueItems,
        YamlConfigArrayContainsConstraints? containsConstraints)
    {
        if (minItems is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minItems), minItems, "minItems 不能为负数。");
        }

        if (maxItems is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItems), maxItems, "maxItems 不能为负数。");
        }

        if (minItems.HasValue &&
            maxItems.HasValue &&
            minItems.Value > maxItems.Value)
        {
            throw new ArgumentException("minItems 不能大于 maxItems。", nameof(minItems));
        }

        MinItems = minItems;
        MaxItems = maxItems;
        UniqueItems = uniqueItems;
        ContainsConstraints = containsConstraints;
    }

    /// <summary>
    ///     获取最小元素数量约束。
    /// </summary>
    public int? MinItems { get; }

    /// <summary>
    ///     获取最大元素数量约束。
    /// </summary>
    public int? MaxItems { get; }

    /// <summary>
    ///     获取是否要求数组元素唯一。
    /// </summary>
    public bool UniqueItems { get; }

    /// <summary>
    ///     获取数组 contains 约束；未声明时返回空。
    /// </summary>
    public YamlConfigArrayContainsConstraints? ContainsConstraints { get; }
}
