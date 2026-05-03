// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging;

/// <summary>
///     日志抽象基类，封装日志级别判断、格式化与异常处理逻辑。
///     平台日志器只需实现 Write 方法即可。
/// </summary>
public abstract class AbstractLogger : IStructuredLogger
{
    /// <summary>
    ///     根日志记录器的名称常量
    /// </summary>
    public const string RootLoggerName = "ROOT";

    private readonly Func<LogLevel> _minLevelProvider;
    private readonly string _name;

    /// <summary>
    ///     使用固定最小日志级别初始化日志器。
    /// </summary>
    /// <param name="name">日志器名称。</param>
    /// <param name="minLevel">最小日志级别。</param>
    protected AbstractLogger(
        string? name = null,
        LogLevel minLevel = LogLevel.Info)
        : this(name, () => minLevel)
    {
    }

    /// <summary>
    ///     使用动态最小日志级别提供器初始化日志器。
    /// </summary>
    /// <param name="name">日志器名称。</param>
    /// <param name="minLevelProvider">最小日志级别提供器。</param>
    protected AbstractLogger(
        string? name,
        Func<LogLevel> minLevelProvider)
    {
        ArgumentNullException.ThrowIfNull(minLevelProvider);

        _name = name ?? RootLoggerName;
        _minLevelProvider = minLevelProvider;
    }

    #region Metadata

    /// <summary>
    ///     获取日志器的名称
    /// </summary>
    /// <returns>日志器名称</returns>
    public string Name()
    {
        return _name;
    }

    #endregion

    /// <summary>
    ///     平台输出入口，由具体实现负责真正的日志写入。
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象（可为null）</param>
    protected abstract void Write(LogLevel level, string message, Exception? exception);

    #region Level Checks

    /// <summary>
    ///     判断指定日志级别是否启用
    /// </summary>
    /// <param name="level">要检查的日志级别</param>
    /// <returns>如果指定级别大于等于最小级别则返回true，否则返回false</returns>
    protected bool IsEnabled(LogLevel level)
    {
        return level >= _minLevelProvider();
    }

    /// <summary>
    ///     检查Trace级别日志是否启用
    /// </summary>
    /// <returns>如果Trace级别启用返回true，否则返回false</returns>
    public bool IsTraceEnabled()
    {
        return IsEnabled(LogLevel.Trace);
    }

    /// <summary>
    ///     检查Debug级别日志是否启用
    /// </summary>
    /// <returns>如果Debug级别启用返回true，否则返回false</returns>
    public bool IsDebugEnabled()
    {
        return IsEnabled(LogLevel.Debug);
    }

    /// <summary>
    ///     检查Info级别日志是否启用
    /// </summary>
    /// <returns>如果Info级别启用返回true，否则返回false</returns>
    public bool IsInfoEnabled()
    {
        return IsEnabled(LogLevel.Info);
    }

    /// <summary>
    ///     检查Warning级别日志是否启用
    /// </summary>
    /// <returns>如果Warning级别启用返回true，否则返回false</returns>
    public bool IsWarnEnabled()
    {
        return IsEnabled(LogLevel.Warning);
    }

    /// <summary>
    ///     检查Error级别日志是否启用
    /// </summary>
    /// <returns>如果Error级别启用返回true，否则返回false</returns>
    public bool IsErrorEnabled()
    {
        return IsEnabled(LogLevel.Error);
    }

    /// <summary>
    ///     检查Fatal级别日志是否启用
    /// </summary>
    /// <returns>如果Fatal级别启用返回true，否则返回false</returns>
    public bool IsFatalEnabled()
    {
        return IsEnabled(LogLevel.Fatal);
    }

    /// <summary>
    ///     检查指定日志级别是否启用
    /// </summary>
    /// <param name="level">要检查的日志级别</param>
    /// <returns>如果指定级别启用返回true，否则返回false</returns>
    /// <exception cref="ArgumentException">当传入的日志级别不被识别时抛出</exception>
    public bool IsEnabledForLevel(LogLevel level)
    {
        // 根据不同的日志级别调用对应的检查方法
        return level switch
        {
            LogLevel.Trace => IsTraceEnabled(),
            LogLevel.Debug => IsDebugEnabled(),
            LogLevel.Info => IsInfoEnabled(),
            LogLevel.Warning => IsWarnEnabled(),
            LogLevel.Error => IsErrorEnabled(),
            LogLevel.Fatal => IsFatalEnabled(),
            _ => throw new ArgumentException($"Level [{level}] not recognized.", nameof(level))
        };
    }

