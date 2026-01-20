namespace GFramework.Game.Abstractions.coroutine;

/// <summary>
/// 协程上下文接口，提供协程执行所需的上下文信息
/// </summary>
public interface ICoroutineContext
{
    /// <summary>
    /// 获取协程作用域
    /// </summary>
    ICoroutineScope Scope { get; }

    /// <summary>
    /// 获取协程调度器
    /// </summary>
    ICoroutineScheduler Scheduler { get; }

    /// <summary>
    /// 获取协程所有者对象
    /// </summary>
    object? Owner { get; }
}