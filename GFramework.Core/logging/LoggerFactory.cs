namespace GFramework.Core.logging;

/// <summary>
/// 日志工厂实现，使用LogConfig配置创建日志记录器
/// </summary>
public class LoggerFactory : ILoggerFactory
{
    private readonly LogConfig _config;
    private readonly Dictionary<string, ILog> _loggers = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">日志配置</param>
    public LoggerFactory(LogConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// 创建指定类别的日志记录器
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <returns>日志记录器实例</returns>
    public ILog Create(string category)
    {
        if (_loggers.TryGetValue(category, out var existingLogger))
        {
            return existingLogger;
        }

        var logger = CreateLogger(category);
        _loggers[category] = logger;
        return logger;
    }

    /// <summary>
    /// 创建全局日志记录器实例
    /// </summary>
    /// <returns>全局日志记录器</returns>
    public ILog CreateGlobalLogger()
    {
        return Create("Global");
    }
    /// <summary>
    /// 创建日志记录器
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <returns>日志记录器实例</returns>
    private ILog CreateLogger(string category)
    {
        var level = _config.GetCategoryLevel(category);
        var consoleLogger = new ConsoleLogger(category, level, null, _config.UseColors);

        if (_config.EnableFile && !string.IsNullOrEmpty(_config.LogFilePath))
        {
            // 创建一个组合日志记录器，同时输出到控制台和文件
            return new CompositeLogger(level, consoleLogger, CreateFileLogger(category));
        }

        return consoleLogger;
    }

    /// <summary>
    /// 创建文件日志记录器
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <returns>文件日志记录器实例</returns>
    private ILog CreateFileLogger(string category)
    {
        try
        {
            var level = _config.GetCategoryLevel(category);
            var directory = Path.GetDirectoryName(_config.LogFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var writer = new StreamWriter(_config.LogFilePath!, append: true);
            return new ConsoleLogger(category, level, writer, useColors: false);
        }
        catch
        {
            return new NullLogger();
        }
    }


    /// <summary>
    /// 组合日志记录器，将日志同时输出到多个目标
    /// </summary>
    /// <param name="minLevel">最小日志级别，低于此级别的日志将被忽略</param>
    /// <param name="consoleLogger">控制台日志记录器</param>
    /// <param name="fileLogger">文件日志记录器</param>
    private sealed class CompositeLogger(
        LogLevel minLevel,
        ILog consoleLogger,
        ILog fileLogger)
        : ILog
    {
        /// <summary>
        /// 记录日志消息到所有配置的日志目标
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="exception">相关异常（可选）</param>
        /// <param name="context">上下文信息（可选）</param>
        public void Log(
            LogLevel level,
            string message,
            Exception? exception = null,
            object? context = null)
        {
            if (!IsEnabled(level))
                return;

            consoleLogger.Log(level, message, exception, context);
            fileLogger.Log(level, message, exception, context);
        }

        /// <summary>
        /// 检查指定的日志级别是否已启用
        /// </summary>
        /// <param name="level">要检查的日志级别</param>
        /// <returns>如果级别已启用则返回true，否则返回false</returns>
        public bool IsEnabled(LogLevel level)
        {
            return level >= minLevel;
        }

        // 快捷方法
        /// <summary>
        /// 记录跟踪级别的日志消息
        /// </summary>
        /// <param name="msg">日志消息</param>
        /// <param name="ctx">上下文信息（可选）</param>
        public void Trace(string msg, object? ctx = null)
            => Log(LogLevel.Trace, msg, null, ctx);

        /// <summary>
        /// 记录调试级别的日志消息
        /// </summary>
        /// <param name="msg">日志消息</param>
        /// <param name="ctx">上下文信息（可选）</param>
        public void Debug(string msg, object? ctx = null)
            => Log(LogLevel.Debug, msg, null, ctx);

        /// <summary>
        /// 记录信息级别的日志消息
        /// </summary>
        /// <param name="msg">日志消息</param>
        /// <param name="ctx">上下文信息（可选）</param>
        public void Info(string msg, object? ctx = null)
            => Log(LogLevel.Info, msg, null, ctx);

        /// <summary>
        /// 记录警告级别的日志消息
        /// </summary>
        /// <param name="msg">日志消息</param>
        /// <param name="ctx">上下文信息（可选）</param>
        public void Warn(string msg, object? ctx = null)
            => Log(LogLevel.Warning, msg, null, ctx);

        /// <summary>
        /// 记录错误级别的日志消息
        /// </summary>
        /// <param name="msg">日志消息</param>
        /// <param name="ex">相关异常（可选）</param>
        /// <param name="ctx">上下文信息（可选）</param>
        public void Error(string msg, Exception? ex = null, object? ctx = null)
            => Log(LogLevel.Error, msg, ex, ctx);

        /// <summary>
        /// 记录致命错误级别的日志消息
        /// </summary>
        /// <param name="msg">日志消息</param>
        /// <param name="ex">相关异常（可选）</param>
        /// <param name="ctx">上下文信息（可选）</param>
        public void Fatal(string msg, Exception? ex = null, object? ctx = null)
            => Log(LogLevel.Fatal, msg, ex, ctx);
    }

}