using System;
using GFramework.Core.logging;

namespace GFramework.Core.Abstractions.logging;

/// <summary>
///     定义日志记录接口，提供日志记录和级别检查功能
/// </summary>
public interface ILogger
{
    /// <summary>
    ///     根日志记录器的名称常量
    /// </summary>
    public const string RootLoggerName = "ROOT";

    /// <summary>
    ///     获取日志记录器的名称
    /// </summary>
    /// <returns>日志记录器的名称</returns>
    string Name();

    #region Level Enabled Check

    /// <summary>
    ///     检查是否启用了Trace级别日志
    /// </summary>
    /// <returns>如果启用了Trace级别日志则返回true，否则返回false</returns>
    bool IsTraceEnabled();

    /// <summary>
    ///     检查是否启用了Debug级别日志
    /// </summary>
    /// <returns>如果启用了Debug级别日志则返回true，否则返回false</returns>
    bool IsDebugEnabled();

    /// <summary>
    ///     检查是否启用了Info级别日志
    /// </summary>
    /// <returns>如果启用了Info级别日志则返回true，否则返回false</returns>
    bool IsInfoEnabled();

    /// <summary>
    ///     检查是否启用了Warn级别日志
    /// </summary>
    /// <returns>如果启用了Warn级别日志则返回true，否则返回false</returns>
    bool IsWarnEnabled();

    /// <summary>
    ///     检查是否启用了Error级别日志
    /// </summary>
    /// <returns>如果启用了Error级别日志则返回true，否则返回false</returns>
    bool IsErrorEnabled();

    /// <summary>
    ///     检查是否启用了Fatal级别日志
    /// </summary>
    /// <returns>如果启用了Fatal级别日志则返回true，否则返回false</returns>
    bool IsFatalEnabled();

