// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Logging;

/// <summary>
///     日志条目，包含完整的日志信息
/// </summary>
public sealed record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string LoggerName,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?>? Properties)
{
    /// <summary>
    ///     获取合并了上下文属性的所有属性
    /// </summary>
    /// <returns>包含日志属性和上下文属性的字典</returns>
    public IReadOnlyDictionary<string, object?> GetAllProperties()
    {
        var contextProps = LogContext.Current;

        if (Properties == null || Properties.Count == 0)
            return contextProps;

        if (contextProps.Count == 0)
            return Properties;

        // 合并属性，日志属性优先
        var merged = new Dictionary<string, object?>(contextProps, StringComparer.Ordinal);
        foreach (var prop in Properties)
        {
            merged[prop.Key] = prop.Value;
        }

        return merged;
    }
}