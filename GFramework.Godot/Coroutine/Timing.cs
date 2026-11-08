// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Logging;
using Microsoft.Extensions.Logging;

namespace GFramework.Godot.Coroutine;

/// <summary>Drives one engine-neutral scheduler from Godot's process, physics and deferred callbacks.</summary>
/// <remarks>
/// Control APIs belong to the Godot main thread. A node's Supervisor scope owns its entire task tree;
/// stage changes preserve ownership and pause policy. Deferred denotes idle dispatch, not render completion.
/// </remarks>
public partial class Timing : Node
{
    private static readonly Dictionary<long, WeakReference<Timing>> ActiveInstances = [];
    private static readonly CoroutineExecutionStage[] SupportedStages =
        [CoroutineExecutionStage.Update, CoroutineExecutionStage.FixedUpdate, CoroutineExecutionStage.Deferred];
    private static Timing? _instance;
    private CoroutineScheduler? _scheduler;
    private GodotTimeSource? _processRealtime;
    private GodotTimeSource? _physicsRealtime;
    private GodotTimeSource? _deferredRealtime;
    private double _processDelta;
    private bool _deferredPending;
    private bool _isStopping;

    /// <summary>Gets managed state only while the host accepts work.</summary>
    private CoroutineScheduler Scheduler => !_isStopping && _scheduler != null
        ? _scheduler
        : throw new InvalidOperationException("Timing is not initialized or has left the scene tree.");

    /// <summary>Gets a default host initialized before deferred mounting, so its first caller can start work immediately.</summary>
    public static Timing Instance
    {
        get
        {
            if (_instance != null && IsInstanceValid(_instance) && !_instance.IsQueuedForDeletion())
                return _instance;
            if (Engine.GetMainLoop() is not SceneTree tree)
                throw new InvalidOperationException("Timing requires an active Godot SceneTree.");
            var existing = tree.Root.GetNodeOrNull<Timing>(nameof(Timing));
            if (existing != null && !existing.IsQueuedForDeletion())
            {
                existing.InitializeScheduler();
                _instance = existing;
                return existing;
            }

            var timing = new Timing { Name = nameof(Timing) };
            timing.InitializeScheduler();
            _instance = timing;
            // Root may currently be adding children. Mount later without exposing uninitialized managed state.
            Callable.From(() => MountDefaultHost(tree.Root, timing)).CallDeferred();
            return timing;
        }
    }

    /// <summary>Gets normal Update task count, including paused and waiting tasks.</summary>
    public int ProcessCoroutines => CountCoroutines(CoroutineExecutionStage.Update, ignorePause: false);

    /// <summary>Gets Update task count for tasks that ignore scene pause.</summary>
    public int ProcessIgnorePauseCoroutines => CountCoroutines(CoroutineExecutionStage.Update, ignorePause: true);

    /// <summary>Gets tasks currently assigned to physics, regardless of pause policy.</summary>
    public int PhysicsCoroutines => CountCoroutines(CoroutineExecutionStage.FixedUpdate);

    /// <summary>Gets tasks currently assigned to deferred dispatch.</summary>
    public int DeferredCoroutines => CountCoroutines(CoroutineExecutionStage.Deferred);

    /// <summary>Initializes each native entry, including re-entry after an earlier tree exit.</summary>
    public override void _EnterTree()
    {
        _isStopping = false;
        ProcessPriority = -1;
        ProcessPhysicsPriority = -1;
        ProcessMode = ProcessModeEnum.Always;
        InitializeScheduler();
    }

    /// <summary>Idempotently initializes manually mounted hosts.</summary>
    public override void _Ready() => InitializeScheduler();

    /// <summary>Cancels scopes and removes the scheduler identity on native tree exit.</summary>
    public override void _ExitTree() => DisposeScheduler();

    /// <summary>Advances Update while paused as well, allowing cancellation cleanup to run.</summary>
    /// <param name="delta">The engine's scaled process increment.</param>
    public override void _Process(double delta)
    {
        _processDelta = delta;
        AdvanceStage(CoroutineExecutionStage.Update, delta, _processRealtime, GetTree().Paused);
        if (!_deferredPending)
        {
            _deferredPending = true;
            CallDeferred(nameof(ProcessDeferred));
        }
    }

