using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine;

/// <summary>
/// 协程槽位类，用于管理单个协程的执行状态和调度信息
/// </summary>
internal sealed class CoroutineSlot
{
    /// <summary>
    /// 协程枚举器，包含协程的执行逻辑
    /// </summary>
    public required IEnumerator<IYieldInstruction> Enumerator;

    /// <summary>
    /// 协程当前状态
    /// </summary>
    public CoroutineState State;

    /// <summary>
    /// 当前等待的指令，用于控制协程的暂停和恢复
    /// </summary>
    public IYieldInstruction? Waiting;
    
    /// <summary>
    /// 协程句柄，用于标识和管理协程实例
    /// </summary>
    public CoroutineHandle Handle;
}
