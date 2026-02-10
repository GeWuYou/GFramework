using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     等待下一个物理固定更新周期的指令
///     主要用于需要与物理系统同步的操作
/// </summary>
public sealed class WaitForFixedUpdate : IYieldInstruction
{
    private bool _completed;

    /// <summary>
    ///     更新方法，在固定更新时被调用
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        // 在固定更新周期中标记完成
        _completed = true;
    }

    /// <summary>
    ///     获取等待是否已完成
    /// </summary>
    public bool IsDone => _completed;
}