// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.State;
using GFramework.Core.State;
using NUnit.Framework;

namespace GFramework.Core.Tests.State;

/// <summary>Regressions for serialized transitions, successful history and fault cleanup.</summary>
[TestFixture]
public sealed class StateMachineOwnershipTests
{
    /// <summary>History return uses the same outgoing guard without consuming a rejected entry.</summary>
    [Test]
    public async Task RejectedReturn_PreservesHistoryAndNotifications()
    {
        await using var machine = new StateMachine();
        var first = new First();
        var second = new Second();
        machine.Register(first).Register(second);
        var events = 0;
        machine.StateChanged += (_, _) => events++;
        await machine.ChangeToAsync<First>();
        await machine.ChangeToAsync<Second>();
        second.Allow = false;
        Assert.That(await machine.GoBackAsync(), Is.False);
        Assert.That(machine.GetPreviousState(), Is.SameAs(first));
        second.Allow = true;
        Assert.That(await machine.GoBackAsync(), Is.True);
        Assert.That(machine.Current, Is.SameAs(first));
        Assert.That(events, Is.EqualTo(3));
    }

    /// <summary>Entry failure prevents further transitions but still cleans the partially entered state.</summary>
    [Test]
    public async Task FailedEntry_IsFaultedAndCleanedOnce()
    {
        var machine = new StateMachine();
        var state = new FailedEntry();
        machine.Register(state);
        await Assert.ThrowsAsync<InvalidOperationException>(() => machine.ChangeToAsync<FailedEntry>());
        Assert.That(machine.Fault, Is.SameAs(state.Error));
        await Assert.ThrowsAsync<InvalidOperationException>(() => machine.ChangeToAsync<FailedEntry>());
        await machine.StopAsync();
        await machine.StopAsync();
        Assert.That(state.Exits, Is.EqualTo(1));
        Assert.That(state.Stops, Is.EqualTo(1));
    }

    /// <summary>Stop waits for entry instead of racing exit with a still-running callback.</summary>
    [Test]
    public async Task Stop_WaitsForTransitionAndSharesCompletion()
    {
        var machine = new StateMachine();
        var state = new WaitingEntry();
        machine.Register(state);
        var transition = machine.ChangeToAsync<WaitingEntry>();
        await state.Entered.Task;
        var firstStop = machine.StopAsync().AsTask();
        var secondStop = machine.StopAsync().AsTask();
        Assert.That(firstStop, Is.SameAs(secondStop));
        Assert.That(firstStop.IsCompleted, Is.False);
        Assert.That(state.Exits, Is.EqualTo(0));
        state.Release.SetResult();
        await transition;
        await firstStop;
        Assert.That(state.Exits, Is.EqualTo(1));
        Assert.That(machine.Current, Is.Null);
    }

    /// <summary>Awaiting this machine from its own callback is rejected without a deadlock.</summary>
    [Test]
    public async Task Reentry_IsRejectedAndCanBeCleaned()
    {
        var machine = new StateMachine();
        machine.Register(new Reentrant(machine));
        await Assert.ThrowsAsync<InvalidOperationException>(() => machine.ChangeToAsync<Reentrant>());
        await machine.StopAsync();
    }

    /// <summary>History capacity keeps the newest entries without reversing them.</summary>
    [Test]
    public async Task HistoryCapacity_PreservesMostRecentFirstOrder()
    {
        await using var machine = new StateMachine(2);
        var first = new First();
        var second = new Second();
        var third = new Third();
        machine.Register(first).Register(second).Register(third);
        await machine.ChangeToAsync<First>();
        await machine.ChangeToAsync<Second>();
        await machine.ChangeToAsync<Third>();
        await machine.ChangeToAsync<First>();
        Assert.That(machine.GetStateHistory(), Is.EqualTo(new IState[] { third, second }));
        Assert.That(await machine.ChangeToAsync<First>(), Is.False);
    }

    /// <summary>A canceled waiter cannot cancel the active transition or poison the machine.</summary>
    [Test]
    public async Task CanceledWaiter_DoesNotCancelActiveTransition()
    {
        await using var machine = new StateMachine();
        var state = new WaitingEntry();
        machine.Register(state).Register(new First());
        var active = machine.ChangeToAsync<WaitingEntry>();
        await state.Entered.Task;
        using var cancellation = new CancellationTokenSource();
        var waiter = machine.ChangeToAsync<First>(cancellation.Token);
        cancellation.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(async () => await waiter.ConfigureAwait(false));
        state.Release.SetResult();
        await active;
        Assert.That(machine.Fault, Is.Null);
    }


