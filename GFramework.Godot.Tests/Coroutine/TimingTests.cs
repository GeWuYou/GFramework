// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;

namespace GFramework.Godot.Tests.Coroutine;

/// <summary>Observes the production adapter's managed scheduling and registry without native Node construction.</summary>
[TestFixture]
[NonParallelizable]
public sealed class TimingTests
{
    private Timing _timing = null!;

    /// <summary>Creates a managed host because native Node construction requires a running engine.</summary>
    [SetUp]
    public void SetUp() => _timing = CreateHost();

    /// <summary>Removes only this test's registry identity and pending work.</summary>
    [TearDown]
    public void TearDown() => _timing.DisposeForTests();

    /// <summary>All convenience segments share task identity and completion tracking.</summary>
    [Test]
    public void Segments_Should_Share_One_Scheduler()
    {
        var scheduler = _timing.GetSchedulerForTests(Segment.Process);
        Assert.That(_timing.GetSchedulerForTests(Segment.PhysicsProcess), Is.SameAs(scheduler));
        Assert.That(_timing.GetSchedulerForTests(Segment.ProcessIgnorePause), Is.SameAs(scheduler));
        Assert.That(_timing.GetSchedulerForTests(Segment.DeferredProcess), Is.SameAs(scheduler));
    }

    /// <summary>Game Update freezes while UI Update remains active.</summary>
    [Test]
    public void ProcessPause_Should_Freeze_Game_But_Advance_Ui()
    {
        var completed = new List<string>();
        var game = _timing.RunCoroutineOnInstance(AfterFrame(() => completed.Add("game")));
        var ui = _timing.RunCoroutineOnInstance(AfterFrame(() => completed.Add("ui")), Segment.ProcessIgnorePause);
        _timing.AdvanceProcessFrameForTests(paused: true);
        Assert.That(completed, Is.EqualTo(["ui"]));
        Assert.That(_timing.GetSchedulerForTests(Segment.Process).IsCoroutineAlive(game), Is.True);
        Assert.That(_timing.GetSchedulerForTests(Segment.Process).IsCoroutineAlive(ui), Is.False);
        _timing.AdvanceProcessFrameForTests(paused: false);
        Assert.That(completed, Is.EqualTo(["ui", "game"]));
    }

    /// <summary>A physics tick cannot consume an ordinary Update wait.</summary>
    [Test]
    public void Physics_Should_Advance_Only_Physics_Stage()
    {
        var completed = new List<string>();
        _timing.RunCoroutineOnInstance(AfterFrame(() => completed.Add("process")));
        _timing.RunCoroutineOnInstance(AfterFrame(() => completed.Add("physics")), Segment.PhysicsProcess);
        _timing.AdvancePhysicsFrameForTests();
        Assert.That(completed, Is.EqualTo(["physics"]));
        Assert.That(_timing.ProcessCoroutines, Is.EqualTo(1));
        Assert.That(_timing.PhysicsCoroutines, Is.Zero);
    }

    /// <summary>The Always-processing Timing host must still freeze game physics on scene pause.</summary>
    [Test]
    public void PhysicsPause_Should_Freeze_Game_Physics()
    {
        var completed = false;
        _timing.RunCoroutineOnInstance(AfterFrame(() => completed = true), Segment.PhysicsProcess);
        _timing.AdvancePhysicsFrameForTests(paused: true);
        Assert.That(completed, Is.False);
        _timing.AdvancePhysicsFrameForTests(paused: false);
        Assert.That(completed, Is.True);
    }

    /// <summary>The controlled driver preserves production Update-before-idle-dispatch ordering.</summary>
    [Test]
    public void Deferred_Should_Follow_Process()
    {
        var order = new List<string>();
        _timing.RunCoroutineOnInstance(AfterFrame(() => order.Add("process")));
        _timing.RunCoroutineOnInstance(AfterFrame(() => order.Add("deferred")), Segment.DeferredProcess);
        _timing.AdvanceProcessFrameForTests(paused: false);
        Assert.That(order, Is.EqualTo(["process", "deferred"]));
    }

