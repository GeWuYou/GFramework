// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.State;
using GFramework.Core.Logging.Appenders;
using GFramework.Core.Pause;
using GFramework.Core.Property;
using GFramework.Core.State;
using GFramework.Core.StateManagement;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Events;

/// <summary>Exercises notification isolation through real token, state, history, binding and flush operations.</summary>
[TestFixture]
public sealed class CommittedStateNotificationTests
{
    private static readonly int[] ExpectedHistory = [2, 0, 2];

    /// <summary>Observers cannot hide the token needed to undo an accepted pause request.</summary>
    [Test]
    public async Task PauseTokenRemainsReturnableAfterAnObserverThrows()
    {
        var manager = new PauseStackManager(NullLogger.Instance);
        var observed = 0;
        manager.OnPauseStateChanged += (_, _) => throw new InvalidOperationException("observer");
        manager.OnPauseStateChanged += (_, _) => observed++;
        var token = manager.Push("dialog");
        Assert.That(manager.IsPaused(), Is.True);
        Assert.That(manager.Pop(token), Is.True);
        Assert.That(manager.IsPaused(), Is.False);
        Assert.That(observed, Is.EqualTo(2));
        await manager.DestroyAsync();
    }

    /// <summary>Observers cannot poison a successfully entered state or suppress later transitions.</summary>
    [Test]
    public async Task StateNotificationFailureDoesNotFaultTransitionsOrHistoryReturn()
    {
        var machine = new StateMachine(observerLogger: NullLogger.Instance);
        machine.Register(new First()).Register(new Second());
        var observed = 0;
        machine.StateChanged += (_, _) => throw new InvalidOperationException("observer");
        machine.StateChanged += (_, _) => observed++;
        Assert.That(await machine.ChangeToAsync<First>(), Is.True);
        Assert.That(await machine.ChangeToAsync<Second>(), Is.True);
        Assert.That(await machine.GoBackAsync(), Is.True);
        Assert.That(machine.Current, Is.TypeOf<First>());
        Assert.That(machine.Fault, Is.Null);
        Assert.That(observed, Is.EqualTo(3));
        await machine.DestroyAsync();
    }

    /// <summary>History, selection views and later subscribers remain coherent after a bad UI observer.</summary>
    [Test]
    public void StoreAndSelectionsContinueAfterObserversThrow()
    {
        var store = new Store<int>(0, historyCapacity: 4, observerLogger: NullLogger.Instance);
        store.RegisterReducer<int>((state, amount) => state + amount);
        var selection = new StoreSelection<int, int>(store, value => value, observerLogger: NullLogger.Instance);
        var received = new List<int>();
        selection.Register(_ => throw new InvalidOperationException("selection observer"));
        selection.Register(received.Add);
        store.Subscribe(_ => throw new InvalidOperationException("store observer"));
        var later = 0;
        store.Subscribe(_ => later++);
        store.Dispatch(2);
        store.Undo();
        store.Redo();
        Assert.That(store.State, Is.EqualTo(2));
        Assert.That(received, Is.EqualTo(ExpectedHistory));
        Assert.That(later, Is.EqualTo(3));
        store.RegisterReducer<string>((_, _) => throw new InvalidOperationException("reducer"));
        Assert.Throws<InvalidOperationException>(() => store.Dispatch("fail"));
        Assert.That(store.State, Is.EqualTo(2));
    }

    /// <summary>Setting a property commits its value even if an observation fails.</summary>
    [Test]
    public void BindablePropertyAttemptsEveryObserverAfterCommit()
    {
        var property = new BindableProperty<int>(observerLogger: NullLogger.Instance);
        var received = 0;
        property.Register(_ => throw new InvalidOperationException("observer"));
        property.Register(value => received = value);
        property.Value = 7;
        Assert.That(property.Value, Is.EqualTo(7));
        Assert.That(received, Is.EqualTo(7));
    }

    /// <summary>Completion and diagnostic callbacks cannot change a completed flush into an exception.</summary>
    [Test]
    public void FlushResultSurvivesCompletionAndDiagnosticFailures()
    {
        using var logs = new AsyncLogAppender(new EmptyAppender(),
            processingErrorHandler: _ => throw new InvalidOperationException("diagnostic"));
        var observed = 0;
        logs.OnFlushCompleted += (_, _) => throw new InvalidOperationException("observer");
        logs.OnFlushCompleted += (_, args) => { if (args.Success) observed++; };
        Assert.That(logs.Flush(TimeSpan.FromSeconds(1)), Is.True);
        Assert.That(observed, Is.EqualTo(1));
    }

    /// <summary>Thread-neutral state callbacks; real entry/exit failures remain covered by ownership tests.</summary>
    private class First : IState
    {
        /// <inheritdoc />
        public void OnEnter(IState? from)
        {
        }
        /// <inheritdoc />
        public void OnExit(IState? to)
        {
        }
        /// <inheritdoc />
        public bool CanTransitionTo(IState target) => true;
    }
    /// <summary>A second state key for transition and history checks.</summary>
    private sealed class Second : First;

    /// <summary>An empty log sink isolates flush completion behavior from I/O.</summary>
    private sealed class EmptyAppender : ILogAppender
    {
        /// <inheritdoc />
        public void Append(LogEntry entry)
        {
        }
        /// <inheritdoc />
        public void Flush()
        {
        }
        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
