// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Events;
using Microsoft.Extensions.Logging;

namespace GFramework.Core.StateManagement;

/// <summary>Contains observation delivery after Store commits; reducers and middleware retain normal failure semantics.</summary>
public sealed partial class Store<TState>
{
    private readonly ILogger? _observerLogger;

    /// <summary>Notifies direct subscribers outside locks without reversing committed state/history.</summary>
    /// <param name="listenersSnapshot">Subscriptions captured when the state was committed.</param>
    /// <param name="state">The committed immutable snapshot.</param>
    private void NotifyListeners(Action<TState>[] listenersSnapshot, TState state)
    {
        foreach (var listener in listenersSnapshot)
            ObserverNotification.Notify(listener, state, _observerLogger);
    }
}
