// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events.Filters;

/// <summary>
///     采样事件过滤器
///     按照指定的采样率过滤事件，用于限制高频事件的处理
/// </summary>
/// <typeparam name="T">事件类型</typeparam>
public sealed class SamplingEventFilter<T> : IEventFilter<T>
{
    private readonly double _samplingRate;
    private long _counter;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="samplingRate">采样率，范围 0.0 到 1.0。例如 0.1 表示只允许 10% 的事件通过</param>
    public SamplingEventFilter(double samplingRate)
    {
        if (samplingRate < 0.0 || samplingRate > 1.0)
            throw new ArgumentOutOfRangeException(nameof(samplingRate), "采样率必须在 0.0 到 1.0 之间");

        _samplingRate = samplingRate;
    }

    /// <inheritdoc />
    public bool ShouldFilter(T eventData)
    {
        if (_samplingRate >= 1.0)
            return false; // 采样率 100%，不过滤

        if (_samplingRate <= 0.0)
            return true; // 采样率 0%，全部过滤

        var count = Interlocked.Increment(ref _counter);
        var threshold = (long)(1.0 / _samplingRate);
        return count % threshold != 0;
    }
}