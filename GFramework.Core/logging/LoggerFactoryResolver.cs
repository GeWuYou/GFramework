using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.logging;

/// <summary>
///     日志工厂提供程序解析器，用于管理和提供日志工厂提供程序实例
/// </summary>
public static class LoggerFactoryResolver
{
    /// <summary>
    ///     获取或设置当前的日志工厂提供程序
    /// </summary>
    /// <value>
    ///     日志工厂提供程序实例，默认为控制台日志工厂提供程序
    /// </value>
    public static ILoggerFactoryProvider Provider { get; set; }
        = new ConsoleLoggerFactoryProvider();

    /// <summary>
    ///     获取或设置日志记录的最小级别
    /// </summary>
    /// <value>
    ///     日志级别枚举值，默认为Info级别
    /// </value>
    public static LogLevel MinLevel { get; set; } = LogLevel.Info;
}