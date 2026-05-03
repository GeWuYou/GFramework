// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Core.Logging;

namespace GFramework.Core.Coroutine;

/// <summary>
///     协程调度器，用于管理和执行协程。
/// </summary>
/// <remarks>
///     该调度器按单线程驱动模型设计，业务代码应始终在同一主线程调用其控制 API。
///     唯一允许跨线程进入调度器的路径是取消令牌回调；该回调只会把待终止句柄入队，
///     真正的清理仍然在下一次 <see cref="Update" /> 中完成。
/// </remarks>
/// <param name="timeSource">缩放时间源，提供调度器默认推进所使用的时间数据。</param>
/// <param name="instanceId">协程实例编号，用于生成带宿主前缀的句柄。</param>
/// <param name="initialCapacity">调度器初始槽位容量；允许为 0，此时首次启动协程会按需自动扩容。</param>
/// <param name="enableStatistics">是否启用协程统计功能。</param>
/// <param name="realtimeTimeSource">
///     非缩放时间源。
///     若未提供，则实时等待指令会退化为使用 <paramref name="timeSource" /> 的时间增量。
/// </param>
/// <param name="executionStage">
///     当前调度器所代表的宿主阶段。
///     阶段型等待指令仅会在匹配的调度器阶段中完成。
/// </param>
public sealed class CoroutineScheduler(
    ITimeSource timeSource,
    byte instanceId = 1,
    int initialCapacity = 256,
    bool enableStatistics = false,
    ITimeSource? realtimeTimeSource = null,
    CoroutineExecutionStage executionStage = CoroutineExecutionStage.Update)
{
    private const int CompletionStatusHistoryLimit = 1024;

    private readonly Dictionary<CoroutineHandle, TaskCompletionSource<CoroutineCompletionStatus>> _completionSources =
        new();

    private readonly Dictionary<CoroutineHandle, CoroutineCompletionStatus> _completionStatuses = new();
    private readonly Queue<CoroutineHandle> _completionStatusOrder = new();
    private readonly Dictionary<string, HashSet<CoroutineHandle>> _grouped = new(StringComparer.Ordinal);
    private readonly ILogger _logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(CoroutineScheduler));
    private readonly Dictionary<CoroutineHandle, CoroutineMetadata> _metadata = new();
    private readonly ConcurrentQueue<CoroutineHandle> _pendingKills = new();

    private readonly ITimeSource _realtimeTimeSource = realtimeTimeSource ?? timeSource ??
        throw new ArgumentNullException(nameof(timeSource));

    private readonly CoroutineStatistics? _statistics = enableStatistics ? new CoroutineStatistics() : null;
    private readonly Dictionary<string, HashSet<CoroutineHandle>> _tagged = new(StringComparer.Ordinal);
    private readonly ITimeSource _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
    private readonly Dictionary<CoroutineHandle, HashSet<CoroutineHandle>> _waiting = new();
    private int _nextSlot;
    private int _pausedCount;

    private CoroutineSlot?[] _slots = new CoroutineSlot?[initialCapacity];

    /// <summary>
    ///     获取默认时间源在当前更新周期内的时间增量。
    /// </summary>
    public double DeltaTime => _timeSource.DeltaTime;

    /// <summary>
    ///     获取实时时间源在当前更新周期内的时间增量。
    /// </summary>
    public double RealtimeDeltaTime => _realtimeTimeSource.DeltaTime;

    /// <summary>
    ///     获取当前调度器代表的执行阶段。
    /// </summary>
    public CoroutineExecutionStage ExecutionStage => executionStage;

    /// <summary>
    ///     获取活跃协程数量。
    /// </summary>
    public int ActiveCoroutineCount { get; private set; }

    /// <summary>
    ///     获取协程统计信息。
    ///     仅当构造时启用了统计功能时才会返回非空对象。
    /// </summary>
    public ICoroutineStatistics? Statistics => _statistics;

    /// <summary>
    ///     当协程执行过程中发生异常时触发。
    /// </summary>
    /// <remarks>
    ///     为了避免阻塞调度器主循环，该事件会被派发到线程池回调中执行。
    ///     如果调用方需要与宿主线程保持一致，请同时订阅 <see cref="OnCoroutineFinished" />。
    /// </remarks>
    public event EventHandler<CoroutineExceptionEventArgs>? OnCoroutineException;

    /// <summary>
    ///     当协程以完成、取消或失败任一结果结束时触发。
    /// </summary>
    /// <remarks>
    ///     该事件在调度器所在的驱动线程中同步触发，适合与宿主生命周期管理逻辑集成。
    /// </remarks>
    public event EventHandler<CoroutineFinishedEventArgs>? OnCoroutineFinished;

    /// <summary>
    ///     检查指定协程句柄是否仍然处于活跃状态。
    /// </summary>
    /// <param name="handle">要检查的协程句柄。</param>
    /// <returns>如果协程仍受该调度器管理，则返回 <see langword="true" />。</returns>
    public bool IsCoroutineAlive(CoroutineHandle handle)
    {
        return _metadata.ContainsKey(handle);
    }

    /// <summary>
    ///     尝试获取指定句柄的当前运行快照。
    /// </summary>
    /// <param name="handle">要查询的协程句柄。</param>
    /// <param name="snapshot">查询成功时返回协程快照。</param>
    /// <returns>如果句柄当前仍然活跃，则返回 <see langword="true" />。</returns>
    public bool TryGetSnapshot(CoroutineHandle handle, out CoroutineSnapshot snapshot)
    {
        if (!_metadata.TryGetValue(handle, out var meta))
        {
            snapshot = default;
            return false;
        }

        var slot = _slots[meta.SlotIndex];
        if (slot == null)
        {
            snapshot = default;
            return false;
        }

        snapshot = CreateSnapshot(meta, slot);
        return true;
    }

    /// <summary>
    ///     获取所有活跃协程的运行快照。
    /// </summary>
    /// <returns>包含所有活跃协程的快照列表。</returns>
    public IReadOnlyList<CoroutineSnapshot> GetActiveSnapshots()
    {
        return _metadata
            .Select(pair => pair.Value)
            .Select(meta => new
            {
                Metadata = meta,
                Slot = _slots[meta.SlotIndex]
            })
            .Where(item => item.Slot is not null)
            .Select(item => CreateSnapshot(item.Metadata, item.Slot!))
            .ToArray();
    }

    /// <summary>
    ///     获取指定协程的完成任务。
    /// </summary>
    /// <param name="handle">要等待完成的协程句柄。</param>
    /// <returns>返回表示协程最终结果的任务。</returns>
    /// <remarks>
    ///     如果句柄已经结束，则返回一个已完成任务。
    ///     如果句柄无效或结果未知，则任务结果为 <see cref="CoroutineCompletionStatus.Unknown" />。
    /// </remarks>
    public Task<CoroutineCompletionStatus> WaitForCompletionAsync(CoroutineHandle handle)
    {
        if (_completionSources.TryGetValue(handle, out var source))
        {
            return source.Task;
        }

        return Task.FromResult(
            TryGetCompletionStatus(handle, out var status)
                ? status
                : CoroutineCompletionStatus.Unknown);
    }

    /// <summary>
    ///     尝试读取协程的已知最终结果。
    /// </summary>
    /// <param name="handle">要查询的协程句柄。</param>
    /// <param name="status">如果查询成功则返回最终状态。</param>
    /// <returns>当调度器仍保留该句柄的完成历史时返回 <see langword="true" />。</returns>
    public bool TryGetCompletionStatus(CoroutineHandle handle, out CoroutineCompletionStatus status)
    {
        return _completionStatuses.TryGetValue(handle, out status);
    }


    #region Run / Update

    /// <summary>
    ///     运行协程。
    /// </summary>
    /// <param name="coroutine">要运行的协程枚举器。</param>
    /// <param name="tag">协程标签，可选。</param>
    /// <param name="priority">协程优先级，默认为 <see cref="CoroutinePriority.Normal" />。</param>
    /// <param name="group">协程分组，可选。</param>
    /// <param name="cancellationToken">用于取消协程的外部令牌。</param>
    /// <returns>返回新创建的协程句柄；如果输入为空或取消已请求，则返回无效句柄。</returns>
    public CoroutineHandle Run(
        IEnumerator<IYieldInstruction>? coroutine,
        string? tag = null,
        CoroutinePriority priority = CoroutinePriority.Normal,
        string? group = null,
        CancellationToken cancellationToken = default)
    {
        if (coroutine == null || cancellationToken.IsCancellationRequested)
        {
            return default;
        }

        var handle = new CoroutineHandle(instanceId);
        var slotIndex = AllocateSlotIndex();
        var slot = CreateRunningSlot(handle, coroutine, priority, cancellationToken);
        RegisterStartedCoroutine(handle, slotIndex, slot, priority, tag, group);
        Prewarm(slotIndex);
        UpdateStatisticsSnapshot();

        return handle;
    }

    /// <summary>
    ///     推进当前调度器中的所有协程。
    /// </summary>
    public void Update()
    {
        _timeSource.Update();
        if (!ReferenceEquals(_realtimeTimeSource, _timeSource))
        {
            _realtimeTimeSource.Update();
        }

        ProcessPendingKills();
        UpdateStatisticsSnapshot();

        var sortedIndices = new List<int>(_nextSlot);
        for (var i = 0; i < _nextSlot; i++)
        {
            var slot = _slots[i];
            if (slot is { State: CoroutineState.Running })
            {
                sortedIndices.Add(i);
            }
        }

        sortedIndices.Sort((a, b) =>
        {
            var slotA = _slots[a];
            var slotB = _slots[b];
            if (slotA == null || slotB == null)
            {
                return 0;
            }

            return slotB.Priority.CompareTo(slotA.Priority);
        });

        foreach (var i in sortedIndices)
        {
            var slot = _slots[i];
            if (slot is not { State: CoroutineState.Running })
            {
                continue;
            }

            try
            {
                ProcessWaitingInstruction(slot);

                if (!IsWaiting(slot))
                {
                    ProcessCoroutineStep(slot, i);
                }
            }
            catch (Exception ex)
            {
                OnError(i, ex);
            }
        }

        UpdateStatisticsSnapshot();
    }

    /// <summary>
    ///     处理协程当前等待指令的推进。
    /// </summary>
    /// <param name="slot">当前协程槽位。</param>
    private void ProcessWaitingInstruction(CoroutineSlot slot)
    {
        if (slot.Waiting == null)
        {
            return;
        }

        if (!CanAdvanceInstruction(slot.Waiting))
        {
            return;
        }

        slot.Waiting.Update(GetInstructionDelta(slot.Waiting));
    }

    /// <summary>
    ///     判断协程当前是否仍处于阻塞等待状态。
    /// </summary>
    /// <param name="slot">协程槽位。</param>
    /// <returns>如果协程仍需等待则返回 <see langword="true" />。</returns>
    private static bool IsWaiting(CoroutineSlot slot)
    {
        return slot.Waiting != null && !slot.Waiting.IsDone;
    }

    /// <summary>
    ///     推进协程到下一条等待指令，或在枚举器结束时完成协程。
    /// </summary>
    /// <param name="slot">要推进的协程槽位。</param>
    /// <param name="slotIndex">协程槽位索引。</param>
    private void ProcessCoroutineStep(CoroutineSlot slot, int slotIndex)
    {
        DisposeWaitingInstruction(slot);

        if (!slot.Enumerator.MoveNext())
        {
            FinalizeCoroutine(slotIndex, CoroutineCompletionStatus.Completed);
            return;
        }

        var current = slot.Enumerator.Current;
        HandleYieldInstruction(slot, current);
    }

    /// <summary>
    ///     处理协程返回的等待指令。
    /// </summary>
    /// <param name="slot">当前协程槽位。</param>
    /// <param name="instruction">当前等待指令。</param>
    private void HandleYieldInstruction(CoroutineSlot slot, IYieldInstruction instruction)
    {
        switch (instruction)
        {
            case WaitForCoroutine waitForCoroutine:
            {
                var targetHandle = Run(waitForCoroutine.Coroutine, cancellationToken: slot.CancellationToken);
                if (!targetHandle.IsValid)
                {
                    waitForCoroutine.Complete();
                    slot.Waiting = null;
                    break;
                }

                slot.Waiting = waitForCoroutine;
                WaitForCoroutine(slot.Handle, targetHandle);
                break;
            }

            default:
                slot.Waiting = instruction;
                break;
        }
    }

    #endregion

    #region Pause / Resume / Kill

    /// <summary>
    ///     暂停指定协程。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <returns>如果成功暂停则返回 <see langword="true" />。</returns>
    public bool Pause(CoroutineHandle handle)
    {
        if (!_metadata.TryGetValue(handle, out var meta))
        {
            return false;
        }

        var slot = _slots[meta.SlotIndex];
        if (slot == null || slot.State != CoroutineState.Running)
        {
            return false;
        }

        slot.State = CoroutineState.Paused;
        meta.State = CoroutineState.Paused;
        _pausedCount++;
        UpdateStatisticsSnapshot();
        return true;
    }

    /// <summary>
    ///     恢复指定协程。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <returns>如果成功恢复则返回 <see langword="true" />。</returns>
    public bool Resume(CoroutineHandle handle)
    {
        if (!_metadata.TryGetValue(handle, out var meta))
        {
            return false;
        }

        var slot = _slots[meta.SlotIndex];
        if (slot == null || slot.State != CoroutineState.Paused)
        {
            return false;
        }

        slot.State = CoroutineState.Running;
        meta.State = CoroutineState.Running;
        _pausedCount--;
        UpdateStatisticsSnapshot();
        return true;
    }

    /// <summary>
    ///     终止指定协程。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <returns>如果成功终止则返回 <see langword="true" />。</returns>
    public bool Kill(CoroutineHandle handle)
    {
        if (!_metadata.TryGetValue(handle, out var meta))
        {
            return false;
        }

        FinalizeCoroutine(meta.SlotIndex, CoroutineCompletionStatus.Cancelled);
        UpdateStatisticsSnapshot();
        return true;
    }

    #endregion

    #region Group Management

    /// <summary>
    ///     暂停指定分组的所有协程。
    /// </summary>
    /// <param name="group">分组名称。</param>
    /// <returns>实际被暂停的协程数量。</returns>
    public int PauseGroup(string group)
    {
        if (!_grouped.TryGetValue(group, out var handles))
        {
            return 0;
        }

        return handles.Count(Pause);
    }

    /// <summary>
    ///     恢复指定分组的所有协程。
    /// </summary>
    /// <param name="group">分组名称。</param>
    /// <returns>实际被恢复的协程数量。</returns>
    public int ResumeGroup(string group)
    {
        if (!_grouped.TryGetValue(group, out var handles))
        {
            return 0;
        }

        return handles.Count(Resume);
    }

    /// <summary>
    ///     终止指定分组的所有协程。
    /// </summary>
    /// <param name="group">分组名称。</param>
    /// <returns>实际被终止的协程数量。</returns>
    public int KillGroup(string group)
    {
        if (!_grouped.TryGetValue(group, out var handles))
        {
            return 0;
        }

        var copy = handles.ToArray();
        return copy.Count(Kill);
    }

    /// <summary>
    ///     获取指定分组当前包含的活跃协程数量。
    /// </summary>
    /// <param name="group">分组名称。</param>
    /// <returns>分组中的活跃协程数量。</returns>
    public int GetGroupCount(string group)
    {
        return _grouped.TryGetValue(group, out var handles) ? handles.Count : 0;
    }

    #endregion

    #region Wait / Tag / Clear

    /// <summary>
    ///     让当前协程等待目标协程完成。
    /// </summary>
    /// <param name="current">当前协程句柄。</param>
    /// <param name="target">目标协程句柄。</param>
    public void WaitForCoroutine(CoroutineHandle current, CoroutineHandle target)
    {
        if (current == target)
        {
            throw new InvalidOperationException("Coroutine cannot wait for itself.");
        }

        if (!_metadata.ContainsKey(target))
        {
            return;
        }

        if (!_waiting.TryGetValue(target, out var set))
        {
            set = [];
            _waiting[target] = set;
        }

        set.Add(current);
    }

    /// <summary>
    ///     根据标签终止协程。
    /// </summary>
    /// <param name="tag">协程标签。</param>
    /// <returns>被终止的协程数量。</returns>
    public int KillByTag(string tag)
    {
        if (!_tagged.TryGetValue(tag, out var handles))
        {
            return 0;
        }

        var copy = handles.ToArray();
        return copy.Count(Kill);
    }

    /// <summary>
    ///     清空当前调度器中的所有协程。
    /// </summary>
    /// <returns>被清理的协程数量。</returns>
    public int Clear()
    {
        var count = ActiveCoroutineCount;

        for (var i = 0; i < _nextSlot; i++)
        {
            if (_slots[i] != null)
            {
                FinalizeCoroutine(i, CoroutineCompletionStatus.Cancelled);
            }
        }

        Array.Clear(_slots);
        _metadata.Clear();
        _tagged.Clear();
        _grouped.Clear();
        _waiting.Clear();
        _completionStatuses.Clear();
        _completionStatusOrder.Clear();

        _nextSlot = 0;
        ActiveCoroutineCount = 0;
        _pausedCount = 0;
        UpdateStatisticsSnapshot();

        return count;
    }

    #endregion

    #region Internal

    /// <summary>
    ///     预热协程槽位，执行协程的第一步。
    /// </summary>
    /// <param name="slotIndex">槽位索引。</param>
    private void Prewarm(int slotIndex)
    {
        var slot = _slots[slotIndex];
        if (slot == null)
        {
            return;
        }

        try
        {
            if (!slot.Enumerator.MoveNext())
            {
                FinalizeCoroutine(slotIndex, CoroutineCompletionStatus.Completed);
            }
            else
            {
                HandleYieldInstruction(slot, slot.Enumerator.Current);
            }
        }
        catch (Exception ex)
        {
            OnError(slotIndex, ex);
        }
    }

    /// <summary>
    ///     按给定结果完成协程并释放相关资源。
    /// </summary>
    /// <param name="slotIndex">目标槽位索引。</param>
    /// <param name="completionStatus">最终结果。</param>
    /// <param name="exception">若协程失败，则传入对应异常。</param>
    private void FinalizeCoroutine(
        int slotIndex,
        CoroutineCompletionStatus completionStatus,
        Exception? exception = null)
    {
        if (!TryGetFinalizableCoroutine(slotIndex, out var slot, out var handle))
        {
            return;
        }

        UpdateCompletionMetadata(handle, completionStatus);
        ReleaseCompletedCoroutine(slotIndex, slot, handle);
        CompleteCoroutineLifecycle(handle, completionStatus);
        OnCoroutineFinished?.Invoke(this, new CoroutineFinishedEventArgs(handle, completionStatus, exception));
    }

    /// <summary>
    ///     处理协程执行中的错误。
    /// </summary>
    /// <param name="slotIndex">槽位索引。</param>
    /// <param name="ex">异常对象。</param>
    private void OnError(int slotIndex, Exception ex)
    {
        var slot = _slots[slotIndex];
        var handle = slot?.Handle ?? default;

        var handler = OnCoroutineException;
        if (handler != null)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    handler(this, new CoroutineExceptionEventArgs(handle, ex));
                }
                catch (Exception callbackEx)
                {
                    _logger.Error($"[CoroutineScheduler] Exception in error callback: {callbackEx}");
                }
            });
        }

        _logger.Error($"[CoroutineScheduler] Coroutine {handle.ToString()} failed with exception: {ex}");
        FinalizeCoroutine(slotIndex, CoroutineCompletionStatus.Faulted, ex);
    }

    /// <summary>
    ///     判断指定等待指令是否允许在当前调度器阶段中推进。
    /// </summary>
    /// <param name="instruction">要检查的等待指令。</param>
    /// <returns>如果当前阶段允许推进该等待指令，则返回 <see langword="true" />。</returns>
    private bool CanAdvanceInstruction(IYieldInstruction instruction)
    {
        return instruction switch
        {
            WaitForFixedUpdate => executionStage == CoroutineExecutionStage.FixedUpdate,
            WaitForEndOfFrame => executionStage == CoroutineExecutionStage.EndOfFrame,
            _ => true
        };
    }

    /// <summary>
    ///     为指定等待指令选择合适的时间增量。
    /// </summary>
    /// <param name="instruction">待推进的等待指令。</param>
    /// <returns>与等待语义匹配的时间增量。</returns>
    private double GetInstructionDelta(IYieldInstruction instruction)
    {
        return instruction switch
        {
            WaitForSecondsRealtime => RealtimeDeltaTime,
            _ => DeltaTime
        };
    }

    /// <summary>
    ///     处理跨线程入队的待终止协程。
    /// </summary>
    private void ProcessPendingKills()
    {
        while (_pendingKills.TryDequeue(out var handle))
        {
            Kill(handle);
        }
    }

    /// <summary>
    ///     为新协程分配槽位索引，并在需要时扩容槽位数组。
    /// </summary>
    /// <returns>可写入的新槽位索引。</returns>
    private int AllocateSlotIndex()
    {
        if (_nextSlot >= _slots.Length)
        {
            Expand();
        }

        return _nextSlot++;
    }

    /// <summary>
    ///     创建处于运行态的协程槽位，并在需要时挂接跨线程取消回调。
    /// </summary>
    /// <param name="handle">新协程句柄。</param>
    /// <param name="coroutine">协程枚举器。</param>
    /// <param name="priority">协程优先级。</param>
    /// <param name="cancellationToken">外部取消令牌。</param>
    /// <returns>已初始化的协程槽位。</returns>
    private CoroutineSlot CreateRunningSlot(
        CoroutineHandle handle,
        IEnumerator<IYieldInstruction> coroutine,
        CoroutinePriority priority,
        CancellationToken cancellationToken)
    {
        var slot = new CoroutineSlot
        {
            CancellationToken = cancellationToken,
            Enumerator = coroutine,
            State = CoroutineState.Running,
            Handle = handle,
            Priority = priority
        };

        RegisterCancellationCallback(slot, handle, cancellationToken);
        return slot;
    }

    /// <summary>
    ///     为支持取消的协程注册待终止排队回调。
    /// </summary>
    /// <param name="slot">目标协程槽位。</param>
    /// <param name="handle">协程句柄。</param>
    /// <param name="cancellationToken">外部取消令牌。</param>
    private void RegisterCancellationCallback(
        CoroutineSlot slot,
        CoroutineHandle handle,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            return;
        }

        // 取消回调可能在任意线程触发，因此这里只做排队，真正清理由 Update 主线程完成。
        slot.CancellationRegistration = cancellationToken.Register(() => _pendingKills.Enqueue(handle));
    }

    /// <summary>
    ///     将新协程写入调度器的槽位、元数据、标签分组和完成状态跟踪结构。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <param name="slotIndex">槽位索引。</param>
    /// <param name="slot">已初始化的协程槽位。</param>
    /// <param name="priority">协程优先级。</param>
    /// <param name="tag">可选标签。</param>
    /// <param name="group">可选分组。</param>
    private void RegisterStartedCoroutine(
        CoroutineHandle handle,
        int slotIndex,
        CoroutineSlot slot,
        CoroutinePriority priority,
        string? tag,
        string? group)
    {
        _slots[slotIndex] = slot;
        _metadata[handle] = CreateCoroutineMetadata(slotIndex, priority, tag, group);
        ResetCompletionTracking(handle);

        if (!string.IsNullOrEmpty(tag))
        {
            AddTag(tag, handle);
        }

        if (!string.IsNullOrEmpty(group))
        {
            AddGroup(group, handle);
        }

        _statistics?.RecordStart(priority, tag);
        ActiveCoroutineCount++;
    }

    /// <summary>
    ///     创建新协程的初始元数据。
    /// </summary>
    /// <param name="slotIndex">槽位索引。</param>
    /// <param name="priority">协程优先级。</param>
    /// <param name="tag">可选标签。</param>
    /// <param name="group">可选分组。</param>
    /// <returns>与新槽位对应的元数据对象。</returns>
    private CoroutineMetadata CreateCoroutineMetadata(
        int slotIndex,
        CoroutinePriority priority,
        string? tag,
        string? group)
    {
        return new CoroutineMetadata
        {
            ExecutionStage = executionStage,
            Group = group,
            Priority = priority,
            SlotIndex = slotIndex,
            StartTime = _timeSource.CurrentTime * 1000,
            State = CoroutineState.Running,
            Tag = tag
        };
    }

    /// <summary>
    ///     重置协程完成跟踪，使复用句柄不会携带上一轮完成结果。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    private void ResetCompletionTracking(CoroutineHandle handle)
    {
        _completionSources[handle] =
            new TaskCompletionSource<CoroutineCompletionStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        _completionStatuses.Remove(handle);
    }

    /// <summary>
    ///     释放单个槽位持有的资源。
    /// </summary>
    /// <param name="slot">待释放的槽位。</param>
    private static void DisposeSlotResources(CoroutineSlot slot)
    {
        DisposeWaitingInstruction(slot);
        slot.CancellationRegistration.Dispose();
        slot.Enumerator.Dispose();
    }

    /// <summary>
    ///     如果当前等待指令实现了可释放接口，则在协程继续前先释放该指令。
    /// </summary>
    /// <param name="slot">当前协程槽位。</param>
    private static void DisposeWaitingInstruction(CoroutineSlot slot)
    {
        if (slot.Waiting is IDisposable disposable)
        {
            disposable.Dispose();
        }

        slot.Waiting = null;
    }

    /// <summary>
    ///     读取可被完成处理的协程槽位与句柄。
    ///     当槽位已空或句柄已失效时，说明该协程已经被其他路径清理，无需重复执行结束逻辑。
    /// </summary>
    /// <param name="slotIndex">槽位索引。</param>
    /// <param name="slot">若成功则返回槽位。</param>
    /// <param name="handle">若成功则返回句柄。</param>
    /// <returns>当存在可完成的协程时返回 <see langword="true" />。</returns>
    private bool TryGetFinalizableCoroutine(int slotIndex, out CoroutineSlot slot, out CoroutineHandle handle)
    {
        var candidate = _slots[slotIndex];
        if (candidate == null)
        {
            slot = null!;
            handle = default;
            return false;
        }

        handle = candidate.Handle;
        if (!handle.IsValid)
        {
            slot = null!;
            return false;
        }

        slot = candidate;
        return true;
    }

    /// <summary>
    ///     根据最终状态更新协程元数据与统计信息。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <param name="completionStatus">最终结果。</param>
    private void UpdateCompletionMetadata(CoroutineHandle handle, CoroutineCompletionStatus completionStatus)
    {
        if (!_metadata.TryGetValue(handle, out var meta))
        {
            return;
        }

        if (meta.State == CoroutineState.Paused && _pausedCount > 0)
        {
            _pausedCount--;
        }

        ApplyCompletionMetadata(meta, completionStatus);
    }

    /// <summary>
    ///     将最终结果映射到元数据状态和统计记录。
    /// </summary>
    /// <param name="meta">协程元数据。</param>
    /// <param name="completionStatus">最终结果。</param>
    private void ApplyCompletionMetadata(CoroutineMetadata meta, CoroutineCompletionStatus completionStatus)
    {
        var executionTime = _timeSource.CurrentTime * 1000 - meta.StartTime;
        switch (completionStatus)
        {
            case CoroutineCompletionStatus.Completed:
                meta.State = CoroutineState.Completed;
                _statistics?.RecordComplete(executionTime, meta.Priority, meta.Tag);
                break;

            case CoroutineCompletionStatus.Faulted:
                meta.State = CoroutineState.Completed;
                _statistics?.RecordFailure(meta.Priority, meta.Tag);
                break;

            case CoroutineCompletionStatus.Cancelled:
                meta.State = CoroutineState.Cancelled;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(completionStatus),
                    completionStatus,
                    "Unsupported coroutine completion status.");
        }
    }

    /// <summary>
    ///     释放已结束协程占用的槽位和索引结构。
    /// </summary>
    /// <param name="slotIndex">槽位索引。</param>
    /// <param name="slot">已结束的协程槽位。</param>
    /// <param name="handle">协程句柄。</param>
    private void ReleaseCompletedCoroutine(int slotIndex, CoroutineSlot slot, CoroutineHandle handle)
    {
        DisposeSlotResources(slot);

        _slots[slotIndex] = null;
        if (ActiveCoroutineCount > 0)
        {
            ActiveCoroutineCount--;
        }

        RemoveTag(handle);
        RemoveGroup(handle);
        _metadata.Remove(handle);
    }

    /// <summary>
    ///     完成协程的等待者唤醒、任务结果和完成历史记录。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <param name="completionStatus">最终结果。</param>
    private void CompleteCoroutineLifecycle(CoroutineHandle handle, CoroutineCompletionStatus completionStatus)
    {
        WakeWaiters(handle);

        if (_completionSources.Remove(handle, out var source))
        {
            source.TrySetResult(completionStatus);
        }

        RecordCompletionStatus(handle, completionStatus);
    }

    /// <summary>
    ///     唤醒所有等待目标协程完成的协程。
    /// </summary>
    /// <param name="handle">已结束的目标协程句柄。</param>
    private void WakeWaiters(CoroutineHandle handle)
    {
        if (!_waiting.TryGetValue(handle, out var waiters))
        {
            return;
        }

        foreach (var waiter in waiters)
        {
            if (!_metadata.TryGetValue(waiter, out var waiterMeta))
            {
                continue;
            }

            var waiterSlot = _slots[waiterMeta.SlotIndex];
            if (waiterSlot == null)
            {
                continue;
            }

            if (waiterSlot.Waiting is WaitForCoroutine waitForCoroutine)
            {
                waitForCoroutine.Complete();
            }

            if (waiterSlot.State != CoroutineState.Paused)
            {
                waiterSlot.State = CoroutineState.Running;
                waiterMeta.State = CoroutineState.Running;
            }
        }

        _waiting.Remove(handle);
    }

    /// <summary>
    ///     创建协程快照。
    /// </summary>
    /// <param name="metadata">协程元数据。</param>
    /// <param name="slot">协程槽位。</param>
    /// <returns>与当前槽位一致的只读快照。</returns>
    private static CoroutineSnapshot CreateSnapshot(CoroutineMetadata metadata, CoroutineSlot slot)
    {
        return new CoroutineSnapshot(
            slot.Handle,
            metadata.State,
            metadata.Priority,
            metadata.Tag,
            metadata.Group,
            metadata.StartTime,
            IsWaiting(slot),
            slot.Waiting?.GetType(),
            metadata.ExecutionStage);
    }

    /// <summary>
    ///     扩展协程槽位数组容量。
    /// </summary>
    private void Expand()
    {
        // 允许构造器以 0 容量启动，用于极简场景或测试；首次分配时至少扩到 1，避免后续写槽位越界。
        var expandedLength = Math.Max(1, _slots.Length * 2);
        Array.Resize(ref _slots, expandedLength);
    }

    /// <summary>
    ///     更新统计对象中的活动快照数据。
    /// </summary>
    private void UpdateStatisticsSnapshot()
    {
        if (_statistics == null)
        {
            return;
        }

        _statistics.ActiveCount = ActiveCoroutineCount;
        _statistics.PausedCount = _pausedCount;
    }

    /// <summary>
    ///     记录协程最终状态，并对历史缓存施加固定上限，避免完成状态字典无限增长。
    /// </summary>
    /// <param name="handle">已结束的协程句柄。</param>
    /// <param name="completionStatus">协程最终状态。</param>
    private void RecordCompletionStatus(CoroutineHandle handle, CoroutineCompletionStatus completionStatus)
    {
        _completionStatuses[handle] = completionStatus;
        _completionStatusOrder.Enqueue(handle);

        while (_completionStatusOrder.Count > CompletionStatusHistoryLimit)
        {
            var expiredHandle = _completionStatusOrder.Dequeue();
            _completionStatuses.Remove(expiredHandle);
        }
    }

    /// <summary>
    ///     为协程添加标签。
    /// </summary>
    /// <param name="tag">标签名称。</param>
    /// <param name="handle">协程句柄。</param>
    private void AddTag(string tag, CoroutineHandle handle)
    {
        if (!_tagged.TryGetValue(tag, out var set))
        {
            set = new HashSet<CoroutineHandle>();
            _tagged[tag] = set;
        }

        set.Add(handle);
        _metadata[handle].Tag = tag;
    }

    /// <summary>
    ///     移除协程标签。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    private void RemoveTag(CoroutineHandle handle)
    {
        if (!_metadata.TryGetValue(handle, out var meta) || meta.Tag == null)
        {
            return;
        }

        if (_tagged.TryGetValue(meta.Tag, out var set))
        {
            set.Remove(handle);
            if (set.Count == 0)
            {
                _tagged.Remove(meta.Tag);
            }
        }

        meta.Tag = null;
    }

    /// <summary>
    ///     为协程添加分组。
    /// </summary>
    /// <param name="group">分组名称。</param>
    /// <param name="handle">协程句柄。</param>
    private void AddGroup(string group, CoroutineHandle handle)
    {
        if (!_grouped.TryGetValue(group, out var set))
        {
            set = new HashSet<CoroutineHandle>();
            _grouped[group] = set;
        }

        set.Add(handle);
        _metadata[handle].Group = group;
    }

    /// <summary>
    ///     移除协程分组。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    private void RemoveGroup(CoroutineHandle handle)
    {
        if (!_metadata.TryGetValue(handle, out var meta) || meta.Group == null)
        {
            return;
        }

        if (_grouped.TryGetValue(meta.Group, out var set))
        {
            set.Remove(handle);
            if (set.Count == 0)
            {
                _grouped.Remove(meta.Group);
            }
        }

        meta.Group = null;
    }

    #endregion
}
