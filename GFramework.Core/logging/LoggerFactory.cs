using System;
using System.Collections.Generic;

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

    private ILog CreateLogger(string category)
    {
        var level = _config.GetCategoryLevel(category);
        var consoleLogger = new ConsoleLogger(category, level, null, _config.UseColors);
        
        if (_config.EnableFile && !string.IsNullOrEmpty(_config.LogFilePath))
        {
            // 创建一个组合日志记录器，同时输出到控制台和文件
            return new CompositeLogger(category, level, consoleLogger, CreateFileLogger(category));
        }
        
        return consoleLogger;
    }

    private ILog CreateFileLogger(string category)
    {
        try
        {
            var level = _config.GetCategoryLevel(category);
            var directory = System.IO.Path.GetDirectoryName(_config.LogFilePath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            var writer = new System.IO.StreamWriter(_config.LogFilePath!, append: true);
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
    private class CompositeLogger : ILog
    {
        private readonly string _category;
        private readonly LogLevel _minLevel;
        private readonly ILog _consoleLogger;
        private readonly ILog _fileLogger;

        public CompositeLogger(string category, LogLevel minLevel, ILog consoleLogger, ILog fileLogger)
        {
            _category = category;
            _minLevel = minLevel;
            _consoleLogger = consoleLogger;
            _fileLogger = fileLogger;
        }

        public void Log(LogLevel level, string message, Exception? exception = null, object? context = null)
        {
            if (!IsEnabled(level))
                return;

            _consoleLogger.Log(level, message, exception, context);
            _fileLogger.Log(level, message, exception, context);
        }

        public bool IsEnabled(LogLevel level) => level >= _minLevel;

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
}