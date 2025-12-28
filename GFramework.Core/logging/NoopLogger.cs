using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.logging;

/// <summary>
///     空操作日志记录器实现，不执行任何实际的日志记录操作
/// </summary>
/// <param name="name">日志记录器名称，默认为null</param>
/// <param name="minLevel">最小日志级别，默认为Info</param>
internal sealed class NoopLogger(
    string? name = null,
    LogLevel minLevel = LogLevel.Info) : AbstractLogger(name ?? ILogger.RootLoggerName, minLevel)
{
    /// <summary>
    ///     重写写入方法，空操作实现，不执行任何实际的日志记录操作
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    protected override void Write(LogLevel level, string message, Exception? exception)
    {
    }
}