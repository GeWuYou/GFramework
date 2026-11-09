// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;

namespace GFramework.Core.Events;

/// <summary>A subscription whose cancellation runs at most once, including concurrent disposal.</summary>
/// <param name="unsubscribe">Detachment action, invoked at most once on the disposing thread.</param>
public sealed class Subscription(Action unsubscribe) : IDisposable
{
    private Action? _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));

    /// <summary>Detach this registration. Repeated calls have no effect.</summary>
    public void Dispose() => Interlocked.Exchange(ref _unsubscribe, value: null)?.Invoke();
}
