// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.UI;

/// <summary>Serializes accepted mutations without keeping the queue occupied by a dialog's result wait.</summary>
public sealed partial class UiService
{
    private bool _busy;

    /// <summary>Appends one operation while synchronously closing admission after stop begins.</summary>
    private Task<T> QueueAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        RejectReentrancy();
        lock (_queueLock)
        {
            ObjectDisposedException.ThrowIf(_stopping, this);
            return AppendAsync(operation, cancellationToken);
        }
    }

    /// <summary>Links tasks in submission order. A failed predecessor cannot poison later independent requests.</summary>
    private Task<T> AppendAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        var task = RunAfterAsync(_tail, operation, cancellationToken);
        _tail = task;
        return task;
    }

    /// <summary>Joins the predecessor, then executes the complete operation on the explicit host dispatcher.</summary>
    private async Task<T> RunAfterAsync<T>(Task predecessor, Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        // Force asynchronous entry: callbacks must not run while the caller holds the queue's admission lock.
        await Task.Yield();
        try
        {
            await predecessor.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Each caller receives its own failure. The queue's ordering barrier itself is always recoverable.
        }
        cancellationToken.ThrowIfCancellationRequested();
        T result = default!;
        await _dispatcher.InvokeAsync(async () =>
        {
            _executing.Value = true;
            _busy = true;
            PublishSnapshot();
            try
            {
                result = await operation(cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                _busy = false;
                _executing.Value = false;
                PublishSnapshot();
            }
        }, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>Rejects self-dependent lifecycle navigation instead of indefinitely waiting behind the current operation.</summary>
    private void RejectReentrancy()
    {
        if (_executing.Value)
            throw new InvalidOperationException("Do not await UI operations from a UI factory, lifecycle, guard, or action callback.");
    }

    /// <summary>Limits inline close permission to the view whose semantic callback is still executing.</summary>
    private bool IsOwnAction(UiInstanceHandle handle) => _actionScope.Value is { Active: true } scope && scope.Handle == handle;

    /// <summary>Allows a semantic callback to close itself while preserving the host dispatcher after context-free awaits.</summary>
    private async Task<UiOperationResult> RunOwnActionCloseAsync(Func<Task<UiOperationResult>> close,
        CancellationToken cancellationToken)
    {
        UiOperationResult result = default;
        await _dispatcher.InvokeAsync(async () => result = await close().ConfigureAwait(true), cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>Publishes an immutable state copy for callers on any thread.</summary>
    private void PublishSnapshot()
    {
        var views = Array.AsReadOnly(_entries.OrderBy(static entry => entry.Options.Layer)
            .ThenBy(static entry => entry.Order).Select(ToSnapshot).ToArray());
        Volatile.Write(ref _snapshot, new UiSnapshot(views, _busy, _stopping));
    }

    /// <summary>Observes cancellation cleanup without allowing cancellation callbacks to throw into their caller.</summary>
    private void RequestDialogCancellation(UiInstanceHandle handle, CancellationToken cancellationToken)
    {
        Task cleanup;
        lock (_queueLock)
        {
            if (_stopping)
                return;
            cleanup = AppendAsync(async _ =>
            {
                if (Find(handle) is { } entry)
                    await CloseEntryAsync(entry, cancelled: cancellationToken).ConfigureAwait(true);
                return true;
            }, CancellationToken.None);
        }
        _ = ObserveCancellationCleanupAsync(cleanup);
    }

    /// <summary>Observes cleanup errors; the dialog completion also receives the same failure.</summary>
    private async Task ObserveCancellationCleanupAsync(Task cleanup)
    {
        try
        {
            await cleanup.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Dialog cancellation cleanup failed");
        }
    }

    /// <summary>Revocable capability shared with child execution contexts of one semantic action.</summary>
    private sealed class ActionScope(UiInstanceHandle handle)
    {
        /// <summary>Gets the only instance the callback may close inline.</summary>
        public UiInstanceHandle Handle { get; } = handle;
        /// <summary>Tracks callback lifetime so escaped execution contexts cannot mutate later operations.</summary>
        public volatile bool Active = true;
    }
}