    /// <summary>Applies the same pause policy to physics as to other stages.</summary>
    /// <param name="delta">The engine's scaled physics increment.</param>
    public override void _PhysicsProcess(double delta) =>
        AdvanceStage(CoroutineExecutionStage.FixedUpdate, delta, _physicsRealtime, GetTree().Paused);

    /// <summary>Advances one coalesced idle callback; rendering need not have completed.</summary>
    private void ProcessDeferred()
    {
        _deferredPending = false;
        if (_scheduler != null && IsInsideTree())
            AdvanceStage(CoroutineExecutionStage.Deferred, _processDelta, _deferredRealtime, GetTree().Paused);
    }

    /// <summary>Initializes the default host before its first business operation.</summary>
    public static void Prewarm() => _ = Instance;

    /// <summary>Runs an independent game task that freezes during scene pause.</summary>
    /// <param name="coroutine">The iterator transferred to the scheduler.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <returns>The task handle.</returns>
    public static CoroutineHandle RunGameCoroutine(IEnumerator<IYieldInstruction> coroutine, string? tag = null) =>
        RunCoroutine(coroutine, Segment.Process, tag);

    /// <summary>Runs an independent UI task that continues during scene pause.</summary>
    /// <param name="coroutine">The iterator transferred to the scheduler.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <returns>The task handle.</returns>
    public static CoroutineHandle RunUiCoroutine(IEnumerator<IYieldInstruction> coroutine, string? tag = null) =>
        RunCoroutine(coroutine, Segment.ProcessIgnorePause, tag);

    /// <summary>Runs an independent iterator on the default host.</summary>
    /// <param name="coroutine">The iterator transferred to the scheduler.</param>
    /// <param name="segment">Its initial stage and pause policy.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <param name="cancellationToken">External cancellation drained on the host thread.</param>
    /// <returns>The task handle, or default for rejected input.</returns>
    public static CoroutineHandle RunCoroutine(IEnumerator<IYieldInstruction> coroutine,
        Segment segment = Segment.Process, string? tag = null, CancellationToken cancellationToken = default) =>
        Instance.RunCoroutineOnInstance(coroutine, segment, tag, cancellationToken);

    /// <summary>Runs a structured flow factory on the default host.</summary>
    /// <param name="factory">Creates a flow with access to explicitly owned children.</param>
    /// <param name="segment">Its initial stage and pause policy.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <param name="cancellationToken">External cancellation.</param>
    /// <returns>The flow handle.</returns>
    public static CoroutineHandle RunCoroutine(Func<CoroutineContext, IEnumerator<IYieldInstruction>> factory,
        Segment segment = Segment.Process, string? tag = null, CancellationToken cancellationToken = default) =>
        Instance.RunCoroutineOnInstance(factory, segment, tag, cancellationToken);

    /// <summary>Runs an iterator owned by a node's Supervisor scope, including descendants.</summary>
    /// <param name="owner">The live node whose tree exit closes the task scope.</param>
    /// <param name="coroutine">The iterator transferred to the scheduler.</param>
    /// <param name="segment">Its initial stage and pause policy.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <param name="cancellationToken">External cancellation.</param>
    /// <returns>The task handle.</returns>
    public static CoroutineHandle RunOwnedCoroutine(Node owner, IEnumerator<IYieldInstruction> coroutine,
        Segment segment = Segment.Process, string? tag = null, CancellationToken cancellationToken = default) =>
        Instance.RunOwnedCoroutineOnInstance(owner, coroutine, segment, tag, cancellationToken);

    /// <summary>Runs a structured flow within a node's Supervisor scope.</summary>
    /// <param name="owner">The live lifetime owner.</param>
    /// <param name="factory">Creates a flow and its owned children.</param>
    /// <param name="segment">Its initial stage and pause policy.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <param name="cancellationToken">External cancellation.</param>
    /// <returns>The flow handle.</returns>
    public static CoroutineHandle RunOwnedCoroutine(Node owner, Func<CoroutineContext, IEnumerator<IYieldInstruction>> factory,
        Segment segment = Segment.Process, string? tag = null, CancellationToken cancellationToken = default) =>
        Instance.RunOwnedCoroutineOnInstance(owner, factory, segment, tag, cancellationToken);

