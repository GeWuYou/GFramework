using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     受时间缩放影响的等待指令
///     明确表示会受到游戏时间缩放的影响
/// </summary>
/// <param name="seconds">需要等待的秒数</param>
public sealed class WaitForSecondsScaled(double seconds) : IYieldInstruction
{
    /// <summary>
    ///     剩余等待时间（受时间缩放影响）
    /// </summary>
    private double _remaining = Math.Max(0, seconds);

    /// <summary>
    ///     更新延迟计时器（受时间缩放影响）
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        _remaining -= deltaTime;
    }

    /// <summary>
    ///     获取延迟是否完成
    /// </summary>
    public bool IsDone => _remaining <= 0;
}