using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     等待所有协程完成的等待指令
/// </summary>
public sealed class WaitForAllCoroutines : IYieldInstruction
{
    private readonly IReadOnlyList<CoroutineHandle> _handles;
    private readonly CoroutineScheduler _scheduler;
    private bool _isDone;

    public WaitForAllCoroutines(
        CoroutineScheduler scheduler,
        IReadOnlyList<CoroutineHandle> handles)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _handles = handles ?? throw new ArgumentNullException(nameof(handles));

        // 空列表直接完成
        _isDone = _handles.Count == 0;
    }

    public void Update(double deltaTime)
    {
        if (_isDone) return;

        // 检查所有协程是否都已完成
        _isDone = _handles.All(handle => !_scheduler.IsCoroutineAlive(handle));
    }

    public bool IsDone => _isDone;
}