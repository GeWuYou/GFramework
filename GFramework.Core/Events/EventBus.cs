// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>An instance-local bus with one priority-ordered channel per notification type.</summary>
/// <remarks>Registry locking does not serialize callbacks or marshal them to a host thread.
/// Each publication owns its snapshot and handled flag, including nested publications.</remarks>
/// <param name="enableStatistics">Whether to collect per-instance delivery statistics.</param>
public sealed class EventBus(bool enableStatistics = false) : IEventBus
{
    private readonly ConcurrentDictionary<Type, object> _channels = new();
    private readonly EventStatistics? _statistics = enableStatistics ? new EventStatistics() : null;

    /// <summary>Optional per-instance notification statistics.</summary>
    public IEventStatistics? Statistics => _statistics;

    /// <inheritdoc />
    public void Publish<T>(T value, EventPropagation propagation = EventPropagation.All)
    {
        if (!Enum.IsDefined(propagation))
            throw new ArgumentOutOfRangeException(nameof(propagation));
        _statistics?.RecordPublish(typeof(T).FullName ?? typeof(T).Name);
        if (!_channels.TryGetValue(typeof(T), out var channel))
            return;
        var snapshot = ((Channel<T>)channel).Snapshot();
        var context = new EventContext<T>(value);
        foreach (var registration in snapshot)
        {
            if (propagation == EventPropagation.Highest && registration.Priority != snapshot[0].Priority)
                break;
            try
            {
                registration.Handler(context);
                _statistics?.RecordHandle();
            }
            catch
            {
                _statistics?.RecordFailure();
                throw;
            }
            if (propagation == EventPropagation.UntilHandled && context.IsHandled)
                break;
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe<T>(Action<T> handler, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return SubscribeWithContext<T>(context => handler(context.Data), priority);
    }

    /// <inheritdoc />
    public IDisposable SubscribeWithContext<T>(Action<EventContext<T>> handler, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var channel = (Channel<T>)_channels.GetOrAdd(typeof(T), static (_, owner) => new Channel<T>(count => owner._statistics?.UpdateListenerCount(typeof(T).FullName ?? typeof(T).Name, count)), this);
        var registration = new Registration<T>(handler, priority);
        channel.Add(registration);
        return new Subscription(() =>
        {
            channel.Remove(registration);
        });
    }

    /// <summary>Identity allows independently detaching duplicate delegates.</summary>
    private sealed record Registration<T>(Action<EventContext<T>> Handler, int Priority);

    /// <summary>Locks protect snapshots only; callbacks execute outside the lock.</summary>
    private sealed class Channel<T>(Action<int> countChanged)
    {
#if NET9_0_OR_GREATER
        private readonly System.Threading.Lock _gate = new();
#else
        private readonly object _gate = new();
#endif
        private readonly List<Registration<T>> _registrations = [];

        /// <summary>Number of active registrations.</summary>
        public int Count
        {
            get
            {
                lock (_gate)
                    return _registrations.Count;
            }
        }

        /// <summary>Append a registration.</summary>
        public void Add(Registration<T> registration)
        {
            lock (_gate)
            {
                _registrations.Add(registration);
                countChanged(_registrations.Count);
            }
        }

        /// <summary>Detach this exact registration.</summary>
        public void Remove(Registration<T> registration)
        {
            lock (_gate)
            {
                var index = _registrations.FindIndex(item => ReferenceEquals(item, registration));
                if (index >= 0)
                    _registrations.RemoveAt(index);
                countChanged(_registrations.Count);
            }
        }

        /// <summary>Capture immutable membership in stable priority order.</summary>
        public Registration<T>[] Snapshot()
        {
            lock (_gate)
                return [.. _registrations.OrderByDescending(item => item.Priority)];
        }
    }
}
