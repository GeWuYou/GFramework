// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>Forwards several source events into a locally owned notification.</summary>
/// <remarks>Dispose the composition to detach its sources; disposing one listener does not detach other listeners.</remarks>
public sealed class OrEvent : IEvent, IDisposable
{
    private readonly SubscriptionScope _sources = new();
    private readonly EasyEvent _event = new();

    /// <summary>Attach another source, whose subscription is owned by this composition.</summary>
    /// <param name="source">Source event.</param>
    /// <returns>This composition.</returns>
    public OrEvent Or(IEvent source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _sources.Add(source.Register(_event.Trigger));
        return this;
    }

    /// <inheritdoc />
    public IDisposable Register(Action onEvent) => _event.Register(onEvent);

    /// <summary>Detach a local listener without changing upstream ownership.</summary>
    /// <param name="onEvent">Registered callback.</param>
    public void UnRegister(Action onEvent) => _event.UnRegister(onEvent);

    /// <summary>Detach source subscriptions once.</summary>
    public void Dispose() => _sources.Dispose();
}
