// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     表示数组节点声明的 <c>contains</c> 匹配约束。
///     该模型把 contains 子 schema 与匹配数量边界聚合在一起，避免数组节点再额外散落多组相关成员。
/// </summary>
internal sealed class YamlConfigArrayContainsConstraints
{
    /// <summary>
    ///     初始化数组 contains 约束模型。
    /// </summary>
    /// <param name="containsNode">contains 子 schema。</param>
    /// <param name="minContains">最小匹配数量；为 <see langword="null" /> 时按 JSON Schema 语义默认 1。</param>
    /// <param name="maxContains">最大匹配数量。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="containsNode"/> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="minContains"/> 或 <paramref name="maxContains"/> 为负数时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="minContains"/> 大于 <paramref name="maxContains"/> 时抛出。</exception>
    public YamlConfigArrayContainsConstraints(
        YamlConfigSchemaNode containsNode,
        int? minContains,
        int? maxContains)
    {
        ArgumentNullException.ThrowIfNull(containsNode);
        if (minContains is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minContains), minContains, "minContains 不能为负数。");
        }

        if (maxContains is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxContains), maxContains, "maxContains 不能为负数。");
        }

        if (minContains.HasValue &&
            maxContains.HasValue &&
            minContains.Value > maxContains.Value)
        {
            throw new ArgumentException("minContains 不能大于 maxContains。", nameof(minContains));
        }

        ContainsNode = containsNode;
        MinContains = minContains;
        MaxContains = maxContains;
    }

    /// <summary>
    ///     获取 contains 子 schema。
    /// </summary>
    public YamlConfigSchemaNode ContainsNode { get; }

    /// <summary>
    ///     获取最小匹配数量；未显式声明时返回空，由调用方按默认值 1 解释。
    /// </summary>
    public int? MinContains { get; }

    /// <summary>
    ///     获取最大匹配数量。
    /// </summary>
    public int? MaxContains { get; }
}
