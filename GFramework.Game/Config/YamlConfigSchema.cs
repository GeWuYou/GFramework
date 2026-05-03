// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     表示已解析并可用于运行时校验的 JSON Schema。
///     该模型保留根节点与引用依赖集合，避免运行时引入完整 schema 引擎。
/// </summary>
internal sealed class YamlConfigSchema
{
    /// <summary>
    ///     初始化一个可用于运行时校验的 schema 模型。
    /// </summary>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="rootNode">根节点模型。</param>
    /// <param name="referencedTableNames">Schema 声明的目标引用表名称集合。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="schemaPath"/>、<paramref name="rootNode"/> 或 <paramref name="referencedTableNames"/> 为 <see langword="null" /> 时抛出。</exception>
    public YamlConfigSchema(
        string schemaPath,
        YamlConfigSchemaNode rootNode,
        IReadOnlyCollection<string> referencedTableNames)
    {
        ArgumentNullException.ThrowIfNull(schemaPath);
        ArgumentNullException.ThrowIfNull(rootNode);
        ArgumentNullException.ThrowIfNull(referencedTableNames);

        SchemaPath = schemaPath;
        RootNode = rootNode;
        ReferencedTableNames = [.. referencedTableNames];
    }

    /// <summary>
    ///     获取 schema 文件路径。
    /// </summary>
    public string SchemaPath { get; }

    /// <summary>
    ///     获取根节点模型。
    /// </summary>
    public YamlConfigSchemaNode RootNode { get; }

    /// <summary>
    ///     获取 schema 声明的目标引用表名称集合。
    ///     该信息用于热重载时推导受影响的依赖表闭包。
    /// </summary>
    public IReadOnlyCollection<string> ReferencedTableNames { get; }
}
