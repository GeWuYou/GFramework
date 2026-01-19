using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Yield;

/// <summary>
///     等待指定帧数的yield指令
/// </summary>
/// <remarks>
///     该指令会在指定的帧数后标记为完成
///     常用于实现跨帧操作
/// </remarks>
public class WaitForFrames : IYieldInstruction
{
    private readonly int _frameCount;
    private int _frame;

    /// <summary>
    ///     初始化WaitForFrames指令的新实例
    /// </summary>
    /// <param name="frames">等待的帧数</param>
    /// <exception cref="System.ArgumentOutOfRangeException">当帧数小于0时抛出</exception>
    public WaitForFrames(int frames)
    {
        if (frames < 0)
            throw new ArgumentOutOfRangeException(nameof(frames), "Frame count must be non-negative");

        _frameCount = frames;
        IsDone = frames == 0;
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

        _frame++;

        if (_frame >= _frameCount)
        {
            IsDone = true;
        }
    }
}