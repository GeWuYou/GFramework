// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>Host phase advancement, pause separation and persistent stage switching.</summary>
[TestFixture]
public sealed class CoroutinePhaseTests
{
    /// <summary>A task remains in the target phase until another explicit switch.</summary>
    [Test]
    public void SwitchTo_PersistsAndDoesNotRunTwiceInOnePhase()
    {
        var scheduler = Create();
        var order = new List<string>();
        IEnumerator<IYieldInstruction> Routine()
        {
            order.Add("start");
            yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
            order.Add("fixed-first");
            yield return new WaitOneFrame();
            order.Add("fixed-second");
            yield return new SwitchTo(CoroutineExecutionStage.Deferred);
            order.Add("deferred");
            yield return new SwitchTo(CoroutineExecutionStage.Update);
            order.Add("update");
        }
        var handle = scheduler.Run(Routine());
        scheduler.Update(CoroutineExecutionStage.Update, 1, 1);
        Assert.That(order, Is.EqualTo(new[] { "start" }));
        scheduler.Update(CoroutineExecutionStage.FixedUpdate, 1, 1);
        scheduler.Update(CoroutineExecutionStage.Update, 1, 1);
        Assert.That(order, Is.EqualTo(new[] { "start", "fixed-first" }));
        scheduler.Update(CoroutineExecutionStage.FixedUpdate, 1, 1);
        scheduler.Update(CoroutineExecutionStage.Deferred, 1, 1);
        scheduler.Update(CoroutineExecutionStage.Update, 1, 1);
        Assert.That(order, Is.EqualTo(new[] { "start", "fixed-first", "fixed-second", "deferred", "update" }));
        Assert.That(scheduler.IsCoroutineAlive(handle), Is.False);
    }

    /// <summary>Switching to the current phase still yields until its next advancement.</summary>
    [Test]
    public void SamePhaseSwitch_StillWaitsForNextAdvancement()
    {
        var scheduler = Create();
        var steps = 0;
        IEnumerator<IYieldInstruction> Routine()
        {
            steps++;
            yield return new SwitchTo(CoroutineExecutionStage.Update);
            steps++;
            yield return new SwitchTo(CoroutineExecutionStage.Update);
            steps++;
        }
        scheduler.Run(Routine());
        Assert.That(steps, Is.EqualTo(1));
        scheduler.Update();
        Assert.That(steps, Is.EqualTo(2));
        scheduler.Update();
        Assert.That(steps, Is.EqualTo(3));
    }

    /// <summary>Host pause applies uniformly to game tasks in every phase, while independent UI tasks can opt out.</summary>
    [TestCase(CoroutineExecutionStage.Update)]
    [TestCase(CoroutineExecutionStage.FixedUpdate)]
    [TestCase(CoroutineExecutionStage.Deferred)]
    public void HostPause_FreezesGameAcrossAllPhases(CoroutineExecutionStage stage)
    {
        var scheduler = Create();
        var game = scheduler.Run(Wait(), stage: stage);
        var ui = scheduler.Run(Wait(), stage: stage, ignorePause: true);
        scheduler.Update(stage, 1, 1, isPaused: true);
        Assert.That(scheduler.IsCoroutineAlive(game), Is.True);
        Assert.That(scheduler.IsCoroutineAlive(ui), Is.False);
        scheduler.Update(stage, 1, 1, isPaused: false);
        Assert.That(scheduler.IsCoroutineAlive(game), Is.False);
    }

    /// <summary>Cancellation cleanup does not depend on advancing gameplay while paused.</summary>
    [Test]
    public void Cancellation_IsDrainedEvenWhenHostPaused()
    {
        var scheduler = Create();
        using var source = new CancellationTokenSource();
        var handle = scheduler.Run(Wait(), stage: CoroutineExecutionStage.FixedUpdate, cancellationToken: source.Token);
        source.Cancel();
        scheduler.Update(CoroutineExecutionStage.Update, 0, 0.1, isPaused: true);
        Assert.That(scheduler.IsCoroutineAlive(handle), Is.False);
        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
    }

    /// <summary>Only the driver thread can mutate runtime state; external callbacks use tokens instead.</summary>
    [Test]
    public void WorkerThreadControl_IsRejected()
    {
        var scheduler = Create();
        var handle = scheduler.Run(Wait());
        Task.Run(() => Assert.Throws<InvalidOperationException>(() => scheduler.Kill(handle))).GetAwaiter().GetResult();
        Assert.That(scheduler.IsCoroutineAlive(handle), Is.True);
    }

    /// <summary>An unavailable result is exposed by Join, but cannot be misreported as a successful Await.</summary>
    [Test]
    public void AwaitUnknown_FailsExplicitly()
    {
        var scheduler = Create();
        IEnumerator<IYieldInstruction> Routine() { yield return scheduler.Await(default); }
        var handle = scheduler.Run(Routine());
        scheduler.Update();
        Assert.That(scheduler.TryGetResult(handle, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
    }

    private static CoroutineScheduler Create() => new(new FakeTimeSource(), supportedStages:
        new[] { CoroutineExecutionStage.Update, CoroutineExecutionStage.FixedUpdate, CoroutineExecutionStage.Deferred });
    private static IEnumerator<IYieldInstruction> Wait() { yield return new WaitOneFrame(); }
}
