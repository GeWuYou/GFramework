// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     协程统计功能测试
/// </summary>
public sealed class CoroutineStatisticsTests
{
    [Test]
    public void Statistics_WhenDisabled_ShouldBeNull()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: false);

        // Act & Assert
        Assert.That(scheduler.Statistics, Is.Null);
    }

    [Test]
    public void Statistics_WhenEnabled_ShouldNotBeNull()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        // Act & Assert
        Assert.That(scheduler.Statistics, Is.Not.Null);
    }

    [Test]
    public void TotalStarted_ShouldTrackStartedCoroutines()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield return new WaitOneFrame();
        }

        // Act
        scheduler.Run(TestCoroutine());
        scheduler.Run(TestCoroutine());
        scheduler.Run(TestCoroutine());

        // Assert
        Assert.That(scheduler.Statistics!.TotalStarted, Is.EqualTo(3));
    }

    [Test]
    public void TotalCompleted_ShouldTrackCompletedCoroutines()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield break; // 立即完成
        }

        // Act
        scheduler.Run(TestCoroutine());
        scheduler.Run(TestCoroutine());
        scheduler.Update();

        // Assert
        Assert.That(scheduler.Statistics!.TotalCompleted, Is.EqualTo(2));
    }

    [Test]
    public void TotalFailed_ShouldTrackFailedCoroutines()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> FailingCoroutine()
        {
            throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // 检测到无法访问的代码
            yield break;
#pragma warning restore CS0162 // 检测到无法访问的代码
        }

        // Act
        scheduler.Run(FailingCoroutine());
        scheduler.Run(FailingCoroutine());
        scheduler.Update();

        // Assert
        Assert.That(scheduler.Statistics!.TotalFailed, Is.EqualTo(2));
    }

    [Test]
    public void ActiveCount_ShouldReflectCurrentActiveCoroutines()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> LongRunningCoroutine()
        {
            for (var i = 0; i < 10; i++)
                yield return new WaitOneFrame();
        }

        // Act
        scheduler.Run(LongRunningCoroutine());
        scheduler.Run(LongRunningCoroutine());
        scheduler.Update();

        // Assert
        Assert.That(scheduler.Statistics!.ActiveCount, Is.EqualTo(2));
    }

    [Test]
    public void PausedCount_ShouldReflectCurrentPausedCoroutines()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield return new WaitOneFrame();
        }

        // Act
        var handle1 = scheduler.Run(TestCoroutine());
        var handle2 = scheduler.Run(TestCoroutine());
        scheduler.Pause(handle1);
        scheduler.Pause(handle2);
        scheduler.Update();

        // Assert
        Assert.That(scheduler.Statistics!.PausedCount, Is.EqualTo(2));
    }

    [Test]
    public void AverageExecutionTimeMs_ShouldCalculateCorrectly()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield return new WaitOneFrame();
            yield return new WaitOneFrame();
        }

        // Act
        scheduler.Run(TestCoroutine());
        scheduler.Run(TestCoroutine());

        // 执行两帧，每帧 16ms
        timeSource.Advance(0.016);
        scheduler.Update();
        timeSource.Advance(0.016);
        scheduler.Update();

        // Assert
        var avgTime = scheduler.Statistics!.AverageExecutionTimeMs;
        Assert.That(avgTime > 0, Is.True);
        Assert.That(avgTime <= 32, Is.True); // 最多 32ms
    }

    [Test]
    public void MaxExecutionTimeMs_ShouldTrackLongestExecution()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> ShortCoroutine()
        {
            yield return new WaitOneFrame();
        }

        IEnumerator<IYieldInstruction> LongCoroutine()
        {
            yield return new WaitOneFrame();
            yield return new WaitOneFrame();
            yield return new WaitOneFrame();
        }

        // Act
        scheduler.Run(ShortCoroutine());
        scheduler.Run(LongCoroutine());

        // 执行足够的帧
        for (var i = 0; i < 4; i++)
        {
            timeSource.Advance(0.016);
            scheduler.Update();
        }

        // Assert
        var maxTime = scheduler.Statistics!.MaxExecutionTimeMs;
        Assert.That(maxTime > 0, Is.True);
    }

    [Test]
    public void GetCountByPriority_ShouldReturnCorrectCount()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield return new WaitOneFrame();
        }

        // Act
        scheduler.Run(TestCoroutine(), priority: CoroutinePriority.High);
        scheduler.Run(TestCoroutine(), priority: CoroutinePriority.High);
        scheduler.Run(TestCoroutine(), priority: CoroutinePriority.Low);

        // Assert
        Assert.That(scheduler.Statistics!.GetCountByPriority(CoroutinePriority.High), Is.EqualTo(2));
        Assert.That(scheduler.Statistics!.GetCountByPriority(CoroutinePriority.Low), Is.EqualTo(1));
        Assert.That(scheduler.Statistics!.GetCountByPriority(CoroutinePriority.Normal), Is.EqualTo(0));
    }

    [Test]
    public void GetCountByTag_ShouldReturnCorrectCount()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield return new WaitOneFrame();
        }

        // Act
        scheduler.Run(TestCoroutine(), tag: "AI");
        scheduler.Run(TestCoroutine(), tag: "AI");
        scheduler.Run(TestCoroutine(), tag: "Physics");

        // Assert
        Assert.That(scheduler.Statistics!.GetCountByTag("AI"), Is.EqualTo(2));
        Assert.That(scheduler.Statistics!.GetCountByTag("Physics"), Is.EqualTo(1));
        Assert.That(scheduler.Statistics!.GetCountByTag("Graphics"), Is.EqualTo(0));
    }

    [Test]
    public void Reset_ShouldClearAllStatistics()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield break;
        }

        // Act
        scheduler.Run(TestCoroutine());
        scheduler.Run(TestCoroutine());
        scheduler.Update();

        scheduler.Statistics!.Reset();

        // Assert
        Assert.That(scheduler.Statistics.TotalStarted, Is.EqualTo(0));
        Assert.That(scheduler.Statistics.TotalCompleted, Is.EqualTo(0));
        Assert.That(scheduler.Statistics.TotalFailed, Is.EqualTo(0));
        Assert.That(scheduler.Statistics.ActiveCount, Is.EqualTo(0));
        Assert.That(scheduler.Statistics.PausedCount, Is.EqualTo(0));
        Assert.That(scheduler.Statistics.AverageExecutionTimeMs, Is.EqualTo(0));
        Assert.That(scheduler.Statistics.MaxExecutionTimeMs, Is.EqualTo(0));
    }

    [Test]
    public void GenerateReport_ShouldReturnFormattedString()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield break;
        }

        // Act
        scheduler.Run(TestCoroutine(), tag: "Test", priority: CoroutinePriority.High);
        scheduler.Update();

        var report = scheduler.Statistics!.GenerateReport();

        // Assert
        Assert.That(report, Is.Not.Null);
        Assert.That(report, Does.Contain("协程统计报告"));
        Assert.That(report, Does.Contain("总启动数"));
        Assert.That(report, Does.Contain("总完成数"));
    }

    [Test]
    public void Statistics_ShouldBeThreadSafe()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield break;
        }

        // Act - 并发读取统计信息
        var tasks = new List<Task>();
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    _ = scheduler.Statistics!.TotalStarted;
                    _ = scheduler.Statistics.TotalCompleted;
                    _ = scheduler.Statistics.AverageExecutionTimeMs;
                    _ = scheduler.Statistics.GenerateReport();
                }
            }));
        }

        // 同时启动和完成协程
        for (var i = 0; i < 100; i++)
        {
            scheduler.Run(TestCoroutine());
        }

        scheduler.Update();

        Task.WaitAll(tasks.ToArray());

        // Assert - 不应该抛出异常
        Assert.That(scheduler.Statistics!.TotalStarted, Is.EqualTo(100));
        Assert.That(scheduler.Statistics.TotalCompleted, Is.EqualTo(100));
    }
}