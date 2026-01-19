using System.Collections;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Yield;

namespace GFramework.Core.Coroutine.Extensions;

/// <summary>
///     协程扩展方法类，提供便捷的协程操作方法
/// </summary>
public static class CoroutineExtensions
{
    /// <summary>
    ///     延迟执行一个动作
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <returns>协程句柄</returns>
    /// <exception cref="System.ArgumentNullException">当scope或action为null时抛出</exception>
    public static ICoroutineHandle LaunchDelayed(this ICoroutineScope scope, float delay, Action action)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        return scope.Launch(DelayedRoutine(delay, action));
    }

    /// <summary>
    ///     循环执行一个动作
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="interval">执行间隔（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <returns>协程句柄</returns>
    /// <exception cref="System.ArgumentNullException">当scope或action为null时抛出</exception>
    public static ICoroutineHandle LaunchRepeating(this ICoroutineScope scope, float interval, Action action)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        return scope.Launch(RepeatingRoutine(interval, action));
    }

    /// <summary>
    ///     循环执行一个动作（带次数限制）
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="interval">执行间隔（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="repeatCount">重复次数</param>
    /// <returns>协程句柄</returns>
    /// <exception cref="System.ArgumentNullException">当scope或action为null时抛出</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">当repeatCount小于1时抛出</exception>
    public static ICoroutineHandle LaunchRepeating(this ICoroutineScope scope, float interval, Action action,
        int repeatCount)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (repeatCount < 1)
            throw new ArgumentOutOfRangeException(nameof(repeatCount), "Repeat count must be at least 1");

        return scope.Launch(RepeatingRoutineWithCount(interval, action, repeatCount));
    }

    /// <summary>
    ///     并行执行多个协程
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="routines">要并行执行的协程列表</param>
    /// <returns>协程句柄</returns>
    /// <exception cref="System.ArgumentNullException">当scope或routines为null时抛出</exception>
    public static ICoroutineHandle LaunchParallel(this ICoroutineScope scope, List<IEnumerator> routines)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        if (routines == null)
            throw new ArgumentNullException(nameof(routines));

        return scope.Launch(new ParallelCoroutine(scope, routines));
    }

    /// <summary>
    ///     序列执行多个协程
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="routines">要序列执行的协程列表</param>
    /// <returns>协程句柄</returns>
    /// <exception cref="System.ArgumentNullException">当scope或routines为null时抛出</exception>
    public static ICoroutineHandle LaunchSequence(this ICoroutineScope scope, List<IEnumerator> routines)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        if (routines == null)
            throw new ArgumentNullException(nameof(routines));

        return scope.Launch(new SequenceCoroutine(scope, routines));
    }

    /// <summary>
    ///     在协程中等待条件满足
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>协程句柄</returns>
    /// <exception cref="System.ArgumentNullException">当scope或predicate为null时抛出</exception>
    public static ICoroutineHandle WaitUntil(this ICoroutineScope scope, Func<bool> predicate)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        return scope.Launch(WaitUntilRoutine(predicate));
    }

    /// <summary>
    ///     在协程中等待条件失效
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>协程句柄</returns>
    /// <exception cref="System.ArgumentNullException">当scope或predicate为null时抛出</exception>
    public static ICoroutineHandle WaitWhile(this ICoroutineScope scope, Func<bool> predicate)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        return scope.Launch(WaitWhileRoutine(predicate));
    }

    /// <summary>
    ///     延迟执行协程
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <returns>协程迭代器</returns>
    private static IEnumerator DelayedRoutine(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    /// <summary>
    ///     循环执行协程
    /// </summary>
    /// <param name="interval">执行间隔（秒）</param>
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

    /// <summary>
    ///     循环执行协程（带次数限制）
    /// </summary>
    /// <param name="interval">执行间隔（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="repeatCount">重复次数</param>
    /// <returns>协程迭代器</returns>
    private static IEnumerator RepeatingRoutineWithCount(float interval, Action action, int repeatCount)
    {
        for (int i = 0; i < repeatCount; i++)
        {
            action?.Invoke();
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    ///     等待条件满足协程
    /// </summary>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>协程迭代器</returns>
    private static IEnumerator WaitUntilRoutine(Func<bool> predicate)
    {
        yield return new WaitUntil(predicate);
    }

    /// <summary>
    ///     等待条件失效协程
    /// </summary>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>协程迭代器</returns>
    private static IEnumerator WaitWhileRoutine(Func<bool> predicate)
    {
        yield return new WaitWhile(predicate);
    }
}