    /// <summary>Runs an independent iterator on this host, consuming any node-bound cancellation.</summary>
    /// <param name="coroutine">The iterator, or null to reject the request.</param>
    /// <param name="segment">Its initial stage and pause policy.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <param name="cancellationToken">External cancellation.</param>
    /// <returns>The task handle or default.</returns>
    public CoroutineHandle RunCoroutineOnInstance(IEnumerator<IYieldInstruction>? coroutine,
        Segment segment = Segment.Process, string? tag = null, CancellationToken cancellationToken = default)
    {
        if (coroutine == null)
            return default;
        return Scheduler.Run(coroutine, tag, cancellationToken: cancellationToken,
            stage: GetStage(segment), ignorePause: segment == Segment.ProcessIgnorePause);
    }

    /// <summary>Runs a structured flow factory on this host.</summary>
    /// <param name="factory">The flow factory.</param>
    /// <param name="segment">Its initial stage and pause policy.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <param name="cancellationToken">External cancellation.</param>
    /// <returns>The task handle.</returns>
    public CoroutineHandle RunCoroutineOnInstance(Func<CoroutineContext, IEnumerator<IYieldInstruction>> factory,
        Segment segment = Segment.Process, string? tag = null, CancellationToken cancellationToken = default) =>
        Scheduler.Run(factory, tag, cancellationToken: cancellationToken,
            stage: GetStage(segment), ignorePause: segment == Segment.ProcessIgnorePause);

    /// <summary>Pauses a task and its owned descendants without mounting another host.</summary>
    /// <param name="handle">The scheduler-qualified handle.</param>
    /// <returns>Whether an active task accepted the request.</returns>
    public static bool PauseCoroutine(CoroutineHandle handle) => GetInstance(handle.SchedulerId)?._scheduler?.Pause(handle) == true;

    /// <summary>Resumes a task while preserving independent child pauses.</summary>
    /// <param name="handle">The scheduler-qualified handle.</param>
    /// <returns>Whether a paused task accepted the request.</returns>
    public static bool ResumeCoroutine(CoroutineHandle handle) => GetInstance(handle.SchedulerId)?._scheduler?.Resume(handle) == true;

    /// <summary>Cancels a task tree without mounting another host.</summary>
    /// <param name="handle">The scheduler-qualified handle.</param>
    /// <returns>Whether an active task was found.</returns>
    public static bool KillCoroutine(CoroutineHandle handle) => GetInstance(handle.SchedulerId)?._scheduler?.Kill(handle) == true;

    /// <summary>Cancels tagged tasks on all registered hosts.</summary>
    /// <param name="tag">The exact management tag.</param>
    /// <returns>The number of accepted matching root requests.</returns>
    public static int KillCoroutines(string tag) => EnumerateActiveInstances().Sum(timing => timing.Scheduler.KillByTag(tag));

    /// <summary>Cancels every current host's tasks without creating a singleton as a side effect.</summary>
    /// <returns>The number of active tasks before cancellation.</returns>
    public static int KillAllCoroutines() => EnumerateActiveInstances().Sum(timing => timing.Scheduler.Clear());

    /// <summary>Queries a running task after any number of stage changes.</summary>
    /// <param name="handle">The scheduler-qualified handle.</param>
    /// <param name="snapshot">The current snapshot if found.</param>
    /// <returns>Whether the task remains active.</returns>
    public static bool TryGetCoroutineSnapshot(CoroutineHandle handle, out CoroutineSnapshot snapshot)
    {
        var scheduler = GetInstance(handle.SchedulerId)?._scheduler;
        if (scheduler != null)
            return scheduler.TryGetSnapshot(handle, out snapshot);
        snapshot = default;
        return false;
    }

    /// <summary>Reads a retained result without requiring knowledge of the task's current or original stage.</summary>
    /// <param name="handle">The scheduler-qualified handle.</param>
    /// <param name="result">The outcome when still retained by its live host.</param>
    /// <returns>Whether the host retains the terminal result.</returns>
    public static bool TryGetCoroutineResult(CoroutineHandle handle, out CoroutineResult result)
    {
        var scheduler = GetInstance(handle.SchedulerId)?._scheduler;
        if (scheduler != null)
            return scheduler.TryGetResult(handle, out result);
        result = default;
        return false;
    }

