using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.Coroutine.Extensions;

/// <summary>
///     协程感知接口的扩展方法
/// </summary>
public static class CoroutineAwareExtensions
{
    /// <summary>
    ///     获取协程调度器
    /// </summary>
    /// <param name="contextAware">上下文感知对象</param>
    /// <returns>协程调度器</returns>
    /// <exception cref="System.ArgumentNullException">当contextAware为null时抛出</exception>
    public static ICoroutineScheduler GetCoroutineScheduler(this IContextAware contextAware)
    {
        if (contextAware == null)
            throw new ArgumentNullException(nameof(contextAware));

        var context = contextAware.GetContext();
        return context.GetService<ICoroutineScheduler>()!;
    }

    /// <summary>
    ///     创建协程作用域
    /// </summary>
    /// <param name="contextAware">上下文感知对象</param>
    /// <param name="name">作用域名称</param>
    /// <param name="priority">作用域内协程的默认优先级</param>
    /// <returns>新创建的协程作用域</returns>
    /// <exception cref="System.ArgumentNullException">当contextAware为null时抛出</exception>
    public static ICoroutineScope CreateCoroutineScope(this IContextAware contextAware, string name,
        CoroutinePriority priority = CoroutinePriority.Normal)
    {
        if (contextAware == null)
            throw new ArgumentNullException(nameof(contextAware));

        var scheduler = GetCoroutineScheduler(contextAware);
        return scheduler.CreateScope(name, priority);
    }

    /// <summary>
    ///     创建协程分组
    /// </summary>
    /// <param name="contextAware">上下文感知对象</param>
    /// <param name="name">分组名称</param>
    /// <param name="priority">分组内协程的默认优先级</param>
    /// <returns>新创建的协程分组</returns>
    /// <exception cref="System.ArgumentNullException">当contextAware为null时抛出</exception>
    public static ICoroutineGroup CreateCoroutineGroup(this IContextAware contextAware, string name,
        CoroutinePriority priority = CoroutinePriority.Normal)
    {
        if (contextAware == null)
            throw new ArgumentNullException(nameof(contextAware));

        var scheduler = GetCoroutineScheduler(contextAware);
        return scheduler.CreateGroup(name, priority);
    }
}