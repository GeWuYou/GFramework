using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     等待所有协程完成的等待指令
/// </summary>
public sealed class WaitForAllCoroutines : IYieldInstruction
{
    private readonly HashSet<CoroutineHandle> _pendingHandles;
    private bool _isDone;

    /// <summary>
    /// 初始化 WaitForAllCoroutines 类的新实例
    /// </summary>
    /// <param name="handles">要等待完成的协程句柄列表</param>
    public WaitForAllCoroutines(IReadOnlyList<CoroutineHandle> handles)
    {
        ArgumentNullException.ThrowIfNull(handles);

        _pendingHandles = new HashSet<CoroutineHandle>(handles);
        _isDone = _pendingHandles.Count == 0;
    }

    /// <summary>
    /// 获取所有待完成的协程句柄
    /// </summary>
    internal IReadOnlyCollection<CoroutineHandle> PendingHandles => _pendingHandles;

    /// <summary>
    /// 更新方法 - 由调度器调用
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        // 不需要做任何事，由调度器通知完成
    }

    /// <summary>
    /// 获取一个值，指示是否所有协程都已完成
    /// </summary>
    public bool IsDone => _isDone;

    /// <summary>
    /// 通知某个协程已完成
    /// </summary>
    /// <param name="handle">已完成的协程句柄</param>
    internal void NotifyCoroutineComplete(CoroutineHandle handle)
    {
        // 从待处理句柄集合中移除已完成的协程句柄
        _pendingHandles.Remove(handle);
        if (_pendingHandles.Count == 0)
            _isDone = true;
    }
}