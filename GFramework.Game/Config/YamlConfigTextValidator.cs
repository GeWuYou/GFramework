// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     提供面向宿主的 YAML 文本校验入口，使保存前校验可以复用运行时同一套 schema 规则。
/// </summary>
public static class YamlConfigTextValidator
{
    // Cache parsed schemas by table/path plus last write time so save-path validation can
    // avoid repeated disk IO and JSON parsing while still observing schema edits.
    private static readonly ConcurrentDictionary<SchemaCacheKey, SchemaCacheEntry> SchemaCache = new();

    /// <summary>
    ///     使用指定 schema 文件同步校验 YAML 文本。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件绝对路径。</param>
    /// <param name="yamlPath">YAML 文件路径，仅用于诊断信息。</param>
    /// <param name="yamlText">待校验的 YAML 文本。</param>
    /// <exception cref="ArgumentException">当 <paramref name="tableName" /> 或 <paramref name="schemaPath" /> 为空白时抛出。</exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="yamlPath" /> 或 <paramref name="yamlText" /> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="GFramework.Game.Abstractions.Config.ConfigLoadException">当 schema 文件不可用，或 YAML 内容与 schema 不匹配时抛出。</exception>
    /// <remarks>
    ///     同步加载 schema 并立即校验，适合非异步上下文；内部委托 <see cref="YamlConfigSchemaValidator.Validate" /> 执行校验逻辑。
    /// </remarks>
    public static void Validate(
        string tableName,
        string schemaPath,
        string yamlPath,
        string yamlText)
    {
        var schema = GetOrLoadSchema(tableName, schemaPath);
        YamlConfigSchemaValidator.Validate(tableName, schema, yamlPath, yamlText);
    }

    /// <summary>
    ///     使用指定 schema 文件异步校验 YAML 文本。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件绝对路径。</param>
    /// <param name="yamlPath">YAML 文件路径，仅用于诊断信息。</param>
    /// <param name="yamlText">待校验的 YAML 文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步校验操作的任务。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="tableName" /> 或 <paramref name="schemaPath" /> 为空白时抛出。</exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="yamlPath" /> 或 <paramref name="yamlText" /> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="GFramework.Game.Abstractions.Config.ConfigLoadException">当 schema 文件不可用，或 YAML 内容与 schema 不匹配时抛出。</exception>
    /// <exception cref="OperationCanceledException">当 <paramref name="cancellationToken" /> 已被触发时抛出。</exception>
    /// <remarks>
    ///     异步加载 schema（调用 <see cref="YamlConfigSchemaValidator.LoadAsync" />）后同步执行校验，适合 I/O 密集场景；
    ///     校验本身不涉及异步操作。
    /// </remarks>
    public static async Task ValidateAsync(
        string tableName,
        string schemaPath,
        string yamlPath,
        string yamlText,
        CancellationToken cancellationToken = default)
    {
        var schema = await GetOrLoadSchemaAsync(tableName, schemaPath, cancellationToken)
            .ConfigureAwait(false);
        YamlConfigSchemaValidator.Validate(tableName, schema, yamlPath, yamlText);
    }

    /// <summary>
    ///     获取可复用的 schema 模型，必要时从磁盘重新加载。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件绝对路径。</param>
    /// <returns>与当前 schema 文件内容匹配的已解析模型。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="tableName" /> 或 <paramref name="schemaPath" /> 为空白时抛出。</exception>
    /// <exception cref="GFramework.Game.Abstractions.Config.ConfigLoadException">当 schema 文件不可用或内容非法时抛出。</exception>
    private static YamlConfigSchema GetOrLoadSchema(
        string tableName,
        string schemaPath)
    {
        var cacheKey = CreateCacheKey(tableName, schemaPath);
        if (TryGetCachedSchema(cacheKey, out var cachedSchema))
        {
            return cachedSchema;
        }

        var lastWriteTimeUtc = File.GetLastWriteTimeUtc(schemaPath);
        var schema = YamlConfigSchemaValidator.Load(tableName, schemaPath);
        CacheSchema(cacheKey, lastWriteTimeUtc, schema);
        return schema;
    }

