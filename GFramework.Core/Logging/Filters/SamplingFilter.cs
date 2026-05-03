// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Time;
using GFramework.Core.Time;

namespace GFramework.Core.Logging.Filters;

/// <summary>
///     日志采样过滤器，用于限制高频日志的输出
///     线程安全：所有方法都是线程安全的
/// </summary>
public sealed class SamplingFilter : ILogFilter
{
    private const int DefaultMaxLoggers = 1000;
    private readonly int _maxLoggers;
    private readonly int _sampleRate;
    private readonly ConcurrentDictionary<string, SamplingState> _samplingStates = new(StringComparer.Ordinal);
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _timeWindow;

    /// <summary>
    ///     创建日志采样过滤器
    /// </summary>
    /// <param name="sampleRate">采样率（每 N 条日志保留 1 条）</param>
    /// <param name="timeWindow">时间窗口（在此时间内应用采样）</param>
    /// <param name="maxLoggers">最大日志记录器数量，超过后使用共享状态</param>
    /// <param name="timeProvider">时间提供者，默认使用系统时间</param>
    public SamplingFilter(
        int sampleRate,
        TimeSpan timeWindow,
        int maxLoggers = DefaultMaxLoggers,
        ITimeProvider? timeProvider = null)
    {
        if (sampleRate <= 0)
            throw new ArgumentException("Sample rate must be greater than 0", nameof(sampleRate));

        if (timeWindow <= TimeSpan.Zero)
            throw new ArgumentException("Time window must be greater than zero", nameof(timeWindow));

        if (maxLoggers <= 0)
            throw new ArgumentException("Max loggers must be greater than 0", nameof(maxLoggers));

        _sampleRate = sampleRate;
        _timeWindow = timeWindow;
        _maxLoggers = maxLoggers;
        _timeProvider = timeProvider ?? new SystemTimeProvider();
    }

    /// <summary>
    ///     判断是否应该记录该日志条目
    /// </summary>
    public bool ShouldLog(LogEntry entry)
    {
        // 如果超过最大日志记录器数量，使用共享状态
        var key = _samplingStates.Count >= _maxLoggers ? "*" : entry.LoggerName;

        var state = _samplingStates.GetOrAdd(key, _ => new SamplingState(_timeProvider));

        return state.ShouldLog(_sampleRate, _timeWindow);
    }

    /// <summary>
    ///     清理过期的采样状态
    /// </summary>
    public void CleanupStaleStates(TimeSpan staleThreshold)
    {
        var now = _timeProvider.UtcNow;
        var keysToRemove = new List<string>();

        foreach (var kvp in _samplingStates)
        {
            if (string.Equals(kvp.Key, "*", StringComparison.Ordinal))
            {
                continue; // 不清理共享状态
            }

            if (kvp.Value.IsStale(now, staleThreshold))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _samplingStates.TryRemove(key, out _);
        }
    }

    /// <summary>
    ///     采样状态
    /// </summary>
    private sealed class SamplingState
    {
#if NET9_0_OR_GREATER
        // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
        private readonly System.Threading.Lock _lock = new();
#else
        // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
        private readonly object _lock = new();
#endif
        private readonly ITimeProvider _timeProvider;
        private long _count;
        private long _lastAccessTicks;
        private long _windowStartTicks;

        public SamplingState(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            var now = timeProvider.UtcNow.Ticks;
            _windowStartTicks = now;
            _lastAccessTicks = now;
        }

        public bool ShouldLog(int sampleRate, TimeSpan timeWindow)
        {
            lock (_lock)
            {
                var now = _timeProvider.UtcNow;
                var nowTicks = now.Ticks;
                Interlocked.Exchange(ref _lastAccessTicks, nowTicks);

                var windowStart = new DateTime(Interlocked.Read(ref _windowStartTicks), DateTimeKind.Utc);

                // 检查是否需要重置时间窗口
                if (now - windowStart >= timeWindow)
                {
                    Interlocked.Exchange(ref _windowStartTicks, nowTicks);
                    _count = 0;
                }

                _count++;

                // 每 N 条保留 1 条
                return _count % sampleRate == 1;
            }
        }

        public bool IsStale(DateTime now, TimeSpan staleThreshold)
        {
            var lastAccess = new DateTime(Interlocked.Read(ref _lastAccessTicks), DateTimeKind.Utc);
            return now - lastAccess > staleThreshold;
        }
    }
}
