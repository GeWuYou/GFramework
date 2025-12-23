namespace GFramework.Core.logging;

/// <summary>
/// 定义日志记录接口，提供日志记录和级别检查功能
/// </summary>
public interface ILog
{
    /// <summary>
    /// 记录指定级别的日志消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息内容</param>
    /// <param name="exception">可选的异常对象，默认为null</param>
    /// <param name="context">可选的上下文对象，默认为null</param>
    void Log(
        LogLevel level,
        string message,
        Exception? exception = null,
        object? context = null
    );

    /// <summary>
    /// 检查指定日志级别是否已启用
    /// </summary>
    /// <param name="level">要检查的日志级别</param>
    /// <returns>如果指定级别已启用则返回true，否则返回false</returns>
    bool IsEnabled(LogLevel level);
}
