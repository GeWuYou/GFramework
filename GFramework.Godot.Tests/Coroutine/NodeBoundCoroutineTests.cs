// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Extensions;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;
using Godot;

namespace GFramework.Godot.Tests.Coroutine;

/// <summary>Exercises the real Godot wrapper's intrinsic lifetime without requiring native node construction.</summary>
[TestFixture]
[NonParallelizable]
public sealed class NodeBoundCoroutineTests
{
    private Timing _timing = null!;

    /// <summary>Creates only managed scheduler state; zero-node wrappers perform no native Godot operations.</summary>
    [SetUp]
    public void SetUp()
    {
        _timing = (Timing)RuntimeHelpers.GetUninitializedObject(typeof(Timing));
        _timing.InitializeForTests();
    }

    /// <summary>Releases this fixture's task ownership and host identity.</summary>
    [TearDown]
    public void TearDown() => _timing.DisposeForTests();

    /// <summary>Raw scope starts consume wrapper cancellation during blocked physics waits, including factories and wrapper chains.</summary>
    /// <param name="factoryStart">Whether the scope receives a factory instead of the iterator directly.</param>
    /// <param name="chained">Whether another node-bound wrapper surrounds the first wrapper.</param>
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void ScopeRun_CancelsIntrinsicLifetimeDuringPausedWait(bool factoryStart, bool chained)
    {
        using var cancellation = new CancellationTokenSource();
        var disposed = 0;
        using var scope = _timing.GetSchedulerForTests(Segment.Process).CreateScope(CoroutineFailurePolicy.Supervisor);
        var wrapper = BindLifetime(Blocked(() => disposed++), chained, cancellation.Token);
        var handle = factoryStart
            ? scope.Run(_ => wrapper, stage: CoroutineExecutionStage.FixedUpdate, cancellationToken: global::System.Threading.CancellationToken.None)
            : scope.Run(wrapper, stage: CoroutineExecutionStage.FixedUpdate, cancellationToken: global::System.Threading.CancellationToken.None);
        cancellation.Cancel();
        _timing.AdvancePhysicsFrameForTests(paused: true);
        Assert.That(Timing.TryGetCoroutineResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(disposed, Is.EqualTo(1));
        Assert.That(scope.ActiveCount, Is.Zero);
    }

    /// <summary>Child starts consume the same intrinsic lifetime while keeping intentional child cancellation isolated from its parent.</summary>
    /// <param name="factoryStart">Whether the child is created through a factory.</param>
    [TestCase(false)]
    [TestCase(true)]
    public void RunChild_CancelsIntrinsicLifetimeDuringPausedWait(bool factoryStart)
    {
        using var cancellation = new CancellationTokenSource();
        var disposed = 0;
        CoroutineHandle child = default;
        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            var wrapper = BindLifetime(Blocked(() => disposed++), chained: true, token: cancellation.Token);
            child = factoryStart ? context.RunChild(_ => wrapper, cancellationToken: global::System.Threading.CancellationToken.None) : context.RunChild(wrapper, cancellationToken: global::System.Threading.CancellationToken.None);
            yield return new Delay(100);
        }

        var parent = _timing.RunCoroutineOnInstance(Parent, Segment.PhysicsProcess, cancellationToken: global::System.Threading.CancellationToken.None);
        cancellation.Cancel();
        _timing.AdvancePhysicsFrameForTests(paused: true);
        Assert.That(Timing.TryGetCoroutineResult(child, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(disposed, Is.EqualTo(1));
        Assert.That(Timing.TryGetCoroutineSnapshot(parent, out _), Is.True);
    }

    /// <summary>Then must retain the bound lifetime and must not run its success action after node cancellation.</summary>
    /// <param name="childStart">Whether the composition starts as an owned child rather than a scope root.</param>
    [TestCase(false)]
    [TestCase(true)]
    public void Then_PreservesIntrinsicLifetimeDuringPausedWait(bool childStart)
    {
        using var cancellation = new CancellationTokenSource();
        var disposed = 0;
        var continued = false;
        using var scope = _timing.GetSchedulerForTests(Segment.Process).CreateScope(CoroutineFailurePolicy.Supervisor);
        var composition = BindLifetime(Blocked(() => disposed++), chained: false, token: cancellation.Token)
            .Then(() => continued = true);
        CoroutineHandle handle = default;
        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            handle = context.RunChild(composition, cancellationToken: global::System.Threading.CancellationToken.None);
            yield return new Delay(100);
        }

        if (childStart)
        {
            scope.Run(Parent, stage: CoroutineExecutionStage.FixedUpdate, cancellationToken: global::System.Threading.CancellationToken.None);
        }
        else
        {
            handle = scope.Run(composition, stage: CoroutineExecutionStage.FixedUpdate, cancellationToken: global::System.Threading.CancellationToken.None);
        }
        cancellation.Cancel();
        _timing.AdvancePhysicsFrameForTests(paused: true);
        Assert.That(Timing.TryGetCoroutineResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
        Assert.That(disposed, Is.EqualTo(1));
        Assert.That(continued, Is.False);
    }

    /// <summary>A failed intrinsic-token getter cannot leak the iterator already transferred into a wrapper.</summary>
    [Test]
    public void LifetimeSetupFailure_DisposesTransferredIterator()
    {
        var expected = new InvalidOperationException("intrinsic lifetime unavailable");
        var disposed = false;
        var inner = new LifetimeIterator(new List<IYieldInstruction>().GetEnumerator(),
            default, expected, () => disposed = true);
        var failure = Assert.Throws<InvalidOperationException>(() => new NodeBoundCoroutine(inner, []));
        Assert.That(failure, Is.SameAs(expected));
        Assert.That(disposed, Is.True);
    }

    /// <summary>Wraps a managed intrinsic lifetime in the production Godot wrapper, without native owners.</summary>
    /// <param name="inner">The user iterator whose actual cleanup is observed.</param>
    /// <param name="token">The managed substitute for a native node-exit signal.</param>
    /// <param name="chained">Whether to add a second wrapper lifetime.</param>
    /// <returns>The production wrapper supplied directly to scope or child start APIs.</returns>
    private static NodeBoundCoroutine BindLifetime(IEnumerator<IYieldInstruction> inner, bool chained, CancellationToken token)
    {
        var wrapper = new NodeBoundCoroutine(new LifetimeIterator(inner, token), []);
        return chained ? new NodeBoundCoroutine(wrapper, []) : wrapper;
    }

    /// <summary>Stays blocked long enough to distinguish token cancellation from ordinary iterator advancement.</summary>
    /// <param name="onDisposed">The observed actual iterator cleanup.</param>
    /// <returns>The blocked user iterator.</returns>
    private static IEnumerator<IYieldInstruction> Blocked(Action onDisposed)
    {
        try
        {
            yield return new Delay(100);
        }
        finally
        {
            onDisposed();
        }
    }

    /// <summary>Supplies a managed lifetime to the real Godot wrapper without a native Node dependency.</summary>
    private sealed class LifetimeIterator(IEnumerator<IYieldInstruction> inner, CancellationToken token,
        Exception? tokenFailure = null, Action? onDisposed = null) : IEnumerator<IYieldInstruction>, ICoroutineCancellationSource
    {
        /// <inheritdoc />
        public CancellationToken CancellationToken => tokenFailure == null ? token : throw tokenFailure;
        /// <inheritdoc />
        public IYieldInstruction Current => inner.Current;
        /// <inheritdoc />
        object IEnumerator.Current => Current;
        /// <inheritdoc />
        public bool MoveNext() => inner.MoveNext();
        /// <inheritdoc />
        public void Reset() => throw new NotSupportedException();
        /// <inheritdoc />
        public void Dispose()
        {
            try
            {
                inner.Dispose();
            }
            finally
            {
                onDisposed?.Invoke();
            }
        }
    }
}
