// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>Parallel child results remain owned by their operation even after the scheduler's diagnostic history evicts them.</summary>
[TestFixture]
public sealed class CoroutineJoinRetentionTests
{
    private const int FastChildCount = 1100;

    /// <summary>A slow first child must not turn successfully completed later children into Unknown outcomes.</summary>
    [Test]
    public void WhenAll_RetainsSuccessfulSiblingResultsBeyondHistoryLimit()
    {
        var scheduler = Create();
        var release = false;
        CoroutineHandle firstCompletedSibling = default;
        scheduler.OnCoroutineFinished += (_, args) =>
        {
            if (!firstCompletedSibling.IsValid) { firstCompletedSibling = args.Handle; }
        };
        var parent = scheduler.Run(context => context.WhenAll(CreateChildren(() => release)));
        Assert.That(scheduler.TryGetResult(firstCompletedSibling, out _), Is.False,
            "The regression must actually exceed diagnostic history capacity.");
        release = true;
        AdvanceUntilCompleted(scheduler, parent);
        Assert.That(Result(scheduler, parent).Status, Is.EqualTo(CoroutineCompletionStatus.Completed));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>Supervision must preserve an early child's actual failure while the operation is joining a slow sibling.</summary>
    [Test]
    public void WhenAll_RetainsOriginalFailureBeyondHistoryLimit()
    {
        var scheduler = Create();
        var release = false;
        var failure = new InvalidOperationException("retained child failure");
        var parent = scheduler.Run(context => context.WhenAll(CreateChildren(() => release, failure)),
            childFailurePolicy: CoroutineFailurePolicy.Supervisor);
        release = true;
        AdvanceUntilCompleted(scheduler, parent);
        Assert.That(Result(scheduler, parent).Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(Result(scheduler, parent).Exception, Is.SameAs(failure));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>A cancelled child remains Cancelled instead of degrading to an unrelated unknown-result failure.</summary>
    [Test]
    public void WhenAll_RetainsCancelledChildBeyondHistoryLimit()
    {
        var scheduler = Create();
        var release = false;
        var parent = scheduler.Run(context => context.WhenAll(CreateChildren(() => release,
                new OperationCanceledException("cancelled child"))),
            childFailurePolicy: CoroutineFailurePolicy.Supervisor);
        release = true;
        AdvanceUntilCompleted(scheduler, parent);
        Assert.That(Result(scheduler, parent).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>Disposing the operation releases explicit result observations even when its iterator is retained by a caller.</summary>
    [Test]
    public void CancelledWhenAll_DoesNotRetainCompletedChildFailures()
    {
        var scheduler = Create();
        var failureReference = StartAndCancelOperation(scheduler, out var retainedIterator);
        for (var index = 0; index < FastChildCount + 2; index++) { scheduler.Run(Complete()); }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
        Assert.That(failureReference.IsAlive, Is.False);
        GC.KeepAlive(retainedIterator);
        GC.KeepAlive(scheduler);
    }

    /// <summary>Intrinsic cancellation may complete during startup; reentrant terminal callbacks cannot evict its owned observation.</summary>
    [Test]
    public void WhenAll_ObservesBeforeSynchronousCancellationCallbacksCanEvictHistory()
    {
        var scheduler = Create();
        var producedCallbacks = false;
        var child = new IntrinsicallyCancelledCoroutine();
        scheduler.OnCoroutineFinished += (_, _) =>
        {
            if (producedCallbacks) { return; }
            producedCallbacks = true;
            for (var index = 0; index < FastChildCount; index++)
            {
                scheduler.Run(new IntrinsicallyCancelledCoroutine());
            }
        };
        var parent = scheduler.Run(context => context.WhenAll(new[] { child }));
        AdvanceUntilCompleted(scheduler, parent);
        Assert.That(Result(scheduler, parent).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(child.MoveCount, Is.Zero);
        Assert.That(child.DisposeCount, Is.EqualTo(1));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    private sealed class IntrinsicallyCancelledCoroutine : IEnumerator<IYieldInstruction>, ICoroutineCancellationSource
    {
        /// <inheritdoc />
        public CancellationToken CancellationToken => new(true);
        /// <summary>How often user code was advanced; pre-cancelled lifetimes must remain unstarted.</summary>
        public int MoveCount { get; private set; }
        /// <summary>How often transferred resources were released.</summary>
        public int DisposeCount { get; private set; }
        /// <inheritdoc />
        public IYieldInstruction Current => throw new InvalidOperationException("An unstarted iterator has no current item.");
        /// <inheritdoc />
        object System.Collections.IEnumerator.Current => Current;
        /// <inheritdoc />
        public bool MoveNext()
        {
            MoveCount++;
            return false;
        }
        /// <inheritdoc />
        public void Reset() => throw new NotSupportedException();
        /// <inheritdoc />
        public void Dispose() => DisposeCount++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference StartAndCancelOperation(CoroutineScheduler scheduler,
        out IEnumerator<IYieldInstruction> retainedIterator)
    {
        var failure = new InvalidOperationException("completed child payload");
        var reference = new WeakReference(failure);
        IEnumerator<IYieldInstruction>? operation = null;
        var parent = scheduler.Run(context =>
        {
            operation = context.WhenAll(new[] { WaitForGate(() => false), Complete(failure) });
            return operation;
        }, childFailurePolicy: CoroutineFailurePolicy.Supervisor);
        scheduler.Kill(parent);
        retainedIterator = operation!;
        return reference;
    }

    private static IEnumerator<IYieldInstruction>[] CreateChildren(Func<bool> released, Exception? failure = null)
    {
        var children = new IEnumerator<IYieldInstruction>[FastChildCount + 2];
        children[0] = WaitForGate(released);
        children[1] = Complete(failure);
        for (var index = 2; index < children.Length; index++) { children[index] = Complete(); }
        return children;
    }

    private static void AdvanceUntilCompleted(CoroutineScheduler scheduler, CoroutineHandle handle)
    {
        for (var index = 0; index < FastChildCount + 10 && scheduler.IsCoroutineAlive(handle); index++)
        {
            scheduler.Update();
        }
        Assert.That(scheduler.IsCoroutineAlive(handle), Is.False, "The operation must finish within its explicit join count.");
    }

    private static CoroutineResult Result(CoroutineScheduler scheduler, CoroutineHandle handle)
    {
        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        return result;
    }

    private static CoroutineScheduler Create() => new(new FakeTimeSource());
    private static IEnumerator<IYieldInstruction> WaitForGate(Func<bool> released) { yield return new WaitUntil(released); }
    private static IEnumerator<IYieldInstruction> Complete(Exception? failure = null)
    {
        if (failure != null) { throw failure; }
        yield break;
    }
}
