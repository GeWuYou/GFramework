// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace GFramework.Core.Events;

/// <summary>A standalone synchronous event retaining instance targets weakly.</summary>
/// <typeparam name="T">Notification type.</typeparam>
/// <param name="statistics">Optional statistics for this local event.</param>
/// <remarks>A live target continues receiving notifications even if its temporary delegate is collected.
/// Holding a token does not keep an instance target alive. Static callbacks remain until explicitly detached.</remarks>
public sealed class WeakEvent<T>(EventStatistics? statistics = null)
{
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private readonly List<Registration> _registrations = [];

    /// <summary>Optional local statistics, keyed by the notification type name.</summary>
    public EventStatistics? Statistics => statistics;

    /// <summary>Register a weak instance target or a strongly held static callback.</summary>
    /// <param name="handler">Callback, including optional multicast delegates.</param>
    /// <returns>An idempotent token which does not root instance targets.</returns>
    public IDisposable Register(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var entries = handler.GetInvocationList().Cast<Action<T>>().Select(value => new Registration(value)).ToArray();
        lock (_gate)
        {
            _registrations.AddRange(entries);
            UpdateStatistics();
        }
        return new Subscription(() =>
        {
            lock (_gate)
            {
                foreach (var entry in entries)
                    _registrations.Remove(entry);
                UpdateStatistics();
            }
        });
    }

    /// <summary>Detach matching callbacks and prune expired instance targets.</summary>
    /// <param name="handler">Callback to detach.</param>
    public void UnRegister(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var callbacks = handler.GetInvocationList().Cast<Action<T>>().ToArray();
        lock (_gate)
        {
            _registrations.RemoveAll(entry => callbacks.Any(entry.Matches) || !entry.IsAlive);
            UpdateStatistics();
        }
    }

    /// <summary>Publish to a live snapshot on the current thread; observer errors propagate.</summary>
    /// <param name="value">Notification value.</param>
    public void Trigger(T value)
    {
        Action<T>[] callbacks;
        lock (_gate)
        {
            var alive = new List<Action<T>>();
            _registrations.RemoveAll(entry =>
            {
                var callback = entry.GetCallback();
                if (callback is null)
                    return true;
                alive.Add(callback);
                return false;
            });
            callbacks = [.. alive];
            UpdateStatistics();
        }
        statistics?.RecordPublish(typeof(T).Name);
        foreach (var callback in callbacks)
        {
            try
            {
                callback(value);
                statistics?.RecordHandle();
            }
            catch { statistics?.RecordFailure(); throw; }
        }
    }

    /// <summary>Remove registrations whose instance targets have been collected.</summary>
    public void Cleanup()
    {
        lock (_gate)
        {
            _registrations.RemoveAll(entry => !entry.IsAlive);
            UpdateStatistics();
        }
    }

    /// <summary>Update the local count while registry mutations remain serialized.</summary>
    private void UpdateStatistics() => statistics?.UpdateListenerCount(typeof(T).Name, _registrations.Count);

    /// <summary>Store method identity separately from the weak target so a transient delegate is not the lifetime anchor.</summary>
    private sealed class Registration
    {
        private readonly MethodInfo _method;
        private readonly WeakReference<object>? _target;
        private readonly Action<T>? _staticHandler;

        /// <summary>Record callback metadata without rooting instance targets.</summary>
        public Registration(Action<T> handler)
        {
            _method = handler.Method;
            if (handler.Target is { } target)
                _target = new WeakReference<object>(target);
            else
                _staticHandler = handler;
        }

        /// <summary>Whether the target still exists or the callback is static.</summary>
        public bool IsAlive => _staticHandler is not null || _target!.TryGetTarget(out _);

        /// <summary>Temporarily root a live instance during the current publication.</summary>
        public Action<T>? GetCallback()
        {
            if (_staticHandler is not null)
                return _staticHandler;
            return _target!.TryGetTarget(out var target) ? _method.CreateDelegate<Action<T>>(target) : null;
        }

        /// <summary>Compare method and target identity for explicit callback removal.</summary>
        public bool Matches(Action<T> handler) => _method == handler.Method &&
            (handler.Target is null ? _staticHandler is not null :
                _target is not null && _target.TryGetTarget(out var target) && ReferenceEquals(target, handler.Target));
    }
}