    /// <summary>Observes a task's eventual result; acquire it before host exit to retain its outcome after teardown.</summary>
    /// <param name="handle">The scheduler-qualified handle.</param>
    /// <returns>The immutable result, or Unknown if the host or retained history is unavailable.</returns>
    public static Task<CoroutineResult> WaitForResultAsync(CoroutineHandle handle) =>
        GetInstance(handle.SchedulerId)?._scheduler?.WaitForResultAsync(handle) ?? Task.FromResult(default(CoroutineResult));

    /// <summary>Looks up a scheduler identity, which is never reused when a node re-enters the tree.</summary>
    /// <param name="schedulerId">The identity exposed by the task handle.</param>
    /// <returns>The host or null after teardown.</returns>
    public static Timing? GetInstance(long schedulerId) =>
        ActiveInstances.TryGetValue(schedulerId, out var reference) && reference.TryGetTarget(out var timing) ? timing : null;

    /// <summary>Determines whether a native node can still own new tasks.</summary>
    /// <param name="node">The potential owner.</param>
    /// <returns>True only for a live attached node not queued for deletion.</returns>
    public static bool IsNodeAlive(Node? node) =>
        node != null && IsInstanceValid(node) && !node.IsQueuedForDeletion() && node.IsInsideTree();

    /// <summary>Schedules an independent delayed action.</summary>
    /// <param name="delay">The scaled delay in seconds.</param>
    /// <param name="action">The action; null creates no task.</param>
    /// <param name="segment">Its initial stage and pause policy.</param>
    /// <returns>The task handle or default.</returns>
    public static CoroutineHandle CallDelayed(double delay, Action? action, Segment segment = Segment.Process) =>
        action == null ? default : RunCoroutine(DelayedCallCoroutine(delay, action), segment);

    /// <summary>Schedules a delayed action whose task tree cannot outlive its node.</summary>
    /// <param name="delay">The scaled delay in seconds.</param>
    /// <param name="action">The action; null creates no task.</param>
    /// <param name="cancelWith">The lifetime owner.</param>
    /// <param name="segment">Its initial stage and pause policy.</param>
    /// <returns>The owned task handle or default.</returns>
    public static CoroutineHandle CallDelayed(double delay, Action? action, Node cancelWith,
        Segment segment = Segment.Process) =>
        action == null ? default : RunOwnedCoroutine(cancelWith, DelayedCallCoroutine(delay, action), segment);

    /// <summary>Initializes managed state before task prewarming or native mounting.</summary>
    private void InitializeScheduler()
    {
        if (_isStopping)
            throw new InvalidOperationException("An exiting Timing host cannot be initialized again before tree re-entry.");
        if (_scheduler != null)
            return;
        _ownerScopes ??= [];
        _processRealtime = GodotTimeSource.CreateRealtime();
        _physicsRealtime = GodotTimeSource.CreateRealtime();
        _deferredRealtime = GodotTimeSource.CreateRealtime();
        InitializeScheduler(new GodotTimeSource(() => _processDelta),
            new DeferredCoroutineLogger(static () => GodotLog.CreateLogger<CoroutineScheduler>()));
    }

    /// <summary>Registers a scheduler only after all instance state exists.</summary>
    /// <param name="timeSource">The default source for Core's convenience Update method.</param>
    /// <param name="logger">The native logger; managed test hosts omit native log rendering.</param>
    private void InitializeScheduler(ITimeSource timeSource, ILogger? logger = null)
    {
        _scheduler = new CoroutineScheduler(timeSource, logger: logger, supportedStages: SupportedStages);
        ActiveInstances.Add(_scheduler.SchedulerId, new WeakReference<Timing>(this));
    }

