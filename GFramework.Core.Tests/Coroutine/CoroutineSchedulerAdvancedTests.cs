// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     协程调度器增强行为测试。
/// </summary>
[TestFixture]
public sealed class CoroutineSchedulerAdvancedTests
{
    /// <summary>
    ///     验证 WaitForSecondsRealtime 使用独立的真实时间源推进。
    /// </summary>
    [Test]
    public void WaitForSecondsRealtime_Should_Use_Realtime_TimeSource_When_Provided()
    {
        var scaledTime = new FakeTimeSource();
        var realtimeTime = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(
            scaledTime,
            realtimeTimeSource: realtimeTime);
        var completed = new global::System.Runtime.CompilerServices.StrongBox<bool>(value: false);

        IEnumerator<IYieldInstruction> Coroutine()
        {
            yield return new WaitForSecondsRealtime(1.0);
            completed.Value = true;
        }

        scheduler.Run(Coroutine());

        scaledTime.Advance(0.1);
        realtimeTime.Advance(0.4);
        scheduler.Update();

        Assert.That(completed.Value, Is.False);
        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(1));

        scaledTime.Advance(0.1);
        realtimeTime.Advance(0.6);
        scheduler.Update();

        Assert.That(completed.Value, Is.True);
        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    ///     验证固定更新等待指令仅在固定阶段调度器中推进。
    /// </summary>
    [Test]
    public void SwitchTo_Should_Fail_Explicitly_When_Host_Does_Not_Drive_Target_Phase()
    {
        var defaultTime = new FakeTimeSource();
        var fixedTime = new FakeTimeSource();
        var defaultScheduler = new CoroutineScheduler(defaultTime);
        var fixedScheduler = new CoroutineScheduler(
            fixedTime,
            executionStage: CoroutineExecutionStage.FixedUpdate);
        var defaultCompleted = new global::System.Runtime.CompilerServices.StrongBox<bool>(value: false);
        var fixedCompleted = new global::System.Runtime.CompilerServices.StrongBox<bool>(value: false);

        IEnumerator<IYieldInstruction> DefaultCoroutine()
        {
            yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
            defaultCompleted.Value = true;
        }

        IEnumerator<IYieldInstruction> FixedCoroutine()
        {
            yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
            fixedCompleted.Value = true;
        }

        var unsupportedHandle = defaultScheduler.Run(DefaultCoroutine());
        fixedScheduler.Run(FixedCoroutine());

        defaultTime.Advance(0.1);
        fixedTime.Advance(0.1);
        defaultScheduler.Update();
        fixedScheduler.Update();

        Assert.That(defaultCompleted.Value, Is.False);
        Assert.That(defaultScheduler.ActiveCoroutineCount, Is.EqualTo(0));
        Assert.That(defaultScheduler.TryGetCompletionStatus(unsupportedHandle, out var status), Is.True);
        Assert.That(status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(fixedCompleted.Value, Is.True);
        Assert.That(fixedScheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    ///     验证取消令牌会在下一次调度循环中终止协程并记录结果。
    /// </summary>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0042:Do not use blocking synchronous methods when an asynchronous method is available",
        Justification = "The scheduler must stay on its construction thread; synchronous token callbacks only enqueue cancellation.")]
    public async Task CancellationToken_Should_Cancel_Coroutine_On_Next_Update()
    {
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        using var cancellationTokenSource = new CancellationTokenSource();

        static IEnumerator<IYieldInstruction> Coroutine()
        {
            yield return new Delay(10);
        }

        var handle = scheduler.Run(Coroutine(), cancellationToken: cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        timeSource.Advance(0.1);
        scheduler.Update();

        var status = await scheduler.WaitForCompletionAsync(handle);

        Assert.That(scheduler.IsCoroutineAlive(handle), Is.False);
        Assert.That(status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
    }

    /// <summary>
    ///     验证调度器可以暴露活跃协程快照。
    /// </summary>
    [Test]
    public void TryGetSnapshot_Should_Return_Current_Waiting_Instruction_And_Stage()
    {
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(
            timeSource,
            executionStage: CoroutineExecutionStage.Deferred);

        static IEnumerator<IYieldInstruction> Coroutine()
        {
            yield return new SwitchTo(CoroutineExecutionStage.Deferred);
        }

        var handle = scheduler.Run(Coroutine(), tag: "ui", group: "frame-end");

        var found = scheduler.TryGetSnapshot(handle, out var snapshot);

        Assert.That(found, Is.True);
        Assert.That(snapshot.Handle, Is.EqualTo(handle));
        Assert.That(snapshot.Tag, Is.EqualTo("ui"));
        Assert.That(snapshot.Group, Is.EqualTo("frame-end"));
        Assert.That(snapshot.IsWaiting, Is.True);
        Assert.That(snapshot.WaitingInstructionType, Is.EqualTo(typeof(SwitchTo)));
        Assert.That(snapshot.ExecutionStage, Is.EqualTo(CoroutineExecutionStage.Deferred));
    }

    /// <summary>
    ///     验证异常结束的协程会记录为 Faulted。
    /// </summary>
    [Test]
    public async Task WaitForCompletionAsync_Should_Return_Faulted_For_Failing_Coroutine()
    {
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        static IEnumerator<IYieldInstruction> Coroutine()
        {
            yield return new WaitOneFrame();
            throw new InvalidOperationException("boom");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }

        var handle = scheduler.Run(Coroutine());
        timeSource.Advance(0.1);
        scheduler.Update();
        timeSource.Advance(0.1);
        scheduler.Update();

        var status = await scheduler.WaitForCompletionAsync(handle);

        Assert.That(status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
    }

    /// <summary>
    ///     验证完成状态缓存有固定上限，避免无限增长。
    /// </summary>
    [Test]
    public void CompletionStatusHistory_Should_Be_Bounded()
    {
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var handles = new List<CoroutineHandle>();

        static IEnumerator<IYieldInstruction> ImmediateCoroutine()
        {
            yield break;
        }

        for (var i = 0; i < 1100; i++)
        {
            handles.Add(scheduler.Run(ImmediateCoroutine()));
        }

        Assert.That(scheduler.TryGetCompletionStatus(handles[0], out _), Is.False);
        Assert.That(scheduler.TryGetCompletionStatus(handles[^1], out var latestStatus), Is.True);
        Assert.That(latestStatus, Is.EqualTo(CoroutineCompletionStatus.Completed));
    }

    /// <summary>
    ///     验证作为首个等待指令的 WaitForCoroutine 会立即启动子协程，并沿用父协程取消令牌。
    /// </summary>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0042:Do not use blocking synchronous methods when an asynchronous method is available",
        Justification = "The scheduler must stay on its construction thread; synchronous token callbacks only enqueue cancellation.")]
    public async Task WaitForCoroutine_Should_Start_Child_During_Prewarm_And_Propagate_Cancellation()
    {
        var timeSource = new FakeTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        using var cancellationTokenSource = new CancellationTokenSource();

        IEnumerator<IYieldInstruction> ChildCoroutine()
        {
            yield return new Delay(10);
        }

        IEnumerator<IYieldInstruction> ParentCoroutine()
        {
            yield return new WaitForCoroutine(ChildCoroutine());
        }

        var handle = scheduler.Run(ParentCoroutine(), cancellationToken: cancellationTokenSource.Token);

        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(2));
        cancellationTokenSource.Cancel();
        timeSource.Advance(0.1);
        scheduler.Update();

        var status = await scheduler.WaitForCompletionAsync(handle);

        Assert.That(status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }
}
