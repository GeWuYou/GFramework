// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>Cancellation observed during a tick wins before the affected task's next continuation or successful completion.</summary>
[TestFixture]
public sealed class CoroutineCancellationBoundaryTests
{
    /// <summary>An earlier FIFO slot can cancel either external or intrinsic lifetimes after the tick-start queue drain.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EarlierSlotCancellation_PreventsLaterSlotContinuation(bool intrinsic)
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        using var source = new CancellationTokenSource();
        var continued = false;
        IEnumerator<IYieldInstruction> Earlier()
        {
            yield return new WaitOneFrame();
            source.Cancel();
        }
        IEnumerator<IYieldInstruction> Later()
        {
            yield return new WaitOneFrame();
            continued = true;
        }
        scheduler.Run(Earlier(), cancellationToken: global::System.Threading.CancellationToken.None);
        var later = intrinsic
            ? scheduler.Run(new BoundIterator(Later(), source.Token), cancellationToken: global::System.Threading.CancellationToken.None)
            : scheduler.Run(Later(), cancellationToken: source.Token);
        scheduler.Update();
        Assert.That(continued, Is.False);
        Assert.That(Result(scheduler, later).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(scheduler.ActiveCoroutineCount, Is.Zero);
    }

    /// <summary>Pre-cancelled inputs remain unstarted and are released exactly once across both token entry points.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public void PreCancelledInput_IsNotAdvanced(bool intrinsic)
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        using var source = new CancellationTokenSource();
        source.Cancel();
        static IEnumerator<IYieldInstruction> Routine()
        {
            yield return new WaitOneFrame();
        }
        var input = new BoundIterator(Routine(), intrinsic ? source.Token : default);
        var handle = scheduler.Run(input, cancellationToken: intrinsic ? default : source.Token);
        Assert.That(input.MoveCalls, Is.Zero);
        Assert.That(input.DisposeCalls, Is.EqualTo(1));
        if (intrinsic)
        {
            Assert.That(Result(scheduler, handle).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        }
        else
        {
            Assert.That(handle.IsValid, Is.False);
        }
    }

    /// <summary>Wait callbacks cannot cancel the token and still let user continuation code execute in that same tick.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public void WaitCallbackCancellation_PreventsContinuation(bool cancelInGetter)
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        using var source = new CancellationTokenSource();
        var continued = false;
        IEnumerator<IYieldInstruction> Routine()
        {
            yield return new CancellingWait(source.Cancel, cancelInGetter);
            continued = true;
        }
        var handle = scheduler.Run(Routine(), cancellationToken: source.Token);
        scheduler.Update();
        Assert.That(continued, Is.False);
        Assert.That(Result(scheduler, handle).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
    }

    /// <summary>The last MoveNext returning false must not overwrite an already requested cancellation with Completed.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public void CancellationInsideLastStep_IsNotReportedAsCompleted(bool directKill)
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        using var source = new CancellationTokenSource();
        CoroutineHandle handle = default;
        IEnumerator<IYieldInstruction> Routine()
        {
            yield return new WaitOneFrame();
            if (directKill)
            {
                scheduler.Kill(handle);
            }
            else
            {
                source.Cancel();
            }
        }
        handle = scheduler.Run(Routine(), cancellationToken: source.Token);
        scheduler.Update();
        Assert.That(Result(scheduler, handle).Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
    }

    private static CoroutineResult Result(CoroutineScheduler scheduler, CoroutineHandle handle)
    {
        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        return result;
    }

    private sealed class CancellingWait(Action cancel, bool cancelInGetter) : IYieldInstruction
    {
        /// <inheritdoc />
        public bool IsDone
        {
            get
            {
                if (cancelInGetter)
                {
                    cancel();
                }
                return true;
            }
        }
        /// <inheritdoc />
        public void Update(double deltaTime)
        {
            if (!cancelInGetter)
            {
                cancel();
            }
        }
    }

    private sealed class BoundIterator(IEnumerator<IYieldInstruction> inner, CancellationToken token)
        : IEnumerator<IYieldInstruction>, ICoroutineCancellationSource
    {
        /// <inheritdoc />
        public CancellationToken CancellationToken => token;
        /// <summary>Number of actual user-code advances.</summary>
        public int MoveCalls
        {
            get; private set;
        }
        /// <summary>Number of resource releases.</summary>
        public int DisposeCalls
        {
            get; private set;
        }
        /// <inheritdoc />
        public IYieldInstruction Current => inner.Current;
        /// <inheritdoc />
        object IEnumerator.Current => Current;
        /// <inheritdoc />
        public bool MoveNext()
        {
            MoveCalls++;
            return inner.MoveNext();
        }
        /// <inheritdoc />
        public void Reset() => throw new NotSupportedException();
        /// <inheritdoc />
        public void Dispose()
        {
            DisposeCalls++;
            inner.Dispose();
        }
    }
}
