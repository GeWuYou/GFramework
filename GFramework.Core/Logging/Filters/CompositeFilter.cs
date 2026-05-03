// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging.Filters;

/// <summary>
///     组合多个过滤器的过滤器（AND 逻辑）
/// </summary>
public sealed class CompositeFilter : ILogFilter
{
    private readonly ILogFilter[] _filters;

    /// <summary>
    ///     创建组合过滤器
    /// </summary>
    /// <param name="filters">要组合的过滤器列表</param>
    public CompositeFilter(params ILogFilter[] filters)
    {
        if (filters == null || filters.Length == 0)
            throw new ArgumentException("At least one filter must be provided.", nameof(filters));

        _filters = filters;
    }

    /// <summary>
    ///     判断日志是否通过所有过滤器（AND 逻辑）
    /// </summary>
    /// <param name="entry">日志条目</param>
    /// <returns>如果所有过滤器都返回 true 则返回 true</returns>
    public bool ShouldLog(LogEntry entry)
    {
        return _filters.All(filter => filter.ShouldLog(entry));
    }
}