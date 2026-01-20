using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 等待直到指定条件满足的协程等待指令
/// </summary>
/// <param name="predicate">用于判断等待条件是否满足的布尔函数委托</param>
public class WaitUntil(Func<bool> predicate) : IYieldInstruction
{
    /// <summary>
    /// 获取等待指令是否已完成
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    /// 更新等待状态，在每一帧调用以检查条件是否满足
    /// </summary>
    /// <param name="deltaTime">自上一帧以来的时间间隔</param>
    public void Update(float deltaTime)
    {
        if (!IsDone) IsDone = predicate();
    }
}