    #endregion

    #region Trace

    /// <summary>
    ///     记录Trace级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Trace(string msg)
    {
        Log(LogLevel.Trace, msg);
    }

    /// <summary>
    ///     记录Trace级别日志（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Trace(string format, object arg)
    {
        Log(LogLevel.Trace, format, arg);
    }

    /// <summary>
    ///     记录Trace级别日志（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Trace(string format, object arg1, object arg2)
    {
        Log(LogLevel.Trace, format, arg1, arg2);
    }

    /// <summary>
    ///     记录Trace级别日志（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Trace(string format, params object[] arguments)
    {
        Log(LogLevel.Trace, format, arguments);
    }

    /// <summary>
    ///     记录Trace级别日志（带异常信息）
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Trace(string msg, Exception t)
    {
        Log(LogLevel.Trace, msg, t);
    }

    #endregion

    #region Debug

    /// <summary>
    ///     记录Debug级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Debug(string msg)
    {
        Log(LogLevel.Debug, msg);
    }

    /// <summary>
    ///     记录Debug级别日志（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Debug(string format, object arg)
    {
        Log(LogLevel.Debug, format, arg);
    }

    /// <summary>
    ///     记录Debug级别日志（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Debug(string format, object arg1, object arg2)
    {
        Log(LogLevel.Debug, format, arg1, arg2);
    }

    /// <summary>
    ///     记录Debug级别日志（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Debug(string format, params object[] arguments)
    {
        Log(LogLevel.Debug, format, arguments);
    }

    /// <summary>
    ///     记录Debug级别日志（带异常信息）
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Debug(string msg, Exception t)
    {
        Log(LogLevel.Debug, msg, t);
    }

    #endregion

    #region Info

    /// <summary>
    ///     记录Info级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Info(string msg)
    {
        Log(LogLevel.Info, msg);
    }

    /// <summary>
    ///     记录Info级别日志（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Info(string format, object arg)
    {
        Log(LogLevel.Info, format, arg);
    }

    /// <summary>
    ///     记录Info级别日志（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Info(string format, object arg1, object arg2)
    {
        Log(LogLevel.Info, format, arg1, arg2);
    }

    /// <summary>
    ///     记录Info级别日志（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Info(string format, params object[] arguments)
    {
        Log(LogLevel.Info, format, arguments);
    }

    /// <summary>
    ///     记录Info级别日志（带异常信息）
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Info(string msg, Exception t)
    {
        Log(LogLevel.Info, msg, t);
    }

    #endregion

    #region Warn

    /// <summary>
    ///     记录Warning级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Warn(string msg)
    {
        Log(LogLevel.Warning, msg);
    }

    /// <summary>
    ///     记录Warning级别日志（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Warn(string format, object arg)
    {
        Log(LogLevel.Warning, format, arg);
    }

    /// <summary>
    ///     记录Warning级别日志（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Warn(string format, object arg1, object arg2)
    {
        Log(LogLevel.Warning, format, arg1, arg2);
    }

    /// <summary>
    ///     记录Warning级别日志（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Warn(string format, params object[] arguments)
    {
        Log(LogLevel.Warning, format, arguments);
    }

    /// <summary>
    ///     记录Warning级别日志（带异常信息）
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Warn(string msg, Exception t)
    {
        Log(LogLevel.Warning, msg, t);
    }

    #endregion

    #region Error

    /// <summary>
    ///     记录Error级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Error(string msg)
    {
        Log(LogLevel.Error, msg);
    }

    /// <summary>
    ///     记录Error级别日志（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Error(string format, object arg)
    {
        Log(LogLevel.Error, format, arg);
    }

    /// <summary>
    ///     记录Error级别日志（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Error(string format, object arg1, object arg2)
    {
        Log(LogLevel.Error, format, arg1, arg2);
    }

    /// <summary>
    ///     记录Error级别日志（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Error(string format, params object[] arguments)
    {
        Log(LogLevel.Error, format, arguments);
    }

