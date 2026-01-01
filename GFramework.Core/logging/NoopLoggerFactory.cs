using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.logging;

/// <summary>
///     无操作日志工厂实现，用于提供空的日志记录功能
/// </summary>
public class NoopLoggerFactory : ILoggerFactory
{
    /// <summary>
    ///     获取指定名称的无操作日志记录器
    /// </summary>
    /// <param name="name">日志记录器的名称</param>
    /// <param name="minLevel">日志记录器的最小日志级别</param>
    /// <returns>返回一个NoopLogger实例，该实例不执行任何实际的日志记录操作</returns>
    public ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info)
    {
        return new NoopLogger();
    }
}