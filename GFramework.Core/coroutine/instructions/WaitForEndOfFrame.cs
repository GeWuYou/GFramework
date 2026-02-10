using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     等待当前帧渲染完成的指令
///     通常用于需要在渲染完成后执行的操作
/// </summary>
public sealed class WaitForEndOfFrame : IYieldInstruction
{
    private bool _completed;

    /// <summary>
    ///     更新方法，在帧末尾被调用
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        // 在帧结束时标记完成
        _completed = true;
    }

    /// <summary>
    ///     获取等待是否已完成
    /// </summary>
    public bool IsDone => _completed;
}