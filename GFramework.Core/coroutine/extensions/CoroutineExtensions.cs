using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;

namespace GFramework.Core.coroutine.extensions;

/// <summary>
///     协程相关的扩展方法
/// </summary>
public static class CoroutineExtensions
{
    /// <summary>
    ///     在指定时间间隔内重复执行动作的协程
    /// </summary>
    /// <param name="interval">执行间隔时间（秒）</param>
    /// <param name="action">要重复执行的动作</param>
    /// <param name="count">重复次数，如果为null则无限重复</param>
    /// <returns>协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> RepeatEvery(
        double interval,
        Action action,
        int? count = null)
    {
        if (count is < 0) yield break;

        var executedCount = 0;
        while (count == null || executedCount < count)
        {
            action?.Invoke();
            yield return new Delay(interval);
            executedCount++;
        }
    }

    /// <summary>
    ///     在指定延迟后执行动作的协程
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <returns>协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> ExecuteAfter(
        double delay,
        Action? action)
    {
        if (delay < 0) yield break;

        yield return new Delay(delay);
        action?.Invoke();
    }

    /// <summary>
    ///     顺序执行多个协程
    /// </summary>
    /// <param name="coroutines">要顺序执行的协程集合</param>
    /// <returns>协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> Sequence(
        params IEnumerator<IYieldInstruction>[] coroutines)
    {
        foreach (var coroutine in coroutines)
        {
            while (coroutine.MoveNext()) yield return coroutine.Current;

            // 清理协程
            coroutine.Dispose();
        }
    }

    /// <summary>
    ///     并行执行多个协程（等待所有协程完成）
    ///     注意：这需要协程调度器的支持，这里提供一个包装器返回多个句柄
    /// </summary>
    /// <param name="scheduler">协程调度器</param>
    /// <param name="coroutines">要并行执行的协程集合</param>
    /// <returns>等待所有协程完成的协程</returns>
    public static IEnumerator<IYieldInstruction> ParallelCoroutines(
        this CoroutineScheduler scheduler,
        params IEnumerator<IYieldInstruction>[]? coroutines)
    {
        if (coroutines == null || coroutines.Length == 0) yield break;

        // 启动所有协程并收集句柄
        var handles = new List<CoroutineHandle>();
        foreach (var coroutine in coroutines)
        {
            var handle = scheduler.Run(coroutine);
            handles.Add(handle);
        }

        // 等待所有协程完成
        yield return new WaitForAllCoroutines(scheduler, handles);
    }

    /// <summary>
    ///     带进度回调的等待协程
    /// </summary>
    /// <param name="totalTime">总等待时间（秒）</param>
    /// <param name="onProgress">进度回调，参数为0-1之间的进度值</param>
    /// <returns>协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> WaitForSecondsWithProgress(
        double totalTime,
        Action<float>? onProgress)
    {
        if (totalTime <= 0)
        {
            onProgress?.Invoke(1.0f);
            yield break;
        }

        onProgress?.Invoke(0.0f);

        if (onProgress != null)
            yield return new WaitForProgress(totalTime, onProgress);
        else
            yield return new Delay(totalTime);
    }
}