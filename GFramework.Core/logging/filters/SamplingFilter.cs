using System.Collections.Concurrent;
using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.logging.filters;

/// <summary>
///     日志采样过滤器，用于限制高频日志的输出
///     线程安全：所有方法都是线程安全的
/// </summary>
public sealed class SamplingFilter : ILogFilter
{
    private readonly int _sampleRate;
    private readonly ConcurrentDictionary<string, SamplingState> _samplingStates = new();
    private readonly TimeSpan _timeWindow;

    /// <summary>
    ///     创建日志采样过滤器
    /// </summary>
    /// <param name="sampleRate">采样率（每 N 条日志保留 1 条）</param>
    /// <param name="timeWindow">时间窗口（在此时间内应用采样）</param>
    public SamplingFilter(int sampleRate, TimeSpan timeWindow)
    {
        if (sampleRate <= 0)
            throw new ArgumentException("Sample rate must be greater than 0", nameof(sampleRate));

        if (timeWindow <= TimeSpan.Zero)
            throw new ArgumentException("Time window must be greater than zero", nameof(timeWindow));

        _sampleRate = sampleRate;
        _timeWindow = timeWindow;
    }

    /// <summary>
    ///     判断是否应该记录该日志条目
    /// </summary>
    public bool ShouldLog(LogEntry entry)
    {
        // 为每个日志记录器维护独立的采样状态
        var key = entry.LoggerName;
        var state = _samplingStates.GetOrAdd(key, _ => new SamplingState());

        return state.ShouldLog(_sampleRate, _timeWindow);
    }

    /// <summary>
    ///     采样状态
    /// </summary>
    private sealed class SamplingState
    {
        private readonly object _lock = new();
        private long _count;
        private DateTime _windowStart = DateTime.UtcNow;

        public bool ShouldLog(int sampleRate, TimeSpan timeWindow)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;

                // 检查是否需要重置时间窗口
                if (now - _windowStart >= timeWindow)
                {
                    _windowStart = now;
                    _count = 0;
                }

                _count++;

                // 每 N 条保留 1 条
                return _count % sampleRate == 1;
            }
        }
    }
}