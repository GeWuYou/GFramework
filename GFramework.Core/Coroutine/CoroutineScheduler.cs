// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Core.Coroutine;

/// <summary>
/// Single-driver coroutine runtime with explicit flow ownership and host phases. Control and update APIs belong to the construction thread;
/// only cancellation-token callbacks may enter from other threads, and they merely enqueue identities for the next host update.
/// </summary>
/// <remarks>Iteration snapshots contain generation-bearing handles, so callbacks may start, stop, or clear tasks safely. Update itself is never reentrant.</remarks>
public sealed partial class CoroutineScheduler : IDisposable
{
    private const int CompletionHistoryLimit = 1024;
    private static long _nextSchedulerId;
    private readonly ITimeSource _timeSource;
    private readonly ITimeSource _realtimeTimeSource;
    private readonly ILogger _logger;
    private readonly int _driverThreadId = System.Environment.CurrentManagedThreadId;
    private readonly HashSet<CoroutineExecutionStage> _supportedStages;
    private readonly List<CoroutineSlot?> _slots;
    private readonly List<ulong> _generations;
    private readonly Stack<int> _freeSlots = new();
    private readonly Dictionary<CoroutineHandle, CoroutineSlot> _active = [];
    private readonly LinkedList<CoroutineSlot>[,] _queues = new LinkedList<CoroutineSlot>[3, 5];
    private readonly List<CoroutineHandle> _updateSnapshot;
    private readonly Queue<CoroutineHandle> _prewarm = new();
    private readonly Queue<CoroutineHandle> _terminations = new();
    private readonly ConcurrentQueue<CoroutineHandle> _pendingKills = new();
    private readonly Dictionary<CoroutineHandle, CoroutineResult> _results = [];
    private readonly Queue<CoroutineHandle> _resultOrder = new();
    private readonly Dictionary<CoroutineHandle, TaskCompletionSource<CoroutineResult>> _resultSources = [];
    private readonly Dictionary<CoroutineHandle, TaskCompletionSource<CoroutineCompletionStatus>> _statusSources = [];
    private readonly CoroutineStatistics? _statistics;
    private bool _updating;
    private bool _prewarming;
    private bool _clearing;
    private int _executingCount;
    private bool _processingTerminations;

    /// <summary>Creates a runtime. The default host supports only its default phase; multi-phase hosts explicitly declare all phases they drive.</summary>
    /// <param name="timeSource">Scaled source used by the parameterless update API.</param>
    /// <param name="initialCapacity">Initial retained slot capacity, including zero.</param>
    /// <param name="enableStatistics">Enables timing and counter diagnostics.</param>
    /// <param name="realtimeTimeSource">Unscaled source; omitted sources reuse the scaled source.</param>
    /// <param name="executionStage">Default initial phase and parameterless update phase.</param>
    /// <param name="logger">Failure logger; omitted loggers discard output.</param>
    /// <param name="supportedStages">The nonempty host phase set, including the default phase.</param>
    public CoroutineScheduler(ITimeSource timeSource, int initialCapacity = 256, bool enableStatistics = false,
        ITimeSource? realtimeTimeSource = null, CoroutineExecutionStage executionStage = CoroutineExecutionStage.Update,
        ILogger? logger = null, IReadOnlyCollection<CoroutineExecutionStage>? supportedStages = null)
    {
        ArgumentNullException.ThrowIfNull(timeSource);
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        ValidateStage(executionStage);
        _timeSource = timeSource;
        _realtimeTimeSource = realtimeTimeSource ?? timeSource;
        _logger = logger ?? NullLogger.Instance;
        ExecutionStage = executionStage;
        _supportedStages = supportedStages == null ? [executionStage]
            : [.. supportedStages];
        if (!_supportedStages.Contains(executionStage))
        {
            throw new ArgumentException("The default phase must be supported.", nameof(supportedStages));
        }
        foreach (var stage in _supportedStages)
        {
            ValidateStage(stage);
        }
        _slots = new List<CoroutineSlot?>(initialCapacity);
        _generations = new List<ulong>(initialCapacity);
        _updateSnapshot = new List<CoroutineHandle>(initialCapacity);
        for (var phase = 0; phase < 3; phase++)
        {
            for (var priority = 0; priority < 5; priority++)
            {
                _queues[phase, priority] = new LinkedList<CoroutineSlot>();
            }
        }
        SchedulerId = Interlocked.Increment(ref _nextSchedulerId);
        _statistics = enableStatistics ? new CoroutineStatistics() : null;
        RootScope = new CoroutineScope(this, CoroutineFailurePolicy.Supervisor);
    }

