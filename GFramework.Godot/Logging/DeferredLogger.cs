// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Defers resolving the real Godot logger until the first logging operation needs it.
/// </summary>
/// <remarks>
///     This wrapper allows static logger fields to be created before <see cref="GodotLog.Configure"/> or
///     <see cref="GodotLog.UseAsDefaultProvider"/> runs. The resolved inner logger is published with an atomic compare
///     exchange so concurrent first-use calls converge on one cached instance without relying on the non-atomic
///     null-coalescing assignment pattern.
/// </remarks>
/// <param name="category">The category passed to the provider when the real logger is first needed.</param>
/// <param name="providerAccessor">The accessor that returns the current provider at first use.</param>
internal sealed class DeferredLogger(string category, Func<ILoggerFactoryProvider> providerAccessor) : IStructuredLogger
{
    private ILogger? _inner;

    /// <summary>
    ///     Gets the resolved inner logger, creating and atomically publishing it on first use.
    /// </summary>
    /// <remarks>
    ///     The property is intentionally the single resolution gate so all delegated members share the same thread-safe
    ///     lazy initialization behavior.
    /// </remarks>
    private ILogger Inner
    {
        get
        {
            var current = Volatile.Read(ref _inner);
            if (current != null)
            {
                return current;
            }

            var createdLogger = ResolveLogger();

            // Multiple callers can resolve concurrently; only one publishes the cached reference.
            return Interlocked.CompareExchange(ref _inner, createdLogger, null) ?? createdLogger;
        }
    }

    /// <summary>
    ///     Gets the category name reported by the resolved logger.
    /// </summary>
    /// <returns>The logger category name.</returns>
    public string Name()
    {
        return Inner.Name();
    }

    /// <summary>
    ///     Returns whether trace messages are enabled by the current provider settings.
    /// </summary>
    /// <returns>true when trace messages should be emitted; otherwise false.</returns>
    public bool IsTraceEnabled()
    {
        return Inner.IsTraceEnabled();
    }

    /// <summary>
    ///     Returns whether debug messages are enabled by the current provider settings.
    /// </summary>
    /// <returns>true when debug messages should be emitted; otherwise false.</returns>
    public bool IsDebugEnabled()
    {
        return Inner.IsDebugEnabled();
    }

    /// <summary>
    ///     Returns whether informational messages are enabled by the current provider settings.
    /// </summary>
    /// <returns>true when informational messages should be emitted; otherwise false.</returns>
    public bool IsInfoEnabled()
    {
        return Inner.IsInfoEnabled();
    }

    /// <summary>
    ///     Returns whether warning messages are enabled by the current provider settings.
    /// </summary>
    /// <returns>true when warning messages should be emitted; otherwise false.</returns>
    public bool IsWarnEnabled()
    {
        return Inner.IsWarnEnabled();
    }

    /// <summary>
    ///     Returns whether error messages are enabled by the current provider settings.
    /// </summary>
    /// <returns>true when error messages should be emitted; otherwise false.</returns>
    public bool IsErrorEnabled()
    {
        return Inner.IsErrorEnabled();
    }

    /// <summary>
    ///     Returns whether fatal messages are enabled by the current provider settings.
    /// </summary>
    /// <returns>true when fatal messages should be emitted; otherwise false.</returns>
    public bool IsFatalEnabled()
    {
        return Inner.IsFatalEnabled();
    }

    /// <summary>
    ///     Returns whether the specified log level is enabled by the current provider settings.
    /// </summary>
    /// <param name="level">The level to check.</param>
    /// <returns>true when the level should be emitted; otherwise false.</returns>
    public bool IsEnabledForLevel(LogLevel level)
    {
        return Inner.IsEnabledForLevel(level);
    }

    /// <summary>
    ///     Writes a trace message through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    public void Trace(string msg)
    {
        Inner.Trace(msg);
    }

    /// <summary>
    ///     Writes a formatted trace message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg">The first format argument.</param>
    public void Trace(string format, object arg)
    {
        Inner.Trace(format, arg);
    }

    /// <summary>
    ///     Writes a formatted trace message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg1">The first format argument.</param>
    /// <param name="arg2">The second format argument.</param>
    public void Trace(string format, object arg1, object arg2)
    {
        Inner.Trace(format, arg1, arg2);
    }

    /// <summary>
    ///     Writes a formatted trace message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arguments">The format arguments.</param>
    public void Trace(string format, params object[] arguments)
    {
        Inner.Trace(format, arguments);
    }

    /// <summary>
    ///     Writes a trace message and exception through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    /// <param name="t">The exception to attach.</param>
    public void Trace(string msg, Exception t)
    {
        Inner.Trace(msg, t);
    }

    /// <summary>
    ///     Writes a debug message through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    public void Debug(string msg)
    {
        Inner.Debug(msg);
    }

    /// <summary>
    ///     Writes a formatted debug message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg">The first format argument.</param>
    public void Debug(string format, object arg)
    {
        Inner.Debug(format, arg);
    }

