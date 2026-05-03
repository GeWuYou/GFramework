// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Globalization;
using System.IO;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging;

/// <summary>
///     控制台日志记录器
/// </summary>
public sealed class ConsoleLogger(
    string? name = null,
    LogLevel minLevel = LogLevel.Info,
    TextWriter? writer = null,
    bool useColors = true) : AbstractLogger(name ?? RootLoggerName, minLevel)
{
    // 静态缓存日志级别字符串，避免重复格式化
    private static readonly string[] LevelStrings =
    [
        "TRACE  ",
        "DEBUG  ",
        "INFO   ",
        "WARNING",
        "ERROR  ",
        "FATAL  "
    ];

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
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var levelStr = LevelStrings[(int)level];
        var log = $"[{timestamp}] {levelStr} [{Name()}] {message}";

        // 添加异常信息到日志
        if (exception != null) log += global::System.Environment.NewLine + exception;

        if (_useColors)
            WriteColored(level, log);
        else
            _writer.WriteLine(log);
    }

    #region Internal Core

    /// <summary>
    ///     以指定颜色写入日志消息（使用 ANSI 转义码）
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    private void WriteColored(LogLevel level, string message)
    {
        var colorCode = GetAnsiColorCode(level);
        _writer.WriteLine($"\x1b[{colorCode}m{message}\x1b[0m");
    }

    /// <summary>
    ///     根据日志级别获取对应的 ANSI 颜色代码
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <returns>ANSI 颜色代码</returns>
    private static string GetAnsiColorCode(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "90", // 暗灰色
            LogLevel.Debug => "36", // 青色
            LogLevel.Info => "37", // 白色
            LogLevel.Warning => "33", // 黄色
            LogLevel.Error => "31", // 红色
            LogLevel.Fatal => "35", // 洋红色
            _ => "37"
        };
    }

    #endregion
}
