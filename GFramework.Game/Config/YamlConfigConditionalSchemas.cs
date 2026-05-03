// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     表示一个对象节点上声明的 object-focused <c>if</c> / <c>then</c> / <c>else</c> 条件约束。
///     三个分支都共享父对象已声明字段集合，不会把分支 schema 扩展成新的生成类型形状。
/// </summary>
internal sealed class YamlConfigConditionalSchemas
{
    /// <summary>
    ///     初始化条件分支约束模型。
    /// </summary>
    /// <param name="ifSchema">条件判断 schema。</param>
    /// <param name="thenSchema">条件命中时需要满足的 schema。</param>
    /// <param name="elseSchema">条件未命中时需要满足的 schema。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="ifSchema"/> 为 <see langword="null" /> 时抛出。</exception>
    public YamlConfigConditionalSchemas(
        YamlConfigSchemaNode ifSchema,
        YamlConfigSchemaNode? thenSchema,
        YamlConfigSchemaNode? elseSchema)
    {
        ArgumentNullException.ThrowIfNull(ifSchema);

        IfSchema = ifSchema;
        ThenSchema = thenSchema;
        ElseSchema = elseSchema;
    }

    /// <summary>
    ///     获取条件判断 schema。
    /// </summary>
    public YamlConfigSchemaNode IfSchema { get; }

    /// <summary>
    ///     获取条件命中时需要满足的 schema。
    /// </summary>
    public YamlConfigSchemaNode? ThenSchema { get; }

    /// <summary>
    ///     获取条件未命中时需要满足的 schema。
    /// </summary>
    public YamlConfigSchemaNode? ElseSchema { get; }
}
