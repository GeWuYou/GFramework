// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System;
using GFramework.Core.Events;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.Events;

/// <summary>Ensures notifications isolate every direct subscriber and even their diagnostic channel.</summary>
[TestFixture]
public sealed class ObserverNotificationTests
{
    /// <summary>A failing reporter cannot prevent a later direct callback from receiving the same payload.</summary>
    [Test]
    public void MulticastObserversAndDiagnosticFailuresAreIndependent()
    {
        var value = new object();
        var received = new List<object>();
        var errors = new List<Exception>();
        Action<object> callbacks = _ => throw new InvalidOperationException("observer");
        callbacks += received.Add;
        ObserverNotification.Notify(callbacks, value, reportError: error =>
        {
            errors.Add(error);
            throw new InvalidOperationException("diagnostic");
        });
        object[] expected = [value];
        Assert.That(received, Is.EqualTo(expected));
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0].Message, Is.EqualTo("observer"));
        Assert.DoesNotThrow(() => ObserverNotification.Notify<object>(observers: null, value));
    }

    /// <summary>A throwing host logger cannot prevent remaining direct notifications.</summary>
    [Test]
    public void LoggerFailureCannotEscapeOrBlockLaterObservers()
    {
        var received = false;
        Action<int> callbacks = _ => throw new InvalidOperationException("observer");
        callbacks += _ => received = true;
        ObserverNotification.Notify(callbacks, 1, new FailingLogger());
        Assert.That(received, Is.True);
    }

    /// <summary>Event delivery preserves sender, payload and order after a canceled observation.</summary>
    [Test]
    public void EventObserversDoNotCancelTheCommittedOperation()
    {
        var sender = new object();
        var args = EventArgs.Empty;
        var received = false;
        EventHandler<EventArgs> callbacks = (_, _) => throw new OperationCanceledException();
        callbacks += (actualSender, actualArgs) =>
        {
            received = ReferenceEquals(actualSender, sender) && ReferenceEquals(actualArgs, args);
        };
        ObserverNotification.Notify(callbacks, sender, args, reportError: _ => { });
        Assert.That(received, Is.True);
    }
    /// <summary>Emulates a host diagnostics failure independently of required business work.</summary>
    private sealed class FailingLogger : ILogger
    {
        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;
        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => throw new InvalidOperationException("logger");
    }
}
