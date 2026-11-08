// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>A local priority notification using the same snapshot and propagation rules as EventBus.</summary>
/// <typeparam name="T">Notification value type.</typeparam>
/// <remarks>Subscriptions own their registrations; callbacks run synchronously on the triggering thread.</remarks>
public sealed class PriorityEvent<T> : IEvent
{
    private readonly EventBus _bus = new();
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private readonly List<Registration> _registrations = [];

    /// <inheritdoc />
    IDisposable IEvent.Register(Action onEvent)
    {
        ArgumentNullException.ThrowIfNull(onEvent);
        return Register(_ => onEvent());
    }

    /// <summary>Subscribe at the given priority; equal priorities retain insertion order across callback kinds.</summary>
    /// <param name="onEvent">Notification callback.</param>
    /// <param name="priority">Larger values execute first.</param>
    /// <returns>An idempotent token for this registration.</returns>
    public IDisposable Register(Action<T> onEvent, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(onEvent);
        return Track(onEvent, () => _bus.Subscribe(onEvent, priority));
    }

    /// <summary>Subscribe with a publication-local handled context.</summary>
    /// <param name="onEvent">Context callback.</param>
    /// <param name="priority">Larger values execute first.</param>
    /// <returns>An idempotent token for this registration.</returns>
    public IDisposable RegisterWithContext(Action<EventContext<T>> onEvent, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(onEvent);
        return Track(onEvent, () => _bus.SubscribeWithContext(onEvent, priority));
    }

    /// <summary>Detach ordinary callback registrations matching the supplied delegate.</summary>
    /// <param name="onEvent">Callback to detach.</param>
    public void UnRegister(Action<T> onEvent) => RemoveMatching(onEvent);

    /// <summary>Detach context callback registrations matching the supplied delegate.</summary>
    /// <param name="onEvent">Callback to detach.</param>
    public void UnRegisterContext(Action<EventContext<T>> onEvent) => RemoveMatching(onEvent);

    /// <summary>Publish synchronously with an independent snapshot and handled flag.</summary>
    /// <param name="value">Notification value.</param>
    /// <param name="propagation">Delivery selection.</param>
    public void Trigger(T value, EventPropagation propagation = EventPropagation.All) => _bus.Publish(value, propagation);

    /// <summary>Count currently attached local registrations.</summary>
    /// <returns>The listener count.</returns>
    public int GetListenerCount()
    {
        lock (_gate)
            return _registrations.Count;
    }

    /// <summary>Give the wrapper its own identity without changing the underlying delivery registration.</summary>
    private IDisposable Track(Delegate handler, Func<IDisposable> subscribe)
    {
        IDisposable subscription;
        Registration registration;
        lock (_gate)
        {
            // Publication may already observe the callback; removal waits for its metadata to be committed.
            subscription = subscribe();
            registration = new Registration(handler, subscription);
            _registrations.Add(registration);
        }
        return new Subscription(() =>
        {
            lock (_gate)
                _registrations.Remove(registration);
            subscription.Dispose();
        });
    }

    /// <summary>Remove metadata under the lock and detach subscriptions outside it.</summary>
    private void RemoveMatching(Delegate handler)
    {
        Registration[] removed;
        lock (_gate)
        {
            removed = _registrations.Where(item => item.Handler.Equals(handler)).ToArray();
            foreach (var registration in removed)
                _registrations.Remove(registration);
        }
        foreach (var registration in removed)
            registration.Subscription.Dispose();
    }

    /// <summary>Metadata for manually detaching a local callback.</summary>
    private sealed record Registration(Delegate Handler, IDisposable Subscription);
}
