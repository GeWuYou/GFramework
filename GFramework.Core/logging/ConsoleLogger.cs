using System;
using System.IO;

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
    public bool IsEnabled(LogLevel level) => level >= _minLevel;

    /// <summary>
    /// 获取当前最小日志级别
    /// </summary>
    public LogLevel MinLevel => _minLevel;

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

    private ConsoleColor GetColor(LogLevel level)
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

    private string FormatContext(object context)
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
    public void Info(string msg, object? ctx = null)
        => Log(LogLevel.Info, msg, null, ctx);

    public void Error(string msg, Exception? ex = null, object? ctx = null)
        => Log(LogLevel.Error, msg, ex, ctx);

    public void Debug(string msg, object? ctx = null)
        => Log(LogLevel.Debug, msg, null, ctx);

    public void Trace(string msg, object? ctx = null)
        => Log(LogLevel.Trace, msg, null, ctx);

    public void Warn(string msg, object? ctx = null)
        => Log(LogLevel.Warning, msg, null, ctx);

    public void Fatal(string msg, object? ctx = null)
        => Log(LogLevel.Fatal, msg, null, ctx);
}