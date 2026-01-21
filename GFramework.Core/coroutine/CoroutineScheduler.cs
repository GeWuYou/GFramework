using System.Collections;
using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine;

public class CoroutineScheduler : ICoroutineScheduler
{
    private readonly List<CoroutineHandle> _active = new();
    private readonly List<CoroutineHandle> _toAdd = new();
    private readonly HashSet<CoroutineHandle> _toRemove = new();
    private int? _ownerThreadId;

    public int ActiveCount => _active.Count + _toAdd.Count;

    public void Update(float deltaTime)
    {
        if (_ownerThreadId == null)
        {
            _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        }
        else if (Thread.CurrentThread.ManagedThreadId != _ownerThreadId)
        {
            throw new InvalidOperationException(
                $"CoroutineScheduler must be updated on same thread. " +
                $"Owner: {_ownerThreadId}, Current: {Thread.CurrentThread.ManagedThreadId}");
        }

        // 先将新协程添加到活动列表
        if (_toAdd.Count > 0)
        {
            _active.AddRange(_toAdd);
            _toAdd.Clear();
        }

        // 遍历活动协程，每帧只推进一步
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var c = _active[i];

            // 检查作用域是否仍然活跃
            if (!c.Context.Scope.IsActive)
            {
                c.Cancel();
                _toRemove.Add(c);
                continue;
            }

            // 跳过由父协程管理的协程
            if (c.IsManagedByParent)
                continue;

            // 更新协程，每帧只推进一步
            ((IYieldInstruction)c).Update(deltaTime);

            // 如果协程完成，标记为待移除
            if (c.IsDone)
                _toRemove.Add(c);
        }

        // 移除已完成的协程
        if (_toRemove.Count > 0)
        {
            _active.RemoveAll(c => _toRemove.Contains(c));
            _toRemove.Clear();
        }
    }

    internal CoroutineHandle StartCoroutine(IEnumerator routine, CoroutineContext context)
    {
        var handle = new CoroutineHandle(routine, context, null);
        
        // 添加到调度队列，协程将在下次 Update 时开始执行
        _toAdd.Add(handle);
        
        return handle;
    }

    internal void RemoveCoroutine(CoroutineHandle handle) => _toRemove.Add(handle);
}