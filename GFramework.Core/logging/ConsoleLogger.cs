namespace GFramework.Core.logging;

/// <summary>
///     控制台日志记录器
/// </summary>
public sealed class ConsoleLogger(
    string? name = null,
    LogLevel minLevel = LogLevel.Info,
    TextWriter? writer = null,
    bool useColors = true) : AbstractLogger(name ?? ILogger.RootLoggerName, minLevel)
{
    private readonly bool _useColors = useColors && writer == Console.Out;
    private readonly TextWriter _writer = writer ?? Console.Out;

    /// <summary>
    ///     写入日志消息到控制台
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息，可为空</param>
    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper().PadRight(7);
        var log = $"[{timestamp}] {levelStr} [{Name()}] {message}";

        // 添加异常信息到日志
        if (exception != null) log += Environment.NewLine + exception;

        if (_useColors)
            WriteColored(level, log);
        else
            _writer.WriteLine(log);
    }

    #region Internal Core

    /// <summary>
    ///     以指定颜色写入日志消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    private void WriteColored(LogLevel level, string message)
    {
        var original = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = GetColor(level);
            _writer.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = original;
        }
    }

    /// <summary>
    ///     根据日志级别获取对应的颜色
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <returns>控制台颜色</returns>
    private static ConsoleColor GetColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Cyan,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.Magenta,
            _ => ConsoleColor.White
        };
    }

    #endregion
}