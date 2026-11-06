// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     表示按类别创建 <see cref="TestLogger" /> 并聚合全部条目的测试 provider，实现标准 <see cref="ILoggerProvider" /> 契约。
/// </summary>
/// </summary>
public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, TestLogger> _loggers = new(StringComparer.Ordinal);
    private readonly List<LogEntry> _logs = new();
    private readonly Lock _sync = new();

    /// <summary>
    ///     创建或复用指定类别的捕获记录器。
    /// </summary>
    /// <param name="categoryName">记录器类别名。</param>
    /// <returns>绑定到该类别的 <see cref="TestLogger" />。</returns>
    public ILogger CreateLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrEmpty(categoryName);
        return _loggers.GetOrAdd(
            categoryName,
            static (name, provider) => new TestLogger(name, provider.OnLogged),
            this);
    }

    /// <summary>
    ///     获取全部类别按写入顺序保存的日志条目快照。
    /// </summary>
    public IReadOnlyList<LogEntry> Logs
    {
        get
        {
            lock (_sync)
            {
                return _logs.ToArray();
            }
        }
    }

    /// <summary>
    ///     清空已捕获的日志条目。
    /// </summary>
    public void Clear()
    {
        lock (_sync)
        {
            _logs.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _loggers.Clear();
    }

    private void OnLogged(LogEntry entry)
    {
        lock (_sync)
        {
            _logs.Add(entry);
        }
    }
}
