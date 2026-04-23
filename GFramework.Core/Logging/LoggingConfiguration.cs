using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging;

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
#pragma warning disable MA0016 // Preserve the established concrete configuration API surface.
    public List<AppenderConfiguration> Appenders { get; set; } = new();
#pragma warning restore MA0016

    /// <summary>
    ///     特定 Logger 的日志级别配置
    /// </summary>
#pragma warning disable MA0016 // Preserve the established concrete configuration API surface.
    public Dictionary<string, LogLevel> LoggerLevels { get; set; } =
        new Dictionary<string, LogLevel>(StringComparer.Ordinal);
#pragma warning restore MA0016
}