    /// <summary>
    ///     检查指定的日志级别是否已启用
    /// </summary>
    /// <param name="level">要检查的日志级别</param>
    /// <returns>如果指定的日志级别已启用则返回true，否则返回false</returns>
    bool IsEnabledForLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => IsTraceEnabled(),
            LogLevel.Debug => IsDebugEnabled(),
            LogLevel.Info => IsInfoEnabled(),
            LogLevel.Warning => IsWarnEnabled(),
            LogLevel.Error => IsErrorEnabled(),
            LogLevel.Fatal => IsFatalEnabled(),
            _ => throw new ArgumentException($"Level [{level}] not recognized.", nameof(level)),
        };
    }

    #endregion

    #region Trace Logging Methods

    /// <summary>
    ///     记录 TRACE 级别的消息
    /// </summary>
    /// <param name="msg">要记录的消息字符串</param>
    void Trace(string msg);

    /// <summary>
    ///     根据指定格式和参数记录 TRACE 级别的消息
    ///     当日志记录器对 TRACE 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg">参数</param>
    void Trace(string format, object arg);

    /// <summary>
    ///     根据指定格式和参数记录 TRACE 级别的消息
    ///     当日志记录器对 TRACE 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg1">第一个参数</param>
    /// <param name="arg2">第二个参数</param>
    void Trace(string format, object arg1, object arg2);

    /// <summary>
    ///     根据指定格式和参数数组记录 TRACE 级别的消息
    ///     当日志记录器对 TRACE 级别禁用时，此方法可避免不必要的字符串连接
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arguments">参数数组</param>
    void Trace(string format, params object[] arguments);

    /// <summary>
    ///     使用伴随消息在 TRACE 级别记录异常
    /// </summary>
    /// <param name="msg">伴随异常的消息</param>
    /// <param name="t">要记录的异常</param>
    void Trace(string msg, Exception t);

    #endregion

    #region Debug Logging Methods

    /// <summary>
    ///     记录 DEBUG 级别的消息
    /// </summary>
    /// <param name="msg">要记录的消息字符串</param>
    void Debug(string msg);

    /// <summary>
    ///     根据指定格式和参数记录 DEBUG 级别的消息
    ///     当日志记录器对 DEBUG 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg">参数</param>
    void Debug(string format, object arg);

    /// <summary>
    ///     根据指定格式和参数记录 DEBUG 级别的消息
    ///     当日志记录器对 DEBUG 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg1">第一个参数</param>
    /// <param name="arg2">第二个参数</param>
    void Debug(string format, object arg1, object arg2);

    /// <summary>
    ///     根据指定格式和参数数组记录 DEBUG 级别的消息
    ///     当日志记录器对 DEBUG 级别禁用时，此方法可避免不必要的字符串连接
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arguments">参数数组</param>
    void Debug(string format, params object[] arguments);

    /// <summary>
    ///     使用伴随消息在 DEBUG 级别记录异常
    /// </summary>
    /// <param name="msg">伴随异常的消息</param>
    /// <param name="t">要记录的异常</param>
    void Debug(string msg, Exception t);

    #endregion

    #region Info Logging Methods

    /// <summary>
    ///     记录 INFO 级别的消息
    /// </summary>
    /// <param name="msg">要记录的消息字符串</param>
    void Info(string msg);

    /// <summary>
    ///     根据指定格式和参数记录 INFO 级别的消息
    ///     当日志记录器对 INFO 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg">参数</param>
    void Info(string format, object arg);

    /// <summary>
    ///     根据指定格式和参数记录 INFO 级别的消息
    ///     当日志记录器对 INFO 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg1">第一个参数</param>
    /// <param name="arg2">第二个参数</param>
    void Info(string format, object arg1, object arg2);

    /// <summary>
    ///     根据指定格式和参数数组记录 INFO 级别的消息
    ///     当日志记录器对 INFO 级别禁用时，此方法可避免不必要的字符串连接
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arguments">参数数组</param>
    void Info(string format, params object[] arguments);

    /// <summary>
    ///     使用伴随消息在 INFO 级别记录异常
    /// </summary>
    /// <param name="msg">伴随异常的消息</param>
    /// <param name="t">要记录的异常</param>
    void Info(string msg, Exception t);

    #endregion

    #region Warn Logging Methods

    /// <summary>
    ///     记录 WARN 级别的消息
    /// </summary>
    /// <param name="msg">要记录的消息字符串</param>
    void Warn(string msg);

    /// <summary>
    ///     根据指定格式和参数记录 WARN 级别的消息
    ///     当日志记录器对 WARN 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg">参数</param>
    void Warn(string format, object arg);

    /// <summary>
    ///     根据指定格式和参数记录 WARN 级别的消息
    ///     当日志记录器对 WARN 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg1">第一个参数</param>
    /// <param name="arg2">第二个参数</param>
    void Warn(string format, object arg1, object arg2);

    /// <summary>
    ///     根据指定格式和参数数组记录 WARN 级别的消息
    ///     当日志记录器对 WARN 级别禁用时，此方法可避免不必要的字符串连接
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arguments">参数数组</param>
    void Warn(string format, params object[] arguments);

    /// <summary>
    ///     使用伴随消息在 WARN 级别记录异常
    /// </summary>
    /// <param name="msg">伴随异常的消息</param>
    /// <param name="t">要记录的异常</param>
    void Warn(string msg, Exception t);

    #endregion

    #region Error Logging Methods

    /// <summary>
    ///     记录 ERROR 级别的消息
    /// </summary>
    /// <param name="msg">要记录的消息字符串</param>
    void Error(string msg);

    /// <summary>
    ///     根据指定格式和参数记录 ERROR 级别的消息
    ///     当日志记录器对 ERROR 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg">参数</param>
    void Error(string format, object arg);

    /// <summary>
    ///     根据指定格式和参数记录 ERROR 级别的消息
    ///     当日志记录器对 ERROR 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg1">第一个参数</param>
    /// <param name="arg2">第二个参数</param>
    void Error(string format, object arg1, object arg2);

    /// <summary>
    ///     根据指定格式和参数数组记录 ERROR 级别的消息
    ///     当日志记录器对 ERROR 级别禁用时，此方法可避免不必要的字符串连接
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arguments">参数数组</param>
    void Error(string format, params object[] arguments);

    /// <summary>
    ///     使用伴随消息在 ERROR 级别记录异常
    /// </summary>
    /// <param name="msg">伴随异常的消息</param>
    /// <param name="t">要记录的异常</param>
    void Error(string msg, Exception t);

    #endregion

    #region Fatal Logging Methods

    /// <summary>
    ///     记录 FATAL 级别的消息
    /// </summary>
    /// <param name="msg">要记录的消息字符串</param>
    void Fatal(string msg);

    /// <summary>
    ///     根据指定格式和参数记录 FATAL 级别的消息
    ///     当日志记录器对 FATAL 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg">参数</param>
    void Fatal(string format, object arg);

    /// <summary>
    ///     根据指定格式和参数记录 FATAL 级别的消息
    ///     当日志记录器对 FATAL 级别禁用时，此方法可避免不必要的对象创建
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arg1">第一个参数</param>
    /// <param name="arg2">第二个参数</param>
    void Fatal(string format, object arg1, object arg2);

    /// <summary>
    ///     根据指定格式和参数数组记录 FATAL 级别的消息
    ///     当日志记录器对 FATAL 级别禁用时，此方法可避免不必要的字符串连接
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="arguments">参数数组</param>
    void Fatal(string format, params object[] arguments);

    /// <summary>
    ///     使用伴随消息在 FATAL 级别记录异常
    /// </summary>
    /// <param name="msg">伴随异常的消息</param>
    /// <param name="t">要记录的异常</param>
    void Fatal(string msg, Exception t);

    #endregion
}