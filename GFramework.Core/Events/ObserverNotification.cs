// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace GFramework.Core.Events;

/// <summary>Delivers observation-only notifications without changing an already committed operation.</summary>
/// <remarks>
/// Each direct subscriber is attempted independently on the calling thread, outside the owner's locks.
/// Use normal calls for required work, guards and lifecycle hooks: their failures must remain observable to callers.
/// An opaque publisher callback remains responsible for its own downstream subscribers.
/// </remarks>
public static class ObserverNotification
{
    /// <summary>Notifies each direct action subscriber and reports failures without throwing.</summary>
    /// <typeparam name="T">The notification payload.</typeparam>
    /// <param name="observers">A snapshot of the directly owned subscribers.</param>
    /// <param name="value">The committed result or current state.</param>
    /// <param name="logger">Optional host diagnostics; Trace is used when no diagnostics are supplied.</param>
    /// <param name="reportError">Optional diagnostic callback, used instead of the logger when supplied.</param>
    public static void Notify<T>(Action<T>? observers, T value, ILogger? logger = null,
        Action<Exception>? reportError = null)
    {
        if (observers is null)
            return;
        foreach (var entry in observers.GetInvocationList())
        {
            try
            {
                ((Action<T>)entry)(value);
            }
            catch (Exception error)
            {
                Report(error, logger, reportError);
            }
        }
    }

    /// <summary>Notifies each direct event subscriber, preserving sender and payload identity.</summary>
    /// <typeparam name="TEventArgs">The event payload type.</typeparam>
    /// <param name="observers">The event's current delegate snapshot.</param>
    /// <param name="sender">The owner publishing the state.</param>
    /// <param name="args">The committed state notification.</param>
    /// <param name="logger">Optional host diagnostics.</param>
    /// <param name="reportError">Optional diagnostic callback, used instead of the logger when supplied.</param>
    public static void Notify<TEventArgs>(EventHandler<TEventArgs>? observers, object sender,
        TEventArgs args, ILogger? logger = null, Action<Exception>? reportError = null)
        where TEventArgs : EventArgs
    {
        if (observers is null)
            return;
        foreach (var entry in observers.GetInvocationList())
        {
            try
            {
                ((EventHandler<TEventArgs>)entry)(sender, args);
            }
            catch (Exception error)
            {
                Report(error, logger, reportError);
            }
        }
    }

    /// <summary>Contains diagnostic failures as well; diagnostics must never veto a committed operation.</summary>
    /// <param name="error">The observation or diagnostic setup failure.</param>
    /// <param name="logger">Optional logging channel.</param>
    /// <param name="reportError">Optional diagnostic callback.</param>
    internal static void Report(Exception error, ILogger? logger, Action<Exception>? reportError)
    {
        try
        {
            if (reportError is not null)
                reportError(error);
            else if (logger is not null)
                logger.LogError(error, "An observation-only notification subscriber failed.");
            else
                Trace.TraceError("An observation-only notification subscriber failed: {0}", error);
        }
        catch (Exception)
        {
            // Logging providers and diagnostic callbacks are external code too.
            // A failed diagnostic channel cannot change the operation or prevent later subscribers.
        }
    }
}
