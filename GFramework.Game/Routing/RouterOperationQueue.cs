// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Routing;

/// <summary>
///     Serializes navigation and shutdown on the context captured when the host root is bound.
///     Reentrant navigation is rejected rather than waiting for its own operation to finish.
/// </summary>
internal sealed class RouterOperationQueue
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly AsyncLocal<bool> _executing = new();
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _stopLock = new();
#else
    private readonly object _stopLock = new();
#endif
    private SynchronizationContext? _context;
    private Task? _stopTask;
    private volatile bool _stopping;

    /// <summary>Captures a host context without introducing an engine dependency.</summary>
    public void BindContext()
    {
        _context = SynchronizationContext.Current;
    }

    /// <summary>Rejects synchronous mutations after stopping or from another host thread.</summary>
    public void EnsureAvailable()
    {
        ObjectDisposedException.ThrowIf(_stopping, this);
        if (_context is not null && SynchronizationContext.Current != _context)
            throw new InvalidOperationException("Synchronous UI operations must run on the bound host context.");
    }

    /// <summary>Queues a navigation operation; independently submitted operations share one gate.</summary>
    /// <param name="operation">Navigation callback, including guards and lifecycle callbacks.</param>
    /// <returns>The completion of this operation.</returns>
    public Task RunAsync(Func<Task> operation)
    {
        ObjectDisposedException.ThrowIf(_stopping, this);
        RejectReentrancy();
        return RunCoreAsync(operation, stopping: false);
    }

    /// <summary>Closes admission immediately and shares one ordered cleanup result.</summary>
    /// <param name="cleanup">Cleanup that bypasses user navigation guards.</param>
    /// <returns>The same shutdown task on repeated calls.</returns>
    public Task StopAsync(Func<Task> cleanup)
    {
        RejectReentrancy();
        lock (_stopLock)
        {
            _stopping = true;
            return _stopTask ??= RunCoreAsync(cleanup, stopping: true);
        }
    }

    /// <summary>Prevents a callback from awaiting navigation behind its own held gate.</summary>
    private void RejectReentrancy()
    {
        if (_executing.Value)
            throw new InvalidOperationException("Do not await nested navigation or shutdown from a route callback.");
    }

    /// <summary>Holds the gate until all callback work completes, including asynchronous cleanup.</summary>
    private async Task RunCoreAsync(Func<Task> operation, bool stopping)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!stopping)
                ObjectDisposedException.ThrowIf(_stopping, this);

            // Set the marker inside this async execution scope so unrelated callers remain independent.
            _executing.Value = true;
            await RouterCallbackDispatcher.InvokeAsync(_context, operation).ConfigureAwait(false);
        }
        finally
        {
            _executing.Value = false;
            _gate.Release();
        }
    }

}
