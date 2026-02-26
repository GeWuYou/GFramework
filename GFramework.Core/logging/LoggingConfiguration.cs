using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.logging;

/// <summary>
///     日志配置类
/// </summary>
public sealed class LoggingConfiguration
{
    /// <summary>
    ///     全局最小日志级别
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Info;

    /// <summary>
    ///     Appender 配置列表
    /// </summary>
    public List<AppenderConfiguration> Appenders { get; set; } = new();

    /// <summary>
    ///     特定 Logger 的日志级别配置
    /// </summary>
    public Dictionary<string, LogLevel> LoggerLevels { get; set; } = new();
}

/// <summary>
///     Appender 配置
/// </summary>
public sealed class AppenderConfiguration
{
    /// <summary>
    ///     Appender 类型（Console, File, RollingFile, Async）
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     格式化器类型（Default, Json）
    /// </summary>
    public string Formatter { get; set; } = "Default";

    /// <summary>
    ///     文件路径（仅用于 File 和 RollingFile）
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    ///     是否使用颜色（仅用于 Console）
    /// </summary>
    public bool UseColors { get; set; } = true;

    /// <summary>
    ///     缓冲区大小（仅用于 Async）
    /// </summary>
    public int BufferSize { get; set; } = 10000;

    /// <summary>
    ///     最大文件大小（仅用于 RollingFile，字节）
    /// </summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    ///     最大文件数量（仅用于 RollingFile）
    /// </summary>
    public int MaxFileCount { get; set; } = 5;

    /// <summary>
    ///     过滤器配置
    /// </summary>
    public FilterConfiguration? Filter { get; set; }

    /// <summary>
    ///     内部 Appender 配置（仅用于 Async）
    /// </summary>
    public AppenderConfiguration? InnerAppender { get; set; }
}

/// <summary>
///     过滤器配置
/// </summary>
public sealed class FilterConfiguration
{
    /// <summary>
    ///     过滤器类型（LogLevel, Namespace, Composite）
    /// </summary>
    public string Type { get; set; } = "LogLevel";

    /// <summary>
    ///     最小日志级别（用于 LogLevel 过滤器）
    /// </summary>
    public LogLevel? MinLevel { get; set; }

    /// <summary>
    ///     命名空间前缀列表（用于 Namespace 过滤器）
    /// </summary>
    public List<string>? Namespaces { get; set; }

    /// <summary>
    ///     子过滤器列表（用于 Composite 过滤器）
    /// </summary>
    public List<FilterConfiguration>? Filters { get; set; }
}