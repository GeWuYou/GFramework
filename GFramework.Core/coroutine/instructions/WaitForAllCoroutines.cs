using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     等待所有协程完成的等待指令
/// </summary>
/// <param name="scheduler">协程调度器，用于检查协程是否存活</param>
/// <param name="handles">协程句柄列表，用于跟踪需要等待的协程</param>
public sealed class WaitForAllCoroutines(
    CoroutineScheduler scheduler,
    IReadOnlyList<CoroutineHandle> handles)
    : IYieldInstruction
{
    private readonly IReadOnlyList<CoroutineHandle> _handles =
        handles ?? throw new ArgumentNullException(nameof(handles));

    private readonly CoroutineScheduler _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

    /// <summary>
    ///     更新方法，在每一帧调用
    /// </summary>
    /// <param name="deltaTime">自上一帧以来的时间间隔</param>
    public void Update(double deltaTime)
    {
        // 不需要做任何事
    }

    /// <summary>
    ///     获取一个值，指示所有协程是否已完成执行
    /// </summary>
    /// <returns>当所有协程都已完成时返回true，否则返回false</returns>
    public bool IsDone
    {
        get
        {
            // 检查所有协程句柄是否都不在调度器中存活
            return _handles.All(handle => !_scheduler.IsCoroutineAlive(handle));
        }
    }
}