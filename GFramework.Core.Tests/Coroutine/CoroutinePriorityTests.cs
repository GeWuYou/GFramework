// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     协程优先级测试
/// </summary>
public sealed class CoroutinePriorityTests
{
    [Test]
    public void Run_WithPriority_ShouldExecuteInPriorityOrder()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var executionOrder = new List<string>();

        IEnumerator<IYieldInstruction> CreateCoroutine(string name)
        {
            yield return new WaitOneFrame(); // 先等待一帧
            executionOrder.Add(name); // 在第一次 Update 时才记录
        }

        // Act - 以不同优先级启动协程
        scheduler.Run(CreateCoroutine("Low"), priority: CoroutinePriority.Low);
        scheduler.Run(CreateCoroutine("High"), priority: CoroutinePriority.High);
        scheduler.Run(CreateCoroutine("Normal"), priority: CoroutinePriority.Normal);
        scheduler.Run(CreateCoroutine("Highest"), priority: CoroutinePriority.Highest);
        scheduler.Run(CreateCoroutine("Lowest"), priority: CoroutinePriority.Lowest);

        scheduler.Update();

        // Assert - 高优先级应该先执行
        Assert.That(executionOrder[0], Is.EqualTo("Highest"));
        Assert.That(executionOrder[1], Is.EqualTo("High"));
        Assert.That(executionOrder[2], Is.EqualTo("Normal"));
        Assert.That(executionOrder[3], Is.EqualTo("Low"));
        Assert.That(executionOrder[4], Is.EqualTo("Lowest"));
    }

    [Test]
    public void Run_WithSamePriority_ShouldExecuteInStartOrder()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var executionOrder = new List<string>();

        IEnumerator<IYieldInstruction> CreateCoroutine(string name)
        {
            executionOrder.Add(name);
            yield return new WaitOneFrame();
        }

        // Act - 以相同优先级启动协程
        scheduler.Run(CreateCoroutine("First"), priority: CoroutinePriority.Normal);
        scheduler.Run(CreateCoroutine("Second"), priority: CoroutinePriority.Normal);
        scheduler.Run(CreateCoroutine("Third"), priority: CoroutinePriority.Normal);

        scheduler.Update();

        // Assert - 相同优先级按启动顺序执行
        Assert.That(executionOrder[0], Is.EqualTo("First"));
        Assert.That(executionOrder[1], Is.EqualTo("Second"));
        Assert.That(executionOrder[2], Is.EqualTo("Third"));
    }

    [Test]
    public void Run_WithDefaultPriority_ShouldUseNormal()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, enableStatistics: true);
        var executed = false;

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            executed = true;
            yield return new WaitOneFrame();
        }

        // Act
        scheduler.Run(TestCoroutine());

        // Assert - 在 Update 之前检查统计
        Assert.That(scheduler.Statistics!.GetCountByPriority(CoroutinePriority.Normal), Is.EqualTo(1));

        scheduler.Update();
        Assert.That(executed, Is.True);
    }

    [Test]
    public void Update_WithMixedPriorities_ShouldRespectPriorityDuringExecution()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var executionOrder = new List<string>();

        IEnumerator<IYieldInstruction> CreateMultiStepCoroutine(string name)
        {
            yield return new WaitOneFrame(); // 先等待，避免在 Prewarm 时执行
            for (var i = 0; i < 3; i++)
            {
                executionOrder.Add($"{name}-{i}");
                yield return new Delay(0.02); // 等待稍长的时间，确保不会在同一帧完成
            }
        }

        // Act
        scheduler.Run(CreateMultiStepCoroutine("Low"), priority: CoroutinePriority.Low);
        scheduler.Run(CreateMultiStepCoroutine("High"), priority: CoroutinePriority.High);

        // 执行多帧，每帧推进 0.016 秒
        // 第一帧：WaitOneFrame 完成
        // 之后每 2 帧（0.032秒）完成一次 Delay(0.02)
        for (var frame = 0; frame < 8; frame++)
        {
            timeSource.Advance(0.016);
            scheduler.Update();
        }

        // Assert - 每帧都应该先执行高优先级
        Assert.That(executionOrder[0], Is.EqualTo("High-0"));
        Assert.That(executionOrder[1], Is.EqualTo("Low-0"));
        Assert.That(executionOrder[2], Is.EqualTo("High-1"));
        Assert.That(executionOrder[3], Is.EqualTo("Low-1"));
        Assert.That(executionOrder[4], Is.EqualTo("High-2"));
        Assert.That(executionOrder[5], Is.EqualTo("Low-2"));
    }
}