namespace GFramework.Core.logging;

/// <summary>
/// 日志记录的静态类，提供全局日志记录功能
/// </summary>
public static class Log
{
    /// <summary>
    /// 获取或设置当前的日志记录器实例
    /// 默认使用 NullLogger，不输出任何日志
    /// </summary>
    public static ILog Instance { get; private set; } = new NullLogger();

    /// <summary>
    /// 设置日志记录器实例
    /// </summary>
    /// <param name="logger">要设置的日志记录器，如果为 null 则使用 NullLogger</param>
    public static void SetLogger(ILog? logger)
    {
        Instance = logger ?? new NullLogger();
    }

    /// <summary>
    /// 检查指定日志级别是否启用
    /// </summary>
    /// <param name="level">要检查的日志级别</param>
    /// <returns>如果指定级别已启用则返回 true，否则返回 false</returns>
    public static bool IsEnabled(LogLevel level) => Instance.IsEnabled(level);

    /// <summary>
    /// 记录信息级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Info(string msg, object? ctx = null)
        => Instance.Log(LogLevel.Info, msg, null, ctx);

    /// <summary>
    /// 记录错误级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ex">相关异常对象（可选）</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Error(string msg, Exception? ex = null, object? ctx = null)
        => Instance.Log(LogLevel.Error, msg, ex, ctx);

    /// <summary>
    /// 记录调试级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Debug(string msg, object? ctx = null)
        => Instance.Log(LogLevel.Debug, msg, null, ctx);

    /// <summary>
    /// 记录跟踪级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Trace(string msg, object? ctx = null)
        => Instance.Log(LogLevel.Trace, msg, null, ctx);

    /// <summary>
    /// 记录警告级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Warn(string msg, object? ctx = null)
        => Instance.Log(LogLevel.Warning, msg, null, ctx);

    /// <summary>
    /// 记录致命错误级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">日志上下文信息（可选）</param>
    public static void Fatal(string msg, object? ctx = null)
        => Instance.Log(LogLevel.Fatal, msg, null, ctx);
}
