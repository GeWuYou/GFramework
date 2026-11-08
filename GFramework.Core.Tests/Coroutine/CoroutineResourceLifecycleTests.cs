// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Extensions;
using GFramework.Core.Coroutine.Instructions;
using Moq;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     验证组合、外部等待与事件包装器在取消和故障时实际释放资源。
/// </summary>
[TestFixture]
public sealed class CoroutineResourceLifecycleTests
{
    /// <summary>
    ///     验证停止 Then 时内部 finally 执行一次且后续动作不执行。
    /// </summary>
    [Test]
    public void Killing_Then_Should_Dispose_Inner_Iterator_Without_Completion_Action()
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var disposed = 0;
        var completed = false;

        IEnumerator<IYieldInstruction> Routine()
        {
            try
            {
                yield return new Delay(100);
            }
            finally
            {
                disposed++;
            }
        }

        var handle = scheduler.Run(Routine().Then(() => completed = true));
        scheduler.Kill(handle);
        scheduler.Kill(handle);

        Assert.That(disposed, Is.EqualTo(1));
        Assert.That(completed, Is.False);
    }

    /// <summary>
    ///     验证组合结果开始前释放，也会释放全部已接收的枚举器。
    /// </summary>
    [Test]
    public void Disposing_Unstarted_Sequence_Should_Dispose_Every_Transferred_Iterator()
    {
        var first = new ProbeEnumerator();
        var second = new ProbeEnumerator();
        var combined = CoroutineExtensions.Sequence(first, second);

        combined.Dispose();
        combined.Dispose();

        Assert.That(first.DisposeCount, Is.EqualTo(1));
        Assert.That(second.DisposeCount, Is.EqualTo(1));
        Assert.That(first.MoveNextCount, Is.Zero);
        Assert.That(second.MoveNextCount, Is.Zero);
    }

    /// <summary>
    ///     验证一个释放异常不会阻止其他枚举器释放。
    /// </summary>
    [Test]
    public void Sequence_Should_Attempt_All_Cleanup_And_Preserve_Execution_Error()
    {
        var failure = new InvalidOperationException("move failed");
        var cleanupFailure = new InvalidOperationException("cleanup failed");
        var first = new ProbeEnumerator(failure, cleanupFailure);
        var second = new ProbeEnumerator();
        var combined = CoroutineExtensions.Sequence(first, second);

        var aggregate = Assert.Throws<AggregateException>(() => combined.MoveNext());

        Assert.That(aggregate!.InnerExceptions, Does.Contain(failure));
        Assert.That(aggregate.InnerExceptions, Does.Contain(cleanupFailure));
        Assert.That(first.DisposeCount, Is.EqualTo(1));
        Assert.That(second.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证提前停止并行包装器时，已经启动的所有分支一起释放。
    /// </summary>
    [Test]
    public void Killing_Parallel_Composition_Should_Cancel_Every_Started_Branch()
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var first = new ProbeEnumerator();
        var second = new ProbeEnumerator();
        var handle = scheduler.Run(context => context.ParallelCoroutines(first, second));
        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(3));

        scheduler.Kill(handle);

        Assert.That(first.DisposeCount, Is.EqualTo(1));
        Assert.That(second.DisposeCount, Is.EqualTo(1));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>
    ///     验证同步失败阻止后续分支启动，并释放未启动的输入。
    /// </summary>
    [Test]
    public void Synchronous_Parallel_Failure_Should_Preserve_Failure_And_Dispose_Pending_Input()
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var failure = new InvalidOperationException("first branch failed");
        var first = new ProbeEnumerator(moveFailure: failure);
        var pending = new ProbeEnumerator();
        var handle = scheduler.Run(context => context.ParallelCoroutines(first, pending));
        scheduler.Update();
        scheduler.Update();

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.SameAs(failure));
        Assert.That(pending.MoveNextCount, Is.Zero);
        Assert.That(pending.DisposeCount, Is.EqualTo(1));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>
    ///     验证并行分支执行失败与兄弟分支清理失败都保留在父流程结果中。
    /// </summary>
    [Test]
    public void Parallel_Failure_Should_Preserve_Original_And_Sibling_Cleanup_Error()
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var failure = new InvalidOperationException("branch execution failed");
        var cleanupFailure = new InvalidOperationException("pending branch cleanup failed");
        var first = new ProbeEnumerator(moveFailure: failure);
        var pending = new ProbeEnumerator(disposeFailure: cleanupFailure);
        var handle = scheduler.Run(context => context.ParallelCoroutines(first, pending));
        for (var frame = 0; frame < 8 && scheduler.IsCoroutineAlive(handle); frame++)
        {
            scheduler.Update();
        }

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.TypeOf<AggregateException>());
        var errors = ((AggregateException)result.Exception!).Flatten().InnerExceptions;
        Assert.That(errors, Does.Contain(failure));
        Assert.That(errors, Does.Contain(cleanupFailure));
        Assert.That(first.DisposeCount, Is.EqualTo(1));
        Assert.That(pending.DisposeCount, Is.EqualTo(1));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>
    ///     验证 Task 失败不能执行协程的成功路径。
    /// </summary>
    [Test]
    public void Faulted_Task_Should_Fault_Coroutine_And_Skip_Continuation()
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var failure = new InvalidOperationException("task failed");
        var continued = false;

        IEnumerator<IYieldInstruction> Routine()
        {
            yield return new WaitForTask(Task.FromException(failure));
            continued = true;
        }

        var handle = scheduler.Run(Routine());
        scheduler.Update();

        Assert.That(continued, Is.False);
        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.SameAs(failure));
    }

    /// <summary>
    ///     验证显式 Faulted 的异步操作即使携带取消异常，也不会被误报为正常取消。
    /// </summary>
    [TestCase("task")]
    [TestCase("generic-task")]
    [TestCase("operation")]
    public void Faulted_Async_Operation_With_Cancellation_Exception_Should_Remain_Faulted(string kind)
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var failure = new OperationCanceledException("an explicit fault, not cancellation");
        IYieldInstruction instruction;
        if (string.Equals(kind, "operation", StringComparison.Ordinal))
        {
            var operation = new AsyncOperation();
            operation.SetException(failure);
            instruction = operation;
        }
        else if (string.Equals(kind, "generic-task", StringComparison.Ordinal))
        {
            instruction = new WaitForTask<int>(Task.FromException<int>(failure));
        }
        else
        {
            instruction = new WaitForTask(Task.FromException(failure));
        }

        IEnumerator<IYieldInstruction> Routine()
        {
            yield return instruction;
        }

        var handle = scheduler.Run(Routine());
        scheduler.Update();

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.SameAs(failure));
    }

    /// <summary>
    ///     验证显式等待目标的清理故障时，保留 Faulted 分类和原始取消异常。
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void Awaiting_Faulted_Cleanup_With_Cancellation_Exception_Should_Remain_Faulted(bool owned)
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var failure = new OperationCanceledException("cleanup failed");
        var child = new ProbeEnumerator(disposeFailure: failure);
        CoroutineHandle target = default;

        IEnumerator<IYieldInstruction> Parent()
        {
            if (owned)
            {
                yield return new WaitForCoroutine(child);
            }
            else
            {
                yield return scheduler.Await(target);
            }
        }

        if (!owned)
        {
            target = scheduler.Run(child);
        }

        var parent = scheduler.Run(Parent(), childFailurePolicy: CoroutineFailurePolicy.Supervisor);
        if (owned)
        {
            target = scheduler.GetActiveSnapshots().Single(snapshot => snapshot.Parent == parent).Handle;
        }

        scheduler.Kill(target);
        for (var frame = 0; frame < 4 && scheduler.IsCoroutineAlive(parent); frame++)
        {
            scheduler.Update();
        }

        Assert.That(scheduler.TryGetResult(parent, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.SameAs(failure));
        Assert.That(child.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证 Supervisor 下 WhenAll 的单个故障仍按照子任务结果分类传播。
    /// </summary>
    [Test]
    public void Supervisor_Parallel_Fault_With_Cancellation_Exception_Should_Remain_Faulted()
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var failure = new OperationCanceledException("faulted task inside supervised operation");

        IEnumerator<IYieldInstruction> Child()
        {
            yield return new WaitForTask(Task.FromException(failure));
        }

        var handle = scheduler.Run(context => context.ParallelCoroutines(Child()),
            childFailurePolicy: CoroutineFailurePolicy.Supervisor);
        for (var frame = 0; frame < 4 && scheduler.IsCoroutineAlive(handle); frame++)
        {
            scheduler.Update();
        }

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.SameAs(failure));
    }

    /// <summary>
    ///     验证 Then 和 Sequence 不会丢失内部监督流程已经确认的故障分类。
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void Composed_Supervisor_Parallel_Fault_Should_Preserve_Explicit_Status(bool sequence)
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var failure = new OperationCanceledException("faulted task inside composed supervision");
        var continued = false;

        IEnumerator<IYieldInstruction> Child()
        {
            yield return new WaitForTask(Task.FromException(failure));
        }

        var handle = scheduler.Run(context =>
        {
            var parallel = context.ParallelCoroutines(Child());
            return sequence
                ? CoroutineExtensions.Sequence(parallel)
                : parallel.Then(() => continued = true);
        }, childFailurePolicy: CoroutineFailurePolicy.Supervisor);
        for (var frame = 0; frame < 8 && scheduler.IsCoroutineAlive(handle); frame++)
        {
            scheduler.Update();
        }

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.SameAs(failure));
        Assert.That(continued, Is.False);
    }

    /// <summary>
    ///     验证普通顺序切换阶段的 Dispose 取消异常也保留为清理失败。
    /// </summary>
    [Test]
    public void Sequence_Transition_Cleanup_Cancellation_Exception_Should_Be_Faulted()
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var failure = new OperationCanceledException("sequence transition cleanup failed");
        var first = new ProbeEnumerator(disposeFailure: failure, completesImmediately: true);
        var second = new ProbeEnumerator();
        var handle = scheduler.Run(CoroutineExtensions.Sequence(first, second));

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.SameAs(failure));
        Assert.That(first.DisposeCount, Is.EqualTo(1));
        Assert.That(second.DisposeCount, Is.EqualTo(1));
        Assert.That(second.MoveNextCount, Is.Zero);
    }

    /// <summary>
    ///     验证取消 Task 被记录为取消，避免误报成功或普通失败。
    /// </summary>
    [Test]
    public void Cancelled_Task_Should_Cancel_Coroutine()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var handle = scheduler.StartTaskAsCoroutine(Task.FromCanceled(source.Token));
        scheduler.Update();

        Assert.That(scheduler.TryGetCompletionStatus(handle, out var status), Is.True);
        Assert.That(status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
    }

    /// <summary>
    ///     验证 Supervisor 隔离独立子任务故障，但显式嵌套等待仍传播失败或取消。
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void Explicit_Owned_Await_Should_Propagate_Result_Inside_Supervisor_Flow(bool cancel)
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var failure = new InvalidOperationException("awaited child failed");
        var continued = false;
        WaitForCoroutine? ownedAwait = null;

        IEnumerator<IYieldInstruction> Child()
        {
            yield return new WaitOneFrame();
            if (cancel)
            {
                throw new OperationCanceledException();
            }

            throw failure;
        }

        IEnumerator<IYieldInstruction> Parent()
        {
            ownedAwait = new WaitForCoroutine(Child());
            yield return ownedAwait;
            continued = true;
        }

        var handle = scheduler.Run(Parent(), childFailurePolicy: CoroutineFailurePolicy.Supervisor);
        for (var frame = 0; frame < 4 && scheduler.IsCoroutineAlive(handle); frame++)
        {
            scheduler.Update();
        }

        Assert.That(continued, Is.False);
        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(cancel
            ? CoroutineCompletionStatus.Cancelled
            : CoroutineCompletionStatus.Faulted));
        Assert.That(ownedAwait!.Result.Status, Is.EqualTo(result.Status));
        if (!cancel)
        {
            Assert.That(result.Exception, Is.SameAs(failure));
        }
    }

    /// <summary>
    ///     验证未完成结果访问快速失败，不阻塞驱动线程。
    /// </summary>
    [Test]
    public void Pending_Task_Result_Should_Not_Block()
    {
        var source = new TaskCompletionSource<int>();
        var instruction = new WaitForTask<int>(source.Task);

        Assert.Throws<InvalidOperationException>(() => _ = instruction.Result);
        Assert.That(instruction.IsDone, Is.False);
        source.SetResult(42);
        Assert.That(instruction.Result, Is.EqualTo(42));
    }

    /// <summary>
    ///     验证任务工厂收到的令牌会在流程停止时取消。
    /// </summary>
    [Test]
    public void Owned_Task_Factory_Should_Receive_Lifetime_Cancellation()
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        CancellationToken received = default;
        var source = new TaskCompletionSource<object?>();
        var handle = scheduler.StartTaskAsCoroutine(token =>
        {
            received = token;
            return source.Task;
        });

        scheduler.Kill(handle);

        Assert.That(received.CanBeCanceled, Is.True);
        Assert.That(received.IsCancellationRequested, Is.True);
        Assert.That(source.Task.IsCompleted, Is.False,
            "The scheduler requests cooperative cancellation; it cannot force the external Task to stop.");
        source.SetResult(null);
    }

    /// <summary>
    ///     验证停止只观察外部 Task 的协程，不会谎报底层任务已结束。
    /// </summary>
    [Test]
    public void Killing_External_Task_Wait_Should_Only_Detach_Observation()
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var source = new TaskCompletionSource<object?>();
        var handle = scheduler.StartTaskAsCoroutine(source.Task);

        scheduler.Kill(handle);

        Assert.That(source.Task.IsCompleted, Is.False);
        source.SetResult(null);
        scheduler.Update();
        Assert.That(scheduler.TryGetCompletionStatus(handle, out var status), Is.True);
        Assert.That(status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
    }

    /// <summary>
    ///     验证事件等待在超时和协程取消两条路径中都注销一次。
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void Event_Timeout_Wrapper_Should_Release_Subscription_On_Each_Terminal_Path(bool cancel)
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var bus = new Mock<IEventBus>();
        var registration = new Mock<IUnRegister>();
        bus.Setup(value => value.Register<int>(It.IsAny<Action<int>>())).Returns(registration.Object);
        var wait = new WaitForEventWithTimeout<int>(new WaitForEvent<int>(bus.Object), 1);

        IEnumerator<IYieldInstruction> Routine()
        {
            yield return wait;
        }

        var handle = scheduler.Run(Routine());
        if (cancel)
        {
            scheduler.Kill(handle);
        }
        else
        {
            scheduler.Update(CoroutineExecutionStage.Update, 2, 2);
            Assert.That(wait.IsTimeout, Is.True);
        }

        wait.Dispose();
        registration.Verify(value => value.UnRegister(), Times.Once);
    }

    /// <summary>
    ///     验证跨线程事件竞争只发布一个完整结果，且注销留给驱动线程。
    /// </summary>
    [Test]
    public async Task Event_Callback_Should_Publish_First_Result_Without_Unregistering_On_Worker()
    {
        var bus = new Mock<IEventBus>();
        var registration = new Mock<IUnRegister>();
        Action<int>? callback = null;
        bus.Setup(value => value.Register<int>(It.IsAny<Action<int>>()))
            .Callback<Action<int>>(value => callback = value)
            .Returns(registration.Object);
        using var wait = new WaitForEvent<int>(bus.Object);

        await Task.WhenAll(Task.Run(() => callback!(1)), Task.Run(() => callback!(2))).ConfigureAwait(false);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.EventData, Is.AnyOf(1, 2));
        registration.Verify(value => value.UnRegister(), Times.Never);
        var winner = wait.EventData;
        callback!(3);
        Assert.That(wait.EventData, Is.EqualTo(winner));
        wait.Update(0);
        registration.Verify(value => value.UnRegister(), Times.Once);
    }

    /// <summary>
    ///     验证一个事件注销失败时仍释放第二个订阅，且重试不重复注销。
    /// </summary>
    [Test]
    public void Multiple_Event_Dispose_Should_Attempt_All_Registrations()
    {
        var bus = new Mock<IEventBus>();
        var first = new Mock<IUnRegister>();
        var second = new Mock<IUnRegister>();
        first.Setup(value => value.UnRegister()).Throws(new InvalidOperationException("unsubscribe failed"));
        bus.Setup(value => value.Register<int>(It.IsAny<Action<int>>())).Returns(first.Object);
        bus.Setup(value => value.Register<string>(It.IsAny<Action<string>>())).Returns(second.Object);
        var wait = new WaitForMultipleEvents<int, string>(bus.Object);

        Assert.Throws<InvalidOperationException>(() => wait.Dispose());
        wait.Dispose();

        first.Verify(value => value.UnRegister(), Times.Once);
        second.Verify(value => value.UnRegister(), Times.Once);
    }

    /// <summary>
    ///     验证第二个订阅注册失败时，已经创建的第一个订阅不会残留。
    /// </summary>
    [Test]
    public void Multiple_Event_Registration_Failure_Should_Release_First_Subscription()
    {
        var bus = new Mock<IEventBus>();
        var first = new Mock<IUnRegister>();
        bus.Setup(value => value.Register<int>(It.IsAny<Action<int>>())).Returns(first.Object);
        bus.Setup(value => value.Register<string>(It.IsAny<Action<string>>()))
            .Throws(new InvalidOperationException("register failed"));

        Assert.Throws<InvalidOperationException>(() => new WaitForMultipleEvents<int, string>(bus.Object));

        first.Verify(value => value.UnRegister(), Times.Once);
    }

    /// <summary>
    ///     记录枚举与释放次数，并可模拟各路径异常的资源探针。
    /// </summary>
    private sealed class ProbeEnumerator(Exception? moveFailure = null, Exception? disposeFailure = null,
        bool completesImmediately = false)
        : IEnumerator<IYieldInstruction>
    {
        /// <summary>获取释放请求次数。</summary>
        public int DisposeCount { get; private set; }

        /// <summary>获取推进次数。</summary>
        public int MoveNextCount { get; private set; }

        /// <inheritdoc />
        public IYieldInstruction Current => new Delay(10);

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            MoveNextCount++;
            if (moveFailure != null)
            {
                throw moveFailure;
            }

            return !completesImmediately;
        }

        /// <inheritdoc />
        public void Reset() => throw new NotSupportedException();

        /// <inheritdoc />
        public void Dispose()
        {
            DisposeCount++;
            if (disposeFailure != null)
            {
                throw disposeFailure;
            }
        }
    }
}
