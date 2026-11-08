// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using Moq;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     在明确的调度边界触发终结，验证同一步取消和异步完成之间不会遗失资源或结果。
/// </summary>
[TestFixture]
public sealed class CoroutineWaitCompletionRaceTests
{
    /// <summary>
    ///     验证 MoveNext 内取消自身后产生的真实事件订阅仍会注销。
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void Self_Cancellation_Should_Dispose_Instruction_Yielded_In_Same_Step(bool duringPrewarm)
    {
        using var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var bus = new Mock<IEventBus>();
        var registration = new Mock<IUnRegister>();
        bus.Setup(value => value.Register<int>(It.IsAny<Action<int>>())).Returns(registration.Object);

        IEnumerator<IYieldInstruction> Routine(CoroutineContext context)
        {
            if (!duringPrewarm)
            {
                yield return new WaitOneFrame();
            }

            scheduler.Kill(context.Handle);
            yield return new WaitForEvent<int>(bus.Object);
        }

        var handle = scheduler.Run(Routine);
        scheduler.Update();

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        bus.Verify(value => value.Register<int>(It.IsAny<Action<int>>()), Times.Once);
        registration.Verify(value => value.UnRegister(), Times.Once);
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>
    ///     验证 Current 读取期间请求取消时，同样只接收清理责任，不推进新等待。
    /// </summary>
    [Test]
    public void Cancellation_From_Current_Getter_Should_Dispose_Without_Updating_Instruction()
    {
        using var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var wait = new DisposableWait();
        var handle = scheduler.Run(context => new CancelOnCurrentEnumerator(
            () => scheduler.Kill(context.Handle), wait));

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(wait.UpdateCount, Is.Zero);
        Assert.That(wait.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证已终止流程产生的嵌套协程只被释放，绝不执行其第一步。
    /// </summary>
    [Test]
    public void Self_Cancellation_Should_Dispose_Unstarted_Yielded_Child()
    {
        using var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var child = new RecordingEnumerator();

        IEnumerator<IYieldInstruction> Routine(CoroutineContext context)
        {
            scheduler.Kill(context.Handle);
            yield return new WaitForCoroutine(child);
        }

        var handle = scheduler.Run(Routine);

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(child.MoveNextCount, Is.Zero);
        Assert.That(child.DisposeCount, Is.EqualTo(1));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>
    ///     验证同一步产生的等待清理故障保留为 Faulted，取消异常也不能被误报为正常取消。
    /// </summary>
    [Test]
    public void Self_Cancellation_Should_Preserve_Yielded_Instruction_Cleanup_Failure()
    {
        using var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var failure = new OperationCanceledException("resource cleanup failed");
        var wait = new DisposableWait(failure);

        IEnumerator<IYieldInstruction> Routine(CoroutineContext context)
        {
            scheduler.Kill(context.Handle);
            yield return wait;
        }

        var handle = scheduler.Run(Routine, cancellationToken: global::System.Threading.CancellationToken.None);

        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.SameAs(failure));
        Assert.That(wait.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     确定性地在 Update 观察未完成之后、IsDone 观察完成之前交付异步结果。
    /// </summary>
    [TestCase("task", "success")]
    [TestCase("task", "fault")]
    [TestCase("task", "faulted-oce")]
    [TestCase("task", "cancel")]
    [TestCase("generic-task", "success")]
    [TestCase("generic-task", "fault")]
    [TestCase("generic-task", "faulted-oce")]
    [TestCase("generic-task", "cancel")]
    [TestCase("operation", "success")]
    [TestCase("operation", "fault")]
    [TestCase("operation", "faulted-oce")]
    [TestCase("operation", "cancel")]
    public void Completion_Between_Update_And_IsDone_Should_Consume_Actual_Outcome(string kind, string outcome)
    {
        using var scheduler = new CoroutineScheduler(new FakeTimeSource());
        Exception failure = string.Equals(outcome, "faulted-oce", StringComparison.Ordinal)
            ? new OperationCanceledException("faulted operation carrying cancellation exception")
            : new InvalidOperationException("worker failure");
        var (instruction, complete) = CreateAsyncWait(kind, outcome, failure);
        var boundary = new CompleteOnDoneRead(instruction, complete);
        var continued = false;

        IEnumerator<IYieldInstruction> Routine()
        {
            yield return boundary;
            continued = true;
        }

        var handle = scheduler.Run(Routine());
        scheduler.Update();

        var expected = string.Equals(outcome, "success", StringComparison.Ordinal)
            ? CoroutineCompletionStatus.Completed
            : string.Equals(outcome, "cancel", StringComparison.Ordinal)
                ? CoroutineCompletionStatus.Cancelled
                : CoroutineCompletionStatus.Faulted;
        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(expected));
        Assert.That(continued, Is.EqualTo(expected == CoroutineCompletionStatus.Completed));
        Assert.That(boundary.UpdateCount, Is.EqualTo(1), "Result consumption must not update the wait twice.");
        Assert.That(boundary.ConsumeCount, Is.EqualTo(1));
        if (expected == CoroutineCompletionStatus.Faulted)
        {
            Assert.That(result.Exception, Is.SameAs(failure));
        }
    }

    private static (IYieldInstruction Instruction, Action Complete) CreateAsyncWait(
        string kind, string outcome, Exception failure)
    {
        if (string.Equals(kind, "operation", StringComparison.Ordinal))
        {
            var operation = new AsyncOperation();
            return (operation, () =>
            {
                if (string.Equals(outcome, "success", StringComparison.Ordinal))
                {
                    operation.SetCompleted();
                }
                else if (string.Equals(outcome, "cancel", StringComparison.Ordinal))
                {
                    operation.SetCanceled(new CancellationToken(canceled: true));
                }
                else
                {
                    operation.SetException(failure);
                }
            }
            );
        }

        var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        IYieldInstruction wait = string.Equals(kind, "generic-task", StringComparison.Ordinal)
            ? new WaitForTask<int>(source.Task)
            : new WaitForTask(source.Task);
        return (wait, () =>
        {
            if (string.Equals(outcome, "success", StringComparison.Ordinal))
            {
                source.SetResult(42);
            }
            else if (string.Equals(outcome, "cancel", StringComparison.Ordinal))
            {
                source.SetCanceled(new CancellationToken(canceled: true));
            }
            else
            {
                source.SetException(failure);
            }
        }
        );
    }

    /// <summary>
    ///     委托真实异步指令，仅控制一次后台完成发生在指定的轮询边界。
    /// </summary>
    private sealed class CompleteOnDoneRead(IYieldInstruction inner, Action complete)
        : IYieldInstruction, ICoroutineCompletionStatusSource, ICoroutineCompletionConsumer
    {
        private bool _completed;

        /// <summary>获取实际等待推进次数。</summary>
        public int UpdateCount
        {
            get; private set;
        }

        /// <summary>获取终结结果消费次数。</summary>
        public int ConsumeCount
        {
            get; private set;
        }

        /// <inheritdoc />
        public bool IsDone
        {
            get
            {
                if (!_completed)
                {
                    _completed = true;
                    // 同步等待测试控制动作，保证后台终结恰好发生在 inner.Update 返回之后。
                    Task.Run(complete).GetAwaiter().GetResult();
                }

                return inner.IsDone;
            }
        }

        /// <inheritdoc />
        CoroutineCompletionStatus ICoroutineCompletionStatusSource.CompletionStatus =>
            ((ICoroutineCompletionStatusSource)inner).CompletionStatus;

        /// <inheritdoc />
        public void Update(double deltaTime)
        {
            UpdateCount++;
            inner.Update(deltaTime);
        }

        /// <inheritdoc />
        void ICoroutineCompletionConsumer.ConsumeCompletion()
        {
            ConsumeCount++;
            ((ICoroutineCompletionConsumer)inner).ConsumeCompletion();
        }
    }

    /// <summary>记录生命周期的等待资源，可模拟清理异常。</summary>
    private sealed class DisposableWait(Exception? failure = null) : IYieldInstruction, IDisposable
    {
        /// <summary>获取推进次数。</summary>
        public int UpdateCount
        {
            get; private set;
        }

        /// <summary>获取释放次数。</summary>
        public int DisposeCount
        {
            get; private set;
        }

        /// <inheritdoc />
        public bool IsDone => false;

        /// <inheritdoc />
        public void Update(double deltaTime) => UpdateCount++;

        /// <inheritdoc />
        public void Dispose()
        {
            DisposeCount++;
            if (failure != null)
            {
                throw failure;
            }
        }
    }

    /// <summary>模拟在 Current 属性交付等待资源前请求终止的枚举器。</summary>
    private sealed class CancelOnCurrentEnumerator(Action cancel, IYieldInstruction instruction)
        : IEnumerator<IYieldInstruction>
    {
        /// <inheritdoc />
        public IYieldInstruction Current
        {
            get
            {
                cancel();
                return instruction;
            }
        }

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext() => true;

        /// <inheritdoc />
        public void Reset() => throw new NotSupportedException();

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    /// <summary>证明取消后的嵌套枚举器从未启动且被释放一次。</summary>
    private sealed class RecordingEnumerator : IEnumerator<IYieldInstruction>
    {
        /// <summary>获取执行次数。</summary>
        public int MoveNextCount
        {
            get; private set;
        }

        /// <summary>获取释放次数。</summary>
        public int DisposeCount
        {
            get; private set;
        }

        /// <inheritdoc />
        public IYieldInstruction Current => new WaitOneFrame();

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            MoveNextCount++;
            return true;
        }

        /// <inheritdoc />
        public void Reset() => throw new NotSupportedException();

        /// <inheritdoc />
        public void Dispose() => DisposeCount++;
    }
}
