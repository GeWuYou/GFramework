// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;
using System.Runtime.CompilerServices;

namespace GFramework.Godot.Tests.Coroutine;

/// <summary>
///     验证 <see cref="Timing" /> 在纯托管测试宿主下仍保持与真实 Godot 生命周期一致的阶段语义。
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class TimingTests
{
    private Timing _timing = null!;

    /// <summary>
    ///     为每个测试准备独立的 Timing 宿主，避免静态实例槽位相互污染。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        // Timing 继承自 Godot.Node；在纯 dotnet test 宿主中直接运行原生构造函数会触发测试进程崩溃。
        // 这里仅为调度语义测试创建未初始化对象，再由 InitializeForTests 补齐纯托管字段与调度器状态。
        _timing = (Timing)RuntimeHelpers.GetUninitializedObject(typeof(Timing));
        _timing.InitializeForTests();
    }

    /// <summary>
    ///     清理测试宿主注册的调度器与实例槽位。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _timing.DisposeForTests();
    }

    /// <summary>
    ///     验证暂停场景时只会冻结普通 Process 协程，忽略暂停段仍会继续推进。
    /// </summary>
    [Test]
    public void AdvanceProcessFrameForTests_Should_Freeze_Process_Segment_But_Keep_IgnorePause_Segment_Running()
    {
        var executedSegments = new List<string>();
        var processHandle = _timing.RunCoroutineOnInstance(
            CompleteAfterOneFrame(() => executedSegments.Add("process")),
            Segment.Process);
        var ignorePauseHandle = _timing.RunCoroutineOnInstance(
            CompleteAfterOneFrame(() => executedSegments.Add("ignore-pause")),
            Segment.ProcessIgnorePause);

        _timing.AdvanceProcessFrameForTests(paused: true);

        Assert.Multiple(() =>
        {
            Assert.That(executedSegments, Is.EqualTo(new[] { "ignore-pause" }));
            Assert.That(_timing.ProcessCoroutines, Is.EqualTo(1));
            Assert.That(_timing.ProcessIgnorePauseCoroutines, Is.EqualTo(0));
            Assert.That(_timing.GetSchedulerForTests(Segment.Process).IsCoroutineAlive(processHandle), Is.True);
            Assert.That(
                _timing.GetSchedulerForTests(Segment.ProcessIgnorePause)
                    .TryGetCompletionStatus(ignorePauseHandle, out var status),
                Is.True);
            Assert.That(status, Is.EqualTo(CoroutineCompletionStatus.Completed));
        });
    }

    /// <summary>
    ///     验证 Physics 帧只会推进 Physics 段，不会提前消费普通 Process 段的等待。
    /// </summary>
    [Test]
    public void AdvancePhysicsFrameForTests_Should_Only_Advance_Physics_Segment()
    {
        var executedSegments = new List<string>();
        var processHandle = _timing.RunCoroutineOnInstance(
            CompleteAfterOneFrame(() => executedSegments.Add("process")),
            Segment.Process);
        var physicsHandle = _timing.RunCoroutineOnInstance(
            CompleteAfterOneFrame(() => executedSegments.Add("physics")),
            Segment.PhysicsProcess);

        _timing.AdvancePhysicsFrameForTests();

        Assert.Multiple(() =>
        {
            Assert.That(executedSegments, Is.EqualTo(new[] { "physics" }));
            Assert.That(_timing.GetSchedulerForTests(Segment.Process).IsCoroutineAlive(processHandle), Is.True);
            Assert.That(_timing.GetSchedulerForTests(Segment.PhysicsProcess).IsCoroutineAlive(physicsHandle), Is.False);
        });
    }

    /// <summary>
    ///     验证帧尾段会在 Process 段之后执行，保持与生产宿主 `_Process -> CallDeferred` 的顺序一致。
    /// </summary>
    [Test]
    public void AdvanceProcessFrameForTests_Should_Run_Deferred_Segment_After_Process_Segment()
    {
        var executionOrder = new List<string>();

        _timing.RunCoroutineOnInstance(
            CompleteAfterOneFrame(() => executionOrder.Add("process")),
            Segment.Process);
        _timing.RunCoroutineOnInstance(
            CompleteAfterOneFrame(() => executionOrder.Add("deferred")),
            Segment.DeferredProcess);

        _timing.AdvanceProcessFrameForTests(paused: false);

        Assert.That(executionOrder, Is.EqualTo(new[] { "process", "deferred" }));
    }

    /// <summary>
    ///     验证 <see cref="WaitForFixedUpdate" /> 只会在 Physics 段完成，避免阶段型等待被错误地提前消费。
    /// </summary>
    [Test]
    public void WaitForFixedUpdate_Should_Only_Complete_On_Physics_Segment()
    {
        var processCompletions = 0;
        var physicsCompletions = 0;

        var processHandle = _timing.RunCoroutineOnInstance(
            CompleteAfterInstruction(new WaitForFixedUpdate(), () => processCompletions++),
            Segment.Process);
        var physicsHandle = _timing.RunCoroutineOnInstance(
            CompleteAfterInstruction(new WaitForFixedUpdate(), () => physicsCompletions++),
            Segment.PhysicsProcess);

        _timing.AdvanceProcessFrameForTests(paused: false);
        _timing.AdvancePhysicsFrameForTests();

        Assert.Multiple(() =>
        {
            Assert.That(processCompletions, Is.EqualTo(0));
            Assert.That(physicsCompletions, Is.EqualTo(1));
            Assert.That(_timing.GetSchedulerForTests(Segment.Process).IsCoroutineAlive(processHandle), Is.True);
            Assert.That(_timing.GetSchedulerForTests(Segment.PhysicsProcess).IsCoroutineAlive(physicsHandle), Is.False);
        });
    }

    /// <summary>
    ///     验证 <see cref="WaitForEndOfFrame" /> 只会在 Deferred 段完成，避免提前穿透到普通 Process 段。
    /// </summary>
    [Test]
    public void WaitForEndOfFrame_Should_Only_Complete_On_Deferred_Segment()
    {
        var processCompletions = 0;
        var deferredCompletions = 0;

        var processHandle = _timing.RunCoroutineOnInstance(
            CompleteAfterInstruction(new WaitForEndOfFrame(), () => processCompletions++),
            Segment.Process);
        var deferredHandle = _timing.RunCoroutineOnInstance(
            CompleteAfterInstruction(new WaitForEndOfFrame(), () => deferredCompletions++),
            Segment.DeferredProcess);

        _timing.AdvanceProcessFrameForTests(paused: false);

        Assert.Multiple(() =>
        {
            Assert.That(processCompletions, Is.EqualTo(0));
            Assert.That(deferredCompletions, Is.EqualTo(1));
            Assert.That(_timing.GetSchedulerForTests(Segment.Process).IsCoroutineAlive(processHandle), Is.True);
            Assert.That(_timing.GetSchedulerForTests(Segment.DeferredProcess).IsCoroutineAlive(deferredHandle), Is.False);
        });
    }

    /// <summary>
    ///     构造一个在单帧等待后执行回调的测试协程。
    /// </summary>
    /// <param name="onCompleted">等待完成后执行的回调。</param>
    /// <returns>供 Timing 运行的协程枚举器。</returns>
    private static IEnumerator<IYieldInstruction> CompleteAfterOneFrame(Action onCompleted)
    {
        ArgumentNullException.ThrowIfNull(onCompleted);

        yield return new WaitOneFrame();
        onCompleted();
    }

    /// <summary>
    ///     构造一个在指定等待指令完成后执行回调的测试协程。
    /// </summary>
    /// <param name="instruction">要验证的等待指令。</param>
    /// <param name="onCompleted">等待完成后执行的回调。</param>
    /// <returns>供 Timing 运行的协程枚举器。</returns>
    private static IEnumerator<IYieldInstruction> CompleteAfterInstruction(
        IYieldInstruction instruction,
        Action onCompleted)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        ArgumentNullException.ThrowIfNull(onCompleted);

        yield return instruction;
        onCompleted();
    }
}
