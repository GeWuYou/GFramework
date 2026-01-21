using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine;

/// <summary>
/// 表示等待一帧的等待指令实现
/// 实现IYieldInstruction接口，用于协程中等待一个游戏帧的执行
/// </summary>
public sealed class WaitOneFrame : IYieldInstruction
{
    private bool _done;

    /// <summary>
    /// 更新方法，在每一帧被调用时将完成状态设置为true
    /// </summary>
    /// <param name="deltaTime">时间间隔，表示当前帧与上一帧的时间差</param>
    public void Update(double deltaTime)
    {
        _done = true;
    }

    /// <summary>
    /// 获取当前等待指令是否已完成
    /// </summary>
    public bool IsDone => _done;
}