// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pause;
using GFramework.Game.Abstractions.UI;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.UI;

/// <summary>Implements preparation, commit and best-effort cleanup on the service's host dispatcher.</summary>
public sealed partial class UiService
{
    /// <summary>Prepares a target before changing page history or native interaction.</summary>
    private async Task<UiOperationResult> OpenCoreAsync(UiRegistration route, object? args, bool replace,
        CancellationToken cancellationToken)
    {
        var existing = route.Options.Kind != UiRouteKind.Window || route.Options.AllowMultiple ? null :
            _entries.LastOrDefault(entry => string.Equals(entry.Route.Key, route.Key, StringComparison.Ordinal));
        if (existing is not null && !replace)
        {
            // Window reuse preserves its original arguments and lease while bringing that instance to the front.
            existing.Order = ++_nextId;
            _host.Attach(existing.View, existing.Options.Layer, existing.Order);
            RecomputePresentation();
            if (existing.Active)
                _host.Focus(existing.View);
            return new UiOperationResult(UiOperationStatus.NoOp, existing.Handle);
        }

        foreach (var guard in _guards)
        {
            if (!await guard.CanOpenAsync(route.Key, cancellationToken).ConfigureAwait(true))
                return new UiOperationResult(UiOperationStatus.Rejected);
        }
        var replacedPages = replace ? _entries.Where(static entry => entry.Options.Kind == UiRouteKind.Page).ToArray()
            : Array.Empty<Entry>();
        if (!await CanCloseAllAsync(replacedPages, cancellationToken).ConfigureAwait(true))
            return new UiOperationResult(UiOperationStatus.Rejected);
        cancellationToken.ThrowIfCancellationRequested();

        var entry = await PrepareAsync(route, args, cancellationToken).ConfigureAwait(true);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (replacedPages.Length != 0)
                await CloseAllCoreAsync(replacedPages).ConfigureAwait(true);
            _entries.Add(entry);
            RecomputePresentation();
            if (entry.Active)
                _host.Focus(entry.View);
            _logger.LogDebug("Opened UI route {RouteKey}, instance {InstanceId}", route.Key, entry.Handle.InstanceId);
            return new UiOperationResult(UiOperationStatus.Applied, entry.Handle);
        }
        catch (Exception failure)
        {
            _entries.Remove(entry);
            var errors = new List<Exception> { failure };
            await CleanupEntryAsync(entry, errors).ConfigureAwait(true);
            TryAction(RecomputePresentation, errors);
            ThrowOperationFailure(failure, errors);
            throw;
        }
    }

    /// <summary>Mounts a hidden lease and opens it before publishing any navigation state.</summary>
    private async Task<Entry> PrepareAsync(UiRegistration route, object? args, CancellationToken cancellationToken)
    {
        var identity = new UiInstanceHandle(_serviceId, ++_nextId);
        var view = await route.Create(args, identity, this,
            (value, ct) => CompleteDialogAsync(identity, value, ct), cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Factory for UI route '{route.Key}' returned null.");
        var entry = new Entry(identity, route, view, identity.InstanceId, route.CreateDialog?.Invoke());
        try
        {
            view.SetVisible(false);
            entry.FocusBookmark = _host.CaptureFocus();
            _host.Attach(view, route.Options.Layer, entry.Order);
            await view.OpenAsync(cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            // Native attachment/readiness and opening may create children. Disable their authored input only after
            // preparation, before computing modal eligibility, without issuing duplicate lifecycle transitions.
            view.SetActive(false);
            return entry;
        }
        catch (Exception failure)
        {
            var errors = new List<Exception> { failure };
            await CleanupEntryAsync(entry, errors).ConfigureAwait(true);
            ThrowOperationFailure(failure, errors);
            throw;
        }
    }

    /// <summary>Checks every leave guard before changing any member of a multi-view operation.</summary>
    private async Task<bool> CanCloseAllAsync(IEnumerable<Entry> entries, CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            foreach (var guard in _guards)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await guard.CanCloseAsync(ToSnapshot(entry), cancellationToken).ConfigureAwait(true))
                    return false;
            }
        }
        return true;
    }

    /// <summary>Applies guarded close once; cleanup after admission is intentionally uncancelled.</summary>
    private async Task<UiOperationResult> CloseGuardedAsync(Entry? entry, bool accepted, object? value,
        CancellationToken cancellationToken)
    {
        if (entry is null)
            return new UiOperationResult(UiOperationStatus.NoOp);
        if (!await CanCloseAllAsync(new[] { entry }, cancellationToken).ConfigureAwait(true))
            return new UiOperationResult(UiOperationStatus.Rejected, entry.Handle);
        cancellationToken.ThrowIfCancellationRequested();
        await CloseEntryAsync(entry, accepted, value).ConfigureAwait(true);
        return new UiOperationResult(UiOperationStatus.Applied, entry.Handle);
    }

    /// <summary>Selects one frontmost return target, skipping HUDs while respecting a non-backable modal barrier.</summary>
    private Task<UiOperationResult> BackCoreAsync(CancellationToken cancellationToken)
    {
        foreach (var window in InFrontOrder().Where(entry => entry.Visible && entry.Options.Kind == UiRouteKind.Window))
        {
            if (window.Options.AllowBack)
                return CloseGuardedAsync(window, accepted: false, value: null, cancellationToken);
            if (window.Options.Modal)
                return Task.FromResult(new UiOperationResult(UiOperationStatus.NoOp, window.Handle));
        }
        var front = _entries.LastOrDefault(static entry => entry.Options.Kind == UiRouteKind.Page);
        if (front is null || !front.Options.AllowBack)
            return Task.FromResult(new UiOperationResult(UiOperationStatus.NoOp, front?.Handle));
        return CloseGuardedAsync(front, accepted: false, value: null, cancellationToken);
    }

    /// <summary>Queues dialog business completion like every other state mutation.</summary>
    internal Task<UiOperationResult> CompleteDialogAsync(UiInstanceHandle handle, object? value,
        CancellationToken cancellationToken)
    {
        Task<UiOperationResult> Complete(CancellationToken token)
        {
            var entry = Find(handle);
            if (entry is not null && entry.Dialog is null)
                throw new InvalidOperationException("The target UI instance is not a dialog.");
            return CloseGuardedAsync(entry, accepted: true, value, token);
        }
        return IsOwnAction(handle)
            ? RunOwnActionCloseAsync(() => Complete(cancellationToken), cancellationToken)
            : QueueAsync(Complete, cancellationToken);
    }

    /// <summary>Removes tracking before callbacks, then restores remaining native and pause state.</summary>
    private async Task CloseEntryAsync(Entry entry, bool accepted = false, object? value = null,
        CancellationToken cancelled = default)
    {
        _entries.Remove(entry);
        var errors = new List<Exception>();
        await CleanupEntryAsync(entry, errors).ConfigureAwait(true);
        TryAction(RecomputePresentation, errors);
        if (entry.Active || entry.Options.Modal)
            TryAction(() => _host.RestoreFocus(entry.FocusBookmark), errors);
        if (errors.Count != 0)
        {
            var exception = new AggregateException("UI close failed after owned resources were reclaimed.", errors);
            entry.Dialog?.Fail(exception);
            throw exception;
        }
        if (cancelled.IsCancellationRequested)
            entry.Dialog?.Cancel(cancelled);
        else
            entry.Dialog?.Finish(accepted, value);
    }

    /// <summary>Cleans every requested instance even if a preceding callback or native release fails.</summary>
    private async Task CloseAllCoreAsync(IEnumerable<Entry> entries)
    {
        var errors = new List<Exception>();
        foreach (var entry in entries.OrderByDescending(static item => item.Order))
        {
            try
            {
                await CloseEntryAsync(entry).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }
        if (errors.Count != 0)
            throw new AggregateException("One or more UI views failed during cleanup.", errors);
    }

    /// <summary>Attempts all cleanup phases exactly once, including when opening only partially succeeded.</summary>
    private async Task CleanupEntryAsync(Entry entry, List<Exception> errors)
    {
        if (entry.Released)
            return;
        entry.Released = true;
        TryAction(() => entry.View.SetActive(false), errors);
        TryAction(() => entry.View.SetVisible(false), errors);
        try
        {
            await entry.View.CloseAsync(CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
        TryAction(() => _host.Detach(entry.View), errors);
        TryAction(() => ReleasePause(entry), errors);
        try
        {
            await entry.View.DisposeAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
    }

    /// <summary>Calculates page visibility, modal interaction barriers and pause requests from one committed state.</summary>
    private void RecomputePresentation()
    {
        var page = _entries.LastOrDefault(static entry => entry.Options.Kind == UiRouteKind.Page);
        var blocked = false;
        foreach (var entry in InFrontOrder())
        {
            var visible = entry.Options.Kind == UiRouteKind.Window || ReferenceEquals(entry, page);
            var active = visible && !blocked && entry.View.IsAlive;
            if (entry.Visible != visible)
            {
                entry.View.SetVisible(visible);
                entry.Visible = visible;
            }
            if (entry.Active != active)
            {
                entry.View.SetActive(active);
                entry.Active = active;
            }
            if (visible && entry.Options.PauseWhileVisible && entry.PauseToken is null)
                entry.PauseToken = _pause!.Push($"UI:{entry.Route.Key}", entry.Options.PauseGroup);
            else if (!visible)
                ReleasePause(entry);
            if (visible && entry.Options.Modal)
                blocked = true;
        }
        PublishSnapshot();
    }

    /// <summary>Releases the token owned by this exact view, never another view's pause request.</summary>
    private void ReleasePause(Entry entry)
    {
        if (entry.PauseToken is not { } token)
            return;
        entry.PauseToken = null;
        _pause!.Pop(token);
    }

    /// <summary>Records cleanup errors while preserving later release attempts.</summary>
    private static void TryAction(Action action, List<Exception> errors)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
    }

    /// <summary>Preserves standard cancellation when cleanup succeeded, while retaining additional cleanup failures.</summary>
    private static void ThrowOperationFailure(Exception failure, List<Exception> errors)
    {
        if (errors.Count > 1)
            throw new AggregateException("UI preparation failed after best-effort cleanup.", errors);
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    /// <summary>Copies current state without exposing mutable native objects or runtime collections.</summary>
    private static UiViewSnapshot ToSnapshot(Entry entry) =>
        new(entry.Handle, entry.Route.Key, entry.Options, entry.Visible, entry.Active);

    /// <summary>Holds exactly one view lease and its independent navigation, focus and pause responsibilities.</summary>
    private sealed class Entry(UiInstanceHandle handle, UiRegistration route, IUiView view, long order,
        IUiDialogCompletion? dialog)
    {
        /// <summary>Gets the instance identity.</summary>
        public UiInstanceHandle Handle { get; } = handle;
        /// <summary>Gets its registered definition.</summary>
        public UiRegistration Route { get; } = route;
        /// <summary>Gets immutable policies.</summary>
        public UiRouteOptions Options => Route.Options;
        /// <summary>Gets the owned view lease.</summary>
        public IUiView View { get; } = view;
        /// <summary>Gets or sets its presentation order.</summary>
        public long Order { get; set; } = order;
        /// <summary>Gets its optional typed dialog completion.</summary>
        public IUiDialogCompletion? Dialog { get; } = dialog;
        /// <summary>Gets or sets its committed visibility.</summary>
        public bool Visible { get; set; }
        /// <summary>Gets or sets its committed input eligibility.</summary>
        public bool Active { get; set; }
        /// <summary>Gets or sets whether cleanup already consumed the lease.</summary>
        public bool Released { get; set; }
        /// <summary>Gets or sets its pause token.</summary>
        public PauseToken? PauseToken { get; set; }
        /// <summary>Gets or sets its native focus restoration bookmark.</summary>
        public object? FocusBookmark { get; set; }
    }
}
