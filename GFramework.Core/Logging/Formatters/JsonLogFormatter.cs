// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Globalization;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging.Formatters;

/// <summary>
///     JSON 格式化器，将日志输出为 JSON 格式
/// </summary>
public sealed class JsonLogFormatter : ILogFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    ///     将日志条目格式化为 JSON 格式
    /// </summary>
    /// <param name="entry">日志条目</param>
    /// <returns>JSON 格式的日志字符串</returns>
    public string Format(LogEntry entry)
    {
        var logObject = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["timestamp"] = entry.Timestamp.ToString("O", CultureInfo.InvariantCulture), // ISO 8601 格式
            ["level"] = entry.Level.ToString().ToUpperInvariant(),
            ["logger"] = entry.LoggerName,
            ["message"] = entry.Message
        };

        // 添加结构化属性
        var properties = entry.GetAllProperties();
        if (properties.Count > 0)
        {
            logObject["properties"] = properties;
        }

        // 添加异常信息
        if (entry.Exception != null)
        {
            logObject["exception"] = new
            {
                type = entry.Exception.GetType().FullName,
                message = entry.Exception.Message,
                stackTrace = entry.Exception.StackTrace
            };
        }

        return JsonSerializer.Serialize(logObject, JsonOptions);
    }
}