    /// <summary>Explicit switching keeps identity and stays at the destination until another switch.</summary>
    [Test]
    public void SwitchTo_Should_Move_Between_Stages_Without_Losing_Handle()
    {
        var order = new List<string>();
        IEnumerator<IYieldInstruction> Flow()
        {
            yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
            order.Add("physics");
            yield return new WaitOneFrame();
            order.Add("physics-again");
            yield return new SwitchTo(CoroutineExecutionStage.Deferred);
            order.Add("deferred");
            yield return new SwitchTo(CoroutineExecutionStage.Update);
            order.Add("update");
        }

        var handle = _timing.RunCoroutineOnInstance(Flow());
        _timing.AdvanceProcessFrameForTests(paused: false);
        Assert.That(order, Is.Empty);
        _timing.AdvancePhysicsFrameForTests();
        Assert.That(order, Is.EqualTo(["physics"]));
        Assert.That(Timing.TryGetCoroutineSnapshot(handle, out var snapshot), Is.True);
        Assert.That(snapshot.ExecutionStage, Is.EqualTo(CoroutineExecutionStage.FixedUpdate));
        _timing.AdvanceProcessFrameForTests(paused: false);
        Assert.That(order.Count, Is.EqualTo(1));
        _timing.AdvancePhysicsFrameForTests();
        _timing.AdvanceProcessFrameForTests(paused: false);
        Assert.That(order, Is.EqualTo(["physics", "physics-again", "deferred"]));
        _timing.AdvanceProcessFrameForTests(paused: false);
        Assert.That(order[^1], Is.EqualTo("update"));
        Assert.That(_timing.GetSchedulerForTests(Segment.Process).TryGetCompletionStatus(handle, out var status), Is.True);
        Assert.That(status, Is.EqualTo(CoroutineCompletionStatus.Completed));
    }

    /// <summary>A UI task retains ignore-pause policy after switching into physics and deferred stages.</summary>
    [Test]
    public void UiPausePolicy_Should_Survive_Stage_Switches()
    {
        var completed = new global::System.Runtime.CompilerServices.StrongBox<bool>(value: false);
        IEnumerator<IYieldInstruction> Flow()
        {
            yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
            yield return new SwitchTo(CoroutineExecutionStage.Deferred);
            completed.Value = true;
        }

        _timing.RunCoroutineOnInstance(Flow(), Segment.ProcessIgnorePause);
        _timing.AdvancePhysicsFrameForTests(paused: true);
        _timing.AdvanceProcessFrameForTests(paused: true);
        Assert.That(completed.Value, Is.True);
    }

    /// <summary>Paused host callbacks still drain external cancellation and execute iterator finally blocks.</summary>
    [Test]
    public void Cancellation_Should_Clean_Up_While_Physics_Is_Paused()
    {
        using var cancellation = new CancellationTokenSource();
        var disposed = false;
        IEnumerator<IYieldInstruction> Flow()
        {
            try
            {
                yield return new Delay(100);
            }
            finally { disposed = true; }
        }

        var handle = _timing.RunCoroutineOnInstance(Flow(), Segment.PhysicsProcess,
            cancellationToken: cancellation.Token);
        cancellation.Cancel();
        _timing.AdvancePhysicsFrameForTests(paused: true);
        Assert.That(disposed, Is.True);
        Assert.That(_timing.GetSchedulerForTests(Segment.Process).TryGetCompletionStatus(handle, out var status), Is.True);
        Assert.That(status, Is.EqualTo(CoroutineCompletionStatus.Cancelled));
    }

    /// <summary>Host teardown disposes descendants even if their parent waits in another stage.</summary>
    [Test]
    public void HostDisposal_Should_Cancel_Structured_Descendants()
    {
        var childDisposed = false;
        IEnumerator<IYieldInstruction> Child()
        {
            try
            {
                yield return new Delay(100);
            }
            finally { childDisposed = true; }
        }
        IEnumerator<IYieldInstruction> Parent(CoroutineContext context)
        {
            context.RunChild(Child(), cancellationToken: global::System.Threading.CancellationToken.None);
            yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
            yield return new Delay(100);
        }

        var handle = _timing.RunCoroutineOnInstance(Parent);
        Assert.That(_timing.GetSchedulerForTests(Segment.Process).ActiveCoroutineCount, Is.EqualTo(2));
        _timing.DisposeForTests();
        Assert.That(childDisposed, Is.True);
        Assert.That(Timing.KillCoroutine(handle), Is.False);
        Assert.That(Timing.GetInstance(handle.SchedulerId), Is.Null);
    }