    /// <summary>Stable owner identity used by host-side handle routing.</summary>
    public long SchedulerId
    {
        get;
    }
    /// <summary>Whether the runtime is closed to new work and host advancement; retained outcomes remain readable.</summary>
    public bool IsDisposed
    {
        get; private set;
    }
    /// <summary>Default initial and parameterless-update phase.</summary>
    public CoroutineExecutionStage ExecutionStage
    {
        get;
    }
    /// <summary>Scaled delta from the most recent host advancement.</summary>
    public double DeltaTime
    {
        get; private set;
    }
    /// <summary>Unscaled delta from the most recent host advancement.</summary>
    public double RealtimeDeltaTime
    {
        get; private set;
    }
    /// <summary>Independent task container; one root failure never cancels other roots.</summary>
    public CoroutineScope RootScope
    {
        get;
    }
    /// <summary>Number of live tasks, including parents joining their children.</summary>
    public int ActiveCoroutineCount => _active.Count;
    /// <summary>Retained slot storage capacity, governed by concurrent peak demand rather than lifetime starts.</summary>
    public int SlotCapacity => _slots.Capacity;
    /// <summary>High-water number of allocated slots, including currently free reusable slots.</summary>
    public int AllocatedSlotCount => _slots.Count;
    /// <summary>Optional diagnostics. Snapshot queries themselves are not allocation-free operations.</summary>
    public ICoroutineStatistics? Statistics => _statistics;
    /// <summary>Raised on the driver thread after a failure has been retained; each subscriber is isolated.</summary>
    public event EventHandler<CoroutineExceptionEventArgs>? OnCoroutineException;
    /// <summary>Raised on the driver thread after terminal cleanup and result publication; subscriber failures are logged.</summary>
    public event EventHandler<CoroutineFinishedEventArgs>? OnCoroutineFinished;

    /// <summary>Creates an independent lifetime container. Use Supervisor for node/scene containers and FailFast for one operation.</summary>
    /// <param name="policy">Immutable peer failure behavior.</param>
    /// <returns>An empty container owned by this scheduler.</returns>
    public CoroutineScope CreateScope(CoroutineFailurePolicy policy = CoroutineFailurePolicy.FailFast)
    {
        AssertDriverThread();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (policy is not (CoroutineFailurePolicy.FailFast or CoroutineFailurePolicy.Supervisor))
        {
            throw new ArgumentOutOfRangeException(nameof(policy));
        }
        return new CoroutineScope(this, policy);
    }

    /// <summary>Runs an independently owned iterator, advancing synchronously to its first wait.</summary>
    /// <param name="coroutine">Iterator transferred to the runtime.</param>
    /// <param name="tag">Diagnostic label.</param>
    /// <param name="priority">FIFO priority within its phase.</param>
    /// <param name="group">Batch-management label.</param>
    /// <param name="cancellationToken">External lifetime cancellation.</param>
    /// <param name="stage">Initial phase, or the default phase.</param>
    /// <param name="ignorePause">Whether host pause leaves this task running.</param>
    /// <param name="childFailurePolicy">Failure behavior for the task's owned children.</param>
    /// <returns>A generation-bearing identity, or default for null/already-cancelled input.</returns>
    [SuppressMessage("Design", "CA1068:CancellationToken parameters must come last",
        Justification = "Preserve the published positional parameter order; reordering would break source and binary consumers.")]
    public CoroutineHandle Run(IEnumerator<IYieldInstruction>? coroutine, string? tag = null,
        CoroutinePriority priority = CoroutinePriority.Normal, string? group = null,
        CancellationToken cancellationToken = default, CoroutineExecutionStage? stage = null,
        bool ignorePause = false, CoroutineFailurePolicy childFailurePolicy = CoroutineFailurePolicy.FailFast)
        => RootScope.Run(coroutine, tag, priority, group, cancellationToken, stage, ignorePause, childFailurePolicy);

