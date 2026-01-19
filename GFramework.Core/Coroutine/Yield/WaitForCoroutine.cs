using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Yield;

/// <summary>
///     等待另一个协程完成的yield指令
/// </summary>
/// <remarks>
///     该指令会在指定的协程完成时标记为完成
///     常用于协程之间的同步
/// </remarks>
public class WaitForCoroutine : IYieldInstruction
{
    private readonly ICoroutineHandle _handle;

    /// <summary>
    ///     初始化WaitForCoroutine指令的新实例
    /// </summary>
    /// <param name="handle">要等待的协程句柄</param>
    /// <exception cref="System.ArgumentNullException">当句柄为null时抛出</exception>
    public WaitForCoroutine(ICoroutineHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        IsDone = handle.IsDone;
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

        IsDone = _handle.IsDone;
    }
}