// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Appenders;
using GFramework.Core.Logging.Filters;
using GFramework.Core.Logging.Formatters;

namespace GFramework.Core.Logging;

/// <summary>
///     日志配置加载器
/// </summary>
public static class LoggingConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    /// <summary>
    ///     从 JSON 文件加载配置
    /// </summary>
    /// <param name="filePath">配置文件路径</param>
    /// <returns>日志配置对象</returns>
    public static LoggingConfiguration LoadFromJson(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<LoggingConfiguration>(json, JsonOptions);

        return config ?? throw new InvalidOperationException("Failed to deserialize configuration.");
    }

    /// <summary>
    ///     从 JSON 字符串加载配置
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <returns>日志配置对象</returns>
    public static LoggingConfiguration LoadFromJsonString(string json)
    {
        var config = JsonSerializer.Deserialize<LoggingConfiguration>(json, JsonOptions);
        return config ?? throw new InvalidOperationException("Failed to deserialize configuration.");
    }

    /// <summary>
    ///     根据配置创建 Logger 工厂
    /// </summary>
    /// <param name="config">日志配置</param>
    /// <returns>Logger 工厂</returns>
    public static ILoggerFactory CreateFactory(LoggingConfiguration config)
    {
        return new ConfigurableLoggerFactory(config);
    }

    /// <summary>
    ///     根据配置创建 Appender
    /// </summary>
    internal static ILogAppender CreateAppender(AppenderConfiguration config)
    {
        var formatter = CreateFormatter(config.Formatter);
        var filter = config.Filter != null ? CreateFilter(config.Filter) : null;

        return config.Type.ToLowerInvariant() switch
        {
            "console" => new ConsoleAppender(formatter, useColors: config.UseColors, filter: filter),

            "file" => new FileAppender(
                config.FilePath ?? throw new InvalidOperationException("FilePath is required for File appender."),
                formatter,
                filter),

            "rollingfile" => new RollingFileAppender(
                config.FilePath ??
                throw new InvalidOperationException("FilePath is required for RollingFile appender."),
                config.MaxFileSize,
                config.MaxFileCount,
                formatter,
                filter),

            "async" => new AsyncLogAppender(
                CreateAppender(config.InnerAppender ??
                               throw new InvalidOperationException("InnerAppender is required for Async appender.")),
                config.BufferSize),

            _ => throw new NotSupportedException($"Appender type '{config.Type}' is not supported.")
        };
    }

    /// <summary>
    ///     根据配置创建格式化器
    /// </summary>
    internal static ILogFormatter CreateFormatter(string formatterType)
    {
        return formatterType.ToLowerInvariant() switch
        {
            "default" => new DefaultLogFormatter(),
            "json" => new JsonLogFormatter(),
            _ => throw new NotSupportedException($"Formatter type '{formatterType}' is not supported.")
        };
    }

    /// <summary>
    ///     根据配置创建过滤器
    /// </summary>
    internal static ILogFilter CreateFilter(FilterConfiguration config)
    {
        return config.Type.ToLowerInvariant() switch
        {
            "loglevel" => new LogLevelFilter(
                config.MinLevel ?? throw new InvalidOperationException("MinLevel is required for LogLevel filter.")),

            "namespace" => new NamespaceFilter(
                config.Namespaces?.ToArray() ??
                throw new InvalidOperationException("Namespaces is required for Namespace filter.")),

            "composite" => new CompositeFilter(
                config.Filters?.Select(CreateFilter).ToArray() ??
                throw new InvalidOperationException("Filters is required for Composite filter.")),

            _ => throw new NotSupportedException($"Filter type '{config.Type}' is not supported.")
        };
    }
}
