namespace GFramework.Core.logging;

/// <summary>
/// 空日志记录器实现，用于禁用日志记录功能
/// </summary>
internal sealed class NullLogger : ILog
{
    /// <summary>
    /// 记录日志消息（空实现，不执行任何操作）
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">相关异常对象（可选）</param>
    /// <param name="context">日志上下文信息（可选）</param>
    public void Log(LogLevel level, string message, Exception? exception, object? context) { }

    /// <summary>
    /// 检查指定日志级别是否启用
    /// </summary>
    /// <param name="level">要检查的日志级别</param>
    /// <returns>始终返回 false，表示所有日志级别都被禁用</returns>
    public bool IsEnabled(LogLevel level) => false;

    /// <summary>
    /// 记录信息级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public void Info(string msg, object? ctx = null) { }

    /// <summary>
    /// 记录错误级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ex">相关异常对象（可选）</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public void Error(string msg, Exception? ex = null, object? ctx = null) { }

    /// <summary>
    /// 记录调试级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public void Debug(string msg, object? ctx = null) { }

    /// <summary>
    /// 记录跟踪级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public void Trace(string msg, object? ctx = null) { }

    /// <summary>
    /// 记录警告级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public void Warn(string msg, object? ctx = null) { }

    /// <summary>
    /// 记录致命错误级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ex">相关异常对象（可选）</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public void Fatal(string msg,Exception? ex = null, object? ctx = null) { }
}
