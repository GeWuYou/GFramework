using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine;

/// <summary>
/// 存储协程元数据信息的内部类，包含协程的状态、枚举器、标签等信息
/// </summary>
internal class CoroutineMetadata
{
    /// <summary>
    /// 协程在调度器中的槽位索引
    /// </summary>
    public int SlotIndex;

    /// <summary>
    /// 协程当前的执行状态
    /// </summary>
    public CoroutineState State;

    /// <summary>
    /// 协程的标签标识符，用于协程的分类和查找
    /// </summary>
    public string? Tag;

    /// <summary>
    /// 判断协程是否处于活跃状态（运行中、暂停或挂起）
    /// </summary>
    public bool IsActive =>
        State is CoroutineState.Running
            or CoroutineState.Paused
            or CoroutineState.Held;
}