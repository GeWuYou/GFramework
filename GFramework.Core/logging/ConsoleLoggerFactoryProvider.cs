using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.logging;

/// <summary>
///     控制台日志记录器工厂提供程序，用于创建控制台日志记录器实例
/// </summary>
public sealed class ConsoleLoggerFactoryProvider : ILoggerFactoryProvider
{
    /// <summary>
    ///     获取或设置日志记录器的最小日志级别，低于此级别的日志将被忽略
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Info;

    /// <summary>
    ///     创建一个日志记录器实例
    /// </summary>
    /// <param name="name">日志记录器的名称，用于标识特定的日志源</param>
    /// <returns>配置了指定名称和最小日志级别的ILogger实例</returns>
    public ILogger CreateLogger(string name)
    {
        return new ConsoleLoggerFactory().GetLogger(name, MinLevel);
    }
}