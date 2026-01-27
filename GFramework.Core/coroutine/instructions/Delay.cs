using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     延迟等待指令，实现IYieldInstruction接口，用于协程中的时间延迟
/// </summary>
/// <param name="seconds">需要延迟的秒数</param>
public sealed class Delay(double seconds) : IYieldInstruction
{
    /// <summary>
    ///     剩余等待时间
    /// </summary>
    private double _remaining = Math.Max(0, seconds);

    /// <summary>
    ///     更新延迟计时器
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