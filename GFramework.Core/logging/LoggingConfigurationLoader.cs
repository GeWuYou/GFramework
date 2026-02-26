using System.IO;
using System.Text.Json;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging.appenders;
using GFramework.Core.logging.filters;
using GFramework.Core.logging.formatters;

namespace GFramework.Core.logging;

/// <summary>
///     日志配置加载器
/// </summary>
public static class LoggingConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
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

/// <summary>
///     可配置的 Logger 工厂
/// </summary>
internal sealed class ConfigurableLoggerFactory : ILoggerFactory
{
    private readonly ILogAppender[] _appenders;
    private readonly LoggingConfiguration _config;

    public ConfigurableLoggerFactory(LoggingConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _appenders = config.Appenders.Select(LoggingConfigurationLoader.CreateAppender).ToArray();
    }

    public ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info)
    {
        // 检查是否有特定 Logger 的级别配置
        var effectiveLevel = _config.LoggerLevels.TryGetValue(name, out var level)
            ? level
            : _config.MinLevel;

        // 如果没有 Appender，返回简单的 ConsoleLogger
        if (_appenders.Length == 0)
        {
            return new ConsoleLogger(name, effectiveLevel);
        }

        // 如果只有一个 Appender 且是 ConsoleAppender，优化为 ConsoleLogger
        if (_appenders.Length == 1 && _appenders[0] is ConsoleAppender)
        {
            return new ConsoleLogger(name, effectiveLevel);
        }

        // 返回 CompositeLogger
        return new CompositeLogger(name, effectiveLevel, _appenders);
    }
}