// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     负责在运行时配置系统内部构造结构化加载异常。
///     该工厂集中封装诊断字段填充，避免不同失败路径对同一语义产生不一致的消息和字段约定。
/// </summary>
internal static class ConfigLoadExceptionFactory
{
    /// <summary>
    ///     创建一个包含结构化诊断信息的配置加载异常。
    /// </summary>
    /// <param name="failureKind">失败类别。</param>
    /// <param name="tableName">配置表名称。</param>
    /// <param name="message">错误消息。</param>
    /// <param name="configDirectoryPath">配置目录绝对路径；不适用时为空。</param>
    /// <param name="yamlPath">YAML 文件绝对路径；不适用时为空。</param>
    /// <param name="schemaPath">schema 文件绝对路径；不适用时为空。</param>
    /// <param name="displayPath">逻辑字段路径；不适用时为空。</param>
    /// <param name="referencedTableName">跨表引用目标表名称；不适用时为空。</param>
    /// <param name="rawValue">原始值或引用值；不适用时为空。</param>
    /// <param name="detail">附加细节；不适用时为空。</param>
    /// <param name="innerException">底层异常；不适用时为空。</param>
    /// <returns>构造完成的配置加载异常。</returns>
    internal static ConfigLoadException Create(
        ConfigLoadFailureKind failureKind,
        string tableName,
        string message,
        string? configDirectoryPath = null,
        string? yamlPath = null,
        string? schemaPath = null,
        string? displayPath = null,
        string? referencedTableName = null,
        string? rawValue = null,
        string? detail = null,
        Exception? innerException = null)
    {
        var diagnostic = new ConfigLoadDiagnostic(
            failureKind,
            tableName,
            configDirectoryPath,
            yamlPath,
            schemaPath,
            displayPath,
            referencedTableName,
            rawValue,
            detail);
        return new ConfigLoadException(diagnostic, message, innerException);
    }
}