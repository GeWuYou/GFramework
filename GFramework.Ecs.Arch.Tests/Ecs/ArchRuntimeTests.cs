// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Arch.System;
using GFramework.Core.Abstractions.Bases;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Ecs.Arch.Abstractions;

namespace GFramework.Ecs.Arch.Tests.Ecs;

/// <summary>Native phase, partial failure, ownership and statistics contracts through the public runtime.</summary>
[TestFixture]
public sealed class ArchRuntimeTests
{
    /// <summary>All roots finish each phase before the next phase begins, preserving stable priority order.</summary>
    [Test]
    public void NativeRootsAndGroupsRunAllThreePhases()
    {
        using var world = World.Create();
        var calls = new List<string>();
        using var first = new Probe(world, calls, "first", 1);
        using var second = new Probe(world, calls, "second", 0);
        using var group = new Group<float>("nested", new Probe(world, calls, "child", 2));
        var runtime = new ArchRuntime(world, [first, second, group]);
        runtime.Initialize();
        calls.Clear();
        runtime.Update(1);
        Assert.That(calls, Is.EqualTo([ "second:before", "child:before", "first:before",
            "second:update", "child:update", "first:update", "second:after", "child:after", "first:after" ]));
        runtime.Stop();
        Assert.That(first.Disposals, Is.Zero, "The caller, not the runtime, owns native resources.");
        Assert.That(calls.TakeLast(2), Is.EqualTo(["first:stop", "second:stop"]));
    }

    /// <summary>Each possible failing phase retains partial timing and closes the update boundary.</summary>
    [TestCase(ArchUpdatePhase.BeforeUpdate)]
    [TestCase(ArchUpdatePhase.Update)]
    [TestCase(ArchUpdatePhase.AfterUpdate)]
    public void FailedPhaseIsVisibleAndDoesNotResume(ArchUpdatePhase phase)
    {
        using var world = World.Create();
        using var first = new Probe(world, [], "first", 0) { Failure = phase };
        using var second = new Probe(world, [], "second", 0);
        var runtime = new ArchRuntime(world, [first, second], new ArchOptions { EnableStatistics = true });
        runtime.Initialize();
        Assert.Throws<ArithmeticException>(() => runtime.Update(1));
        var snapshot = runtime.GetStatistics()!;
        Assert.That(snapshot.FailedFrames, Is.EqualTo(1));
        Assert.That(snapshot.CompletedFrames, Is.Zero);
        Assert.That(snapshot.Systems[0].FailedPhase, Is.EqualTo(phase));
        Assert.That(snapshot.Systems[0].Completed, Is.False);
        Assert.That(snapshot.Systems[0].BeforeUpdateDuration, Is.Not.Null);
        if (phase == ArchUpdatePhase.BeforeUpdate)
            Assert.That(snapshot.Systems[1].BeforeUpdateDuration, Is.Null);
        Assert.Throws<InvalidOperationException>(() => runtime.Update(1));
        runtime.Stop();
        runtime.Stop();
        Assert.That(first.Stops, Is.EqualTo(1));
        Assert.That(second.Stops, Is.EqualTo(1));
    }

    /// <summary>Failed initialization still stops every attempted system, and stop errors never skip predecessors.</summary>
    [Test]
    public void PartialInitializationAndStopErrorsRetainCleanup()
    {
        using var world = World.Create();
        using var first = new Probe(world, [], "first", 0) { FailStop = true };
        using var second = new Probe(world, [], "second", 0) { FailStart = true, FailStop = true };
        using var untouched = new Probe(world, [], "third", 0);
        var runtime = new ArchRuntime(world, [first, second, untouched]);
        Assert.Throws<ArithmeticException>(runtime.Initialize);
        var error = Assert.Throws<AggregateException>(runtime.Stop)!;
        Assert.That(error.InnerExceptions, Has.Count.EqualTo(2));
        Assert.That(first.Stops, Is.EqualTo(1));
        Assert.That(second.Stops, Is.EqualTo(1));
        Assert.That(untouched.Stops, Is.Zero);
        Assert.That(Assert.Throws<AggregateException>(runtime.Stop), Is.SameAs(error));
        Assert.Throws<InvalidOperationException>(runtime.Initialize);
    }

