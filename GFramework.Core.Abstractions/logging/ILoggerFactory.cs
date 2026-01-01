namespace GFramework.Core.Abstractions.logging;

/// <summary>
///     定义日志工厂接口，用于创建日志记录器实例
/// </summary>
public interface ILoggerFactory
{
    /// <summary>
    ///     根据指定的名称获取日志记录器实例
    /// </summary>
    /// <param name="name">日志记录器的名称</param>
    /// <param name="minLevel">最小日志级别</param>
    /// <returns>指定名称的日志记录器实例</returns>
    ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info);
}