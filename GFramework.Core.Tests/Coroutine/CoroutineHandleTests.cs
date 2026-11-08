// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>Behavioral identity tests: cross-runtime isolation and generation-safe slot reuse.</summary>
[TestFixture]
public sealed class CoroutineHandleTests
{
    /// <summary>The default value never identifies an allocated task.</summary>
    [Test]
    public void DefaultHandle_IsInvalid()
    {
        Assert.That(default(CoroutineHandle).IsValid, Is.False);
    }

    /// <summary>Recycled slots must reject a previously issued identity.</summary>
    [Test]
    public void ReusedSlot_RejectsStaleHandle()
    {
        var scheduler = new CoroutineScheduler(new FakeTimeSource(), initialCapacity: 1);
        var previous = scheduler.Run(Wait());
        scheduler.Kill(previous);
        var current = scheduler.Run(Wait());
        Assert.Multiple(() =>
        {
            Assert.That(previous, Is.Not.EqualTo(current));
            Assert.That(scheduler.Kill(previous), Is.False);
            Assert.That(scheduler.IsCoroutineAlive(current), Is.True);
            Assert.That(scheduler.AllocatedSlotCount, Is.EqualTo(1));
            Assert.That(previous.SchedulerId, Is.EqualTo(current.SchedulerId));
        });
    }

    /// <summary>Scheduler identities are independent from the old 4-bit host table.</summary>
    [Test]
    public void MoreThanFifteenSchedulers_HaveIndependentIdentities()
    {
        var ids = new HashSet<long>();
        for (var index = 0; index < 40; index++)
        {
            var scheduler = new CoroutineScheduler(new FakeTimeSource());
            var handle = scheduler.Run(Wait());
            Assert.That(ids.Add(handle.SchedulerId), Is.True);
            Assert.That(handle.SchedulerId, Is.EqualTo(scheduler.SchedulerId));
        }
    }

    /// <summary>A foreign handle cannot stop a local task even when both occupy the first slot.</summary>
    [Test]
    public void ForeignIdentity_CannotControlLocalTask()
    {
        var first = new CoroutineScheduler(new FakeTimeSource());
        var second = new CoroutineScheduler(new FakeTimeSource());
        var firstHandle = first.Run(Wait());
        var secondHandle = second.Run(Wait());
        Assert.That(second.Kill(firstHandle), Is.False);
        Assert.That(second.IsCoroutineAlive(secondHandle), Is.True);
        var copy = firstHandle;
        Assert.That(copy == firstHandle, Is.True);
        Assert.That(copy.GetHashCode(), Is.EqualTo(firstHandle.GetHashCode()));
        Assert.That(copy.Equals((object)firstHandle), Is.True);
        Assert.That(copy.Equals("other"), Is.False);
    }

    private static IEnumerator<IYieldInstruction> Wait()
    {
        yield return new Delay(100);
    }
}
