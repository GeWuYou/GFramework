using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 等待条件为假的等待指令，当指定的谓词条件变为false时完成等待
/// </summary>
/// <param name="predicate">用于判断是否继续等待的条件函数，返回true表示继续等待，返回false表示等待结束</param>
public class WaitWhile(Func<bool> predicate) : IYieldInstruction
{
    /// <summary>
    /// 获取等待指令是否已完成
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    /// 更新等待状态，检查谓词条件是否满足结束等待的要求
    /// </summary>
    /// <param name="deltaTime">自上次更新以来的时间间隔</param>
    public void Update(float deltaTime)
    {
        if (!IsDone) IsDone = !predicate();
    }
}