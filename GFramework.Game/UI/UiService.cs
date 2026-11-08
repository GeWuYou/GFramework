// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pause;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Abstractions.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Game.UI;

/// <summary>
/// Standalone UI runtime with explicit factories, host, dispatcher and optional pause manager.
/// The runtime owns view leases until close/stop; the host owns native mounting and focus. A task chain orders all
/// mutations, including dialog completion, while waiting for a user's dialog result never holds that chain.
/// </summary>
public sealed partial class UiService : IUiService
{
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly IReadOnlyDictionary<string, UiRegistration> _registrations;
    private readonly IUiHost _host;
    private readonly IGameDispatcher _dispatcher;
    private readonly IPauseStackManager? _pause;
    private readonly ILogger _logger;
    private readonly List<IUiGuard> _guards;
    private readonly List<Entry> _entries = [];
    private readonly AsyncLocal<bool> _executing = new();
    private readonly AsyncLocal<ActionScope?> _actionScope = new();
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _queueLock = new();
#else
    private readonly object _queueLock = new();
#endif
    private Task _tail = Task.CompletedTask;
    private Task? _stopTask;
    private long _nextId;
    private volatile bool _stopping;
    private UiSnapshot _snapshot = new([], IsBusy: false, IsStopping: false);

    /// <summary>Creates an independent UI host. Dependencies are externally owned; view leases are service-owned.</summary>
    /// <param name="routes">Typed registration set copied for this service.</param>
    /// <param name="host">Native mounting and focus adapter.</param>
    /// <param name="dispatcher">Dispatcher used for every factory, lifecycle callback and host operation.</param>
    /// <param name="pause">Optional pause stack; required when a route requests pause.</param>
    /// <param name="logger">Optional standard diagnostic logger.</param>
    /// <param name="guards">Stable admission/close policies evaluated in enumeration order.</param>
    public UiService(UiRegistry routes, IUiHost host, IGameDispatcher dispatcher,
        IPauseStackManager? pause = null, ILogger<UiService>? logger = null, IEnumerable<IUiGuard>? guards = null)
    {
        ArgumentNullException.ThrowIfNull(routes);
        _registrations = routes.CopyRegistrations();
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _pause = pause;
        _logger = logger ?? NullLogger<UiService>.Instance;
        _guards = guards?.ToList() ?? [];
        if (_registrations.Values.Any(static registration => registration.Options.PauseWhileVisible) && pause is null)
            throw new ArgumentException("A pause manager is required by the registered UI pause policy.", nameof(pause));
    }

    /// <inheritdoc />
    public UiSnapshot Snapshot => Volatile.Read(ref _snapshot);

    /// <inheritdoc />
    public Task<UiOperationResult> PushAsync<TArgs>(UiRoute<TArgs> route, TArgs args,
        CancellationToken cancellationToken = default) =>
        QueueAsync(ct => OpenCoreAsync(Resolve(route, UiRouteKind.Page), args, replace: false, ct), cancellationToken);

    /// <inheritdoc />
    public Task<UiOperationResult> ReplaceAsync<TArgs>(UiRoute<TArgs> route, TArgs args,
        CancellationToken cancellationToken = default) =>
        QueueAsync(ct => OpenCoreAsync(Resolve(route, UiRouteKind.Page), args, replace: true, ct), cancellationToken);

    /// <inheritdoc />
    public Task<UiOperationResult> ShowAsync<TArgs>(UiRoute<TArgs> route, TArgs args,
        CancellationToken cancellationToken = default) =>
        QueueAsync(ct => OpenCoreAsync(Resolve(route, UiRouteKind.Window), args, replace: false, ct), cancellationToken);

