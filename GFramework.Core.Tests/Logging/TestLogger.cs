// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     表示供日志相关测试复用的标准捕获记录器，实现 Microsoft.Extensions.Logging 的 <see cref="ILogger" /> 契约。
/// </summary>
/// <remarks>
///     并发写入会通过内部锁串行化；<see cref="Logs" /> 每次返回快照，避免断言观察到正在被修改的可变集合。
/// </remarks>
public sealed class TestLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Action<LogEntry>? _onLogged;
    private readonly List<LogEntry> _logs = new();
    private readonly Lock _sync = new();

    /// <summary>
    ///     初始化 <see cref="TestLogger" /> 的新实例。
    /// </summary>
    /// <param name="categoryName">记录器类别名。</param>
    /// <param name="onLogged">每条条目捕获后的可选回调，供聚合容器使用。</param>
    public TestLogger(string categoryName = "TestLogger", Action<LogEntry>? onLogged = null)
    {
        _categoryName = categoryName;
        _onLogged = onLogged;
    }

    /// <summary>
    ///     获取按写入顺序保存的日志条目快照。
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
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <summary>
    ///     把一次标准日志调用转换为 <see cref="LogEntry" /> 并保存到内存。
    /// </summary>
    /// <param name="logLevel">日志级别。</param>
    /// <param name="eventId">事件标识。</param>
    /// <param name="state">调用方状态；标准结构化状态为键值对列表。</param>
    /// <param name="exception">相关异常；没有异常时为 <see langword="null" />。</param>
    /// <param name="formatter">把状态格式化为消息文本的委托。</param>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        if (!IsEnabled(logLevel))
            return;

        var entry = new LogEntry(
            DateTime.Now,
            logLevel,
            _categoryName,
            formatter(state, exception),
            exception,
            ExtractProperties(state));

        lock (_sync)
        {
            _logs.Add(entry);
        }

        _onLogged?.Invoke(entry);
    }

    /// <summary>识别标准结构化状态（键值对列表）并提取为属性字典；其余状态返回 null。</summary>
    private static IReadOnlyDictionary<string, object?>? ExtractProperties<TState>(TState state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> pairs)
            return null;
        var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var pair in pairs)
        {
            // 标准状态列表中的 OriginalFormat 是消息模板，不作为业务属性输出。
            if (string.Equals(pair.Key, "{OriginalFormat}", StringComparison.Ordinal))
                continue;
            dictionary[pair.Key] = pair.Value;
        }
        return dictionary;
    }
}
