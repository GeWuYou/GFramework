// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace GFramework.Godot.Config;

/// <summary>
///     描述一个 Godot YAML 配置表在资源目录中的来源信息。
/// </summary>
public sealed class GodotYamlConfigTableSource
{
    /// <summary>
    ///     初始化一个配置表来源描述。
    /// </summary>
    /// <param name="tableName">运行时表名称。</param>
    /// <param name="configRelativePath">
    ///     相对配置根目录的 YAML 目录。
    ///     该路径必须保持为无根相对路径，且不能包含 <c>.</c>、<c>..</c>、<c>res://</c>、<c>user://</c>、<c>:</c>
    ///     或磁盘根路径前缀。
    /// </param>
    /// <param name="schemaRelativePath">
    ///     相对配置根目录的 schema 文件路径；未启用 schema 时为空。
    ///     如果提供，同样必须保持为无根相对路径，且不能包含 <c>.</c>、<c>..</c>、<c>:</c> 或任何绝对路径前缀。
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <paramref name="tableName" />、<paramref name="configRelativePath" /> 或 <paramref name="schemaRelativePath" />
    ///     不满足非空白且安全相对路径的约束时抛出。
    /// </exception>
    public GodotYamlConfigTableSource(
        string tableName,
        string configRelativePath,
        string? schemaRelativePath = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        }

        if (string.IsNullOrWhiteSpace(configRelativePath))
        {
            throw new ArgumentException("Config relative path cannot be null or whitespace.",
                nameof(configRelativePath));
        }

        if (!IsSafeRelativePath(configRelativePath))
        {
            throw new ArgumentException(
                "Config relative path must be a safe relative path without root segments or traversal markers.",
                nameof(configRelativePath));
        }

        if (schemaRelativePath != null && string.IsNullOrWhiteSpace(schemaRelativePath))
        {
            throw new ArgumentException(
                "Schema relative path cannot be empty or whitespace when provided.",
                nameof(schemaRelativePath));
        }

        if (schemaRelativePath != null && !IsSafeRelativePath(schemaRelativePath))
        {
            throw new ArgumentException(
                "Schema relative path must be a safe relative path without root segments or traversal markers.",
                nameof(schemaRelativePath));
        }

        TableName = tableName;
        ConfigRelativePath = configRelativePath;
        SchemaRelativePath = schemaRelativePath;
    }

    /// <summary>
    ///     获取运行时表名称。
    /// </summary>
    public string TableName { get; }

    /// <summary>
    ///     获取相对配置根目录的 YAML 目录路径。
    ///     该值始终保持为无根相对路径，不会包含 <c>.</c>、<c>..</c> 或 <c>:</c> 段。
    /// </summary>
    public string ConfigRelativePath { get; }

    /// <summary>
    ///     获取相对配置根目录的 schema 文件路径；未启用 schema 校验时为空。
    ///     该值在非空时始终保持为无根相对路径，不会包含 <c>.</c>、<c>..</c> 或 <c>:</c> 段。
    /// </summary>
    public string? SchemaRelativePath { get; }

    private static bool IsSafeRelativePath(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        if (normalizedPath.StartsWith("/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("res://", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("user://", StringComparison.Ordinal) ||
            Path.IsPathRooted(path) ||
            HasWindowsDrivePrefix(normalizedPath))
        {
            return false;
        }

        if (normalizedPath.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var segment in normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment is "." or "..")
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasWindowsDrivePrefix(string path)
    {
        return path.Length >= 2 &&
               char.IsLetter(path[0]) &&
               path[1] == ':';
    }
}