    /// <summary>Runs a flow factory with explicit access to child ownership.</summary>
    /// <param name="factory">Factory invoked on the driver thread.</param>
    /// <param name="tag">Diagnostic label.</param>
    /// <param name="priority">FIFO priority.</param>
    /// <param name="group">Batch-management label.</param>
    /// <param name="cancellationToken">External lifetime cancellation.</param>
    /// <param name="stage">Initial phase.</param>
    /// <param name="ignorePause">Whether host pause leaves this task running.</param>
    /// <param name="childFailurePolicy">Failure behavior for owned children.</param>
    /// <returns>The new flow's identity, including synchronous failures.</returns>
    [SuppressMessage("Design", "CA1068:CancellationToken parameters must come last",
        Justification = "Preserve the published positional parameter order; reordering would break source and binary consumers.")]
    public CoroutineHandle Run(Func<CoroutineContext, IEnumerator<IYieldInstruction>> factory, string? tag = null,
        CoroutinePriority priority = CoroutinePriority.Normal, string? group = null,
        CancellationToken cancellationToken = default, CoroutineExecutionStage? stage = null,
        bool ignorePause = false, CoroutineFailurePolicy childFailurePolicy = CoroutineFailurePolicy.FailFast)
        => RootScope.Run(factory, tag, priority, group, cancellationToken, stage, ignorePause, childFailurePolicy);

    /// <summary>Whether the exact generation remains owned by this runtime.</summary>
    public bool IsCoroutineAlive(CoroutineHandle handle) => TryGetSlot(handle, out _);

    /// <summary>Reads a retained immutable outcome; terminal history is bounded to 1024 entries.</summary>
    public bool TryGetResult(CoroutineHandle handle, out CoroutineResult result) => _results.TryGetValue(handle, out result);

    /// <summary>Reads a retained terminal status.</summary>
    public bool TryGetCompletionStatus(CoroutineHandle handle, out CoroutineCompletionStatus status)
    {
        var found = TryGetResult(handle, out var result);
        status = result.Status;
        return found;
    }

    /// <summary>Obtains a lazily allocated completion task; a task obtained while alive survives history eviction.</summary>
    public Task<CoroutineResult> WaitForResultAsync(CoroutineHandle handle)
    {
        AssertDriverThread();
        if (_resultSources.TryGetValue(handle, out var source))
        {
            return source.Task;
        }
        if (!IsCoroutineAlive(handle))
        {
            return Task.FromResult(_results.GetValueOrDefault(handle));
        }
        source = new TaskCompletionSource<CoroutineResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _resultSources.Add(handle, source);
        return source.Task;
    }

    /// <summary>Obtains the terminal-status task without allocating task objects for unobserved coroutines.</summary>
    public Task<CoroutineCompletionStatus> WaitForCompletionAsync(CoroutineHandle handle)
    {
        AssertDriverThread();
        if (_statusSources.TryGetValue(handle, out var source))
        {
            return source.Task;
        }
        if (!IsCoroutineAlive(handle))
        {
            return Task.FromResult(_results.GetValueOrDefault(handle).Status);
        }
        source = new TaskCompletionSource<CoroutineCompletionStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        _statusSources.Add(handle, source);
        return source.Task;
    }

