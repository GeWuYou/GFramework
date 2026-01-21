using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine;

/// <summary>
/// 协程调度器，用于管理和执行协程
/// </summary>
/// <param name="timeSource">时间源接口，提供时间相关数据</param>
/// <param name="instanceId">实例ID，默认为1</param>
/// <param name="initialCapacity">初始容量，默认为256</param>
public sealed class CoroutineScheduler(
    ITimeSource timeSource,
    byte instanceId = 1,
    int initialCapacity = 256)
{
    private readonly Dictionary<CoroutineHandle, CoroutineMetadata> _metadata = new();
    private readonly Dictionary<string, HashSet<CoroutineHandle>> _tagged = new();
    private readonly ITimeSource _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
    private readonly Dictionary<CoroutineHandle, HashSet<CoroutineHandle>> _waiting = new();
    private int _activeCount;
    private int _nextSlot;

    private CoroutineSlot?[] _slots = new CoroutineSlot?[initialCapacity];

    /// <summary>
    /// 获取时间差值
    /// </summary>
    public double DeltaTime => _timeSource.DeltaTime;

    /// <summary>
    /// 获取活跃协程数量
    /// </summary>
    public int ActiveCoroutineCount => _activeCount;

    #region Run / Update

    /// <summary>
    /// 运行协程
    /// </summary>
    /// <param name="coroutine">要运行的协程枚举器</param>
    /// <param name="tag">协程标签，可选</param>
    /// <returns>协程句柄</returns>
    public CoroutineHandle Run(
        IEnumerator<IYieldInstruction>? coroutine,
        string? tag = null)
    {
        if (coroutine == null)
            return default;

        if (_nextSlot >= _slots.Length)
            Expand();

        var handle = new CoroutineHandle(instanceId);
        var slotIndex = _nextSlot++;

        var slot = new CoroutineSlot
        {
            Enumerator = coroutine,
            State = CoroutineState.Running
        };

        _slots[slotIndex] = slot;
        _metadata[handle] = new CoroutineMetadata
        {
            SlotIndex = slotIndex,
            State = CoroutineState.Running,
            Tag = tag
        };

        if (!string.IsNullOrEmpty(tag))
            AddTag(tag, handle);

        Prewarm(slotIndex);
        _activeCount++;

        return handle;
    }

    /// <summary>
    /// 更新所有协程状态
    /// </summary>
    public void Update()
    {
        _timeSource.Update();
        var delta = _timeSource.DeltaTime;

        // 遍历所有槽位并更新协程状态
        for (var i = 0; i < _nextSlot; i++)
        {
            var slot = _slots[i];
            if (slot == null || slot.State != CoroutineState.Running)
                continue;

            try
            {
                // 1️⃣ 处理等待指令
                if (slot.Waiting != null)
                {
                    slot.Waiting.Update(delta);
                    if (!slot.Waiting.IsDone)
                        continue;

                    slot.Waiting = null;
                }

                // 2️⃣ 推进协程
                if (!slot.Enumerator.MoveNext())
                {
                    Complete(i);
                }
                else
                {
                    slot.Waiting = slot.Enumerator.Current;
                }
            }
            catch (Exception ex)
            {
                OnError(i, ex);
            }
        }
    }

    #endregion

    #region Pause / Resume / Kill

    /// <summary>
    /// 暂停指定协程
    /// </summary>
    /// <param name="handle">协程句柄</param>
    /// <returns>是否成功暂停</returns>
    public bool Pause(CoroutineHandle handle)
    {
        if (!_metadata.TryGetValue(handle, out var meta))
            return false;

        var slot = _slots[meta.SlotIndex];
        if (slot == null || slot.State != CoroutineState.Running)
            return false;

        slot.State = CoroutineState.Paused;
        meta.State = CoroutineState.Paused;
        return true;
    }

    /// <summary>
    /// 恢复指定协程
    /// </summary>
    /// <param name="handle">协程句柄</param>
    /// <returns>是否成功恢复</returns>
    public bool Resume(CoroutineHandle handle)
    {
        if (!_metadata.TryGetValue(handle, out var meta))
            return false;

        var slot = _slots[meta.SlotIndex];
        if (slot == null || slot.State != CoroutineState.Paused)
            return false;

        slot.State = CoroutineState.Running;
        meta.State = CoroutineState.Running;
        return true;
    }

    /// <summary>
    /// 终止指定协程
    /// </summary>
    /// <param name="handle">协程句柄</param>
    /// <returns>是否成功终止</returns>
    public bool Kill(CoroutineHandle handle)
    {
        if (!_metadata.TryGetValue(handle, out var meta))
            return false;

        Complete(meta.SlotIndex);
        return true;
    }

    #endregion

    #region Wait / Tag / Clear

    /// <summary>
    /// 让当前协程等待目标协程完成
    /// </summary>
    /// <param name="current">当前协程句柄</param>
    /// <param name="target">目标协程句柄</param>
    public void WaitForCoroutine(
        CoroutineHandle current,
        CoroutineHandle target)
    {
        if (current == target)
            throw new InvalidOperationException("Coroutine cannot wait for itself.");

        if (!_metadata.ContainsKey(target))
            return;

        if (_metadata.TryGetValue(current, out var meta))
        {
            var slot = _slots[meta.SlotIndex];
            if (slot != null)
            {
                slot.State = CoroutineState.Held;
                meta.State = CoroutineState.Held;
            }
        }

        if (!_waiting.TryGetValue(target, out var set))
        {
            set = new HashSet<CoroutineHandle>();
            _waiting[target] = set;
        }

        set.Add(current);
    }

    /// <summary>
    /// 根据标签终止协程
    /// </summary>
    /// <param name="tag">协程标签</param>
    /// <returns>被终止的协程数量</returns>
    public int KillByTag(string tag)
    {
        if (!_tagged.TryGetValue(tag, out var handles))
            return 0;

        var copy = handles.ToArray();
        var count = 0;

        foreach (var h in copy)
            if (Kill(h))
                count++;

        return count;
    }

    /// <summary>
    /// 清空所有协程
    /// </summary>
    /// <returns>被清除的协程数量</returns>
    public int Clear()
    {
        var count = _activeCount;

        Array.Clear(_slots);
        _metadata.Clear();
        _tagged.Clear();
        _waiting.Clear();

        _nextSlot = 0;
        _activeCount = 0;

        return count;
    }

    #endregion

    #region Internal

    /// <summary>
    /// 预热协程槽位，执行协程的第一步
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    private void Prewarm(int slotIndex)
    {
        var slot = _slots[slotIndex];
        if (slot == null)
            return;

        try
        {
            if (!slot.Enumerator.MoveNext())
            {
                Complete(slotIndex);
            }
            else
            {
                slot.Waiting = slot.Enumerator.Current;
            }
        }
        catch (Exception ex)
        {
            OnError(slotIndex, ex);
        }
    }

    /// <summary>
    /// 完成指定槽位的协程
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    private void Complete(int slotIndex)
    {
        var slot = _slots[slotIndex];
        if (slot == null)
            return;

        _slots[slotIndex] = null;
        _activeCount--;

        CoroutineHandle handle = default;
        foreach (var kv in _metadata)
        {
            if (kv.Value.SlotIndex == slotIndex)
            {
                handle = kv.Key;
                break;
            }
        }

        if (!handle.IsValid)
            return;

        RemoveTag(handle);
        _metadata.Remove(handle);

        // 唤醒等待者
        if (_waiting.TryGetValue(handle, out var waiters))
        {
            foreach (var waiter in waiters)
            {
                if (_metadata.TryGetValue(waiter, out var meta))
                {
                    var s = _slots[meta.SlotIndex];
                    if (s != null)
                    {
                        s.State = CoroutineState.Running;
                        meta.State = CoroutineState.Running;
                    }
                }
            }

            _waiting.Remove(handle);
        }
    }

    /// <summary>
    /// 处理协程执行中的错误
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    /// <param name="ex">异常对象</param>
    private void OnError(int slotIndex, Exception ex)
    {
        Console.Error.WriteLine(ex);
        Complete(slotIndex);
    }

    /// <summary>
    /// 扩展协程槽位数组容量
    /// </summary>
    private void Expand()
    {
        Array.Resize(ref _slots, _slots.Length * 2);
    }

    /// <summary>
    /// 为协程添加标签
    /// </summary>
    /// <param name="tag">标签名称</param>
    /// <param name="handle">协程句柄</param>
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
    /// 移除协程标签
    /// </summary>
    /// <param name="handle">协程句柄</param>
    private void RemoveTag(CoroutineHandle handle)
    {
        if (!_metadata.TryGetValue(handle, out var meta) || meta.Tag == null)
            return;

        if (_tagged.TryGetValue(meta.Tag, out var set))
        {
            set.Remove(handle);
            if (set.Count == 0)
                _tagged.Remove(meta.Tag);
        }

        meta.Tag = null;
    }

    #endregion
}