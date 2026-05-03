// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     描述一个 YAML 配置表注册项的参数集合。
///     该选项对象用于替代不断增加的位置参数重载，
///     让消费者在启用 schema 校验、主键比较器或未来扩展项时仍能保持调用点可读。
/// </summary>
/// <typeparam name="TKey">配置主键类型。</typeparam>
/// <typeparam name="TValue">配置值类型。</typeparam>
public sealed class YamlConfigTableRegistrationOptions<TKey, TValue>
    where TKey : notnull
{
    private const string TableNameCannotBeNullOrWhiteSpaceMessage = "Table name cannot be null or whitespace.";
    private const string RelativePathCannotBeNullOrWhiteSpaceMessage = "Relative path cannot be null or whitespace.";

    /// <summary>
    ///     使用最小必需参数创建配置表注册选项。
    /// </summary>
    /// <param name="tableName">运行时配置表名称。</param>
    /// <param name="relativePath">相对配置根目录的子目录。</param>
    /// <param name="keySelector">配置项主键提取器。</param>
    /// <exception cref="ArgumentException">
    ///     当 <paramref name="tableName" /> 或 <paramref name="relativePath" /> 为 null、空字符串或空白字符串时抛出。
    /// </exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="keySelector" /> 为 null 时抛出。</exception>
    public YamlConfigTableRegistrationOptions(
        string tableName,
        string relativePath,
        Func<TValue, TKey> keySelector)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException(TableNameCannotBeNullOrWhiteSpaceMessage, nameof(tableName));
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException(RelativePathCannotBeNullOrWhiteSpaceMessage, nameof(relativePath));
        }

        ArgumentNullException.ThrowIfNull(keySelector);

        TableName = tableName;
        RelativePath = relativePath;
        KeySelector = keySelector;
    }

    /// <summary>
    ///     获取运行时配置表名称。
    /// </summary>
    public string TableName { get; }

    /// <summary>
    ///     获取相对配置根目录的子目录。
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    ///     获取相对配置根目录的 schema 文件路径。
    ///     当该值为空时，当前注册项不会启用 schema 校验。
    /// </summary>
    public string? SchemaRelativePath { get; init; }

    /// <summary>
    ///     获取配置项主键提取器。
    /// </summary>
    public Func<TValue, TKey> KeySelector { get; }

    /// <summary>
    ///     获取可选的主键比较器。
    /// </summary>
    public IEqualityComparer<TKey>? Comparer { get; init; }
}