// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging;

/// <summary>
///     带缓存的日志工厂包装器，避免重复创建相同名称的日志记录器实例
/// </summary>
public sealed class CachedLoggerFactory : ILoggerFactory
{
    private readonly ConcurrentDictionary<string, ILogger> _cache = new(StringComparer.Ordinal);
    private readonly ILoggerFactory _innerFactory;

    /// <summary>
    ///     创建缓存日志工厂实例
    /// </summary>
    /// <param name="innerFactory">内部日志工厂</param>
    public CachedLoggerFactory(ILoggerFactory innerFactory)
    {
        _innerFactory = innerFactory ?? throw new ArgumentNullException(nameof(innerFactory));
    }

    /// <summary>
    ///     获取或创建指定名称的日志记录器（带缓存）
    /// </summary>
    /// <param name="name">日志记录器名称</param>
    /// <param name="minLevel">最小日志级别</param>
    /// <returns>日志记录器实例</returns>
    public ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info)
    {
        var cacheKey = $"{name}:{minLevel}";
        return _cache.GetOrAdd(cacheKey, _ => _innerFactory.GetLogger(name, minLevel));
    }
}
