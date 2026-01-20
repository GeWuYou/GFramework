using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 等待条件为假时继续执行的协程等待指令
/// </summary>
/// <param name="predicate">用于判断是否继续等待的布尔函数委托</param>
public class WaitWhile(Func<bool> predicate) : IYieldInstruction
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
        // 当前未完成时，检查谓词条件来更新完成状态
        if (!IsDone) IsDone = !predicate();
    }

    /// <summary>
    /// 重置等待指令状态
    /// </summary>
    public void Reset()
    {
        IsDone = false;
    }
}