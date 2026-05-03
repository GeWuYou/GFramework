// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging.Filters;

/// <summary>
///     按命名空间前缀过滤的过滤器
/// </summary>
public sealed class NamespaceFilter : ILogFilter
{
    private readonly string[] _allowedPrefixes;

    /// <summary>
    ///     创建命名空间过滤器
    /// </summary>
    /// <param name="allowedPrefixes">允许的命名空间前缀列表</param>
    public NamespaceFilter(params string[] allowedPrefixes)
    {
        if (allowedPrefixes == null || allowedPrefixes.Length == 0)
            throw new ArgumentException("At least one namespace prefix must be provided.", nameof(allowedPrefixes));

        _allowedPrefixes = allowedPrefixes;
    }

    /// <summary>
    ///     判断日志记录器名称是否匹配允许的命名空间前缀
    /// </summary>
    /// <param name="entry">日志条目</param>
    /// <returns>如果匹配任一前缀返回 true</returns>
    public bool ShouldLog(LogEntry entry)
    {
        return _allowedPrefixes.Any(prefix => entry.LoggerName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}