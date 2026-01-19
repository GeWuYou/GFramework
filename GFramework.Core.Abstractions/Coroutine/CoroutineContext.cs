using System;
using System.Collections;

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程上下文类，携带协程执行所需的元数据和状态信息
/// </summary>
/// <remarks>
///     协程上下文在整个协程生命周期中保持不变，用于传递协程相关的配置和引用
/// </remarks>
public class CoroutineContext
{
    /// <summary>
    ///     初始化协程上下文的新实例
    /// </summary>
    /// <param name="scope">协程所属的作用域</param>
    /// <param name="scheduler">协程调度器</param>
    /// <param name="owner">协程的所有者对象（可选）</param>
    /// <param name="priority">协程的优先级</param>
    /// <param name="name">协程的名称</param>
    public CoroutineContext(
        ICoroutineScope scope,
        ICoroutineScheduler scheduler,
        object? owner = null,
        CoroutinePriority priority = CoroutinePriority.Normal,
        string? name = null)
    {
        Scope = scope;
        Scheduler = scheduler;
        Owner = owner;
        Priority = priority;
        Name = name ?? $"Coroutine_{GetHashCode():X8}";
    }

    /// <summary>
    ///     获取协程所属的作用域
    /// </summary>
    public ICoroutineScope Scope { get; }

    /// <summary>
    ///     获取协程调度器
    /// </summary>
    public ICoroutineScheduler Scheduler { get; }

    /// <summary>
    ///     获取协程的所有者对象
    /// </summary>
    public object? Owner { get; }

    /// <summary>
    ///     获取协程的优先级
    /// </summary>
    public CoroutinePriority Priority { get; }

    /// <summary>
    ///     获取协程的名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     获取或设置用户自定义数据
    /// </summary>
    public object? UserData { get; set; }

    /// <summary>
    ///     获取协程的原始迭代器
    /// </summary>
    public IEnumerator? Routine { get; internal set; }

    /// <summary>
    ///     获取协程的启动时间
    /// </summary>
    public DateTime StartTime { get; internal set; } = DateTime.UtcNow;
}