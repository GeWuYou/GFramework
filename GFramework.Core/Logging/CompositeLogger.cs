// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging;

/// <summary>
///     组合日志记录器，支持同时输出到多个 Appender
/// </summary>
public sealed class CompositeLogger : AbstractLogger, IDisposable
{
    private readonly ILogAppender[] _appenders;

    /// <summary>
    ///     创建组合日志记录器
    /// </summary>
    /// <param name="name">日志记录器名称</param>
    /// <param name="minLevel">最小日志级别</param>
    /// <param name="appenders">日志输出器列表</param>
    public CompositeLogger(
        string name,
        LogLevel minLevel,
        params ILogAppender[] appenders)
        : base(name, minLevel)
    {
        if (appenders == null || appenders.Length == 0)
            throw new ArgumentException("At least one appender must be provided.", nameof(appenders));

        _appenders = appenders;
    }

    /// <summary>
    ///     释放所有 Appender 资源
    /// </summary>
    public void Dispose()
    {
        foreach (var appender in _appenders)
        {
            if (appender is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    ///     写入日志到所有 Appender
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象</param>
    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        var entry = new LogEntry(
            DateTime.UtcNow,
            level,
            Name(),
            message,
            exception,
            null);

        foreach (var appender in _appenders)
        {
            appender.Append(entry);
        }
    }

    /// <summary>
    ///     使用指定的日志级别记录消息和结构化属性
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="properties">结构化属性键值对</param>
    public override void Log(LogLevel level, string message, params (string Key, object? Value)[] properties)
    {
        if (!IsEnabled(level)) return;

        var propsDict = properties.Length > 0
            ? properties.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal)
            : null;

        var entry = new LogEntry(
            DateTime.UtcNow,
            level,
            Name(),
            message,
            null,
            propsDict);

        foreach (var appender in _appenders)
        {
            appender.Append(entry);
        }
    }

    /// <summary>
    ///     使用指定的日志级别记录消息、异常和结构化属性
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象</param>
    /// <param name="properties">结构化属性键值对</param>
    public override void Log(LogLevel level, string message, Exception? exception,
        params (string Key, object? Value)[] properties)
    {
        if (!IsEnabled(level)) return;

        var propsDict = properties.Length > 0
            ? properties.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal)
            : null;

        var entry = new LogEntry(
            DateTime.UtcNow,
            level,
            Name(),
            message,
            exception,
            propsDict);

        foreach (var appender in _appenders)
        {
            appender.Append(entry);
        }
    }

    /// <summary>
    ///     刷新所有 Appender 的缓冲区
    /// </summary>
    public void Flush()
    {
        foreach (var appender in _appenders)
        {
            appender.Flush();
        }
    }
}