    /// <summary>
    ///     异步获取可复用的 schema 模型，必要时从磁盘重新加载。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件绝对路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>与当前 schema 文件内容匹配的已解析模型。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="tableName" /> 或 <paramref name="schemaPath" /> 为空白时抛出。</exception>
    /// <exception cref="GFramework.Game.Abstractions.Config.ConfigLoadException">当 schema 文件不可用或内容非法时抛出。</exception>
    private static async Task<YamlConfigSchema> GetOrLoadSchemaAsync(
        string tableName,
        string schemaPath,
        CancellationToken cancellationToken)
    {
        var cacheKey = CreateCacheKey(tableName, schemaPath);
        if (TryGetCachedSchema(cacheKey, out var cachedSchema))
        {
            return cachedSchema;
        }

        var lastWriteTimeUtc = File.GetLastWriteTimeUtc(schemaPath);
        var schema = await YamlConfigSchemaValidator.LoadAsync(tableName, schemaPath, cancellationToken)
            .ConfigureAwait(false);
        CacheSchema(cacheKey, lastWriteTimeUtc, schema);
        return schema;
    }

    /// <summary>
    ///     创建 schema 缓存键，并提前执行与公开入口一致的参数契约检查。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件绝对路径。</param>
    /// <returns>用于缓存查找的稳定键。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="tableName" /> 或 <paramref name="schemaPath" /> 为空白时抛出。</exception>
    private static SchemaCacheKey CreateCacheKey(
        string tableName,
        string schemaPath)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        }

        if (string.IsNullOrWhiteSpace(schemaPath))
        {
            throw new ArgumentException("Schema path cannot be null or whitespace.", nameof(schemaPath));
        }

        return new SchemaCacheKey(tableName, schemaPath);
    }

    /// <summary>
    ///     尝试命中当前 schema 文件版本对应的缓存项。
    /// </summary>
    /// <param name="cacheKey">缓存键。</param>
    /// <param name="schema">命中的 schema；未命中时为 <see langword="null" />。</param>
    /// <returns>当缓存项仍与当前文件时间戳一致时返回 <see langword="true" />。</returns>
    private static bool TryGetCachedSchema(
        SchemaCacheKey cacheKey,
        out YamlConfigSchema schema)
    {
        var lastWriteTimeUtc = File.GetLastWriteTimeUtc(cacheKey.SchemaPath);
        if (SchemaCache.TryGetValue(cacheKey, out var cacheEntry) &&
            cacheEntry.LastWriteTimeUtc == lastWriteTimeUtc)
        {
            schema = cacheEntry.Schema;
            return true;
        }

        schema = null!;
        return false;
    }

    /// <summary>
    ///     使用读取前捕获的文件时间戳刷新 schema 缓存。
    ///     这样即使 schema 在读取过程中发生变化，后续访问也会因时间戳变新而重新加载，
    ///     避免把“旧内容 + 新时间戳”写入缓存。
    /// </summary>
    /// <param name="cacheKey">缓存键。</param>
    /// <param name="lastWriteTimeUtc">本次读取开始前捕获的 schema 文件修改时间。</param>
    /// <param name="schema">最新加载的 schema。</param>
    private static void CacheSchema(
        SchemaCacheKey cacheKey,
        DateTime lastWriteTimeUtc,
        YamlConfigSchema schema)
    {
        SchemaCache[cacheKey] = new SchemaCacheEntry(lastWriteTimeUtc, schema);
    }

    /// <summary>
    ///     表示一个 schema 缓存键。
    /// </summary>
    /// <param name="TableName">所属配置表名称。</param>
    /// <param name="SchemaPath">Schema 文件绝对路径。</param>
    private readonly record struct SchemaCacheKey(
        string TableName,
        string SchemaPath);

    /// <summary>
    ///     表示一个带文件时间戳的 schema 缓存条目。
    /// </summary>
    /// <param name="LastWriteTimeUtc">加载时观察到的 schema 文件修改时间。</param>
    /// <param name="Schema">已解析的 schema 模型。</param>
    private readonly record struct SchemaCacheEntry(
        DateTime LastWriteTimeUtc,
        YamlConfigSchema Schema);
}
