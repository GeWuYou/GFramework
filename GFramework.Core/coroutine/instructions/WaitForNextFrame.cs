using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     等待下一帧的指令（与WaitOneFrame功能相同，提供另一种命名选择）
///     用于需要明确表达"等待到下一帧开始"的场景
/// </summary>
public sealed class WaitForNextFrame : IYieldInstruction
{
    private bool _completed;

    /// <summary>
    ///     更新方法，在下一帧被调用时将完成状态设置为true
    /// </summary>
    /// <param name="deltaTime">时间间隔</param>
    public void Update(double deltaTime)
    {
        _completed = true;
    }

    /// <summary>
    ///     获取当前等待指令是否已完成
    /// </summary>
    public bool IsDone => _completed;
}