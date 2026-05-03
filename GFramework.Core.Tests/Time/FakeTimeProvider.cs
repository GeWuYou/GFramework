// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Time;

namespace GFramework.Core.Tests.Time;

/// <summary>
///     可控制的时间提供者，用于测试
/// </summary>
public sealed class FakeTimeProvider : ITimeProvider
{
    private DateTime _currentTime;

    /// <summary>
    ///     创建可控制的时间提供者
    /// </summary>
    /// <param name="initialTime">初始时间，默认为 2024-01-01 00:00:00 UTC</param>
    public FakeTimeProvider(DateTime? initialTime = null)
    {
        _currentTime = initialTime ?? new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    ///     获取当前时间
    /// </summary>
    public DateTime UtcNow => _currentTime;

    /// <summary>
    ///     前进指定的时间
    /// </summary>
    public void Advance(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }

    /// <summary>
    ///     设置当前时间
    /// </summary>
    public void SetTime(DateTime time)
    {
        _currentTime = time;
    }
}