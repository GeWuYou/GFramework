// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Filters;
using GFramework.Core.Tests.Time;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

[TestFixture]
public class SamplingFilterTests
{
    [Test]
    public void SamplingFilter_Should_Sample_Logs_By_Rate()
    {
        var timeProvider = new FakeTimeProvider();
        var filter = new SamplingFilter(sampleRate: 3, timeWindow: TimeSpan.FromMinutes(1), timeProvider: timeProvider);
        var entry = new LogEntry(
            timeProvider.UtcNow,
            LogLevel.Info,
            "TestLogger",
            "Test message",
            null,
            null
        );

        // 前 3 条：第 1 条通过，第 2、3 条被过滤
        Assert.That(filter.ShouldLog(entry), Is.True); // 1st
        Assert.That(filter.ShouldLog(entry), Is.False); // 2nd
        Assert.That(filter.ShouldLog(entry), Is.False); // 3rd

        // 第 4 条通过（新周期）
        Assert.That(filter.ShouldLog(entry), Is.True); // 4th
        Assert.That(filter.ShouldLog(entry), Is.False); // 5th
        Assert.That(filter.ShouldLog(entry), Is.False); // 6th
    }

    [Test]
    public void SamplingFilter_Should_Reset_After_Time_Window()
    {
        var timeProvider = new FakeTimeProvider();
        var filter = new SamplingFilter(sampleRate: 2, timeWindow: TimeSpan.FromMilliseconds(100),
            timeProvider: timeProvider);
        var entry = new LogEntry(
            timeProvider.UtcNow,
            LogLevel.Info,
            "TestLogger",
            "Test message",
            null,
            null
        );

        // 第一个窗口
        Assert.That(filter.ShouldLog(entry), Is.True); // 1st
        Assert.That(filter.ShouldLog(entry), Is.False); // 2nd

        // 前进时间超过窗口
        timeProvider.Advance(TimeSpan.FromMilliseconds(150));

        // 新窗口应该重置计数
        Assert.That(filter.ShouldLog(entry), Is.True); // 1st in new window
        Assert.That(filter.ShouldLog(entry), Is.False); // 2nd in new window
    }

    [Test]
    public void SamplingFilter_Should_Maintain_Separate_State_Per_Logger()
    {
        var timeProvider = new FakeTimeProvider();
        var filter = new SamplingFilter(sampleRate: 2, timeWindow: TimeSpan.FromMinutes(1), timeProvider: timeProvider);

        var entry1 = new LogEntry(timeProvider.UtcNow, LogLevel.Info, "Logger1", "Message", null, null);
        var entry2 = new LogEntry(timeProvider.UtcNow, LogLevel.Info, "Logger2", "Message", null, null);

        // Logger1 的第一条
        Assert.That(filter.ShouldLog(entry1), Is.True);

        // Logger2 的第一条（独立计数）
        Assert.That(filter.ShouldLog(entry2), Is.True);

        // Logger1 的第二条
        Assert.That(filter.ShouldLog(entry1), Is.False);

        // Logger2 的第二条
        Assert.That(filter.ShouldLog(entry2), Is.False);
    }

    [Test]
    public void SamplingFilter_Should_Use_Shared_State_When_Max_Loggers_Exceeded()
    {
        var timeProvider = new FakeTimeProvider();
        var filter = new SamplingFilter(sampleRate: 2, timeWindow: TimeSpan.FromMinutes(1), maxLoggers: 2,
            timeProvider: timeProvider);

        var entry1 = new LogEntry(timeProvider.UtcNow, LogLevel.Info, "Logger1", "Message", null, null);
        var entry2 = new LogEntry(timeProvider.UtcNow, LogLevel.Info, "Logger2", "Message", null, null);
        var entry3 = new LogEntry(timeProvider.UtcNow, LogLevel.Info, "Logger3", "Message", null, null);

        // Logger1 和 Logger2 各自独立
        Assert.That(filter.ShouldLog(entry1), Is.True); // Logger1: 1st
        Assert.That(filter.ShouldLog(entry2), Is.True); // Logger2: 1st

        // Logger3 超过限制，使用共享状态
        Assert.That(filter.ShouldLog(entry3), Is.True); // Shared: 1st
        Assert.That(filter.ShouldLog(entry3), Is.False); // Shared: 2nd
    }

    [Test]
    public void SamplingFilter_Should_Cleanup_Stale_States()
    {
        var timeProvider = new FakeTimeProvider();
        var filter = new SamplingFilter(sampleRate: 2, timeWindow: TimeSpan.FromMinutes(1), timeProvider: timeProvider);

        var entry = new LogEntry(timeProvider.UtcNow, LogLevel.Info, "TestLogger", "Message", null, null);

        // 使用过滤器
        filter.ShouldLog(entry);

        // 前进时间
        timeProvider.Advance(TimeSpan.FromHours(2));

        // 清理过期状态
        filter.CleanupStaleStates(TimeSpan.FromHours(1));

        // 验证状态已被清理（通过再次使用应该重新开始计数）
        Assert.That(filter.ShouldLog(entry), Is.True); // 应该是新的第一条
    }

    [Test]
    public void SamplingFilter_Should_Throw_When_SampleRate_Is_Invalid()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new SamplingFilter(sampleRate: 0, timeWindow: TimeSpan.FromMinutes(1));
        });

        Assert.Throws<ArgumentException>(() =>
        {
            new SamplingFilter(sampleRate: -1, timeWindow: TimeSpan.FromMinutes(1));
        });
    }

    [Test]
    public void SamplingFilter_Should_Throw_When_TimeWindow_Is_Invalid()
    {
        Assert.Throws<ArgumentException>(() => { new SamplingFilter(sampleRate: 2, timeWindow: TimeSpan.Zero); });

        Assert.Throws<ArgumentException>(() =>
        {
            new SamplingFilter(sampleRate: 2, timeWindow: TimeSpan.FromSeconds(-1));
        });
    }

    [Test]
    public void SamplingFilter_Should_Throw_When_MaxLoggers_Is_Invalid()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new SamplingFilter(sampleRate: 2, timeWindow: TimeSpan.FromMinutes(1), maxLoggers: 0);
        });

        Assert.Throws<ArgumentException>(() =>
        {
            new SamplingFilter(sampleRate: 2, timeWindow: TimeSpan.FromMinutes(1), maxLoggers: -1);
        });
    }

    [Test]
    public void SamplingFilter_Should_Be_Thread_Safe()
    {
        var timeProvider = new FakeTimeProvider();
        var filter = new SamplingFilter(sampleRate: 10, timeWindow: TimeSpan.FromMinutes(1),
            timeProvider: timeProvider);
        var entry = new LogEntry(timeProvider.UtcNow, LogLevel.Info, "TestLogger", "Message", null, null);

        var passedCount = 0;
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    if (filter.ShouldLog(entry))
                    {
                        Interlocked.Increment(ref passedCount);
                    }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // 1000 条日志，采样率 10，应该通过约 100 条
        Assert.That(passedCount, Is.InRange(90, 110));
    }
}