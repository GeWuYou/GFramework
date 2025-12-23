namespace GFramework.Core.logging;

/// <summary>
/// 控制台日志记录器实现，支持日志级别控制和格式化输出
/// </summary>
public sealed class ConsoleLogger : ILog
{
    private readonly LogLevel _minLevel;
    private readonly string? _category;
    private readonly TextWriter? _writer;
    private readonly bool _useColors;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <param name="minLevel">最小日志级别（默认Info）</param>
    /// <param name="writer">输出流（默认控制台）</param>
    /// <param name="useColors">是否使用颜色输出（默认true）</param>
    public ConsoleLogger(
        string? category = null,
        LogLevel minLevel = LogLevel.Info,
        TextWriter? writer = null,
        bool useColors = true)
    {
        _category = category;
        _minLevel = minLevel;
        _writer = writer ?? Console.Out;
        _useColors = useColors && _writer == Console.Out;
    }

    /// <summary>
    /// 记录日志消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息（可选）</param>
    /// <param name="context">上下文信息（可选）</param>
    public void Log(LogLevel level, string message, Exception? exception = null, object? context = null)
    {
        if (!IsEnabled(level))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper().PadRight(7);
        var categoryStr = _category != null ? $"[{_category}] " : "";
        var contextStr = context != null ? $" | Context: {FormatContext(context)}" : "";
        var exceptionStr = exception != null ? $"\nException: {exception}" : "";

        var logMessage = $"[{timestamp}] {levelStr} {categoryStr}{message}{contextStr}{exceptionStr}";

        if (_useColors)
        {
            WriteColored(level, logMessage);
        }
        else
        {
            _writer.WriteLine(logMessage);
        }
    }

    /// <summary>
    /// 检查指定日志级别是否启用
    /// </summary>
    /// <param name="level">要检查的日志级别</param>
    /// <returns>如果启用则返回true，否则返回false</returns>
    public bool IsEnabled(LogLevel level) => level >= _minLevel;

    /// <summary>
    /// 获取当前最小日志级别
    /// </summary>
    public LogLevel MinLevel => _minLevel;

    /// <summary>
    /// 使用颜色写入日志消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">要写入的消息</param>
    private void WriteColored(LogLevel level, string message)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = GetColor(level);
            _writer.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    /// 根据日志级别获取对应的颜色
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <returns>对应的控制台颜色</returns>
    private static ConsoleColor GetColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Debug => ConsoleColor.Cyan,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.Magenta,
            _ => ConsoleColor.White
        };
    }

    /// <summary>
    /// 格式化上下文对象为字符串
    /// </summary>
    /// <param name="context">上下文对象</param>
    /// <returns>格式化后的字符串</returns>
    private static string FormatContext(object context)
    {
        return context switch
        {
            string str => str,
            int num => num.ToString(),
            null => "null",
            _ => $"{context.GetType().Name}: {context}"
        };
    }

    // 快捷方法实现

    /// <summary>
    /// 记录信息级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">上下文信息（可选）</param>
    public void Info(string msg, object? ctx = null)
        => Log(LogLevel.Info, msg, null, ctx);

    /// <summary>
    /// 记录错误级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ex">异常信息（可选）</param>
    /// <param name="ctx">上下文信息（可选）</param>
    public void Error(string msg, Exception? ex = null, object? ctx = null)
        => Log(LogLevel.Error, msg, ex, ctx);

    /// <summary>
    /// 记录调试级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">上下文信息（可选）</param>
    public void Debug(string msg, object? ctx = null)
        => Log(LogLevel.Debug, msg, null, ctx);

    /// <summary>
    /// 记录跟踪级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">上下文信息（可选）</param>
    public void Trace(string msg, object? ctx = null)
        => Log(LogLevel.Trace, msg, null, ctx);

    /// <summary>
    /// 记录警告级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ctx">上下文信息（可选）</param>
    public void Warn(string msg, object? ctx = null)
        => Log(LogLevel.Warning, msg, null, ctx);

    /// <summary>
    /// 记录致命错误级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="ex">异常信息（可选）</param>
    /// <param name="ctx">上下文信息（可选）</param>
    public void Fatal(string msg,Exception? ex = null, object? ctx = null)
        => Log(LogLevel.Fatal, msg, ex, ctx);
}
