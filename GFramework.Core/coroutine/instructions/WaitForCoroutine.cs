using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
/// 等待协程完成的指令类，实现IYieldInstruction接口
/// </summary>
public sealed class WaitForCoroutine : IYieldInstruction
{
    private bool _done;

    /// <summary>
    /// 更新方法，用于处理时间更新逻辑
    /// </summary>
    /// <param name="delta">时间增量</param>
    public void Update(double delta)
    {
    }

    /// <summary>
    /// 获取协程是否已完成的状态
    /// </summary>
    public bool IsDone => _done;

    /// <summary>
    /// 内部方法，用于标记协程完成状态
    /// </summary>
    internal void Complete() => _done = true;
}