namespace GFramework.Core.Abstractions.coroutine;

/// <summary>
/// 表示协程的执行状态枚举
/// </summary>
public enum CoroutineState
{
    /// <summary>
    /// 协程正在运行中
    /// </summary>
    Running,

    /// <summary>
    /// 协程已暂停
    /// </summary>
    Paused,

    /// <summary>
    /// 协程被锁定或等待其他协程完成
    /// </summary>
    Held,

    /// <summary>
    /// 协程已完成执行
    /// </summary>
    Completed,

    /// <summary>
    /// 协程已被取消
    /// </summary>
    Cancelled,
}