    /// <summary>Creates a non-owning wait which exposes an outcome without propagating it.</summary>
    public JoinCoroutine Join(CoroutineHandle handle)
    {
        var join = new JoinCoroutine(this, handle);
        CaptureCompletedJoin(join, handle);
        return join;
    }
    /// <summary>Creates a non-owning wait which propagates failure and cancellation to its awaiting flow.</summary>
    public AwaitCoroutine Await(CoroutineHandle handle)
    {
        var join = new AwaitCoroutine(this, handle);
        CaptureCompletedJoin(join, handle);
        return join;
    }

    private void CaptureCompletedJoin(JoinCoroutine join, CoroutineHandle handle)
    {
        if (!IsCoroutineAlive(handle) && (!handle.IsValid || handle.SchedulerId == SchedulerId))
        {
            join.Complete(_results.GetValueOrDefault(handle));
        }
    }

    /// <summary>Returns an allocating diagnostic snapshot of all active tasks.</summary>
    public IReadOnlyList<CoroutineSnapshot> GetActiveSnapshots() => [.. _active.Values.Select(CreateSnapshot)];

    /// <summary>Reads an exact live generation's current phase, ownership and wait state.</summary>
    public bool TryGetSnapshot(CoroutineHandle handle, out CoroutineSnapshot snapshot)
    {
        if (TryGetSlot(handle, out var slot))
        {
            snapshot = CreateSnapshot(slot);
            return true;
        }
        snapshot = default;
        return false;
    }

    /// <summary>Sets the task's own pause request; descendants inherit it without losing their independent requests.</summary>
    public bool Pause(CoroutineHandle handle) => SetPaused(handle, paused: true);
    /// <summary>Clears only the task's own pause request.</summary>
    public bool Resume(CoroutineHandle handle) => SetPaused(handle, paused: false);

    /// <summary>Cancels a task and its owned descendants; executing user code unwinds before disposal.</summary>
    public bool Kill(CoroutineHandle handle)
    {
        AssertDriverThread();
        if (!TryGetSlot(handle, out var slot))
        {
            return false;
        }
        RequestTermination(slot, new CoroutineResult(CoroutineCompletionStatus.Cancelled));
        RefreshStatistics();
        return true;
    }

    /// <summary>Pauses independently labelled group members.</summary>
    public int PauseGroup(string group) => ChangeMatching(slot => string.Equals(slot.Group, group, StringComparison.Ordinal), Pause);
    /// <summary>Resumes independently labelled group members.</summary>
    public int ResumeGroup(string group) => ChangeMatching(slot => string.Equals(slot.Group, group, StringComparison.Ordinal), Resume);
    /// <summary>Cancels group members and their descendants.</summary>
    public int KillGroup(string group) => ChangeMatching(slot => string.Equals(slot.Group, group, StringComparison.Ordinal), Kill);
    /// <summary>Counts live group members without assigning ownership semantics to the label.</summary>
    public int GetGroupCount(string group) => _active.Values.Count(slot => string.Equals(slot.Group, group, StringComparison.Ordinal));
    /// <summary>Cancels tagged tasks and their descendants.</summary>
    public int KillByTag(string tag) => ChangeMatching(slot => string.Equals(slot.Tag, tag, StringComparison.Ordinal), Kill);

    /// <summary>Installs a real non-owning dependency. Self, cyclic and cross-runtime dependencies are rejected.</summary>
    public void WaitForCoroutine(CoroutineHandle current, CoroutineHandle target)
    {
        AssertDriverThread();
        if (current == target)
        {
            throw new InvalidOperationException("A coroutine cannot wait for itself.");
        }
        if (TryGetSlot(current, out var slot))
        {
            if (slot.Executing)
            {
                throw new InvalidOperationException("An executing task cannot have its wait externally replaced.");
            }
            var join = Join(target);
            ValidateJoin(slot, join);
            try
            {
                ReleaseWait(slot);
                if (IsCoroutineAlive(current))
                {
                    slot.Waiting = join;
                    InstallJoin(slot, join);
                }
            }
            catch (Exception exception)
            {
                RequestTermination(slot, new CoroutineResult(CoroutineCompletionStatus.Faulted, exception));
                throw;
            }
        }
    }

