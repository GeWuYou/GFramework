// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>
///     事件统计信息实现类
///     线程安全：使用 Interlocked 操作确保计数器的原子性
/// </summary>
public sealed class EventStatistics : IEventStatistics
{
    private readonly Dictionary<string, int> _listenerCountByType = new(StringComparer.Ordinal);
#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _lock = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _lock = new();
#endif
    private readonly Dictionary<string, long> _publishCountByType = new(StringComparer.Ordinal);
    private long _totalFailed;
    private long _totalHandled;
    private long _totalPublished;

    /// <inheritdoc />
    public long TotalPublished => Interlocked.Read(ref _totalPublished);

    /// <inheritdoc />
    public long TotalHandled => Interlocked.Read(ref _totalHandled);

    /// <inheritdoc />
    public long TotalFailed => Interlocked.Read(ref _totalFailed);

    /// <inheritdoc />
    public int ActiveEventTypes
    {
        get
        {
            lock (_lock)
            {
                return _publishCountByType.Count;
            }
        }
    }

    /// <inheritdoc />
    public int ActiveListeners
    {
        get
        {
            lock (_lock)
            {
                return _listenerCountByType.Values.Sum();
            }
        }
    }

    /// <inheritdoc />
    public long GetPublishCount(string eventType)
    {
        lock (_lock)
        {
            return _publishCountByType.TryGetValue(eventType, out var count) ? count : 0;
        }
    }

    /// <inheritdoc />
    public int GetListenerCount(string eventType)
    {
        lock (_lock)
        {
            return _listenerCountByType.TryGetValue(eventType, out var count) ? count : 0;
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        Interlocked.Exchange(ref _totalPublished, 0);
        Interlocked.Exchange(ref _totalHandled, 0);
        Interlocked.Exchange(ref _totalFailed, 0);

        lock (_lock)
        {
            _publishCountByType.Clear();
            _listenerCountByType.Clear();
        }
    }

    /// <inheritdoc />
    public string GenerateReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine(FormattableString.Invariant($"=== 事件统计报告 ==="));
        sb.AppendLine(FormattableString.Invariant($"总发布数: {TotalPublished}"));
        sb.AppendLine(FormattableString.Invariant($"总处理数: {TotalHandled}"));
        sb.AppendLine(FormattableString.Invariant($"总失败数: {TotalFailed}"));
        sb.AppendLine(FormattableString.Invariant($"活跃事件类型: {ActiveEventTypes}"));
        sb.AppendLine(FormattableString.Invariant($"活跃监听器: {ActiveListeners}"));

        lock (_lock)
        {
            if (_publishCountByType.Count > 0)
            {
                sb.AppendLine(FormattableString.Invariant($"\n按事件类型统计（发布次数）:"));
                foreach (var kvp in _publishCountByType.OrderByDescending(x => x.Value))
                    sb.AppendLine(FormattableString.Invariant($"  {kvp.Key}: {kvp.Value}"));
            }

            if (_listenerCountByType.Count > 0)
            {
                sb.AppendLine(FormattableString.Invariant($"\n按事件类型统计（监听器数量）:"));
                foreach (var kvp in _listenerCountByType.OrderByDescending(x => x.Value))
                    sb.AppendLine(FormattableString.Invariant($"  {kvp.Key}: {kvp.Value}"));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     记录事件发布
    /// </summary>
    /// <param name="eventType">事件类型名称</param>
    public void RecordPublish(string eventType)
    {
        Interlocked.Increment(ref _totalPublished);

        lock (_lock)
        {
            _publishCountByType.TryGetValue(eventType, out var count);
            _publishCountByType[eventType] = count + 1;
        }
    }

    /// <summary>
    ///     记录事件处理
    /// </summary>
    public void RecordHandle()
    {
        Interlocked.Increment(ref _totalHandled);
    }

    /// <summary>
    ///     记录事件处理失败
    /// </summary>
    public void RecordFailure()
    {
        Interlocked.Increment(ref _totalFailed);
    }

    /// <summary>
    ///     更新事件类型的监听器数量
    /// </summary>
    /// <param name="eventType">事件类型名称</param>
    /// <param name="count">监听器数量</param>
    public void UpdateListenerCount(string eventType, int count)
    {
        lock (_lock)
        {
            if (count > 0)
            {
                _listenerCountByType[eventType] = count;
            }
            else
            {
                _listenerCountByType.Remove(eventType);
            }
        }
    }
}
