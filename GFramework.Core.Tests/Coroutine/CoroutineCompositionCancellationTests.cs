// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Extensions;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>Official sequence wrappers preserve component lifetimes without continuing a cancelled operation.</summary>
[TestFixture]
public sealed class CoroutineCompositionCancellationTests
{
    /// <summary>Cancellation crosses official wrappers while the operation is blocked and the host is paused.</summary>
    [TestCase("then")]
    [TestCase("sequence")]
    [TestCase("nested")]
    public void IntrinsicCancellation_StopsComposedLongWaitWithoutRunningSuccessAction(string composition)
    {
        var scheduler = Create();
        using var source = new CancellationTokenSource();
        var first = new LifetimeProbe(new Delay(3600), token: source.Token);
        var continued = false;
        var operation = composition switch
        {
            "then" => first.Then(() => continued = true),
            "sequence" => CoroutineExtensions.Sequence(first, Call(() => continued = true)),
            _ => first.Then(() => continued = true).Then(Call(() => continued = true)),
        };
        var handle = scheduler.Run(operation, cancellationToken: global::System.Threading.CancellationToken.None);
        Task.Run(source.Cancel, cancellationToken: global::System.Threading.CancellationToken.None).GetAwaiter().GetResult();
        scheduler.Update(CoroutineExecutionStage.Update, 0, 0, isPaused: true);
        Assert.That(Result(scheduler, handle).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(continued, Is.False);
        Assert.That(first.MoveCount, Is.EqualTo(1));
        Assert.That(first.DisposeCount, Is.EqualTo(1));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>Future components are already owned, so losing their lifetime cancels the operation and releases unstarted inputs.</summary>
    [Test]
    public void FutureOwnedComponentCancellation_ReleasesBothStartedAndUnstartedInputs()
    {
        var scheduler = Create();
        using var source = new CancellationTokenSource();
        var first = new LifetimeProbe(new Delay(3600));
        var future = new LifetimeProbe(new Delay(3600), token: source.Token);
        var handle = scheduler.Run(CoroutineExtensions.Sequence(first, future), cancellationToken: global::System.Threading.CancellationToken.None);
        source.Cancel();
        scheduler.Update(CoroutineExecutionStage.Update, 0, 0, isPaused: true);
        Assert.That(Result(scheduler, handle).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(first.DisposeCount, Is.EqualTo(1));
        Assert.That(future.MoveCount, Is.Zero);
        Assert.That(future.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>Natural completion relinquishes the old lifetime before Dispose can end it.</summary>
    [Test]
    public void CompletedComponentLifetime_DoesNotCancelSubsequentWork()
    {
        var scheduler = Create();
        using var source = new CancellationTokenSource();
        var first = new LifetimeProbe(instruction: null, cleanup: source.Cancel, token: source.Token);
        var second = new LifetimeProbe(new Delay(3600));
        var handle = scheduler.Run(first.Then(second), cancellationToken: global::System.Threading.CancellationToken.None);
        scheduler.Update(CoroutineExecutionStage.Update, 0, 0, isPaused: true);
        Assert.That(source.IsCancellationRequested, Is.True);
        Assert.That(first.DisposeCount, Is.EqualTo(1));
        Assert.That(second.MoveCount, Is.EqualTo(1));
        Assert.That(second.DisposeCount, Is.Zero);
        Assert.That(scheduler.IsCoroutineAlive(handle), Is.True);
        scheduler.Kill(handle);
        Assert.That(second.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>Cancellation from inside the final component cannot be mistaken for natural success before queued kills are drained.</summary>
    [Test]
    public void CancellationDuringMoveNext_DoesNotRunThenAction()
    {
        var scheduler = Create();
        using var source = new CancellationTokenSource();
        var first = new LifetimeProbe(instruction: null, firstStep: source.Cancel, token: source.Token);
        var continued = false;
        var handle = scheduler.Run(first.Then(() => continued = true), cancellationToken: global::System.Threading.CancellationToken.None);
        Assert.That(Result(scheduler, handle).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(continued, Is.False);
        Assert.That(first.DisposeCount, Is.EqualTo(1));
    }

    private static CoroutineScheduler Create() => new(new FakeTimeSource());
    private static CoroutineResult Result(CoroutineScheduler scheduler, CoroutineHandle handle)
    {
        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        return result;
    }
    private static IEnumerator<IYieldInstruction> Call(Action action)
    {
        action();
        yield break;
    }

    private sealed class LifetimeProbe(IYieldInstruction? instruction,
        Action? firstStep = null, Action? cleanup = null, CancellationToken token = default) : IEnumerator<IYieldInstruction>, ICoroutineCancellationSource
    {
        /// <inheritdoc />
        public CancellationToken CancellationToken => token;
        /// <summary>Number of component advances.</summary>
        public int MoveCount
        {
            get; private set;
        }
        /// <summary>Number of ownership releases.</summary>
        public int DisposeCount
        {
            get; private set;
        }
        /// <inheritdoc />
        public IYieldInstruction Current => instruction!;
        /// <inheritdoc />
        object System.Collections.IEnumerator.Current => Current;
        /// <inheritdoc />
        public bool MoveNext()
        {
            MoveCount++;
            if (MoveCount != 1)
            {
                return false;
            }
            firstStep?.Invoke();
            return instruction != null;
        }
        /// <inheritdoc />
        public void Reset() => throw new NotSupportedException();
        /// <inheritdoc />
        public void Dispose()
        {
            DisposeCount++;
            cleanup?.Invoke();
        }
    }
}