    /// <summary>
    ///     Writes a formatted debug message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg1">The first format argument.</param>
    /// <param name="arg2">The second format argument.</param>
    public void Debug(string format, object arg1, object arg2)
    {
        Inner.Debug(format, arg1, arg2);
    }

    /// <summary>
    ///     Writes a formatted debug message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arguments">The format arguments.</param>
    public void Debug(string format, params object[] arguments)
    {
        Inner.Debug(format, arguments);
    }

    /// <summary>
    ///     Writes a debug message and exception through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    /// <param name="t">The exception to attach.</param>
    public void Debug(string msg, Exception t)
    {
        Inner.Debug(msg, t);
    }

    /// <summary>
    ///     Writes an informational message through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    public void Info(string msg)
    {
        Inner.Info(msg);
    }

    /// <summary>
    ///     Writes a formatted informational message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg">The first format argument.</param>
    public void Info(string format, object arg)
    {
        Inner.Info(format, arg);
    }

    /// <summary>
    ///     Writes a formatted informational message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg1">The first format argument.</param>
    /// <param name="arg2">The second format argument.</param>
    public void Info(string format, object arg1, object arg2)
    {
        Inner.Info(format, arg1, arg2);
    }

    /// <summary>
    ///     Writes a formatted informational message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arguments">The format arguments.</param>
    public void Info(string format, params object[] arguments)
    {
        Inner.Info(format, arguments);
    }

    /// <summary>
    ///     Writes an informational message and exception through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    /// <param name="t">The exception to attach.</param>
    public void Info(string msg, Exception t)
    {
        Inner.Info(msg, t);
    }

    /// <summary>
    ///     Writes a warning message through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    public void Warn(string msg)
    {
        Inner.Warn(msg);
    }

    /// <summary>
    ///     Writes a formatted warning message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg">The first format argument.</param>
    public void Warn(string format, object arg)
    {
        Inner.Warn(format, arg);
    }

    /// <summary>
    ///     Writes a formatted warning message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg1">The first format argument.</param>
    /// <param name="arg2">The second format argument.</param>
    public void Warn(string format, object arg1, object arg2)
    {
        Inner.Warn(format, arg1, arg2);
    }

    /// <summary>
    ///     Writes a formatted warning message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arguments">The format arguments.</param>
    public void Warn(string format, params object[] arguments)
    {
        Inner.Warn(format, arguments);
    }

    /// <summary>
    ///     Writes a warning message and exception through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    /// <param name="t">The exception to attach.</param>
    public void Warn(string msg, Exception t)
    {
        Inner.Warn(msg, t);
    }

    /// <summary>
    ///     Writes an error message through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    public void Error(string msg)
    {
        Inner.Error(msg);
    }

    /// <summary>
    ///     Writes a formatted error message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg">The first format argument.</param>
    public void Error(string format, object arg)
    {
        Inner.Error(format, arg);
    }

    /// <summary>
    ///     Writes a formatted error message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg1">The first format argument.</param>
    /// <param name="arg2">The second format argument.</param>
    public void Error(string format, object arg1, object arg2)
    {
        Inner.Error(format, arg1, arg2);
    }

    /// <summary>
    ///     Writes a formatted error message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arguments">The format arguments.</param>
    public void Error(string format, params object[] arguments)
    {
        Inner.Error(format, arguments);
    }

    /// <summary>
    ///     Writes an error message and exception through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    /// <param name="t">The exception to attach.</param>
    public void Error(string msg, Exception t)
    {
        Inner.Error(msg, t);
    }

    /// <summary>
    ///     Writes a fatal message through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    public void Fatal(string msg)
    {
        Inner.Fatal(msg);
    }

    /// <summary>
    ///     Writes a formatted fatal message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg">The first format argument.</param>
    public void Fatal(string format, object arg)
    {
        Inner.Fatal(format, arg);
    }

    /// <summary>
    ///     Writes a formatted fatal message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg1">The first format argument.</param>
    /// <param name="arg2">The second format argument.</param>
    public void Fatal(string format, object arg1, object arg2)
    {
        Inner.Fatal(format, arg1, arg2);
    }

    /// <summary>
    ///     Writes a formatted fatal message through the resolved logger.
    /// </summary>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arguments">The format arguments.</param>
    public void Fatal(string format, params object[] arguments)
    {
        Inner.Fatal(format, arguments);
    }

    /// <summary>
    ///     Writes a fatal message and exception through the resolved logger.
    /// </summary>
    /// <param name="msg">The message to write.</param>
    /// <param name="t">The exception to attach.</param>
    public void Fatal(string msg, Exception t)
    {
        Inner.Fatal(msg, t);
    }

    /// <summary>
    ///     Writes a message at the specified level through the resolved logger.
    /// </summary>
    /// <param name="level">The level to write.</param>
    /// <param name="message">The message to write.</param>
    public void Log(LogLevel level, string message)
    {
        LogFallback(level, message, exception: null);
    }

    /// <summary>
    ///     Writes a formatted message at the specified level while preserving deferred formatting semantics.
    /// </summary>
    /// <param name="level">The level to write.</param>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg">The first format argument.</param>
    public void Log(LogLevel level, string format, object arg)
    {
        Inner.Log(level, format, arg);
    }

