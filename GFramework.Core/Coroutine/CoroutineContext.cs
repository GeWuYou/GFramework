// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Coroutine;

/// <summary>A live flow's explicit child-ownership boundary; do not retain it beyond the parent task's lifetime.</summary>
public sealed class CoroutineContext
{
    /// <summary>Binds a context before invoking the task factory.</summary>
    internal CoroutineContext(CoroutineScheduler scheduler, CoroutineHandle handle, CoroutineScope scope)
    {
        Scheduler = scheduler;
        Handle = handle;
        Scope = scope;
    }

    /// <summary>The driver which executes this flow.</summary>
    public CoroutineScheduler Scheduler { get; }
    /// <summary>The parent flow's identity.</summary>
    public CoroutineHandle Handle { get; }
    /// <summary>The lifetime container shared with descendants.</summary>
    public CoroutineScope Scope { get; }
    /// <summary>A lazily created token cancelled when the flow terminates unsuccessfully.</summary>
    public CancellationToken CancellationToken => Scheduler.GetContextCancellationToken(Handle);

    /// <summary>Adds an external lifetime to this flow. Callbacks only enqueue cancellation; disposal remains on the driver thread.</summary>
    /// <param name="cancellationToken">Additional lifetime token; registrations are released with the flow.</param>
    public void CancelWhen(CancellationToken cancellationToken) => Scheduler.RegisterContextCancellation(Handle, cancellationToken);

    /// <summary>Starts an owned child, inheriting phase and pause behavior unless explicitly changed.</summary>
    /// <param name="coroutine">Child iterator transferred to the scheduler.</param>
    /// <param name="tag">Optional child diagnostic label.</param>
    /// <param name="priority">Child priority.</param>
    /// <param name="group">Optional batch label.</param>
    /// <param name="cancellationToken">Additional external cancellation.</param>
    /// <param name="stage">Initial phase, defaulting to the parent's current phase.</param>
    /// <param name="ignorePause">Pause behavior, defaulting to the parent.</param>
    /// <param name="childFailurePolicy">Policy for the child's own descendants.</param>
    /// <returns>The owned child's identity.</returns>
    public CoroutineHandle RunChild(IEnumerator<IYieldInstruction>? coroutine, string? tag = null,
        CoroutinePriority priority = CoroutinePriority.Normal, string? group = null,
        CancellationToken cancellationToken = default, CoroutineExecutionStage? stage = null,
        bool? ignorePause = null, CoroutineFailurePolicy childFailurePolicy = CoroutineFailurePolicy.FailFast)
    {
        return Scheduler.StartChild(Handle, coroutine, null, tag, priority, group, cancellationToken, stage,
            ignorePause, childFailurePolicy);
    }

    /// <summary>Starts an owned child factory with access to its own context.</summary>
    /// <param name="factory">Factory executed on the driver thread.</param>
    /// <param name="tag">Optional diagnostic label.</param>
    /// <param name="priority">Child priority.</param>
    /// <param name="group">Optional batch label.</param>
    /// <param name="cancellationToken">Additional external cancellation.</param>
    /// <param name="stage">Initial phase, defaulting to the parent's current phase.</param>
    /// <param name="ignorePause">Pause behavior, defaulting to the parent.</param>
    /// <param name="childFailurePolicy">Policy for the child's descendants.</param>
    /// <returns>The owned child's identity.</returns>
    public CoroutineHandle RunChild(Func<CoroutineContext, IEnumerator<IYieldInstruction>> factory, string? tag = null,
        CoroutinePriority priority = CoroutinePriority.Normal, string? group = null,
        CancellationToken cancellationToken = default, CoroutineExecutionStage? stage = null,
        bool? ignorePause = null, CoroutineFailurePolicy childFailurePolicy = CoroutineFailurePolicy.FailFast)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Scheduler.StartChild(Handle, null, factory, tag, priority, group, cancellationToken, stage,
            ignorePause, childFailurePolicy);
    }

    /// <summary>Starts an explicitly observed child, installing its result source before start-time callbacks can evict diagnostic history.</summary>
    /// <param name="coroutine">The child iterator whose ownership is transferred to the flow.</param>
    /// <param name="result">The retained outcome, independent of bounded diagnostic history.</param>
    /// <returns>The child identity, using the same default options as an ordinary child start.</returns>
    internal CoroutineHandle RunChildObserved(IEnumerator<IYieldInstruction> coroutine, out Task<CoroutineResult> result)
    {
        var source = new TaskCompletionSource<CoroutineResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        result = source.Task;
        return Scheduler.StartChild(Handle, coroutine, null, null, CoroutinePriority.Normal, null,
            default, null, null, CoroutineFailurePolicy.FailFast, source);
    }

    /// <summary>Runs children concurrently, joining all results and propagating any failure after cleanup.</summary>
    /// <param name="coroutines">Iterators transferred to the parent flow.</param>
    /// <returns>An iterator to yield or compose inside this flow.</returns>
    public IEnumerator<IYieldInstruction> WhenAll(IEnumerable<IEnumerator<IYieldInstruction>> coroutines)
    {
        ArgumentNullException.ThrowIfNull(coroutines);
        return new CoroutineChildrenEnumerator(this, coroutines.ToArray());
    }
}