    /// <summary>Samples clocks continuously, including paused ticks, preventing resume-time catch-up.</summary>
    /// <param name="stage">The engine callback's stage.</param>
    /// <param name="delta">Its scaled increment.</param>
    /// <param name="realtime">Its per-stage monotonic source.</param>
    /// <param name="paused">The current scene-pause state.</param>
    private void AdvanceStage(CoroutineExecutionStage stage, double delta, GodotTimeSource? realtime, bool paused)
    {
        realtime?.Update();
        _scheduler?.Update(stage, delta, realtime?.DeltaTime ?? delta, paused);
    }

    /// <summary>Invalidates routing before callbacks can attempt replacement work on an exiting host.</summary>
    private void DisposeScheduler()
    {
        if (_scheduler == null)
            return;
        var scheduler = _scheduler;
        _isStopping = true;
        _scheduler = null;
        ActiveInstances.Remove(scheduler.SchedulerId);
        if (ReferenceEquals(_instance, this))
            _instance = null;
        List<Exception>? failures = null;
        // Close the complete runtime before user finally callbacks can publish replacement tasks through captured scopes.
        TryCleanup(scheduler.Dispose, ref failures);
        TryCleanup(DisposeOwnedScopes, ref failures);
        _processRealtime = null;
        _physicsRealtime = null;
        _deferredRealtime = null;
        _deferredPending = false;
        if (failures != null)
            throw new AggregateException("Timing shutdown cleanup failed.", failures);
    }

    /// <summary>Continues independent shutdown operations after a cleanup failure.</summary>
    /// <param name="cleanup">The operation to attempt.</param>
    /// <param name="failures">The accumulated cleanup failures.</param>
    private static void TryCleanup(Action cleanup, ref List<Exception>? failures)
    {
        try
        {
            cleanup();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }
    }

    /// <summary>Mounts only while the captured native root remains usable.</summary>
    /// <param name="root">The root captured when initialization began.</param>
    /// <param name="timing">The preinitialized host.</param>
    private static void MountDefaultHost(Node root, Timing timing)
    {
        if (!IsInstanceValid(timing) || timing.IsQueuedForDeletion() || timing.IsInsideTree())
            return;
        if (IsInstanceValid(root) && root.IsInsideTree() && !root.IsQueuedForDeletion())
            root.AddChild(timing);
        else
        {
            timing.DisposeScheduler();
            timing.QueueFree();
        }
    }

    /// <summary>Copies hosts before callbacks can mutate the registry.</summary>
    /// <returns>The hosts whose managed schedulers remain active.</returns>
    private static Timing[] EnumerateActiveInstances() => [.. ActiveInstances.Values
        .Select(reference => reference.TryGetTarget(out var timing) ? timing : null)
        .Where(timing => timing?._scheduler != null).Cast<Timing>()];

    /// <summary>Counts snapshots for diagnostics; it is deliberately outside the frame hot path.</summary>
    /// <param name="stage">The current stage to count.</param>
    /// <param name="ignorePause">An optional pause-policy filter.</param>
    /// <returns>The matching active task count.</returns>
    private int CountCoroutines(CoroutineExecutionStage stage, bool? ignorePause = null) =>
        _scheduler?.GetActiveSnapshots().Count(snapshot => snapshot.ExecutionStage == stage &&
            (!ignorePause.HasValue || snapshot.IgnorePause == ignorePause.Value)) ?? 0;

    /// <summary>Maps convenience segments to one scheduler's stages.</summary>
    /// <param name="segment">The starting segment.</param>
    /// <returns>The corresponding Core stage.</returns>
    private static CoroutineExecutionStage GetStage(Segment segment) => segment switch
    {
        Segment.Process or Segment.ProcessIgnorePause => CoroutineExecutionStage.Update,
        Segment.PhysicsProcess => CoroutineExecutionStage.FixedUpdate,
        Segment.DeferredProcess => CoroutineExecutionStage.Deferred,
        _ => throw new ArgumentOutOfRangeException(nameof(segment), segment, "Unsupported coroutine segment."),
    };

    /// <summary>Executes one action when its delay completes.</summary>
    /// <param name="delay">The scaled delay.</param>
    /// <param name="action">The action.</param>
    /// <returns>The iterator transferred to the scheduler.</returns>
    private static IEnumerator<IYieldInstruction> DelayedCallCoroutine(double delay, Action action)
    {
        yield return new Delay(delay);
        action();
    }
}
