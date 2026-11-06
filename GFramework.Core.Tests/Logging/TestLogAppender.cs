// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     表示供日志输出管线测试复用的内存输出器。
/// </summary>
/// <remarks>
///     并发写入会通过内部锁串行化；<see cref="Entries" /> 每次返回快照，避免断言观察到正在被修改的可变集合。
/// </remarks>
public sealed class TestLogAppender : ILogAppender
{
    private readonly List<LogEntry> _entries = new();
    private readonly Lock _sync = new();

    /// <summary>
    ///     获取一个值，该值指示 <see cref="Flush" /> 是否已被调用。
    /// </summary>
    public bool FlushCalled { get; private set; }

    /// <summary>
    ///     获取一个值，该值指示 <see cref="Dispose" /> 是否已被调用。
    /// </summary>
    public bool DisposeCalled { get; private set; }

    /// <summary>
    ///     获取按写入顺序保存的日志条目快照。
    /// </summary>
    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_sync)
            {
                return _entries.ToArray();
            }
        }
    }

    /// <summary>
    ///     将日志条目追加到内存列表，供断言读取。
    /// </summary>
    /// <param name="entry">日志条目。</param>
    public void Append(LogEntry entry)
    {
        lock (_sync)
        {
            _entries.Add(entry);
        }
    }

    /// <summary>
    ///     标记刷新已发生。
    /// </summary>
    public void Flush()
    {
        FlushCalled = true;
    }

    /// <summary>
    ///     标记释放已发生。
    /// </summary>
    public void Dispose()
    {
        DisposeCalled = true;
    }
}
