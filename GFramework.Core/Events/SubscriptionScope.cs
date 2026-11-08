// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Events;

/// <summary>Owns tokens and detaches them in reverse registration order.</summary>
/// <remarks>Adding after disposal immediately disposes the token. Cleanup executes outside the lock.</remarks>
public sealed class SubscriptionScope : IDisposable
{
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private List<IDisposable>? _subscriptions = [];

    /// <summary>Transfer a token into this scope.</summary>
    /// <param name="subscription">Token to dispose with the scope.</param>
    public void Add(IDisposable subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        lock (_gate)
        {
            if (_subscriptions is not null)
            {
                _subscriptions.Add(subscription);
                return;
            }
        }
        subscription.Dispose();
    }

    /// <summary>Detach once, attempting every cleanup even if one fails.</summary>
    public void Dispose()
    {
        List<IDisposable>? subscriptions;
        lock (_gate)
        {
            subscriptions = _subscriptions;
            _subscriptions = null;
        }
        if (subscriptions is null)
            return;
        List<Exception>? errors = null;
        for (var index = subscriptions.Count - 1; index >= 0; index--)
        {
            try
            {
                subscriptions[index].Dispose();
            }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is not null)
            throw new AggregateException("Subscription cleanup failed.", errors);
    }
}
