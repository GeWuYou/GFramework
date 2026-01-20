using System.Collections;
using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 全局协程作用域管理器，提供全局唯一的协程执行环境
/// </summary>
public static class GlobalCoroutineScope
{
    private static CoroutineScope? _instance;

    /// <summary>
    /// 获取当前全局协程作用域是否已初始化
    /// </summary>
    public static bool IsInitialized => _instance != null;

    /// <summary>
    /// 尝试获取当前全局协程作用域实例
    /// </summary>
    /// <param name="scope">输出参数，如果初始化则返回协程作用域实例，否则返回null</param>
    /// <returns>如果全局协程作用域已初始化则返回true，否则返回false</returns>
    public static bool TryGetScope(out ICoroutineScope? scope)
    {
        scope = _instance;
        return _instance != null;
    }

    /// <summary>
    /// 初始化全局协程作用域
    /// </summary>
    /// <param name="scheduler">用于执行协程的调度器</param>
    public static void Initialize(CoroutineScheduler scheduler)
    {
        _instance = new CoroutineScope(scheduler, "GlobalScope");
    }

    /// <summary>
    /// 在全局协程作用域中启动一个协程
    /// </summary>
    /// <param name="routine">要执行的协程枚举器</param>
    /// <returns>协程句柄，用于控制和监控协程执行</returns>
    /// <exception cref="InvalidOperationException">当全局协程作用域未初始化时抛出</exception>
    public static ICoroutineHandle Launch(IEnumerator routine)
    {
        return _instance == null
            ? throw new InvalidOperationException("GlobalCoroutineScope not initialized. Call Initialize() first.")
            : _instance.Launch(routine);
    }
}