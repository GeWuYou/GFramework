using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     基于真实时间的等待指令（不受时间缩放影响）
///     适用于需要精确计时的场景，如UI动画、计时器等
/// </summary>
/// <param name="seconds">需要等待的秒数</param>
public sealed class WaitForSecondsRealtime(double seconds) : IYieldInstruction
{
    /// <summary>
    ///     剩余等待时间（真实时间）
    /// </summary>
    private double _remaining = Math.Max(0, seconds);

    /// <summary>
    ///     更新延迟计时器（使用真实时间）
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