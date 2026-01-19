using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Yield;

/// <summary>
///     等待指定秒数的yield指令
/// </summary>
/// <remarks>
///     该指令会在指定的秒数后标记为完成
///     常用于实现延时操作
/// </remarks>
public class WaitForSeconds : IYieldInstruction
{
    private readonly float _duration;
    private float _elapsed;

    /// <summary>
    ///     初始化WaitForSeconds指令的新实例
    /// </summary>
    /// <param name="seconds">等待的秒数</param>
    /// <exception cref="System.ArgumentOutOfRangeException">当秒数小于0时抛出</exception>
    public WaitForSeconds(float seconds)
    {
        if (seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Wait time must be non-negative");

        _duration = seconds;
        IsDone = seconds == 0;
    }

    /// <summary>
    ///     获取指令是否已完成
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    ///     更新指令状态
    /// </summary>
    /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
    public void Update(float deltaTime)
    {
        if (IsDone) return;

        _elapsed += deltaTime;

        if (_elapsed >= _duration)
        {
            IsDone = true;
        }
    }
}