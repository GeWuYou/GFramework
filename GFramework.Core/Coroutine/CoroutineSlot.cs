// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine;

/// <summary>One live generation; detached from indexes before user cleanup executes.</summary>
internal sealed class CoroutineSlot
{
    /// <summary>The identity checked after callbacks and snapshot traversal.</summary>
    internal required CoroutineHandle Handle;
    /// <summary>The owned iterator, released exactly once.</summary>
    internal IEnumerator<IYieldInstruction>? Enumerator;
    /// <summary>The current owned wait instruction.</summary>
    internal IYieldInstruction? Waiting;
    /// <summary>The common lifetime container.</summary>
    internal required CoroutineScope Scope;
    /// <summary>The direct owning flow, if any.</summary>
    internal CoroutineHandle Parent;
    /// <summary>Children joined before successful completion.</summary>
    internal readonly HashSet<CoroutineHandle> Children = new();
    /// <summary>Non-owning observers waiting for this task.</summary>
    internal readonly HashSet<CoroutineHandle> Waiters = new();
    /// <summary>The dependency installed by the current wait.</summary>
    internal CoroutineHandle WaitTarget;
    /// <summary>Lazily created cancellation source for task factories.</summary>
    internal CancellationTokenSource? CancellationSource;
    /// <summary>External registration; its callback only enqueues an identity.</summary>
    internal CancellationTokenRegistration CancellationRegistration;
    /// <summary>Additional factory-provided lifetimes; allocated only when requested.</summary>
    internal List<CancellationTokenRegistration>? AdditionalCancellationRegistrations;
    /// <summary>Token visible to task factories and descendants.</summary>
    internal CancellationToken CancellationToken;
    /// <summary>Intrusive phase/priority queue membership.</summary>
    internal LinkedListNode<CoroutineSlot>? QueueNode;
    /// <summary>Current host phase.</summary>
    internal CoroutineExecutionStage Stage;
    /// <summary>Order within the phase.</summary>
    internal CoroutinePriority Priority;
    /// <summary>Whether host pause leaves this task running.</summary>
    internal bool IgnorePause;
    /// <summary>Independent pause request, preserved when an ancestor resumes.</summary>
    internal bool Paused;
    /// <summary>Whether user code is currently on the stack.</summary>
    internal bool Executing;
    /// <summary>Whether the iterator has ended and is joining children.</summary>
    internal bool BodyCompleted;
    /// <summary>Termination deferred until executing code unwinds.</summary>
    internal CoroutineResult? PendingResult;
    /// <summary>Whether descendants have received the terminal cancellation request.</summary>
    internal bool ChildrenCancellationRequested;
    /// <summary>Whether the flow's asynchronous operation token has been cancelled.</summary>
    internal bool CancellationRequested;
    /// <summary>Child failure behavior, independent of the container's peer policy.</summary>
    internal CoroutineFailurePolicy ChildFailurePolicy = CoroutineFailurePolicy.FailFast;
    /// <summary>Diagnostic label without lifetime semantics.</summary>
    internal string? Tag;
    /// <summary>Batch-management label without lifetime semantics.</summary>
    internal string? Group;
    /// <summary>Monotonic wall-clock lifetime origin.</summary>
    internal long StartedTimestamp;
    /// <summary>Cumulative time spent inside iterator and instruction code.</summary>
    internal double ExecutionTimeMs;
}
