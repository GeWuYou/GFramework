// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     协程分组管理测试
/// </summary>
public sealed class CoroutineGroupTests
{
    [Test]
    public void Run_WithGroup_ShouldAddToGroup()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield return new WaitOneFrame();
        }

        // Act
        scheduler.Run(TestCoroutine(), group: "TestGroup");
        scheduler.Run(TestCoroutine(), group: "TestGroup");
        scheduler.Run(TestCoroutine(), group: "OtherGroup");

        // Assert
        Assert.That(scheduler.GetGroupCount("TestGroup"), Is.EqualTo(2));
        Assert.That(scheduler.GetGroupCount("OtherGroup"), Is.EqualTo(1));
    }

    [Test]
    public void PauseGroup_ShouldPauseAllCoroutinesInGroup()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var executionCount = 0;

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            while (true)
            {
                executionCount++;
                yield return new WaitOneFrame();
            }
        }

        // Act
        scheduler.Run(TestCoroutine(), group: "TestGroup");
        scheduler.Run(TestCoroutine(), group: "TestGroup");

        scheduler.Update(); // 第一次更新，执行计数应为 4（每个协程执行2次）
        var pausedCount = scheduler.PauseGroup("TestGroup");
        scheduler.Update(); // 暂停后更新，执行计数不应增加

        // Assert
        Assert.That(pausedCount, Is.EqualTo(2));
        Assert.That(executionCount, Is.EqualTo(4)); // 第一次更新时每个协程执行了2次
    }

    [Test]
    public void ResumeGroup_ShouldResumeAllCoroutinesInGroup()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var executionCount = 0;

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            while (true)
            {
                executionCount++;
                yield return new WaitOneFrame();
            }
        }

        // Act
        scheduler.Run(TestCoroutine(), group: "TestGroup");
        scheduler.Run(TestCoroutine(), group: "TestGroup");

        scheduler.Update(); // 第一次更新
        scheduler.PauseGroup("TestGroup");
        scheduler.Update(); // 暂停期间
        var resumedCount = scheduler.ResumeGroup("TestGroup");
        scheduler.Update(); // 恢复后更新

        // Assert
        Assert.That(resumedCount, Is.EqualTo(2));
        Assert.That(executionCount, Is.EqualTo(6)); // 第一次 4，恢复后 2
    }

    [Test]
    public void KillGroup_ShouldKillAllCoroutinesInGroup()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield return new WaitOneFrame();
        }

        // Act
        scheduler.Run(TestCoroutine(), group: "TestGroup");
        scheduler.Run(TestCoroutine(), group: "TestGroup");
        scheduler.Run(TestCoroutine(), group: "OtherGroup");

        var initialCount = scheduler.ActiveCoroutineCount;
        var killedCount = scheduler.KillGroup("TestGroup");

        // Assert
        Assert.That(initialCount, Is.EqualTo(3));
        Assert.That(killedCount, Is.EqualTo(2));
        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(1));
        Assert.That(scheduler.GetGroupCount("TestGroup"), Is.EqualTo(0));
    }

    [Test]
    public void GetGroupCount_WithNonExistentGroup_ShouldReturnZero()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        // Act & Assert
        Assert.That(scheduler.GetGroupCount("NonExistent"), Is.EqualTo(0));
    }

    [Test]
    public void Complete_ShouldRemoveFromGroup()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        IEnumerator<IYieldInstruction> TestCoroutine()
        {
            yield return new WaitOneFrame(); // 等待一帧后完成
        }

        // Act
        scheduler.Run(TestCoroutine(), group: "TestGroup");
        scheduler.Run(TestCoroutine(), group: "TestGroup");

        Assert.That(scheduler.GetGroupCount("TestGroup"), Is.EqualTo(2));

        scheduler.Update(); // 协程完成

        // Assert
        Assert.That(scheduler.GetGroupCount("TestGroup"), Is.EqualTo(0));
    }

    [Test]
    public void PauseGroup_WithMixedGroups_ShouldOnlyAffectTargetGroup()
    {
        // Arrange
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var group1Count = 0;
        var group2Count = 0;

        IEnumerator<IYieldInstruction> Group1Coroutine()
        {
            while (true)
            {
                group1Count++;
                yield return new WaitOneFrame();
            }
        }

        IEnumerator<IYieldInstruction> Group2Coroutine()
        {
            while (true)
            {
                group2Count++;
                yield return new WaitOneFrame();
            }
        }

        // Act
        scheduler.Run(Group1Coroutine(), group: "Group1");
        scheduler.Run(Group2Coroutine(), group: "Group2");

        scheduler.Update(); // 第一次更新
        scheduler.PauseGroup("Group1");
        scheduler.Update(); // Group1 暂停，Group2 继续

        // Assert
        Assert.That(group1Count, Is.EqualTo(2)); // Group1 执行了2次（第一次更新）
        Assert.That(group2Count, Is.EqualTo(3)); // Group2 执行了3次（第一次更新2次，第二次更新1次）
    }
}