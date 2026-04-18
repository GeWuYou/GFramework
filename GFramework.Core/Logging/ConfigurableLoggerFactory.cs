using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Appenders;

namespace GFramework.Core.Logging;

/// <summary>
///     可配置的 Logger 工厂。
/// </summary>
internal sealed class ConfigurableLoggerFactory : ILoggerFactory, IDisposable
{
    private readonly ILogAppender[] _appenders;
    private readonly LoggingConfiguration _config;
    private int _disposed;

    /// <summary>
    ///     初始化一个基于日志配置创建输出管线的工厂实例。
    /// </summary>
    /// <param name="config">日志配置。</param>
    public ConfigurableLoggerFactory(LoggingConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // 反序列化输入可能显式把集合写成 null，这里统一归一化为可安全枚举的空集合。
        _config.Appenders ??= [];
        _config.LoggerLevels ??= new Dictionary<string, LogLevel>(StringComparer.Ordinal);
        _appenders = _config.Appenders.Select(LoggingConfigurationLoader.CreateAppender).ToArray();
    }

    /// <summary>
    ///     释放内部 Appender 持有的资源。
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        foreach (var appender in _appenders)
        {
            switch (appender)
            {
                case AsyncLogAppender asyncLogAppender:
                    asyncLogAppender.Dispose();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }

    /// <summary>
    ///     为指定名称创建日志记录器，并应用最匹配的命名空间级别配置。
    /// </summary>
    /// <param name="name">日志记录器名称。</param>
    /// <param name="minLevel">调用方要求的最小日志级别下限；最终级别不会低于该值。</param>
    /// <returns>可写入日志的记录器实例。</returns>
    /// <remarks>
    ///     当配置文件与调用方同时提供默认级别时，会取两者中更严格的那一个；
    ///     若命中更具体的命名空间级别覆盖，则以该覆盖配置为准。
    /// </remarks>
    public ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info)
    {
        var effectiveLevel = _config.MinLevel > minLevel ? _config.MinLevel : minLevel;
        var bestMatchLength = -1;

        foreach (var kvp in _config.LoggerLevels)
        {
            var isExactMatch = string.Equals(name, kvp.Key, StringComparison.Ordinal);
            if (isExactMatch)
            {
                effectiveLevel = kvp.Value;
                break;
            }

            var isPrefixMatch = name.StartsWith(kvp.Key + ".", StringComparison.Ordinal);
            if (isPrefixMatch && kvp.Key.Length > bestMatchLength)
            {
                // 多个命名空间前缀都能命中时，最长前缀代表最具体的配置。
                bestMatchLength = kvp.Key.Length;
                effectiveLevel = kvp.Value;
            }
        }

        if (_appenders.Length == 0)
        {
            return new ConsoleLogger(name, effectiveLevel);
        }

        if (_appenders.Length == 1 && _appenders[0] is ConsoleAppender)
        {
            return new ConsoleLogger(name, effectiveLevel);
        }

        return new CompositeLogger(name, effectiveLevel, _appenders);
    }
}
