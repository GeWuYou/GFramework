// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Coroutine;

public sealed partial class CoroutineScheduler
{
    /// <summary>Advances the configured default time sources and default phase.</summary>
    public void Update()
    {
        AssertDriverThread();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (_updating || _prewarming)
        {
            throw new InvalidOperationException("Coroutine updates are not reentrant.");
        }
        _timeSource.Update();
        if (!ReferenceEquals(_timeSource, _realtimeTimeSource))
        {
            _realtimeTimeSource.Update();
        }
        Update(ExecutionStage, _timeSource.DeltaTime, _realtimeTimeSource.DeltaTime);
    }

    /// <summary>Advances one actual host phase. Cancellation cleanup runs even when the host is paused.</summary>
    /// <param name="stage">The phase being driven by the host.</param>
    /// <param name="deltaTime">Finite nonnegative scaled delta.</param>
    /// <param name="realtimeDeltaTime">Finite nonnegative real delta.</param>
    /// <param name="isPaused">Whether game tasks are frozen; tasks marked ignorePause continue.</param>
    public void Update(CoroutineExecutionStage stage, double deltaTime, double realtimeDeltaTime, bool isPaused = false)
    {
        AssertDriverThread();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        EnsureSupportedStage(stage);
        if (!double.IsFinite(deltaTime) || deltaTime < 0 || !double.IsFinite(realtimeDeltaTime) || realtimeDeltaTime < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), "Host deltas must be finite and nonnegative.");
        }
        if (_updating || _prewarming)
        {
            throw new InvalidOperationException("Coroutine updates are not reentrant.");
        }
        _updating = true;
        var started = _statistics == null ? 0 : Stopwatch.GetTimestamp();
        try
        {
            DeltaTime = deltaTime;
            RealtimeDeltaTime = realtimeDeltaTime;
            while (_pendingKills.TryDequeue(out var handle))
            {
                Kill(handle);
            }
            // Snapshot identities rather than list nodes: callbacks can remove/reuse slots or move phases.
            _updateSnapshot.Clear();
            for (var priority = 4; priority >= 0; priority--)
            {
                for (var node = _queues[(int)stage, priority].First; node != null; node = node.Next)
                {
                    _updateSnapshot.Add(node.Value.Handle);
                }
            }
            foreach (var handle in _updateSnapshot)
            {
                if (!TryGetSlot(handle, out var slot) || slot.Stage != stage || IsPaused(slot)
                    || (isPaused && !slot.IgnorePause))
                {
                    continue;
                }
                AdvanceSlot(slot, initial: false);
            }
        }
        finally
        {
            _updateSnapshot.Clear();
            _updating = false;
            if (_statistics is { } statistics)
            {
                statistics.LastUpdateElapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            }
            RefreshStatistics();
        }
    }

    /// <summary>Creates and registers a generation before running factory or iterator code.</summary>
    internal CoroutineHandle Start(CoroutineScope scope, CoroutineHandle parent,
        IEnumerator<IYieldInstruction>? coroutine, Func<CoroutineContext, IEnumerator<IYieldInstruction>>? factory,
        string? tag, CoroutinePriority priority, string? group,
        CoroutineExecutionStage? stage, bool ignorePause, CoroutineFailurePolicy childFailurePolicy,
        TaskCompletionSource<CoroutineResult>? resultSource = null, CancellationToken cancellationToken = default)
    {
        AssertDriverThread();
        if (IsDisposed || _clearing || scope.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(CoroutineScope), "The owner is closed or being cleared.");
        }
        if (RejectInput(coroutine, factory, cancellationToken))
        {
            resultSource?.TrySetResult(default);
            return default;
        }
        var actualStage = ValidateStartOptions(priority, stage, childFailurePolicy);
        var index = AllocateSlot();
        var handle = new CoroutineHandle(SchedulerId, index, _generations[index]);
        var slot = new CoroutineSlot
        {
            Handle = handle,
            Scope = scope,
            Parent = parent,
            Enumerator = coroutine,
            Stage = actualStage,
            Priority = priority,
            IgnorePause = ignorePause,
            Tag = tag,
            Group = group,
            CancellationToken = cancellationToken,
            StartedTimestamp = Stopwatch.GetTimestamp(),
            ChildFailurePolicy = childFailurePolicy,
        };
        try
        {
            scope.Register(handle);
        }
        catch
        {
            _freeSlots.Push(index);
            throw;
        }
        RegisterActiveSlot(slot, resultSource);
        if (cancellationToken.CanBeCanceled)
        {
            slot.CancellationRegistration = cancellationToken.Register(() => _pendingKills.Enqueue(handle));
        }
        InitializeFactory(slot, factory);
        BindCoroutineCancellation(slot);
        if (slot.PendingResult is { } pending)
        {
            RequestTermination(slot, pending);
        }
        else
        {
            _prewarm.Enqueue(handle);
            DrainPrewarm();
        }
        RefreshStatistics();
        return handle;
    }

    /// <summary>Validates launch options before reserving any task identity or ownership.</summary>
    private CoroutineExecutionStage ValidateStartOptions(CoroutinePriority priority,
        CoroutineExecutionStage? stage, CoroutineFailurePolicy childFailurePolicy)
    {
        if (priority is < CoroutinePriority.Lowest or > CoroutinePriority.Highest)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }
        ValidateFailurePolicy(childFailurePolicy);
        var actualStage = stage ?? ExecutionStage;
        EnsureSupportedStage(actualStage);
        return actualStage;
    }

    private void RegisterActiveSlot(CoroutineSlot slot, TaskCompletionSource<CoroutineResult>? resultSource)
    {
        _slots[slot.Handle.SlotIndex] = slot;
        _active.Add(slot.Handle, slot);
        if (resultSource != null)
        {
            // Explicit observers must exist before any user factory, intrinsic cancellation or completion callback can run.
            _resultSources.Add(slot.Handle, resultSource);
        }
        if (TryGetSlot(slot.Parent, out var parentSlot))
        {
            parentSlot.Children.Add(slot.Handle);
        }
        slot.QueueNode = _queues[(int)slot.Stage, (int)slot.Priority].AddLast(slot);
        _statistics?.RecordStart(slot.Priority, slot.Tag);
    }

    private bool RejectInput(IEnumerator<IYieldInstruction>? coroutine,
        Func<CoroutineContext, IEnumerator<IYieldInstruction>>? factory, CancellationToken cancellationToken)
    {
        if ((coroutine != null || factory != null) && !cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        try
        {
            coroutine?.Dispose();
        }
        catch (Exception exception)
        {
            LogSafely(exception, "Rejected coroutine cleanup failed.", default);
        }
        return true;
    }

    private static void ValidateFailurePolicy(CoroutineFailurePolicy policy)
    {
        if (policy is not (CoroutineFailurePolicy.FailFast or CoroutineFailurePolicy.Supervisor))
        {
            throw new ArgumentOutOfRangeException(nameof(policy));
        }
    }

    private void InitializeFactory(CoroutineSlot slot, Func<CoroutineContext, IEnumerator<IYieldInstruction>>? factory)
    {
        if (factory == null)
        {
            return;
        }
        slot.Executing = true;
        _executingCount++;
        try
        {
            slot.Enumerator = factory(new CoroutineContext(this, slot.Handle, slot.Scope))
                ?? throw new InvalidOperationException("A coroutine factory returned null.");
        }
        catch (Exception exception)
        {
            slot.PendingResult = CombineResults(slot.PendingResult, FailureResult(exception));
        }
        finally
        {
            slot.Executing = false;
            _executingCount--;
        }
    }

    /// <summary>
    ///     在用户枚举器取得执行机会前绑定其宿主生命周期；令牌访问失败也保留枚举器清理责任。
    /// </summary>
    private void BindCoroutineCancellation(CoroutineSlot slot)
    {
        if (slot.PendingResult != null || slot.Enumerator is not ICoroutineCancellationSource source)
        {
            return;
        }

        slot.Executing = true;
        _executingCount++;
        try
        {
            RegisterContextCancellation(slot.Handle, source.CancellationToken);
        }
        catch (Exception exception)
        {
            slot.PendingResult = CombineResults(slot.PendingResult,
                new CoroutineResult(CoroutineCompletionStatus.Faulted, exception));
        }
        finally
        {
            slot.Executing = false;
            _executingCount--;
        }
    }

    /// <summary>Starts a child after checking that its parent is still accepting owned work.</summary>
    internal CoroutineHandle StartChild(CoroutineHandle parent, IEnumerator<IYieldInstruction>? coroutine,
        Func<CoroutineContext, IEnumerator<IYieldInstruction>>? factory, string? tag, CoroutinePriority priority,
        string? group, CoroutineExecutionStage? stage, bool? ignorePause,
        CoroutineFailurePolicy childFailurePolicy, TaskCompletionSource<CoroutineResult>? resultSource = null,
        CancellationToken cancellationToken = default)
    {
        AssertDriverThread();
        if (!TryGetSlot(parent, out var slot) || slot.PendingResult != null || slot.BodyCompleted)
        {
            throw new InvalidOperationException("The parent flow no longer accepts child tasks.");
        }
        return Start(slot.Scope, parent, coroutine, factory, tag, priority, group,
            stage ?? slot.Stage, ignorePause ?? slot.IgnorePause, childFailurePolicy, resultSource, cancellationToken);
    }

    /// <summary>Creates cancellation infrastructure only when user code requests a token for an asynchronous operation.</summary>
    internal CancellationToken GetContextCancellationToken(CoroutineHandle handle)
    {
        AssertDriverThread();
        if (!TryGetSlot(handle, out var slot))
        {
            return new CancellationToken(canceled: true);
        }
        slot.CancellationSource ??= slot.CancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(slot.CancellationToken)
            : new CancellationTokenSource();
        return slot.CancellationSource.Token;
    }

    /// <summary>Registers an additional external lifetime without permitting cross-thread state mutation.</summary>
    internal void RegisterContextCancellation(CoroutineHandle handle, CancellationToken cancellationToken)
    {
        AssertDriverThread();
        if (!TryGetSlot(handle, out var slot))
        {
            throw new InvalidOperationException("The flow has already completed.");
        }
        if (!cancellationToken.CanBeCanceled)
        {
            return;
        }
        var registration = cancellationToken.Register(() => _pendingKills.Enqueue(handle));
        (slot.AdditionalCancellationRegistrations ??= []).Add(registration);
        (slot.AdditionalCancellationTokens ??= []).Add(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            RequestTermination(slot, new CoroutineResult(CoroutineCompletionStatus.Cancelled));
        }
    }

    private int AllocateSlot()
    {
        if (_freeSlots.TryPop(out var index))
        {
            _generations[index] = checked(_generations[index] + 1);
            return index;
        }
        index = _slots.Count;
        _slots.Add(null);
        _generations.Add(1);
        return index;
    }

    private void DrainPrewarm()
    {
        if (_prewarming || _executingCount != 0)
        {
            return;
        }
        _prewarming = true;
        try
        {
            // Yielding a child appends work rather than recursively advancing an unbounded iterator chain.
            while (_prewarm.TryDequeue(out var handle))
            {
                if (TryGetSlot(handle, out var slot))
                {
                    AdvanceSlot(slot, initial: true);
                }
            }
        }
        finally
        {
            _prewarming = false;
        }
    }

    private void AdvanceSlot(CoroutineSlot slot, bool initial)
    {
        if (slot.PendingResult != null || CancelIfRequested(slot) || slot.BodyCompleted)
        {
            return;
        }
        slot.Executing = true;
        _executingCount++;
        var started = _statistics == null ? 0 : Stopwatch.GetTimestamp();
        try
        {
            if ((!initial && !AdvanceWait(slot)) || slot.PendingResult != null || CancelIfRequested(slot))
            {
                return;
            }
            AdvanceIterator(slot);
        }
        catch (Exception ex)
        {
            slot.PendingResult = CombineResults(slot.PendingResult, FailureResult(ex, slot));
        }
        finally
        {
            if (_statistics != null)
            {
                slot.ExecutionTimeMs += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            }
            slot.Executing = false;
            _executingCount--;
            if (slot.PendingResult is { } result)
            {
                RequestTermination(slot, result);
            }
            DrainPrewarm();
        }
    }

    private void AdvanceIterator(CoroutineSlot slot)
    {
        if (!slot.Enumerator!.MoveNext())
        {
            CancelIfRequested(slot);
            slot.BodyCompleted = true;
            if (slot.Children.Count == 0 && slot.PendingResult == null)
            {
                slot.PendingResult = new CoroutineResult(CoroutineCompletionStatus.Completed);
            }
            return;
        }
        var instruction = slot.Enumerator.Current;
        CancelIfRequested(slot);
        AcquireYieldedInstruction(slot, instruction);
    }

    private bool CancelIfRequested(CoroutineSlot slot)
    {
        var requested = slot.CancellationToken.IsCancellationRequested;
        if (!requested && slot.AdditionalCancellationTokens != null)
        {
            foreach (var token in slot.AdditionalCancellationTokens)
            {
                if (token.IsCancellationRequested)
                {
                    requested = true;
                    break;
                }
            }
        }
        if (!requested)
        {
            return false;
        }
        // The queue was drained at tick start, but an earlier slot may have cancelled this lifetime during this same tick.
        RequestTermination(slot, new CoroutineResult(CoroutineCompletionStatus.Cancelled));
        return true;
    }

    /// <summary>
    ///     接收已经由 MoveNext 创建的等待资源；任务在同一步终止时只取得清理责任，不再启动工作。
    /// </summary>
    private void AcquireYieldedInstruction(CoroutineSlot slot, IYieldInstruction? instruction)
    {
        if (slot.PendingResult == null)
        {
            InstallInstruction(slot, instruction);
            return;
        }

        slot.Waiting = instruction;
        if (instruction is WaitForCoroutine child)
        {
            // 该子枚举器从未启动，仍须履行已经交出的资源所有权。
            slot.PendingResult = CleanupResource(slot.PendingResult.Value, child.Coroutine.Dispose);
        }
    }

    private bool AdvanceWait(CoroutineSlot slot)
    {
        var waiting = slot.Waiting;
        if (waiting == null)
        {
            return true;
        }
        waiting.Update(waiting is WaitForSecondsRealtime ? RealtimeDeltaTime : DeltaTime);
        if (slot.PendingResult != null || CancelIfRequested(slot))
        {
            return false;
        }
        var isDone = waiting.IsDone;
        if (CancelIfRequested(slot) || !isDone)
        {
            return false;
        }
        if (waiting is ICoroutineCompletionConsumer completion)
        {
            // 完成可发生在上面的 Update 与 IsDone 之间；先消费最终结果再释放等待。
            completion.ConsumeCompletion();
        }
        if (waiting is JoinCoroutine { PropagateFailure: true } join)
        {
            join.Result.ThrowIfFailed();
        }
        if (waiting is WaitForCoroutine owned)
        {
            owned.Result.ThrowIfFailed();
        }
        try
        {
            ReleaseWait(slot);
        }
        catch (Exception exception)
        {
            // Disposal is a failure boundary, even if a resource reports its failure using an OCE.
            slot.PendingResult = CombineResults(slot.PendingResult,
                new CoroutineResult(CoroutineCompletionStatus.Faulted, exception));
            return false;
        }
        return slot.PendingResult == null;
    }

    private void InstallInstruction(CoroutineSlot slot, IYieldInstruction? instruction)
    {
        slot.Waiting = instruction;
        switch (instruction)
        {
            case SwitchTo phase:
                EnsureSupportedStage(phase.Stage);
                if (slot.Stage != phase.Stage)
                {
                    _queues[(int)slot.Stage, (int)slot.Priority].Remove(slot.QueueNode!);
                    slot.Stage = phase.Stage;
                    _queues[(int)slot.Stage, (int)slot.Priority].AddLast(slot.QueueNode!);
                }
                break;
            case WaitForCoroutine child:
                // The parent owns the child before child prewarming, including immediate completion/failure.
                var handle = StartChild(slot.Handle, child.Coroutine, factory: null, tag: null, slot.Priority, group: null,
                    slot.Stage, slot.IgnorePause, slot.ChildFailurePolicy, cancellationToken: slot.CancellationToken);
                if (TryGetSlot(handle, out var target))
                {
                    slot.WaitTarget = handle;
                    target.Waiters.Add(slot.Handle);
                }
                else
                {
                    child.Complete(_results.GetValueOrDefault(handle));
                }
                break;
            case JoinCoroutine join:
                InstallJoin(slot, join);
                break;
        }
    }

    private void InstallJoin(CoroutineSlot slot, JoinCoroutine join)
    {
        ValidateJoin(slot, join);
        if (join.IsDone)
        {
            return;
        }
        if (TryGetSlot(join.Handle, out var target))
        {
            slot.WaitTarget = join.Handle;
            target.Waiters.Add(slot.Handle);
        }
        else
        {
            join.Complete(_results.GetValueOrDefault(join.Handle));
        }
    }

    private void ValidateJoin(CoroutineSlot slot, JoinCoroutine join)
    {
        if (!ReferenceEquals(join.Scheduler, this) || (join.Handle.IsValid && join.Handle.SchedulerId != SchedulerId))
        {
            throw new InvalidOperationException("Coroutine dependencies must use their owning scheduler.");
        }
        if (join.IsDone)
        {
            return;
        }
        if (DependsOn(join.Handle, slot.Handle, []))
        {
            throw new InvalidOperationException("Coroutine wait would create an ownership/dependency cycle.");
        }
    }

    private bool DependsOn(CoroutineHandle handle, CoroutineHandle sought, HashSet<CoroutineHandle> visited)
    {
        if (handle == sought)
        {
            return true;
        }
        if (!visited.Add(handle) || !TryGetSlot(handle, out var slot))
        {
            return false;
        }
        if (slot.WaitTarget.IsValid && DependsOn(slot.WaitTarget, sought, visited))
        {
            return true;
        }
        foreach (var child in slot.Children)
        {
            if (DependsOn(child, sought, visited))
            {
                return true;
            }
        }
        return false;
    }

    private void EnsureSupportedStage(CoroutineExecutionStage stage)
    {
        ValidateStage(stage);
        if (!_supportedStages.Contains(stage))
        {
            throw new InvalidOperationException($"The host does not drive coroutine phase {stage}.");
        }
    }
}
