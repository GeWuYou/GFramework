// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     表示一个对象节点上声明的属性数量约束、字段依赖约束、条件子 schema 与组合约束。
///     该模型将对象级约束与数组 / 标量约束拆开保存，避免运行时节点继续暴露无关成员。
/// </summary>
internal sealed class YamlConfigObjectConstraints
{
    /// <summary>
    ///     初始化对象约束模型。
    /// </summary>
    /// <param name="minProperties">最小属性数量约束。</param>
    /// <param name="maxProperties">最大属性数量约束。</param>
    /// <param name="dependentRequired">对象内字段依赖约束。</param>
    /// <param name="dependentSchemas">对象内条件 schema 约束。</param>
    /// <param name="allOfSchemas">对象内组合 schema 约束。</param>
    /// <param name="conditionalSchemas">对象内条件分支约束。</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="minProperties"/> 或 <paramref name="maxProperties"/> 为负数时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="minProperties"/> 大于 <paramref name="maxProperties"/> 时抛出。</exception>
    public YamlConfigObjectConstraints(
        int? minProperties,
        int? maxProperties,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? dependentRequired,
        IReadOnlyDictionary<string, YamlConfigSchemaNode>? dependentSchemas,
        IReadOnlyList<YamlConfigSchemaNode>? allOfSchemas,
        YamlConfigConditionalSchemas? conditionalSchemas)
    {
        if (minProperties is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minProperties), minProperties, "minProperties 不能为负数。");
        }

        if (maxProperties is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxProperties), maxProperties, "maxProperties 不能为负数。");
        }

        if (minProperties.HasValue &&
            maxProperties.HasValue &&
            minProperties.Value > maxProperties.Value)
        {
            throw new ArgumentException("minProperties 不能大于 maxProperties。", nameof(minProperties));
        }

        MinProperties = minProperties;
        MaxProperties = maxProperties;
        DependentRequired = dependentRequired;
        DependentSchemas = dependentSchemas;
        AllOfSchemas = allOfSchemas;
        ConditionalSchemas = conditionalSchemas;
    }

    /// <summary>
    ///     获取最小属性数量约束。
    /// </summary>
    public int? MinProperties { get; }

    /// <summary>
    ///     获取最大属性数量约束。
    /// </summary>
    public int? MaxProperties { get; }

    /// <summary>
    ///     获取对象内字段依赖约束。
    ///     键表示“触发字段”，值表示“触发字段出现后还必须存在的同级字段集合”。
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? DependentRequired { get; }

    /// <summary>
    ///     获取对象内条件 schema 约束。
    ///     键表示“触发字段”，值表示“触发字段出现后当前对象还必须满足的额外 schema 子树”。
    /// </summary>
    public IReadOnlyDictionary<string, YamlConfigSchemaNode>? DependentSchemas { get; }

    /// <summary>
    ///     获取对象内 <c>allOf</c> 组合约束。
    ///     每个条目都表示“当前对象还必须额外满足的 focused constraint block”。
    /// </summary>
    public IReadOnlyList<YamlConfigSchemaNode>? AllOfSchemas { get; }

    /// <summary>
    ///     获取对象内 object-focused <c>if</c> / <c>then</c> / <c>else</c> 条件约束。
    ///     该模型会先用 <c>if</c> 试匹配当前对象，再只对命中的分支叠加 focused constraint block。
    /// </summary>
    public YamlConfigConditionalSchemas? ConditionalSchemas { get; }
}
