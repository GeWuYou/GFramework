using System.Collections;

namespace GFramework.Game.coroutine;

/// <summary>
/// 全局协程作用域管理器，提供全局唯一的协程执行环境
/// </summary>
public static class GlobalCoroutineScope
{
    private static CoroutineScope? _instance;

    /// <summary>
    /// 获取全局协程作用域实例，如果未初始化则抛出异常
    /// </summary>
    private static CoroutineScope Instance =>
        _instance ?? throw new InvalidOperationException("GlobalScope not initialized");

    /// <summary>
    /// 初始化全局协程作用域
    /// </summary>
    /// <param name="scheduler">协程调度器实例</param>
    public static void Initialize(CoroutineScheduler scheduler) =>
        _instance = new CoroutineScope(scheduler, "GlobalScope");

    /// <summary>
    /// 在全局作用域中启动一个协程
    /// </summary>
    /// <param name="routine">要执行的协程枚举器</param>
    /// <returns>协程句柄，用于控制和管理协程生命周期</returns>
    public static CoroutineHandle Launch(IEnumerator routine) => Instance.Launch(routine);
}