    /// <summary>
    ///     记录Error级别日志（带异常信息）
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Error(string msg, Exception t)
    {
        Log(LogLevel.Error, msg, t);
    }

    #endregion

    #region Fatal

    /// <summary>
    ///     记录Fatal级别日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void Fatal(string msg)
    {
        Log(LogLevel.Fatal, msg);
    }

    /// <summary>
    ///     记录Fatal级别日志（带格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Fatal(string format, object arg)
    {
        Log(LogLevel.Fatal, format, arg);
    }

    /// <summary>
    ///     记录Fatal级别日志（带两个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Fatal(string format, object arg1, object arg2)
    {
        Log(LogLevel.Fatal, format, arg1, arg2);
    }

    /// <summary>
    ///     记录Fatal级别日志（带多个格式化参数）
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Fatal(string format, params object[] arguments)
    {
        Log(LogLevel.Fatal, format, arguments);
    }

    /// <summary>
    ///     记录Fatal级别日志（带异常信息）
    /// </summary>
    /// <param name="msg">日志消息</param>
    /// <param name="t">异常对象</param>
    public void Fatal(string msg, Exception t)
    {
        Log(LogLevel.Fatal, msg, t);
    }

    #endregion

    #region Generic Log Methods

    /// <summary>
    ///     使用指定的日志级别记录消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">要记录的消息字符串</param>
    public void Log(LogLevel level, string message)
    {
        if (!IsEnabled(level)) return;
        Write(level, message, null);
    }

    /// <summary>
    ///     使用指定的日志级别根据格式和参数记录消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="format">格式字符串</param>
    /// <param name="arg">参数</param>
    public void Log(LogLevel level, string format, object arg)
    {
        if (!IsEnabled(level)) return;
        Write(level, string.Format(format, arg), null);
    }

    /// <summary>
    ///     使用指定的日志级别根据格式和参数记录消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="format">格式字符串</param>
    /// <param name="arg1">第一个参数</param>
    /// <param name="arg2">第二个参数</param>
    public void Log(LogLevel level, string format, object arg1, object arg2)
    {
        if (!IsEnabled(level)) return;
        Write(level, string.Format(format, arg1, arg2), null);
    }

    /// <summary>
    ///     使用指定的日志级别根据格式和参数数组记录消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="format">格式字符串</param>
    /// <param name="arguments">参数数组</param>
    public void Log(LogLevel level, string format, params object[] arguments)
    {
        if (!IsEnabled(level)) return;
        Write(level, string.Format(format, arguments), null);
    }

    /// <summary>
    ///     使用指定的日志级别记录消息和异常
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">伴随异常的消息</param>
    /// <param name="exception">要记录的异常</param>
    public void Log(LogLevel level, string message, Exception exception)
    {
        if (!IsEnabled(level)) return;
        Write(level, message, exception);
    }

    #endregion

    #region Structured Log Methods

    /// <summary>
    ///     使用指定的日志级别记录消息和结构化属性
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="properties">结构化属性键值对</param>
    public virtual void Log(LogLevel level, string message, params (string Key, object? Value)[] properties)
    {
        if (!IsEnabled(level)) return;

        // 默认实现：将属性附加到消息后面
        if (properties.Length > 0)
        {
            var propsStr = string.Join(", ", properties.Select(p => $"{p.Key}={p.Value}"));
            Write(level, $"{message} | {propsStr}", null);
        }
        else
        {
            Write(level, message, null);
        }
    }

    /// <summary>
    ///     使用指定的日志级别记录消息、异常和结构化属性
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象</param>
    /// <param name="properties">结构化属性键值对</param>
    public virtual void Log(LogLevel level, string message, Exception? exception,
        params (string Key, object? Value)[] properties)
    {
        if (!IsEnabled(level)) return;

        // 默认实现：将属性附加到消息后面
        if (properties.Length > 0)
        {
            var propsStr = string.Join(", ", properties.Select(p => $"{p.Key}={p.Value}"));
            Write(level, $"{message} | {propsStr}", exception);
        }
        else
        {
            Write(level, message, exception);
        }
    }

    #endregion

    #region Core Pipeline (Private)

    #endregion
}
