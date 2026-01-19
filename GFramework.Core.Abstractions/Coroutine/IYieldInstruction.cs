namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     Yield指令接口，定义协程的等待条件
/// </summary>
/// <remarks>
///     Yield指令用于在协程中实现暂停和等待逻辑
///     协程调度器会在每帧更新所有活动的yield指令，直到指令标记为完成
/// </remarks>
public interface IYieldInstruction
{
    /// <summary>
    ///     获取指令是否已完成
    /// </summary>
    bool IsDone { get; }

    /// <summary>
    ///     更新指令状态
    /// </summary>
    /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
    void Update(float deltaTime);
}