namespace GFramework.Core.logging;

/// <summary>
/// 控制台日志记录器
/// </summary>
public sealed class ConsoleLogger : ILogger
{
    private readonly LogLevel _minLevel;
    private readonly string _name;
    private readonly TextWriter _writer;
    private readonly bool _useColors;

    /// <summary>
    /// 初始化控制台日志记录器实例
    /// </summary>
    /// <param name="name">日志记录器名称，默认为根日志记录器名称</param>
    /// <param name="minLevel">最小日志级别，默认为Info级别</param>
    /// <param name="writer">文本写入器，默认为控制台输出</param>
    /// <param name="useColors">是否使用颜色输出，默认为true</param>
    public ConsoleLogger(
        string? name = null,
        LogLevel minLevel = LogLevel.Info,
        TextWriter? writer = null,
        bool useColors = true)
    {
        _name = name ?? ILogger.RootLoggerName;
        _minLevel = minLevel;
        _writer = writer ?? Console.Out;
        _useColors = useColors && _writer == Console.Out;
    }

    #region Metadata

    /// <summary>
    /// 获取日志记录器名称
    /// </summary>
    /// <returns>日志记录器名称</returns>
    public string Name() => _name;

    #endregion

    #region Level Checks

    /// <summary>
    /// 检查是否启用Trace级别日志
    /// </summary>
    /// <returns>如果启用返回true，否则返回false</returns>
    public bool IsTraceEnabled() => IsEnabled(LogLevel.Trace);

    /// <summary>
    /// 检查是否启用Debug级别日志
    /// </summary>
    /// <returns>如果启用返回true，否则返回false</returns>
    public bool IsDebugEnabled() => IsEnabled(LogLevel.Debug);

    /// <summary>
    /// 检查是否启用Info级别日志
    /// </summary>
    /// <returns>如果启用返回true，否则返回false</returns>
    public bool IsInfoEnabled()  => IsEnabled(LogLevel.Info);

    /// <summary>
    /// 检查是否启用Warn级别日志
    /// </summary>
    /// <returns>如果启用返回true，否则返回false</returns>
    public bool IsWarnEnabled()  => IsEnabled(LogLevel.Warning);

    /// <summary>
    /// 检查是否启用Error级别日志
    /// </summary>
    /// <returns>如果启用返回true，否则返回false</returns>
    public bool IsErrorEnabled() => IsEnabled(LogLevel.Error);

    /// <summary>
    /// 检查是否启用Fatal级别日志
    /// </summary>
    /// <returns>如果启用返回true，否则返回false</returns>
    public bool IsFatalEnabled() => IsEnabled(LogLevel.Fatal);

    /// <summary>
    /// 检查指定日志级别是否启用
    /// </summary>
    /// <param name="level">要检查的日志级别</param>
    /// <returns>如果启用返回true，否则返回false</returns>
    private bool IsEnabled(LogLevel level) => level >= _minLevel;

    #endregion

    #region Trace

    /// <summary>
    /// 记录Trace级别日志消息
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Trace(string msg) => LogIfEnabled(LogLevel.Trace, msg);

    /// <summary>
    /// 记录Trace级别日志消息（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Trace(string format, object arg) => LogIfEnabled(LogLevel.Trace, format, arg);

    /// <summary>
    /// 记录Trace级别日志消息（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Trace(string format, object arg1, object arg2) => LogIfEnabled(LogLevel.Trace, format, arg1, arg2);

    /// <summary>
    /// 记录Trace级别日志消息（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Trace(string format, params object[] arguments) => LogIfEnabled(LogLevel.Trace, format, arguments);

    /// <summary>
    /// 记录Trace级别异常日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Trace(string msg, Exception t) => LogException(LogLevel.Trace, msg, t);

    #endregion

    #region Debug

    /// <summary>
    /// 记录Debug级别日志消息
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Debug(string msg) => LogIfEnabled(LogLevel.Debug, msg);

    /// <summary>
    /// 记录Debug级别日志消息（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Debug(string format, object arg) => LogIfEnabled(LogLevel.Debug, format, arg);

    /// <summary>
    /// 记录Debug级别日志消息（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Debug(string format, object arg1, object arg2) => LogIfEnabled(LogLevel.Debug, format, arg1, arg2);

    /// <summary>
    /// 记录Debug级别日志消息（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Debug(string format, params object[] arguments) => LogIfEnabled(LogLevel.Debug, format, arguments);

    /// <summary>
    /// 记录Debug级别异常日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Debug(string msg, Exception t) => LogException(LogLevel.Debug, msg, t);

    #endregion

    #region Info

    /// <summary>
    /// 记录Info级别日志消息
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Info(string msg) => LogIfEnabled(LogLevel.Info, msg);

    /// <summary>
    /// 记录Info级别日志消息（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Info(string format, object arg) => LogIfEnabled(LogLevel.Info, format, arg);

