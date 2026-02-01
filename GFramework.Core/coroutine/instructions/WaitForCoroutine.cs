using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     等待协程完成的指令类，实现IYieldInstruction接口
/// </summary>
/// <param name="coroutine">需要等待完成的协程枚举器</param>
public sealed class WaitForCoroutine(IEnumerator<IYieldInstruction> coroutine) : IYieldInstruction
{
    /// <summary>
    /// 获取内部协程枚举器
    /// </summary>
    internal IEnumerator<IYieldInstruction> Coroutine => coroutine;

    /// <summary>
    /// 获取当前等待的协程是否已完成
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    /// 更新方法，用于处理协程等待逻辑
    /// </summary>
    /// <param name="delta">时间增量</param>
    public void Update(double delta)
    {
    }

    /// <summary>
    /// 标记协程等待完成
    /// </summary>
    internal void Complete()
    {
        IsDone = true;
    }
}