    /// <inheritdoc />
    public async Task<DialogResult<TResult>> ShowDialogAsync<TArgs, TResult>(UiDialogRoute<TArgs, TResult> route,
        TArgs args, CancellationToken cancellationToken = default)
    {
        UiDialogCompletion<TResult>? completion = null;
        UiInstanceHandle? handle = null;
        var opened = await QueueAsync(async ct =>
        {
            var definition = Resolve(route, UiRouteKind.Window);
            if (definition.CreateDialog is null)
                throw new InvalidOperationException("Register a dialog with the typed dialog factory overload before showing it.");
            var result = await OpenCoreAsync(definition, args, replace: false, ct).ConfigureAwait(true);
            handle = result.Handle;
            if (handle is { } identity)
                completion = Find(identity)?.Dialog as UiDialogCompletion<TResult>
                    ?? throw new InvalidOperationException("The route was not registered as a typed dialog.");
            return result;
        }, cancellationToken).ConfigureAwait(false);
        if (opened.Status == UiOperationStatus.Rejected || completion is null || handle is null)
            return new DialogResult<TResult>(Accepted: false, default);

        var registration = cancellationToken.Register(() => RequestDialogCancellation(handle.Value, cancellationToken));

        // Observe cancellation outside the mutation chain. Cleanup is queued, then its completion releases the waiter.
        await using (registration.ConfigureAwait(false))
        {
            return await completion.Task.ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task<UiOperationResult> CloseAsync(UiInstanceHandle handle, CancellationToken cancellationToken = default)
    {
        ValidateHandle(handle);
        if (IsOwnAction(handle))
            return RunOwnActionCloseAsync(() => CloseGuardedAsync(Find(handle), accepted: false, value: null, cancellationToken), cancellationToken);
        return QueueAsync(ct => CloseGuardedAsync(Find(handle), accepted: false, value: null, ct), cancellationToken);
    }

    /// <inheritdoc />
    public Task<UiOperationResult> BackAsync(CancellationToken cancellationToken = default) =>
        QueueAsync(BackCoreAsync, cancellationToken);

    /// <inheritdoc />
    public Task<UiOperationResult> ClearAsync(CancellationToken cancellationToken = default) => QueueAsync(async ct =>
    {
        if (!await CanCloseAllAsync(_entries, ct).ConfigureAwait(true))
            return new UiOperationResult(UiOperationStatus.Rejected);
        var hadEntries = _entries.Count != 0;
        await CloseAllCoreAsync([.. _entries]).ConfigureAwait(true);
        return new UiOperationResult(hadEntries ? UiOperationStatus.Applied : UiOperationStatus.NoOp);
    }, cancellationToken);

    /// <inheritdoc />
    public Task<bool> DispatchActionAsync(UiInputAction action, CancellationToken cancellationToken = default) =>
        QueueAsync(async ct =>
        {
            var owner = InFrontOrder().FirstOrDefault(entry => entry.Visible && entry.Active && Captures(entry.Options, action));
            if (owner is null)
                return false;
            var handled = false;
            var actionScope = new ActionScope(owner.Handle);
            _actionScope.Value = actionScope;
            try
            {
                if (owner.View is IUiActionTarget target)
                    handled = await target.HandleActionAsync(action, ct).ConfigureAwait(true);
            }
            finally
            {
                // Child execution contexts may outlive this callback; they must not retain inline mutation permission.
                actionScope.Active = false;
                _actionScope.Value = null;
            }
            if (!handled && action == UiInputAction.Cancel && owner.Options.AllowBack && Find(owner.Handle) is not null)
                await CloseGuardedAsync(owner, accepted: false, value: null, ct).ConfigureAwait(true);
            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public Task StopAsync()
    {
        RejectReentrancy();
        lock (_queueLock)
        {
            _stopping = true;
            Volatile.Write(ref _snapshot, Snapshot with
            {
                IsStopping = true,
            });
            return _stopTask ??= AppendAsync(async _ =>
            {
                await CloseAllCoreAsync([.. _entries]).ConfigureAwait(true);
                return true;
            }, CancellationToken.None);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(StopAsync());

    /// <summary>Checks typed definition identity before erasing its factory arguments.</summary>
    private UiRegistration Resolve<TArgs>(UiRoute<TArgs> route, UiRouteKind expected)
    {
        ArgumentNullException.ThrowIfNull(route);
        if (!_registrations.TryGetValue(route.Key, out var registration) || !ReferenceEquals(registration.Definition, route))
            throw new InvalidOperationException($"UI route '{route.Key}' is not registered with this definition.");
        if (route.Options.Kind != expected)
            throw new ArgumentException($"UI route '{route.Key}' requires {route.Options.Kind} operations.", nameof(route));
        return registration;
    }

    /// <summary>Rejects cross-host handles even when the local numeric identity happens to match.</summary>
    private void ValidateHandle(UiInstanceHandle handle)
    {
        if (handle.ServiceId != _serviceId)
            throw new ArgumentException("The UI handle belongs to a different service.", nameof(handle));
    }

    /// <summary>Finds one service-owned instance on the mutation dispatcher.</summary>
    private Entry? Find(UiInstanceHandle handle) => _entries.Find(entry => entry.Handle == handle);

    /// <summary>Orders visible interaction candidates using the same visual ordering passed to the host.</summary>
    private IEnumerable<Entry> InFrontOrder() =>
        _entries.OrderByDescending(static entry => entry.Options.Layer).ThenByDescending(static entry => entry.Order);

    /// <summary>Maps semantic actions without depending on any engine event type.</summary>
    private static bool Captures(UiRouteOptions options, UiInputAction action) => action switch
    {
        UiInputAction.Cancel => options.CapturedActions.HasFlag(UiInputActionMask.Cancel),
        UiInputAction.Confirm => options.CapturedActions.HasFlag(UiInputActionMask.Confirm),
        _ => false,
    };
}