    /// <summary>
    /// 记录Info级别日志消息（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Info(string format, object arg1, object arg2) => LogIfEnabled(LogLevel.Info, format, arg1, arg2);

    /// <summary>
    /// 记录Info级别日志消息（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Info(string format, params object[] arguments) => LogIfEnabled(LogLevel.Info, format, arguments);

    /// <summary>
    /// 记录Info级别异常日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Info(string msg, Exception t) => LogException(LogLevel.Info, msg, t);

    #endregion

    #region Warn

    /// <summary>
    /// 记录Warn级别日志消息
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Warn(string msg) => LogIfEnabled(LogLevel.Warning, msg);

    /// <summary>
    /// 记录Warn级别日志消息（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Warn(string format, object arg) => LogIfEnabled(LogLevel.Warning, format, arg);

    /// <summary>
    /// 记录Warn级别日志消息（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Warn(string format, object arg1, object arg2) => LogIfEnabled(LogLevel.Warning, format, arg1, arg2);

    /// <summary>
    /// 记录Warn级别日志消息（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Warn(string format, params object[] arguments) => LogIfEnabled(LogLevel.Warning, format, arguments);

    /// <summary>
    /// 记录Warn级别异常日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Warn(string msg, Exception t) => LogException(LogLevel.Warning, msg, t);

    #endregion

    #region Error

    /// <summary>
    /// 记录Error级别日志消息
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Error(string msg) => LogIfEnabled(LogLevel.Error, msg);

    /// <summary>
    /// 记录Error级别日志消息（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Error(string format, object arg) => LogIfEnabled(LogLevel.Error, format, arg);

    /// <summary>
    /// 记录Error级别日志消息（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Error(string format, object arg1, object arg2) => LogIfEnabled(LogLevel.Error, format, arg1, arg2);

    /// <summary>
    /// 记录Error级别日志消息（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Error(string format, params object[] arguments) => LogIfEnabled(LogLevel.Error, format, arguments);

    /// <summary>
    /// 记录Error级别异常日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Error(string msg, Exception t) => LogException(LogLevel.Error, msg, t);

    #endregion

    #region Fatal

    /// <summary>
    /// 记录Fatal级别日志消息
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Fatal(string msg) => LogIfEnabled(LogLevel.Fatal, msg);

    /// <summary>
    /// 记录Fatal级别日志消息（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Fatal(string format, object arg) => LogIfEnabled(LogLevel.Fatal, format, arg);

    /// <summary>
    /// 记录Fatal级别日志消息（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Fatal(string format, object arg1, object arg2) => LogIfEnabled(LogLevel.Fatal, format, arg1, arg2);

    /// <summary>
    /// 记录Fatal级别日志消息（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Fatal(string format, params object[] arguments) => LogIfEnabled(LogLevel.Fatal, format, arguments);

    /// <summary>
    /// 记录Fatal级别异常日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Fatal(string msg, Exception t) => LogException(LogLevel.Fatal, msg, t);

    #endregion

    #region Internal Core

    /// <summary>
    /// 如果指定级别已启用，则记录日志消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    private void LogIfEnabled(LogLevel level, string message)
    {
        if (!IsEnabled(level)) return;
        LogInternal(level, message, null);
    }

    /// <summary>
    /// 如果指定级别已启用，则记录格式化日志消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数数组</param>
    private void LogIfEnabled(LogLevel level, string format, params object[] args)
    {
        if (!IsEnabled(level)) return;
        LogInternal(level, string.Format(format, args), null);
    }

    /// <summary>
    /// 如果指定级别已启用，则记录异常日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象</param>
    private void LogException(LogLevel level, string message, Exception exception)
    {
        if (!IsEnabled(level)) return;
        LogInternal(level, message, exception);
    }

    /// <summary>
    /// 内部日志记录方法
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象（可选）</param>
    private void LogInternal(LogLevel level, string message, Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper().PadRight(7);
        var log = $"[{timestamp}] {levelStr} [{_name}] {message}";

        // 添加异常信息到日志
        if (exception != null)
        {
            log += Environment.NewLine + exception;
        }

        if (_useColors)
        {
            WriteColored(level, log);
        }
        else
        {
            _writer.WriteLine(log);
        }
    }

    /// <summary>
    /// 以指定颜色写入日志消息
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
    /// 根据日志级别获取对应的颜色
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <returns>控制台颜色</returns>
    private static ConsoleColor GetColor(LogLevel level) => level switch
    {
        LogLevel.Trace => ConsoleColor.DarkGray,
        LogLevel.Debug => ConsoleColor.Cyan,
        LogLevel.Info => ConsoleColor.White,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Fatal => ConsoleColor.Magenta,
        _ => ConsoleColor.White
    };

    #endregion
}
