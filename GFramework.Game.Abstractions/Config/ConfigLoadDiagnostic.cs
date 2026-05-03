// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Config;

/// <summary>
///     表示一次配置加载失败的结构化诊断信息。
///     该模型旨在为日志、测试断言、编辑器联动和热重载失败回调提供稳定字段，
///     避免调用方只能依赖异常消息文本做脆弱解析。
/// </summary>
public sealed class ConfigLoadDiagnostic
{
    /// <summary>
    ///     初始化一个配置加载诊断对象。
    /// </summary>
    /// <param name="failureKind">失败类别。</param>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="configDirectoryPath">配置目录绝对路径；不适用时为空。</param>
    /// <param name="yamlPath">配置文件绝对路径；不适用时为空。</param>
    /// <param name="schemaPath">schema 文件绝对路径；不适用时为空。</param>
    /// <param name="displayPath">逻辑字段路径；无法定位到字段时为空。</param>
    /// <param name="referencedTableName">跨表引用目标表名称；非引用失败时为空。</param>
    /// <param name="rawValue">原始值或引用值；不适用时为空。</param>
    /// <param name="detail">附加细节，用于补充无法结构化成独立字段的上下文。</param>
    /// <exception cref="ArgumentException">当 <paramref name="tableName" /> 为空时抛出。</exception>
    public ConfigLoadDiagnostic(
        ConfigLoadFailureKind failureKind,
        string tableName,
        string? configDirectoryPath = null,
        string? yamlPath = null,
        string? schemaPath = null,
        string? displayPath = null,
        string? referencedTableName = null,
        string? rawValue = null,
        string? detail = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        }

        FailureKind = failureKind;
        TableName = tableName;
        ConfigDirectoryPath = configDirectoryPath;
        YamlPath = yamlPath;
        SchemaPath = schemaPath;
        DisplayPath = displayPath;
        ReferencedTableName = referencedTableName;
        RawValue = rawValue;
        Detail = detail;
    }

    /// <summary>
    ///     获取失败类别。
    /// </summary>
    public ConfigLoadFailureKind FailureKind { get; }

    /// <summary>
    ///     获取所属配置表名称。
    /// </summary>
    public string TableName { get; }

    /// <summary>
    ///     获取配置目录绝对路径。
    /// </summary>
    public string? ConfigDirectoryPath { get; }

    /// <summary>
    ///     获取触发失败的 YAML 文件绝对路径。
    /// </summary>
    public string? YamlPath { get; }

    /// <summary>
    ///     获取触发失败的 schema 文件绝对路径。
    /// </summary>
    public string? SchemaPath { get; }

    /// <summary>
    ///     获取便于展示的字段路径。
    ///     对于根级失败或文件级失败，该值可能为空。
    /// </summary>
    public string? DisplayPath { get; }

    /// <summary>
    ///     获取跨表引用目标表名称。
    /// </summary>
    public string? ReferencedTableName { get; }

    /// <summary>
    ///     获取与失败相关的原始值。
    ///     该字段通常用于 enum 违规、跨表引用缺失或类型转换失败等场景。
    /// </summary>
    public string? RawValue { get; }

    /// <summary>
    ///     获取补充细节。
    ///     当失败上下文无法拆成更多稳定字段时，该值用于保留关键说明。
    /// </summary>
    public string? Detail { get; }
}