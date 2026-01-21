using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine;

/// <summary>
/// 等待直到指定条件满足的协程等待指令
/// </summary>
/// <param name="predicate">用于判断等待是否完成的条件函数</param>
public class WaitUntil(Func<bool> predicate) : IYieldInstruction
{
    /// <summary>
    /// 获取当前等待指令是否已完成
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    /// 更新等待状态
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(float deltaTime)
    {
        // 每次更新都重新评估条件，但一旦完成就保持完成状态
        if (!IsDone) IsDone = predicate();
    }

    /// <summary>
    /// 重置等待指令状态
    /// </summary>
    public void Reset()
    {
        IsDone = false;
    }
}