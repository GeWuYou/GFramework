// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Godot.Coroutine;

/// <summary>
///     Godot 时间源实现，用于为协程调度器提供缩放时间或真实时间数据。
/// </summary>
/// <param name="timeProvider">
///     时间提供函数。
///     在默认模式下该函数返回“本帧增量”；在绝对时间模式下该函数返回“当前绝对时间（秒）”。
/// </param>
/// <param name="useAbsoluteTime">
///     是否把 <paramref name="timeProvider" /> 返回值解释为绝对时间。
///     启用后，<see cref="Update" /> 会通过相邻两次读数计算 <see cref="DeltaTime" />。
/// </param>
public sealed class GodotTimeSource(Func<double> timeProvider, bool useAbsoluteTime = false) : ITimeSource
{
    private readonly Func<double> _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private bool _initialized;
    private double _lastAbsoluteTime;

    /// <summary>
    ///     获取当前累计时间。
    /// </summary>
    public double CurrentTime { get; private set; }

    /// <summary>
    ///     获取上一帧的时间增量。
    /// </summary>
    public double DeltaTime { get; private set; }

    /// <summary>
    ///     更新时间源，计算新的时间增量与累计时间。
    /// </summary>
    public void Update()
    {
        var value = _timeProvider();
        if (useAbsoluteTime)
        {
            if (!_initialized)
            {
                _initialized = true;
                _lastAbsoluteTime = value;
                CurrentTime = value;
                DeltaTime = 0;
                return;
            }

            // 对绝对时间源做单调钳制，避免 provider 回拨后把 CurrentTime 也拉回去。
            var nextTime = Math.Max(value, _lastAbsoluteTime);
            DeltaTime = nextTime - _lastAbsoluteTime;
            _lastAbsoluteTime = nextTime;
            CurrentTime = nextTime;
            return;
        }

        DeltaTime = value;
        CurrentTime += DeltaTime;
    }

    /// <summary>
    ///     创建基于 Godot 单调时钟的真实时间源。
    /// </summary>
    /// <returns>返回一个不受场景暂停与时间缩放影响的时间源实例。</returns>
    public static GodotTimeSource CreateRealtime()
    {
        return new GodotTimeSource(
            () => Time.GetTicksUsec() / 1_000_000.0,
            useAbsoluteTime: true);
    }

    /// <summary>
    ///     重置时间源到初始状态。
    /// </summary>
    public void Reset()
    {
        CurrentTime = 0;
        DeltaTime = 0;
        _initialized = false;
        _lastAbsoluteTime = 0;
    }
}