    /// <summary>A child context captured during entry may issue a later operation once that entry has ended.</summary>
    [Test]
    public async Task CapturedCallbackContext_ExpiresAfterEntry()
    {
        await using var machine = new StateMachine();
        var entry = new CapturingEntry(machine);
        machine.Register(entry).Register(new First());
        await machine.ChangeToAsync<CapturingEntry>();
        entry.Release.SetResult();
        await entry.Child.ConfigureAwait(false);
        Assert.That(machine.Current, Is.TypeOf<First>());
    }

    /// <summary>Cancellation of a guard leaves the unchanged state usable, regardless of async implementation.</summary>
    [Test]
    public async Task CanceledGuard_DoesNotFaultUnchangedState()
    {
        await using var machine = new StateMachine();
        var guarded = new CancelableGuard();
        machine.Register(guarded).Register(new First());
        await machine.ChangeToAsync<CancelableGuard>();
        using var cancellation = new CancellationTokenSource();
        var transition = machine.ChangeToAsync<First>(cancellation.Token);
        await guarded.Checking.Task;
        cancellation.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(async () => await transition.ConfigureAwait(false));
        Assert.That(machine.Fault, Is.Null);
        Assert.That(machine.Current, Is.SameAs(guarded));
        guarded.Block = false;
        Assert.That(await machine.ChangeToAsync<First>(), Is.True);
    }

    /// <summary>Captures entry execution context without waiting for the child while inside entry.</summary>
    private sealed class CapturingEntry(StateMachine machine) : StateBase
    {
        /// <summary>Release the child only after the owning transition has completed.</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Child operation result observed by the test.</summary>
        public Task Child { get; private set; } = Task.CompletedTask;
        /// <inheritdoc />
        public override void OnEnter(IState? from)
        {
            Child = Task.Run(async () =>
            {
                await Release.Task.ConfigureAwait(false);
                await machine.ChangeToAsync<First>().ConfigureAwait(false);
            });
        }
    }

    /// <summary>A guard with a cancellable asynchronous wait before any state mutation.</summary>
    private sealed class CancelableGuard : AsyncStateBase
    {
        /// <summary>Signals the guard is actually waiting.</summary>
        public TaskCompletionSource Checking { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Whether the guard should wait for cancellation.</summary>
        public bool Block { get; set; } = true;
        /// <inheritdoc />
        public override async Task<bool> CanTransitionToAsync(IState target, CancellationToken cancellationToken = default)
        {
            if (!Block)
                return true;
            Checking.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }

    /// <summary>Simple states with controllable outgoing guards.</summary>
    private class First : StateBase
    {
        /// <summary>Whether to permit leaving.</summary>
        public bool Allow { get; set; } = true;
        /// <inheritdoc />
        public override bool CanTransitionTo(IState target) => Allow;
    }
    /// <summary>Distinct registration type.</summary>
    private sealed class Second : First;
    /// <summary>Distinct registration type.</summary>
    private sealed class Third : First;
    /// <summary>State acquiring a resource before entry fails.</summary>
    private sealed class FailedEntry : StateBase, IDestroyable
    {
        /// <summary>Stable original failure.</summary>
        public InvalidOperationException Error { get; } = new("entry");
        /// <summary>Terminal exits observed.</summary>
        public int Exits
        {
            get; private set;
        }
        /// <summary>Business stops observed.</summary>
        public int Stops
        {
            get; private set;
        }
        /// <inheritdoc />
        public override void OnEnter(IState? from) => throw Error;
        /// <inheritdoc />
        public override void OnExit(IState? to) => Exits++;
        /// <inheritdoc />
        public void Destroy() => Stops++;
    }
    /// <summary>Externally released entry barrier for deterministic overlap tests.</summary>
    private sealed class WaitingEntry : AsyncStateBase
    {
        /// <summary>Signals entry has begun.</summary>
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Allows entry to finish.</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Terminal exits observed.</summary>
        public int Exits
        {
            get; private set;
        }
        /// <inheritdoc />
        public override async Task OnEnterAsync(IState? from, CancellationToken cancellationToken = default)
        {
            Entered.SetResult();
            await Release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        /// <inheritdoc />
        public override Task OnExitAsync(IState? to, CancellationToken cancellationToken = default)
        {
            Exits++;
            return Task.CompletedTask;
        }
    }
    /// <summary>A callback must never wait for its own operation semaphore.</summary>
    private sealed class Reentrant(StateMachine machine) : AsyncStateBase
    {
        /// <inheritdoc />
        public override async Task OnEnterAsync(IState? from, CancellationToken cancellationToken = default) =>
            await machine.ChangeToAsync<Reentrant>(cancellationToken).ConfigureAwait(false);
    }
}