    /// <summary>
    ///     Writes a formatted message at the specified level while preserving deferred formatting semantics.
    /// </summary>
    /// <param name="level">The level to write.</param>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arg1">The first format argument.</param>
    /// <param name="arg2">The second format argument.</param>
    public void Log(LogLevel level, string format, object arg1, object arg2)
    {
        Inner.Log(level, format, arg1, arg2);
    }

    /// <summary>
    ///     Writes a formatted message at the specified level while preserving deferred formatting semantics.
    /// </summary>
    /// <param name="level">The level to write.</param>
    /// <param name="format">The format string interpreted by the resolved logger.</param>
    /// <param name="arguments">The format arguments.</param>
    public void Log(LogLevel level, string format, params object[] arguments)
    {
        Inner.Log(level, format, arguments);
    }

    /// <summary>
    ///     Writes a message and exception at the specified level through the resolved logger.
    /// </summary>
    /// <param name="level">The level to write.</param>
    /// <param name="message">The message to write.</param>
    /// <param name="exception">The exception to attach.</param>
    public void Log(LogLevel level, string message, Exception exception)
    {
        LogFallback(level, message, exception);
    }

    /// <summary>
    ///     Writes a structured message through the resolved logger when it supports structured properties.
    /// </summary>
    /// <param name="level">The level to write.</param>
    /// <param name="message">The message to write.</param>
    /// <param name="properties">The structured properties to attach.</param>
    public void Log(LogLevel level, string message, params (string Key, object? Value)[] properties)
    {
        if (Inner is IStructuredLogger structuredLogger)
        {
            structuredLogger.Log(level, message, properties);
            return;
        }

        LogFallback(level, message, exception: null, properties);
    }

    /// <summary>
    ///     Writes a structured message and exception through the resolved logger when it supports structured properties.
    /// </summary>
    /// <param name="level">The level to write.</param>
    /// <param name="message">The message to write.</param>
    /// <param name="exception">The optional exception to attach.</param>
    /// <param name="properties">The structured properties to attach.</param>
    public void Log(LogLevel level, string message, Exception? exception, params (string Key, object? Value)[] properties)
    {
        if (Inner is IStructuredLogger structuredLogger)
        {
            structuredLogger.Log(level, message, exception, properties);
            return;
        }

        LogFallback(level, message, exception, properties);
    }

    /// <summary>
    ///     Resolves the real logger from the current provider for the deferred category.
    /// </summary>
    /// <returns>The logger created by the current provider.</returns>
    private ILogger ResolveLogger()
    {
        return providerAccessor().CreateLogger(category);
    }

    /// <summary>
    ///     Routes a message through the non-structured logger surface when the resolved logger lacks structured support.
    /// </summary>
    /// <param name="level">The level to write.</param>
    /// <param name="message">The message to write.</param>
    /// <param name="exception">The optional exception to attach.</param>
    /// <param name="properties">The structured properties rendered into a suffix for fallback loggers.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="level"/> is not supported.</exception>
    private void LogFallback(
        LogLevel level,
        string message,
        Exception? exception,
        params (string Key, object? Value)[] properties)
    {
        var suffix = properties.Length == 0
            ? string.Empty
            : " | " + string.Join(", ", properties.Select(static property => $"{property.Key}={property.Value}"));
        var rendered = message + suffix;

        switch (level)
        {
            case LogLevel.Trace:
                WriteFallback(rendered, exception, Inner.Trace, Inner.Trace);
                break;
            case LogLevel.Debug:
                WriteFallback(rendered, exception, Inner.Debug, Inner.Debug);
                break;
            case LogLevel.Info:
                WriteFallback(rendered, exception, Inner.Info, Inner.Info);
                break;
            case LogLevel.Warning:
                WriteFallback(rendered, exception, Inner.Warn, Inner.Warn);
                break;
            case LogLevel.Error:
                WriteFallback(rendered, exception, Inner.Error, Inner.Error);
                break;
            case LogLevel.Fatal:
                WriteFallback(rendered, exception, Inner.Fatal, Inner.Fatal);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported log level.");
        }
    }

    /// <summary>
    ///     Routes a non-structured message through the fallback path.
    /// </summary>
    /// <param name="level">The level to write.</param>
    /// <param name="message">The message to write.</param>
    /// <param name="exception">The optional exception to attach.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="level"/> is not supported.</exception>
    private void LogFallback(LogLevel level, string message, Exception? exception)
    {
        LogFallback(level, message, exception, []);
    }

    /// <summary>
    ///     Chooses the message-only or exception-aware write delegate for fallback logging.
    /// </summary>
    /// <param name="message">The rendered fallback message.</param>
    /// <param name="exception">The optional exception to attach.</param>
    /// <param name="writeMessage">The delegate used when no exception is present.</param>
    /// <param name="writeException">The delegate used when an exception is present.</param>
    private static void WriteFallback(
        string message,
        Exception? exception,
        Action<string> writeMessage,
        Action<string, Exception> writeException)
    {
        if (exception == null)
        {
            writeMessage(message);
        }
        else
        {
            writeException(message, exception);
        }
    }
}
