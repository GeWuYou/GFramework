// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>Structured ownership, termination ordering, observer isolation and join behavior.</summary>
[TestFixture]
public sealed class CoroutineOwnershipTests
{
    /// <summary>Registering before child startup prevents losing a synchronously completed child's notification.</summary>
    [Test]
    public void SynchronousChildCompletion_ResumesParent()
    {
        var scheduler = Create();
        var resumed = false;
        IEnumerator<IYieldInstruction> Parent()
        {
            yield return new WaitForCoroutine(Immediate());
            resumed = true;
        }
        var handle = scheduler.Run(Parent());
        scheduler.Update();
        Assert.That(resumed, Is.True);
        Assert.That(Result(scheduler, handle).Status, Is.EqualTo(CoroutineCompletionStatus.Completed));
    }

    /// <summary>A naturally completed parent body remains alive until its children release their resources.</summary>
    [Test]
    public void ParentCompletion_JoinsChildren()
    {
        var scheduler = Create();
        var childDisposed = false;
        IEnumerator<IYieldInstruction> Child()
        {
            try
            {
                yield return new WaitOneFrame();
            }
            finally
            {
                childDisposed = true;
            }
        }
        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            context.RunChild(Child());
            yield break;
        }
        var handle = scheduler.Run(Parent);
        var completion = scheduler.WaitForResultAsync(handle);
        Assert.That(completion.IsCompleted, Is.False);
        scheduler.Update();
        Assert.That(childDisposed, Is.True);
        Assert.That(completion.Result.Status, Is.EqualTo(CoroutineCompletionStatus.Completed));
    }

    /// <summary>One branch's failure fails its flow and cancels sibling branches.</summary>
    [Test]
    public void FailFastChild_FailsParentAndCancelsSibling()
    {
        var scheduler = Create();
        var failure = new InvalidOperationException("branch failure");
        CoroutineHandle sibling = default;
        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            sibling = context.RunChild(Wait());
            context.RunChild(FailAfterFrame(failure));
            yield return new Delay(100);
        }
        var parent = scheduler.Run(Parent);
        scheduler.Update();
        Assert.That(Result(scheduler, parent).Exception, Is.SameAs(failure));
        Assert.That(Result(scheduler, sibling).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>A flow's explicit Supervisor policy isolates child failures even inside a FailFast container.</summary>
    [Test]
    public void SupervisorFlow_IsolatesDescendantFromFailFastContainer()
    {
        var scheduler = Create();
        using var scope = scheduler.CreateScope();
        CoroutineHandle child = default;
        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            child = context.RunChild(FailAfterFrame(new InvalidOperationException("isolated")));
            yield return new Delay(100);
        }
        var parent = scope.Run(Parent, childFailurePolicy: CoroutineFailurePolicy.Supervisor);
        var independent = scope.Run(Wait());
        scheduler.Update();
        Assert.That(Result(scheduler, child).Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(scheduler.IsCoroutineAlive(parent), Is.True);
        Assert.That(scheduler.IsCoroutineAlive(independent), Is.True);
        Assert.That(scope.IsDisposed, Is.False);
    }

    /// <summary>Independent roots in a Supervisor container do not inherit sibling failure.</summary>
    [TestCase(CoroutineFailurePolicy.Supervisor, true)]
    [TestCase(CoroutineFailurePolicy.FailFast, false)]
    public void ScopePolicy_ControlsIndependentPeers(CoroutineFailurePolicy policy, bool peerSurvives)
    {
        var scheduler = Create();
        using var scope = scheduler.CreateScope(policy);
        var peer = scope.Run(Wait());
        scope.Run(FailAfterFrame(new InvalidOperationException("root")));
        scheduler.Update();
        Assert.That(scheduler.IsCoroutineAlive(peer), Is.EqualTo(peerSurvives));
    }

    /// <summary>Stopping one child intentionally does not cancel its parent or a sibling.</summary>
    [Test]
    public void IndividualChildCancellation_DoesNotFailParent()
    {
        var scheduler = Create();
        CoroutineHandle child = default;
        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            child = context.RunChild(Wait());
            yield return new Delay(100);
        }
        var parent = scheduler.Run(Parent);
        scheduler.Kill(child);
        Assert.That(scheduler.IsCoroutineAlive(parent), Is.True);
    }

    /// <summary>Ancestor pause clears independently from a child's own pause request.</summary>
    [Test]
    public void ResumeParent_PreservesChildPauseRequest()
    {
        var scheduler = Create();
        CoroutineHandle child = default;
        var childAdvanced = false;
        IEnumerator<IYieldInstruction> Child()
        {
            yield return new WaitOneFrame();
            childAdvanced = true;
        }
        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            child = context.RunChild(Child());
            yield return new Delay(100);
        }
        var parent = scheduler.Run(Parent);
        scheduler.Pause(parent);
        scheduler.Pause(child);
        scheduler.Update();
        scheduler.Resume(parent);
        scheduler.Update();
        Assert.That(childAdvanced, Is.False);
        scheduler.Resume(child);
        scheduler.Update();
        Assert.That(childAdvanced, Is.True);
    }

    /// <summary>An executing child can stop its ancestor, but ancestor completion cannot precede child finally.</summary>
    [Test]
    public void ChildCancelsParent_ParentCompletesOnlyAfterChildUnwinds()
    {
        var scheduler = Create();
        CoroutineHandle parent = default;
        var childFinally = false;
        var completionObserved = false;
        IEnumerator<IYieldInstruction> Child()
        {
            try
            {
                yield return new WaitOneFrame();
                scheduler.Kill(parent);
                Assert.That(completionObserved, Is.False);
                Assert.That(childFinally, Is.False);
                yield return new Delay(100);
            }
            finally
            {
                childFinally = true;
            }
        }
        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            context.RunChild(Child());
            yield return new Delay(100);
        }
        parent = scheduler.Run(Parent);
        var completion = scheduler.WaitForResultAsync(parent);
        scheduler.OnCoroutineFinished += (_, args) =>
        {
            if (args.Handle == parent)
            {
                completionObserved = childFinally;
            }
        };
        scheduler.Update();
        Assert.That(completionObserved, Is.True);
        Assert.That(completion.Result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>Self-cancellation defers iterator disposal until the current MoveNext call returns.</summary>
    [Test]
    public void SelfKill_DoesNotDisposeExecutingIterator()
    {
        var scheduler = Create();
        CoroutineHandle handle = default;
        var unwound = false;
        var disposed = false;
        IEnumerator<IYieldInstruction> Routine()
        {
            try
            {
                yield return new WaitOneFrame();
                scheduler.Kill(handle);
                Assert.That(disposed, Is.False);
                unwound = true;
                yield return new Delay(100);
            }
            finally
            {
                disposed = true;
            }
        }
        handle = scheduler.Run(Routine());
        scheduler.Update();
        Assert.That(unwound && disposed, Is.True);
    }

    /// <summary>Throwing observers cannot block later observers or prevent slot reuse.</summary>
    [Test]
    public void CompletionObservers_AreIsolatedAndMayStartReplacement()
    {
        var scheduler = Create();
        var notified = 0;
        CoroutineHandle replacement = default;
        scheduler.OnCoroutineFinished += (_, _) => throw new InvalidOperationException("observer");
        scheduler.OnCoroutineFinished += (_, _) =>
        {
            notified++;
            if (!replacement.IsValid)
            {
                replacement = scheduler.Run(Wait());
            }
        };
        var handle = scheduler.Run(Immediate());
        Assert.That(notified, Is.EqualTo(1));
        Assert.That(Result(scheduler, handle).Status, Is.EqualTo(CoroutineCompletionStatus.Completed));
        Assert.That(scheduler.IsCoroutineAlive(replacement), Is.True);
        Assert.That(scheduler.Kill(handle), Is.False);
    }

    /// <summary>Runtime closure is global: retaining a separate scope cannot revive work during disposal.</summary>
    [Test]
    public void Dispose_ClosesIndependentScopesBeforeUserCleanup()
    {
        var scheduler = Create();
        var retained = scheduler.CreateScope(CoroutineFailurePolicy.Supervisor);
        var rejected = false;
        IEnumerator<IYieldInstruction> Routine()
        {
            try
            {
                yield return new Delay(100);
            }
            finally
            {
                rejected = Assert.Throws<ObjectDisposedException>(() => retained.Run(Wait())) != null;
            }
        }
        var handle = retained.Run(Routine());
        scheduler.Dispose();
        Assert.That(rejected, Is.True);
        Assert.That(Result(scheduler, handle).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.Throws<ObjectDisposedException>(() => scheduler.CreateScope());
        Assert.Throws<ObjectDisposedException>(() => scheduler.Update());
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>Non-owning joins observe cancellation without cancelling their target when the waiter is stopped.</summary>
    [Test]
    public void Join_DoesNotTakeTargetOwnership()
    {
        var scheduler = Create();
        var target = scheduler.Run(Wait());
        var waiter = scheduler.Run(WaitFor(scheduler.Join(target)));
        scheduler.Kill(waiter);
        Assert.That(scheduler.IsCoroutineAlive(target), Is.True);
        var join = scheduler.Join(target);
        var observer = scheduler.Run(WaitFor(join));
        scheduler.Kill(target);
        scheduler.Update();
        Assert.That(join.Result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(Result(scheduler, observer).Status, Is.EqualTo(CoroutineCompletionStatus.Completed));
    }

    /// <summary>Await propagates the original exception rather than silently succeeding after an observed failure.</summary>
    [Test]
    public void Await_PropagatesOriginalFailure()
    {
        var scheduler = Create();
        var failure = new InvalidOperationException("target");
        var target = scheduler.Run(FailAfterFrame(failure));
        var waiter = scheduler.Run(WaitFor(scheduler.Await(target)));
        scheduler.Update();
        Assert.That(Result(scheduler, waiter).Exception, Is.SameAs(failure));
    }

    /// <summary>Installed waits and already acquired joins keep their outcomes after terminal-history eviction.</summary>
    [Test]
    public void JoinOutcome_SurvivesHistoryEviction()
    {
        var scheduler = Create();
        var target = scheduler.Run(Immediate());
        var captured = scheduler.Join(target);
        for (var index = 0; index < 1100; index++)
        {
            scheduler.Run(Immediate());
        }
        Assert.That(scheduler.TryGetResult(target, out _), Is.False);
        var waiter = scheduler.Run(WaitFor(captured));
        scheduler.Update();
        Assert.That(captured.Result.Status, Is.EqualTo(CoroutineCompletionStatus.Completed));
        Assert.That(Result(scheduler, waiter).Status, Is.EqualTo(CoroutineCompletionStatus.Completed));
    }

    /// <summary>Cycle detection includes implicit ownership edges, since parents must join their children.</summary>
    [Test]
    public void ChildAwaitingParent_FailsInsteadOfDeadlocking()
    {
        var scheduler = Create();
        CoroutineHandle parent = default;
        IEnumerator<IYieldInstruction> Child()
        {
            yield return new WaitOneFrame();
            yield return scheduler.Join(parent);
        }
        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            context.RunChild(Child());
            yield return new Delay(100);
        }
        parent = scheduler.Run(Parent);
        scheduler.Update();
        Assert.That(Result(scheduler, parent).Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>Deep startup and teardown use queues rather than recursive iterator or parent completion calls.</summary>
    [Test]
    public void DeepOwnedChain_CancelsWithoutRecursiveTeardown()
    {
        var scheduler = Create();
        IEnumerator<IYieldInstruction> Chain(int remaining)
        {
            if (remaining > 0)
            {
                yield return new WaitForCoroutine(Chain(remaining - 1));
            }
            else
            {
                yield return new Delay(100);
            }
        }
        var parent = scheduler.Run(Chain(2000));
        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(2001));
        scheduler.Kill(parent);
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
        Assert.That(Result(scheduler, parent).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
    }

    /// <summary>Storage grows with peak concurrency, not cumulative starts over a long-lived session.</summary>
    [Test]
    public void RepeatedStarts_DoNotGrowHistoricalSlotScan()
    {
        var scheduler = Create();
        for (var index = 0; index < 10000; index++)
        {
            scheduler.Run(Immediate());
        }
        Assert.That(scheduler.AllocatedSlotCount, Is.EqualTo(1));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>Successful wait completion cannot turn a Dispose failure into cooperative cancellation.</summary>
    [Test]
    public void CompletedWaitCleanup_CancellationExceptionRemainsFaulted()
    {
        var scheduler = Create();
        var failure = new OperationCanceledException("cleanup failure");
        var continued = false;
        IEnumerator<IYieldInstruction> Routine()
        {
            yield return new CleanupFailureWait(failure);
            continued = true;
        }
        var handle = scheduler.Run(Routine());
        scheduler.Update();
        Assert.That(continued, Is.False);
        Assert.That(Result(scheduler, handle).Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(Result(scheduler, handle).Exception, Is.SameAs(failure));
    }

    private sealed class CleanupFailureWait(Exception exception) : IYieldInstruction, IDisposable
    {
        /// <inheritdoc />
        public bool IsDone => true;
        /// <inheritdoc />
        public void Update(double deltaTime)
        {
        }
        /// <inheritdoc />
        public void Dispose() => throw exception;
    }

    private static CoroutineScheduler Create() => new(new FakeTimeSource());
    private static CoroutineResult Result(CoroutineScheduler scheduler, CoroutineHandle handle)
    {
        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        return result;
    }
    private static IEnumerator<IYieldInstruction> Immediate()
    {
        yield break;
    }
    private static IEnumerator<IYieldInstruction> Wait()
    {
        yield return new Delay(100);
    }
    private static IEnumerator<IYieldInstruction> WaitFor(IYieldInstruction instruction)
    {
        yield return instruction;
    }
    private static IEnumerator<IYieldInstruction> FailAfterFrame(Exception exception)
    {
        yield return new WaitOneFrame();
        throw exception;
    }
}
