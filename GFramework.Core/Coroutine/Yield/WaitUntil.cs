using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Yield;

/// <summary>
///     等待条件满足的yield指令
/// </summary>
/// <remarks>
///     该指令会在谓词函数返回true时标记为完成
///     常用于等待某个条件成立
/// </remarks>
public class WaitUntil : IYieldInstruction
{
    private readonly Func<bool> _predicate;

    /// <summary>
    ///     初始化WaitUntil指令的新实例
    /// </summary>
    /// <param name="predicate">条件判断函数，返回true表示条件满足</param>
    /// <exception cref="System.ArgumentNullException">当谓词为null时抛出</exception>
    public WaitUntil(Func<bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        IsDone = false;
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

        IsDone = _predicate();
    }
}