    /// <summary>Snapshots observe entity changes, remain immutable and cannot expose mutable runtime arrays.</summary>
    [Test]
    public void StatisticsAreOptInAndSnapshotsAreIndependent()
    {
        using var world = World.Create();
        using var system = new Probe(world, [], "probe", 0);
        var options = new ArchOptions { EnableStatistics = true };
        var runtime = new ArchRuntime(world, [system], options);
        options.EnableStatistics = false;
        runtime.Initialize();
        var entity = world.Create();
        runtime.Update(1);
        var first = runtime.GetStatistics()!;
        world.Destroy(entity);
        runtime.Update(1);
        var second = runtime.GetStatistics()!;
        Assert.That(first.EntityCount, Is.EqualTo(1));
        Assert.That(first.CompletedFrames, Is.EqualTo(1));
        Assert.That(second.EntityCount, Is.Zero);
        Assert.That(second.CompletedFrames, Is.EqualTo(2));
        Assert.That(first.Systems[0].Completed, Is.True);
        Assert.That(first.Systems[0].UpdateDuration, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ArchSystemStatistics>)first.Systems).Clear());
        runtime.Stop();
        var disabled = new ArchRuntime(world, []);
        disabled.Initialize();
        disabled.Update(1);
        Assert.That(disabled.GetStatistics(), Is.Null);
        disabled.Stop();
    }

    /// <summary>Callbacks cannot reenter stop and foreign threads cannot touch the native runtime.</summary>
    [Test]
    public void HostThreadAndReentryAreEnforced()
    {
        using var world = World.Create();
        using var system = new Probe(world, [], "probe", 0);
        var runtime = new ArchRuntime(world, [system]);
        runtime.Initialize();
        Task.Run(() => Assert.Throws<InvalidOperationException>(() => runtime.Update(1))).GetAwaiter().GetResult();
        system.OnUpdateAction = () => runtime.Stop();
        Assert.Throws<InvalidOperationException>(() => runtime.Update(1));
        runtime.Stop();
        Assert.That(system.Stops, Is.EqualTo(1));
    }

    /// <summary>Duplicate root instances cannot start or stop twice.</summary>
    [Test]
    public void DuplicateRootsAreRejected()
    {
        using var world = World.Create();
        using var system = new Probe(world, [], "probe", 0);
        Assert.Throws<ArgumentException>(() => new ArchRuntime(world, [system, system]));
    }

    /// <summary>Optional adapter initialization is shared by its native and framework entrypoints.</summary>
    [Test]
    public void AdapterInitializationEntrypointsShareOneAttempt()
    {
        using var world = World.Create();
        using var adapter = new InitializationProbe(world);
        adapter.Initialize();
        ((ISystem<float>)adapter).Initialize();
        Assert.That(adapter.Starts, Is.EqualTo(1));
    }

    /// <summary>Observes the optional bridge's common initialization path.</summary>
    private sealed class InitializationProbe(World world) : ArchSystemAdapter<float>(world)
    {
        /// <summary>Number of actual initialization hooks.</summary>
        public int Starts
        {
            get; private set;
        }
        /// <inheritdoc />
        protected override void OnArchInitialize() => Starts++;
    }

    /// <summary>A native BaseSystem with independent business-stop and final-release observations.</summary>
    private sealed class Probe(World world, List<string> calls, string name, int priority)
        : BaseSystem<World, float>(world), IPrioritized, IDestroyable
    {
        /// <inheritdoc />
        public int Priority => priority;
        /// <summary>The callback selected to fail.</summary>
        public ArchUpdatePhase? Failure
        {
            get; init;
        }
        /// <summary>Whether initialization fails.</summary>
        public bool FailStart
        {
            get; init;
        }
        /// <summary>Whether business stop fails.</summary>
        public bool FailStop
        {
            get; init;
        }
        /// <summary>Observed business stop calls.</summary>
        public int Stops
        {
            get; private set;
        }
        /// <summary>Observed final disposal calls.</summary>
        public int Disposals
        {
            get; private set;
        }
        /// <summary>Optional reentry probe.</summary>
        public Action? OnUpdateAction
        {
            get; set;
        }
        /// <inheritdoc />
        public override void Initialize()
        {
            calls.Add(name + ":start");
            if (FailStart)
                throw new ArithmeticException("start");
        }
        /// <inheritdoc />
        public override void BeforeUpdate(in float t) => Phase(ArchUpdatePhase.BeforeUpdate, "before");
        /// <inheritdoc />
        public override void Update(in float t)
        {
            OnUpdateAction?.Invoke();
            Phase(ArchUpdatePhase.Update, "update");
        }
        /// <inheritdoc />
        public override void AfterUpdate(in float t) => Phase(ArchUpdatePhase.AfterUpdate, "after");
        /// <inheritdoc />
        public void Destroy()
        {
            Stops++;
            calls.Add(name + ":stop");
            if (FailStop)
                throw new ArithmeticException("stop");
        }
        /// <inheritdoc />
        public override void Dispose() => Disposals++;
        /// <summary>Records one native callback before optionally failing.</summary>
        private void Phase(ArchUpdatePhase phase, string label)
        {
            calls.Add(name + ":" + label);
            if (Failure == phase)
                throw new ArithmeticException(label);
        }
    }
}
