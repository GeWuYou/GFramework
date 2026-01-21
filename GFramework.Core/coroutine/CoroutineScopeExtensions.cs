using System.Collections;
using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine;

/// <summary>
/// 为ICoroutineScope提供扩展方法，支持延迟执行和重复执行协程功能
/// </summary>
public static class CoroutineScopeExtensions
{
    /// <summary>
    /// 在指定延迟时间后启动一个协程来执行给定的动作
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <returns>协程句柄，可用于控制或停止协程</returns>
    public static ICoroutineHandle LaunchDelayed(this ICoroutineScope scope, float delay, Action action)
    {
        return scope.Launch(DelayedRoutine(delay, action));
    }

    /// <summary>
    /// 创建一个延迟执行的协程例程
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="action">要执行的动作，可为空</param>
    /// <returns>协程迭代器</returns>
    private static IEnumerator DelayedRoutine(float delay, Action? action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    /// <summary>
    /// 启动一个重复执行的协程，按照指定间隔时间循环执行给定动作
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="interval">执行间隔时间（秒）</param>
    /// <param name="action">要重复执行的动作</param>
    /// <returns>协程句柄，可用于控制或停止协程</returns>
    public static ICoroutineHandle LaunchRepeating(this ICoroutineScope scope, float interval, Action action)
    {
        return scope.Launch(RepeatingRoutine(interval, action));
    }

    /// <summary>
    /// 创建一个重复执行的协程例程
    /// </summary>
    /// <param name="interval">执行间隔时间（秒）</param>
    /// <param name="action">要重复执行的动作</param>
    /// <returns>协程迭代器</returns>
    private static IEnumerator RepeatingRoutine(float interval, Action action)
    {
        // 持续循环执行动作并等待指定间隔
        while (true)
        {
            action?.Invoke();
            yield return new WaitForSeconds(interval);
        }
    }
}