    /// <summary>Cancels the current task set; cleanup callbacks cannot add work while the clear operation is active.</summary>
    public int Clear()
    {
        AssertDriverThread();
        if (_clearing)
        {
            return 0;
        }
        var handles = _active.Keys.ToArray();
        _clearing = true;
        try
        {
            foreach (var handle in handles)
            {
                Kill(handle);
            }
        }
        finally
        {
            _clearing = false;
        }
        RefreshStatistics();
        return handles.Length;
    }

    /// <summary>Closes the runtime before cancelling tasks, preventing cleanup callbacks from launching replacement work in any scope.</summary>
    public void Dispose()
    {
        AssertDriverThread();
        if (IsDisposed)
        {
            return;
        }
        IsDisposed = true;
        RootScope.Dispose();
        Clear();
    }

    /// <summary>Enforces the single-driver contract before state-changing operations.</summary>
    internal void AssertDriverThread()
    {
        if (System.Environment.CurrentManagedThreadId != _driverThreadId)
        {
            throw new InvalidOperationException("Coroutine control APIs must run on the scheduler's driver thread.");
        }
    }

    private static void ValidateStage(CoroutineExecutionStage stage)
    {
        if (stage is < CoroutineExecutionStage.Update or > CoroutineExecutionStage.Deferred)
        {
            throw new ArgumentOutOfRangeException(nameof(stage));
        }
    }

    private bool TryGetSlot(CoroutineHandle handle, out CoroutineSlot slot)
    {
        if (handle.SchedulerId == SchedulerId && handle.Generation != 0 && handle.SlotIndex >= 0 && handle.SlotIndex < _slots.Count
            && _slots[handle.SlotIndex] is { } candidate && candidate.Handle == handle)
        {
            slot = candidate;
            return true;
        }
        slot = null!;
        return false;
    }

    private bool SetPaused(CoroutineHandle handle, bool paused)
    {
        AssertDriverThread();
        if (!TryGetSlot(handle, out var slot) || slot.Paused == paused)
        {
            return false;
        }
        slot.Paused = paused;
        RefreshStatistics();
        return true;
    }

    private int ChangeMatching(Func<CoroutineSlot, bool> predicate, Func<CoroutineHandle, bool> change)
    {
        AssertDriverThread();
        return _active.Values.Where(predicate).Select(slot => slot.Handle).ToArray().Count(change);
    }

    private bool IsPaused(CoroutineSlot slot)
    {
        if (slot.Paused)
        {
            return true;
        }
        while (TryGetSlot(slot.Parent, out var parent))
        {
            if (parent.Paused)
            {
                return true;
            }
            slot = parent;
        }
        return false;
    }

    private CoroutineSnapshot CreateSnapshot(CoroutineSlot slot)
    {
        var lifetime = Stopwatch.GetElapsedTime(slot.StartedTimestamp).TotalMilliseconds;
        return new CoroutineSnapshot(slot.Handle, IsPaused(slot) ? CoroutineState.Paused : CoroutineState.Running,
            slot.Priority, slot.Tag, slot.Group, slot.StartedTimestamp * 1000.0 / Stopwatch.Frequency,
            slot.Waiting != null || slot.BodyCompleted, slot.Waiting?.GetType(), slot.Stage, slot.IgnorePause,
            slot.Parent, lifetime, slot.ExecutionTimeMs);
    }

    private void RefreshStatistics()
    {
        if (_statistics == null)
        {
            return;
        }
        var paused = 0;
        foreach (var slot in _active.Values)
        {
            if (IsPaused(slot))
            {
                paused++;
            }
        }
        _statistics.ActiveCount = ActiveCoroutineCount;
        _statistics.PausedCount = paused;
    }
}
