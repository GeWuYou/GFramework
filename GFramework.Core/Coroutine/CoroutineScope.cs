// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine;

/// <summary>
/// Owns the lifetime of a set of tasks, including their descendants. Use FailFast for one operation and Supervisor for independent object tasks.
/// All operations belong to the scheduler's driver thread. Disposing the scope cancels its tasks and prevents new work.
/// </summary>
public sealed class CoroutineScope : IDisposable
{
    private readonly HashSet<CoroutineHandle> _members = new();
    private bool _cancelling;

    /// <summary>Creates a lifetime container attached to exactly one scheduler.</summary>
    internal CoroutineScope(CoroutineScheduler scheduler, CoroutineFailurePolicy failurePolicy)
    {
        Scheduler = scheduler;
        FailurePolicy = failurePolicy;
    }

    /// <summary>The scheduler responsible for this scope's execution and cleanup.</summary>
    public CoroutineScheduler Scheduler { get; }
    /// <summary>The immutable failure behavior between independent member tasks.</summary>
    public CoroutineFailurePolicy FailurePolicy { get; }
    /// <summary>Whether this container has been closed to new tasks.</summary>
    public bool IsDisposed { get; private set; }
    /// <summary>Number of live member tasks, including descendants.</summary>
    public int ActiveCount => _members.Count;

    /// <summary>Starts an iterator owned by this scope. The first step executes synchronously.</summary>
    /// <param name="coroutine">The iterator transferred to this scope.</param>
    /// <param name="tag">Diagnostic label.</param>
    /// <param name="priority">FIFO priority within its phase.</param>
    /// <param name="group">Batch-management label.</param>
    /// <param name="cancellationToken">External lifetime cancellation.</param>
    /// <param name="stage">Initial phase, or the scheduler default.</param>
    /// <param name="ignorePause">Whether host pause leaves this task running.</param>
    /// <param name="childFailurePolicy">Failure behavior inside the task's owned flow.</param>
    /// <returns>The new identity, or default for null or already-cancelled input.</returns>
    public CoroutineHandle Run(IEnumerator<IYieldInstruction>? coroutine, string? tag = null,
        CoroutinePriority priority = CoroutinePriority.Normal, string? group = null,
        CancellationToken cancellationToken = default, CoroutineExecutionStage? stage = null,
        bool ignorePause = false, CoroutineFailurePolicy childFailurePolicy = CoroutineFailurePolicy.FailFast)
    {
        return Scheduler.Start(this, default, coroutine, null, tag, priority, group, cancellationToken, stage,
            ignorePause, childFailurePolicy);
    }

    /// <summary>Starts a flow which can create explicitly owned children through its context.</summary>
    /// <param name="factory">Factory invoked synchronously on the driver thread.</param>
    /// <param name="tag">Diagnostic label.</param>
    /// <param name="priority">FIFO priority within its phase.</param>
    /// <param name="group">Batch-management label.</param>
    /// <param name="cancellationToken">External lifetime cancellation.</param>
    /// <param name="stage">Initial phase, or the scheduler default.</param>
    /// <param name="ignorePause">Whether host pause leaves this task running.</param>
    /// <param name="childFailurePolicy">Failure behavior inside the task's owned flow.</param>
    /// <returns>The identity, including when the factory fails synchronously.</returns>
    public CoroutineHandle Run(Func<CoroutineContext, IEnumerator<IYieldInstruction>> factory, string? tag = null,
        CoroutinePriority priority = CoroutinePriority.Normal, string? group = null,
        CancellationToken cancellationToken = default, CoroutineExecutionStage? stage = null,
        bool ignorePause = false, CoroutineFailurePolicy childFailurePolicy = CoroutineFailurePolicy.FailFast)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Scheduler.Start(this, default, null, factory, tag, priority, group, cancellationToken, stage,
            ignorePause, childFailurePolicy);
    }

    /// <summary>Cancels current members while keeping the scope reusable. New work during cancellation is rejected.</summary>
    /// <returns>The number of live tasks present when cancellation began.</returns>
    public int Cancel()
    {
        Scheduler.AssertDriverThread();
        if (_cancelling)
        {
            return 0;
        }
        var handles = _members.ToArray();
        _cancelling = true;
        try
        {
            foreach (var handle in handles)
            {
                Scheduler.Kill(handle);
            }
        }
        finally
        {
            _cancelling = false;
        }
        return handles.Length;
    }

    /// <summary>Closes this scope before cancellation so cleanup callbacks cannot attach new work.</summary>
    public void Dispose()
    {
        Scheduler.AssertDriverThread();
        if (IsDisposed)
        {
            return;
        }
        IsDisposed = true;
        Cancel();
    }

    /// <summary>Registers lifetime ownership before any user code runs.</summary>
    internal void Register(CoroutineHandle handle)
    {
        if (IsDisposed || _cancelling)
        {
            throw new ObjectDisposedException(nameof(CoroutineScope), "The scope is closed or cancelling.");
        }
        _members.Add(handle);
    }

    /// <summary>Removes ownership before completion notification.</summary>
    internal void Unregister(CoroutineHandle handle) => _members.Remove(handle);

    /// <summary>A failure closes a FailFast scope; supervisor members retain independent results.</summary>
    internal void OnFailure()
    {
        if (FailurePolicy == CoroutineFailurePolicy.FailFast)
        {
            Dispose();
        }
    }
}
