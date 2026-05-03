// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Logging;

/// <summary>
///     支持结构化日志的日志记录器接口
/// </summary>
public interface IStructuredLogger : ILogger
{
    /// <summary>
    ///     使用指定的日志级别记录消息和结构化属性
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="properties">结构化属性键值对</param>
    void Log(LogLevel level, string message, params (string Key, object? Value)[] properties);

    /// <summary>
    ///     使用指定的日志级别记录消息、异常和结构化属性
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象</param>
    /// <param name="properties">结构化属性键值对</param>
    void Log(LogLevel level, string message, Exception? exception, params (string Key, object? Value)[] properties);
}