using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
/// 等待所有协程完成的等待指令
/// </summary>
public sealed class WaitForAllCoroutines(
    CoroutineScheduler scheduler,
    IReadOnlyList<CoroutineHandle> handles)
    : IYieldInstruction
{
    private readonly CoroutineScheduler _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    private readonly IReadOnlyList<CoroutineHandle> _handles = handles ?? throw new ArgumentNullException(nameof(handles));

    public void Update(double deltaTime)
    {
        // 不需要做任何事
    }

    public bool IsDone
    {
        get
        {
            return _handles.All(handle => !_scheduler.IsCoroutineAlive(handle));
        }
    }
}
