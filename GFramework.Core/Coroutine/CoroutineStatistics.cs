// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Globalization;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine;

/// <summary>
///     协程统计信息实现类
///     线程安全：使用 Interlocked 操作确保计数器的原子性
/// </summary>
internal sealed class CoroutineStatistics : ICoroutineStatistics
{
    private readonly Dictionary<CoroutinePriority, int> _countByPriority = new();
    private readonly Dictionary<string, int> _countByTag = new(StringComparer.Ordinal);
#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _lock = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _lock = new();
#endif
    private int _activeCount;
    private double _maxExecutionTimeMs;
    private int _pausedCount;
    private long _totalCompleted;
    private long _totalExecutionTimeMs;
    private long _totalFailed;
    private long _totalStarted;

    /// <inheritdoc />
    public long TotalStarted => Interlocked.Read(ref _totalStarted);

    /// <inheritdoc />
    public long TotalCompleted => Interlocked.Read(ref _totalCompleted);

    /// <inheritdoc />
    public long TotalFailed => Interlocked.Read(ref _totalFailed);

    /// <inheritdoc />
    public int ActiveCount
    {
        get => Interlocked.CompareExchange(ref _activeCount, 0, 0);
        set => Interlocked.Exchange(ref _activeCount, value);
    }

    /// <inheritdoc />
    public int PausedCount
    {
        get => Interlocked.CompareExchange(ref _pausedCount, 0, 0);
        set => Interlocked.Exchange(ref _pausedCount, value);
    }

    /// <inheritdoc />
    public double AverageExecutionTimeMs
    {
        get
        {
            var completed = Interlocked.Read(ref _totalCompleted);
            if (completed == 0)
                return 0;

            var totalTime = Interlocked.Read(ref _totalExecutionTimeMs);
            return (double)totalTime / completed;
        }
    }

    /// <inheritdoc />
    public double MaxExecutionTimeMs
    {
        get
        {
            lock (_lock)
            {
                return _maxExecutionTimeMs;
            }
        }
    }

    /// <inheritdoc />
    public int GetCountByPriority(CoroutinePriority priority)
    {
        lock (_lock)
        {
            return _countByPriority.TryGetValue(priority, out var count) ? count : 0;
        }
    }

    /// <inheritdoc />
    public int GetCountByTag(string tag)
    {
        lock (_lock)
        {
            return _countByTag.TryGetValue(tag, out var count) ? count : 0;
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        Interlocked.Exchange(ref _totalStarted, 0);
        Interlocked.Exchange(ref _totalCompleted, 0);
        Interlocked.Exchange(ref _totalFailed, 0);
        Interlocked.Exchange(ref _totalExecutionTimeMs, 0);
        Interlocked.Exchange(ref _activeCount, 0);
        Interlocked.Exchange(ref _pausedCount, 0);

        lock (_lock)
        {
            _maxExecutionTimeMs = 0;
            _countByPriority.Clear();
            _countByTag.Clear();
        }
    }

    /// <inheritdoc />
    public string GenerateReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine(FormattableString.Invariant($"=== 协程统计报告 ==="));
        sb.AppendLine(FormattableString.Invariant($"总启动数: {TotalStarted}"));
        sb.AppendLine(FormattableString.Invariant($"总完成数: {TotalCompleted}"));
        sb.AppendLine(FormattableString.Invariant($"总失败数: {TotalFailed}"));
        sb.AppendLine(FormattableString.Invariant($"当前活跃: {ActiveCount}"));
        sb.AppendLine(FormattableString.Invariant($"当前暂停: {PausedCount}"));
        sb.AppendLine(FormattableString.Invariant(
            $"平均执行时间: {AverageExecutionTimeMs.ToString("F2", CultureInfo.InvariantCulture)} ms"));
        sb.AppendLine(FormattableString.Invariant(
            $"最大执行时间: {MaxExecutionTimeMs.ToString("F2", CultureInfo.InvariantCulture)} ms"));

        lock (_lock)
        {
            if (_countByPriority.Count > 0)
            {
                sb.AppendLine(FormattableString.Invariant($"\n按优先级统计:"));
                foreach (var kvp in _countByPriority.OrderByDescending(x => x.Key))
                    sb.AppendLine(FormattableString.Invariant($"  {kvp.Key}: {kvp.Value}"));
            }

            if (_countByTag.Count > 0)
            {
                sb.AppendLine(FormattableString.Invariant($"\n按标签统计:"));
                foreach (var kvp in _countByTag.OrderByDescending(x => x.Value))
                    sb.AppendLine(FormattableString.Invariant($"  {kvp.Key}: {kvp.Value}"));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     记录协程启动
    /// </summary>
    /// <param name="priority">协程优先级</param>
    /// <param name="tag">协程标签</param>
    public void RecordStart(CoroutinePriority priority, string? tag)
    {
        Interlocked.Increment(ref _totalStarted);

        lock (_lock)
        {
            _countByPriority.TryGetValue(priority, out var count);
            _countByPriority[priority] = count + 1;

            if (!string.IsNullOrEmpty(tag))
            {
                _countByTag.TryGetValue(tag, out var tagCount);
                _countByTag[tag] = tagCount + 1;
            }
        }
    }

    /// <summary>
    ///     记录协程完成
    /// </summary>
    /// <param name="executionTimeMs">执行时间（毫秒）</param>
    /// <param name="priority">协程优先级</param>
    /// <param name="tag">协程标签</param>
    public void RecordComplete(double executionTimeMs, CoroutinePriority priority, string? tag)
    {
        Interlocked.Increment(ref _totalCompleted);
        Interlocked.Add(ref _totalExecutionTimeMs, (long)executionTimeMs);

        lock (_lock)
        {
            if (executionTimeMs > _maxExecutionTimeMs)
                _maxExecutionTimeMs = executionTimeMs;

            _countByPriority.TryGetValue(priority, out var count);
            _countByPriority[priority] = Math.Max(0, count - 1);

            if (!string.IsNullOrEmpty(tag))
            {
                _countByTag.TryGetValue(tag, out var tagCount);
                _countByTag[tag] = Math.Max(0, tagCount - 1);
                if (_countByTag[tag] == 0)
                    _countByTag.Remove(tag);
            }
        }
    }

    /// <summary>
    ///     记录协程失败
    /// </summary>
    /// <param name="priority">协程优先级</param>
    /// <param name="tag">协程标签</param>
    public void RecordFailure(CoroutinePriority priority, string? tag)
    {
        Interlocked.Increment(ref _totalFailed);

        lock (_lock)
        {
            _countByPriority.TryGetValue(priority, out var count);
            _countByPriority[priority] = Math.Max(0, count - 1);

            if (!string.IsNullOrEmpty(tag))
            {
                _countByTag.TryGetValue(tag, out var tagCount);
                _countByTag[tag] = Math.Max(0, tagCount - 1);
                if (_countByTag[tag] == 0)
                    _countByTag.Remove(tag);
            }
        }
    }
}
