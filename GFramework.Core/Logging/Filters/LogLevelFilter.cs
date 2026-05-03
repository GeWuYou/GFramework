// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging.Filters;

/// <summary>
///     按日志级别过滤的过滤器
/// </summary>
public sealed class LogLevelFilter : ILogFilter
{
    private readonly LogLevel _minLevel;

    /// <summary>
    ///     创建日志级别过滤器
    /// </summary>
    /// <param name="minLevel">最小日志级别</param>
    public LogLevelFilter(LogLevel minLevel)
    {
        _minLevel = minLevel;
    }

    /// <summary>
    ///     判断日志级别是否满足最小级别要求
    /// </summary>
    /// <param name="entry">日志条目</param>
    /// <returns>如果日志级别大于等于最小级别返回 true</returns>
    public bool ShouldLog(LogEntry entry)
    {
        return entry.Level >= _minLevel;
    }
}