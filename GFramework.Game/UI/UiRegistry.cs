// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;

namespace GFramework.Game.UI;

/// <summary>
/// Registers typed view factories at the composition root. A service takes a stable copy, so later registrations
/// cannot change an existing host. Factories must reclaim native objects if they fail before returning a view lease.
/// </summary>
public sealed class UiRegistry
{
    private readonly Dictionary<string, UiRegistration> _routes = new(StringComparer.Ordinal);

    /// <summary>Registers a page or window with typed arguments and an instance-specific close context.</summary>
    /// <typeparam name="TArgs">Opening argument type.</typeparam>
    /// <param name="route">Definition reused at call sites.</param>
    /// <param name="factory">Factory called on the host dispatcher.</param>
    /// <returns>This registry for composition.</returns>
    public UiRegistry Register<TArgs>(UiRoute<TArgs> route,
        Func<TArgs, UiViewContext, CancellationToken, Task<IUiView>> factory)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(factory);
        Add(new UiRegistration(route.Key, route, route.Options,
            (args, handle, service, _, token) =>
                factory((TArgs)args!, new UiViewContext(handle, ct => service.CloseAsync(handle, ct)), token)));
        return this;
    }

    /// <summary>Registers a dialog whose view submits a typed result through its explicit context.</summary>
    /// <typeparam name="TArgs">Opening argument type.</typeparam>
    /// <typeparam name="TResult">Business result type.</typeparam>
    /// <param name="route">Dialog definition reused at call sites.</param>
    /// <param name="factory">Factory called on the host dispatcher.</param>
    /// <returns>This registry for composition.</returns>
    public UiRegistry Register<TArgs, TResult>(UiDialogRoute<TArgs, TResult> route,
        Func<TArgs, UiDialogContext<TResult>, CancellationToken, Task<IUiView>> factory)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(factory);
        Add(new UiRegistration(route.Key, route, route.Options,
            (args, handle, service, complete, token) => factory((TArgs)args!,
                new UiDialogContext<TResult>(handle, ct => service.CloseAsync(handle, ct),
                    (value, ct) => complete(value, ct)), token), () => new UiDialogCompletion<TResult>()));
        return this;
    }

    /// <summary>Copies registrations so an existing service has a deterministic definition set.</summary>
    internal IReadOnlyDictionary<string, UiRegistration> CopyRegistrations() =>
        new Dictionary<string, UiRegistration>(_routes, StringComparer.Ordinal);

    /// <summary>Rejects duplicate keys at registration time rather than during gameplay.</summary>
    private void Add(UiRegistration registration)
    {
        if (!_routes.TryAdd(registration.Key, registration))
            throw new InvalidOperationException($"UI route '{registration.Key}' is already registered.");
    }
}
