// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Coroutine.Extensions;

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
        Action? action,
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
    ///     顺序执行多个协程，并拥有所有传入枚举器的释放责任。
    /// </summary>
    /// <param name="coroutines">要顺序执行的协程集合</param>
    /// <returns>协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> Sequence(
        params IEnumerator<IYieldInstruction>[] coroutines)
    {
        ArgumentNullException.ThrowIfNull(coroutines);
        return new OwnedCoroutineEnumerator(coroutines);
    }

    /// <summary>
    ///     并行运行当前流程的子任务，并保留结构化父子取消与失败策略。
    /// </summary>
    /// <param name="context">拥有子任务的流程上下文。</param>
    /// <param name="coroutines">需要并行执行的子任务。</param>
    /// <returns>等待全部子任务收尾的枚举器。</returns>
    public static IEnumerator<IYieldInstruction> ParallelCoroutines(
        this CoroutineContext context,
        params IEnumerator<IYieldInstruction>[] coroutines)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(coroutines);
        return context.WhenAll(coroutines);
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