    /// <summary>Cleanup callbacks cannot attach replacement work through a host that is already stopping.</summary>
    [Test]
    public void HostDisposal_Should_Reject_Reentrant_Starts()
    {
        var rejected = false;
        IEnumerator<IYieldInstruction> Flow()
        {
            try
            {
                yield return new Delay(100);
            }
            finally
            {
                try
                {
                    _timing.RunCoroutineOnInstance(WaitLong());
                }
                catch (InvalidOperationException) { rejected = true; }
            }
        }

        _timing.RunCoroutineOnInstance(Flow());
        _timing.DisposeForTests();
        Assert.That(rejected, Is.True);
    }

    /// <summary>Scheduler-qualified routing supports more than fifteen simultaneous hosts without collisions.</summary>
    [Test]
    public void Registry_Should_Support_More_Than_Fifteen_Hosts()
    {
        var hosts = new List<Timing>();
        try
        {
            for (var index = 0; index < 20; index++)
                hosts.Add(CreateHost());
            var handles = hosts.Select(host => host.RunCoroutineOnInstance(WaitLong())).ToArray();
            Assert.That(handles.Select(handle => handle.SchedulerId).Distinct().Count(), Is.EqualTo(20));
            Assert.That(Timing.KillCoroutine(handles[5]), Is.True);
            for (var index = 0; index < hosts.Count; index++)
            {
                Assert.That(hosts[index].GetSchedulerForTests(Segment.Process).IsCoroutineAlive(handles[index]),
                    Is.EqualTo(index != 5));
            }
        }
        finally
        {
            foreach (var host in hosts)
                host.DisposeForTests();
        }
    }

    /// <summary>Reinitializing an exited host cannot make its previous handles address replacement work.</summary>
    [Test]
    public void Reinitialization_Should_Replace_Scheduler_Identity()
    {
        var oldHandle = _timing.RunCoroutineOnInstance(WaitLong());
        _timing.DisposeForTests();
        _timing.InitializeForTests();
        var newHandle = _timing.RunCoroutineOnInstance(WaitLong());
        Assert.That(oldHandle.SchedulerId, Is.Not.EqualTo(newHandle.SchedulerId));
        Assert.That(Timing.KillCoroutine(oldHandle), Is.False);
        Assert.That(Timing.TryGetCoroutineSnapshot(newHandle, out _), Is.True);
    }

    /// <summary>Supervisor failures remain observable without terminating independent tasks.</summary>
    [Test]
    public void ResultFacade_Should_Retain_Failure_Without_Cancelling_Independent_Work()
    {
        var expected = new InvalidOperationException("observed failure");
        IEnumerator<IYieldInstruction> Failing()
        {
            yield return new WaitOneFrame();
            throw expected;
        }

        var failure = _timing.RunCoroutineOnInstance(Failing());
        var independent = _timing.RunCoroutineOnInstance(WaitLong());
        var completion = Timing.WaitForResultAsync(failure);
        _timing.AdvanceProcessFrameForTests(paused: false);
        Assert.That(Timing.TryGetCoroutineResult(failure, out var result), Is.True);
        Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
        Assert.That(result.Exception, Is.SameAs(expected));
        Assert.That(completion.IsCompletedSuccessfully, Is.True);
        Assert.That(completion.Result, Is.EqualTo(result));
        Assert.That(Timing.TryGetCoroutineSnapshot(independent, out _), Is.True);
    }

    /// <summary>Creates a test host without invoking Godot's unavailable native Node constructor.</summary>
    /// <returns>The registered managed host.</returns>
    private static Timing CreateHost()
    {
        var host = (Timing)RuntimeHelpers.GetUninitializedObject(typeof(Timing));
        host.InitializeForTests();
        return host;
    }

    /// <summary>Creates a one-frame wait followed by observable work.</summary>
    /// <param name="action">The observable action.</param>
    /// <returns>The test iterator.</returns>
    private static IEnumerator<IYieldInstruction> AfterFrame(Action action)
    {
        yield return new WaitOneFrame();
        action();
    }

    /// <summary>Keeps a task alive long enough for ownership and routing observations.</summary>
    /// <returns>The test iterator.</returns>
    private static IEnumerator<IYieldInstruction> WaitLong()
    {
        yield return new Delay(100);
    }
}
