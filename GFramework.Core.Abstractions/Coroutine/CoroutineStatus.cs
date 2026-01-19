namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程状态枚举，定义协程的生命周期状态
/// </summary>
public enum CoroutineStatus
{
    /// <summary>
    ///     初始状态 - 协程已创建但尚未开始执行
    /// </summary>
    Pending = 0,

    /// <summary>
    ///     运行中 - 协程正在执行
    /// </summary>
    Running = 1,

    /// <summary>
    ///     暂停中 - 协程已被暂停，等待恢复
    /// </summary>
    Paused = 2,

    /// <summary>
    ///     已完成 - 协程正常执行完成
    /// </summary>
    Completed = 3,

    /// <summary>
    ///     已取消 - 协程被主动取消
    /// </summary>
    Cancelled = 4,

    /// <summary>
    ///     出错 - 协程执行过程中发生异常
    /// </summary>
    Error = 5
}