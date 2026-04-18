using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging;

/// <summary>
///     过滤器配置。
/// </summary>
public sealed class FilterConfiguration
{
    /// <summary>
    ///     过滤器类型（LogLevel, Namespace, Composite）。
    /// </summary>
    public string Type { get; set; } = "LogLevel";

    /// <summary>
    ///     最小日志级别（用于 LogLevel 过滤器）。
    /// </summary>
    public LogLevel? MinLevel { get; set; }

    /// <summary>
    ///     命名空间前缀列表（用于 Namespace 过滤器）。
    /// </summary>
    public List<string>? Namespaces { get; set; }

    /// <summary>
    ///     子过滤器列表（用于 Composite 过滤器）。
    /// </summary>
    public List<FilterConfiguration>? Filters { get; set; }
}
