using System.Collections;
using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 为协程作用域提供扩展方法，支持延迟执行和重复执行功能
/// </summary>
public static class CoroutineScopeExtensions
{
    /// <summary>
    /// 启动一个延迟执行的协程
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="action">延迟后要执行的动作</param>
    /// <returns>协程句柄，可用于控制协程的生命周期</returns>
    public static CoroutineHandle LaunchDelayed(this ICoroutineScope scope, float delay, Action action)
        => ((CoroutineScope)scope).Launch(DelayedRoutine(delay, action));

    /// <summary>
    /// 创建延迟执行的协程迭代器
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <returns>协程迭代器</returns>
    private static IEnumerator DelayedRoutine(float delay, Action? action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    /// <summary>
    /// 启动一个重复执行的协程
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="interval">重复间隔时间（秒）</param>
    /// <param name="action">每次重复时要执行的动作</param>
    /// <returns>协程句柄，可用于控制协程的生命周期</returns>
    public static CoroutineHandle LaunchRepeating(this ICoroutineScope scope, float interval, Action action)
        => ((CoroutineScope)scope).Launch(RepeatingRoutine(interval, action));

    /// <summary>
    /// 创建重复执行的协程迭代器
    /// </summary>
    /// <param name="interval">重复间隔时间（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <returns>协程迭代器</returns>
    private static IEnumerator RepeatingRoutine(float interval, Action action)
    {
        while (true)
        {
            action?.Invoke();
            yield return new WaitForSeconds(interval);
        }
    }
}