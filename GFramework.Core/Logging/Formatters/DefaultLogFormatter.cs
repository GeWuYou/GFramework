// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text;
using System.Globalization;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging.Formatters;

/// <summary>
///     默认日志格式化器，保持与现有格式兼容
/// </summary>
public sealed class DefaultLogFormatter : ILogFormatter
{
    private static readonly string[] LevelStrings =
    [
        "TRACE  ",
        "DEBUG  ",
        "INFO   ",
        "WARNING",
        "ERROR  ",
        "FATAL  "
    ];

    /// <summary>
    ///     将日志条目格式化为默认格式
    /// </summary>
    /// <param name="entry">日志条目</param>
    /// <returns>格式化后的日志字符串</returns>
    public string Format(LogEntry entry)
    {
        var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var levelStr = LevelStrings[(int)entry.Level];
        var sb = new StringBuilder();

        sb.Append('[').Append(timestamp).Append("] ")
            .Append(levelStr).Append(" [")
            .Append(entry.LoggerName).Append("] ")
            .Append(entry.Message);

        // 添加结构化属性
        var properties = entry.GetAllProperties();
        if (properties.Count > 0)
        {
            sb.Append(" |");
            foreach (var prop in properties)
            {
                sb.Append(' ').Append(prop.Key).Append('=').Append(prop.Value);
            }
        }

        // 添加异常信息
        if (entry.Exception != null)
        {
            sb.Append(global::System.Environment.NewLine).Append(entry.Exception);
        }

        return sb.ToString();
    }
}
