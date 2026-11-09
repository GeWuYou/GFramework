// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;
using GFramework.Core.Events;
using NUnit.Framework;

namespace GFramework.Core.Tests.Events;

/// <summary>Regressions for notification channel and subscription ownership boundaries.</summary>
[TestFixture]
public sealed class EventOwnershipTests
{
    /// <summary>Default publication sees all registrations and preserves equal-priority insertion order.</summary>
    [Test]
    public void MixedRegistrations_ShareStablePriorityChannel()
    {
        var bus = new EventBus();
        var seen = new List<int>();
        using var first = bus.Subscribe<int>(_ => seen.Add(1));
        using var high = bus.Subscribe<int>(_ => seen.Add(2), 10);
        using var last = bus.Subscribe<int>(_ => seen.Add(3));
        bus.Publish(0);
        Assert.That(seen, Is.EqualTo([2, 1, 3]));
    }

    /// <summary>Disposing one of two equal callbacks leaves the other subscription active.</summary>
    [Test]
    public void DuplicateDelegates_HaveIndependentTokens()
    {
        var bus = new EventBus();
        var calls = 0;
        void Handler(int _) => calls++;
        var first = bus.Subscribe<int>(Handler);
        using var second = bus.Subscribe<int>(Handler);
        first.Dispose();
        first.Dispose();
        bus.Publish(0);
        Assert.That(calls, Is.EqualTo(1));
    }

    /// <summary>Nested publication has a separate handled context and cannot reset its parent.</summary>
    [Test]
    public void NestedPublication_DoesNotResetHandledFlag()
    {
        var bus = new EventBus();
        var seen = new List<int>();
        using var high = bus.SubscribeWithContext<int>(context =>
        {
            seen.Add(context.Data);
            if (context.Data == 1)
            {
                context.MarkAsHandled();
                bus.Publish(2, EventPropagation.UntilHandled);
            }
        }, 10);
        using var low = bus.Subscribe<int>(value => seen.Add(value * 10));
        bus.Publish(1, EventPropagation.UntilHandled);
        Assert.That(seen, Is.EqualTo([1, 2, 20]));
    }

    /// <summary>Membership changes while publishing apply to the next snapshot.</summary>
    [Test]
    public void CallbackChanges_DoNotAlterActiveSnapshot()
    {
        var bus = new EventBus();
        var seen = new List<int>();
        IDisposable? second = null;
        using var scope = new SubscriptionScope();
        scope.Add(bus.Subscribe<int>(_ =>
        {
            seen.Add(1);
            second!.Dispose();
            scope.Add(bus.Subscribe<int>(_ => seen.Add(3)));
        }, 10));
        second = bus.Subscribe<int>(_ => seen.Add(2));
        scope.Add(second);
        bus.Publish(0);
        Assert.That(seen, Is.EqualTo([1, 2]));
        seen.Clear();
        bus.Publish(0);
        Assert.That(seen, Is.EqualTo([1, 3]));
    }

    /// <summary>Failures stop delivery and retain the original exception identity.</summary>
    [Test]
    public void CallbackException_PropagatesAndStopsDelivery()
    {
        var bus = new EventBus();
        var error = new InvalidOperationException("observer");
        var laterCalled = false;
        using var first = bus.Subscribe<int>(_ => throw error, 1);
        using var later = bus.Subscribe<int>(_ => laterCalled = true);
        Assert.That(Assert.Throws<InvalidOperationException>(() => bus.Publish(0)), Is.SameAs(error));
        Assert.That(laterCalled, Is.False);
    }

    /// <summary>A scope attempts every detachment even when one fails and is terminal after disposal.</summary>
    [Test]
    public void Scope_AttemptsAllTokensAndDisposesLateAdditions()
    {
        var scope = new SubscriptionScope();
        var detached = 0;
        scope.Add(new Subscription(() => detached++));
        scope.Add(new Subscription(() => throw new InvalidOperationException("detach")));
        Assert.Throws<AggregateException>(scope.Dispose);
        scope.Dispose();
        scope.Add(new Subscription(() => detached++));
        Assert.That(detached, Is.EqualTo(2));
    }

    /// <summary>One observer's disposal must not break other observers of an event composition.</summary>
    [Test]
    public void OrEvent_SeparatesListenerAndSourceLifetime()
    {
        var source = new Event<int>();
        using var composed = new OrEvent().Or(source);
        var detached = composed.Register(() => { });
        var calls = 0;
        using var active = composed.Register(() => calls++);
        detached.Dispose();
        source.Trigger(0);
        Assert.That(calls, Is.EqualTo(1));
        composed.Dispose();
        source.Trigger(0);
        Assert.That(calls, Is.EqualTo(1));
    }
    /// <summary>Weak lifetime follows the target rather than an ephemeral delegate allocation.</summary>
    [Test]
    public void WeakEvent_KeepsLiveTargetsAndDetachesMulticast()
    {
        var notification = new WeakEvent<int>();
        var first = new WeakListener();
        var second = new WeakListener();
        Action<int> callbacks = first.Receive;
        callbacks += second.Receive;
        using var token = notification.Register(callbacks);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        notification.Trigger(1);
        Assert.That(first.Calls, Is.EqualTo(1));
        Assert.That(second.Calls, Is.EqualTo(1));
        notification.UnRegister(callbacks);
        notification.Trigger(1);
        Assert.That(first.Calls, Is.EqualTo(1));
        Assert.That(second.Calls, Is.EqualTo(1));
        GC.KeepAlive(first);
        GC.KeepAlive(second);
    }

    /// <summary>Owning the subscription token does not keep the subscriber target alive.</summary>
    [Test]
    public void WeakEvent_TokenDoesNotRootTarget()
    {
        var notification = new WeakEvent<int>();
        var (target, token) = RegisterTransient(notification);
        using var subscription = token;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.That(target.TryGetTarget(out _), Is.False);
        Assert.DoesNotThrow(() => notification.Trigger(1));
    }

    /// <summary>Prevent test-stack lifetime extension from disguising a strong subscriber reference.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (WeakReference<WeakListener> Target, IDisposable Token) RegisterTransient(WeakEvent<int> notification)
    {
        var target = new WeakListener();
        return (new WeakReference<WeakListener>(target), notification.Register(target.Receive));
    }

    /// <summary>Observable instance target for weak registration lifetime.</summary>
    private sealed class WeakListener
    {
        /// <summary>Deliveries observed.</summary>
        public int Calls
        {
            get; private set;
        }
        /// <summary>Receive a notification.</summary>
        /// <param name="_">Ignored notification value required by the delegate signature.</param>
        public void Receive(int _) => Calls++;
    }

}
