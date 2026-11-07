// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Runtime;

namespace GFramework.Game.Scene;

/// <summary>Serializes scene callbacks on an explicit host and shares one ordered shutdown task.</summary>
internal sealed class SceneOperationQueue(IGameDispatcher dispatcher)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly AsyncLocal<bool> _executing = new();
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _stopLock = new();
#else
    private readonly object _stopLock = new();
#endif
    private volatile bool _stopping;
    private Task? _stop;

    /// <summary>Accepts a navigation operation while the host is available.</summary>
    /// <param name="operation">All guards, callbacks and state mutations.</param>
    /// <param name="cancellationToken">Cancels waiting before callbacks start.</param>
    /// <returns>The complete operation task.</returns>
    public Task RunAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        EnsureAdmission();
        return RunCoreAsync(operation, cancellationToken);
    }

    /// <summary>Rejects queued navigation from a callback and closes admission immediately.</summary>
    /// <param name="cleanup">Non-cancelable best-effort cleanup.</param>
    /// <returns>The same task on every stop request.</returns>
    public Task StopAsync(Func<Task> cleanup)
    {
        RejectReentrancy();
        lock (_stopLock)
        {
            _stopping = true;
            return _stop ??= RunCoreAsync(cleanup, CancellationToken.None, stopping: true);
        }
    }

    /// <summary>Checks synchronous admission without taking ownership of an operation.</summary>
    private void EnsureAdmission()
    {
        ObjectDisposedException.ThrowIf(_stopping, this);
        RejectReentrancy();
    }

    /// <summary>Prevents a lifecycle hook from waiting behind its own operation.</summary>
    private void RejectReentrancy()
    {
        if (_executing.Value)
            throw new InvalidOperationException("Do not await nested scene navigation or stop from a scene callback.");
    }

    /// <summary>Keeps host and operation ownership until callbacks have actually finished.</summary>
    private async Task RunCoreAsync(Func<Task> operation, CancellationToken cancellationToken, bool stopping = false)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!stopping)
                ObjectDisposedException.ThrowIf(_stopping, this);
            _executing.Value = true;
            await dispatcher.InvokeAsync(operation, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _executing.Value = false;
            _gate.Release();
        }
    }
}
