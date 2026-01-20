using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 协程上下文类，用于封装协程执行所需的环境信息
/// </summary>
/// <param name="scope">协程作用域，定义协程的执行范围</param>
/// <param name="scheduler">协程调度器，负责协程的调度和执行管理</param>
/// <param name="owner">协程的所有者对象，可为空，默认为null</param>
public class CoroutineContext(ICoroutineScope scope, CoroutineScheduler scheduler, object? owner = null)
    : ICoroutineContext
{
    /// <summary>
    /// 获取协程作用域
    /// </summary>
    public ICoroutineScope Scope { get; } = scope;

    /// <summary>
    /// 获取协程调度器
    /// </summary>
    public ICoroutineScheduler Scheduler { get; } = scheduler;

    /// <summary>
    /// 获取协程所有者对象
    /// </summary>
    public object? Owner { get; } = owner;
}