// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Coroutine.Extensions;

/// <summary>
///     Task与协程之间的扩展方法
/// </summary>
public static class TaskCoroutineExtensions
{
    /// <summary>
    ///     将Task转换为协程等待指令
    /// </summary>
    /// <param name="task">要等待的Task</param>
    /// <returns>等待Task的协程指令</returns>
    public static WaitForTask AsCoroutineInstruction(this Task task)
    {
        return new WaitForTask(task);
    }

    /// <summary>
    ///     将泛型Task转换为协程等待指令
    /// </summary>
    /// <typeparam name="T">Task返回值的类型</typeparam>
    /// <param name="task">要等待的Task</param>
    /// <returns>等待Task的协程指令</returns>
    public static WaitForTask<T> AsCoroutineInstruction<T>(this Task<T> task)
    {
        return new WaitForTask<T>(task);
    }

    /// <summary>
    ///     在调度器中观察已有 Task，并等待其真实终结结果。
    /// </summary>
    /// <param name="scheduler">协程调度器</param>
    /// <param name="task">要等待的Task</param>
    /// <returns>协程句柄</returns>
    /// <remarks>该重载不拥有 Task 的取消权；停止观察协程不会取消底层任务。</remarks>
    public static CoroutineHandle StartTaskAsCoroutine(this CoroutineScheduler scheduler, Task task)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        return scheduler.Run(task.ToCoroutineEnumerator());
    }

    /// <summary>
    ///     在调度器中观察已有泛型 Task，并等待其真实终结结果。
    /// </summary>
    /// <typeparam name="T">Task返回值的类型</typeparam>
    /// <param name="scheduler">协程调度器</param>
    /// <param name="task">要等待的Task</param>
    /// <returns>协程句柄</returns>
    /// <remarks>需要联动取消底层操作时使用接收 CancellationToken 的任务工厂重载。</remarks>
    public static CoroutineHandle StartTaskAsCoroutine<T>(this CoroutineScheduler scheduler, Task<T> task)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        return scheduler.Run(task.ToCoroutineEnumerator());
    }

    /// <summary>
    ///     在流程内创建 Task，并把流程生命周期令牌传给底层操作。
    /// </summary>
    /// <param name="scheduler">负责生命周期的调度器。</param>
    /// <param name="taskFactory">必须协作响应令牌的任务工厂。</param>
    /// <param name="cancellationToken">附加的外部取消令牌。</param>
    /// <returns>观察任务结果的协程句柄。</returns>
    /// <remarks>取消仍依赖底层操作响应令牌；调度器不会强行终止 Task。</remarks>
    public static CoroutineHandle StartTaskAsCoroutine(
        this CoroutineScheduler scheduler,
        Func<CancellationToken, Task> taskFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(taskFactory);
        return scheduler.Run(context => taskFactory(context.CancellationToken).ToCoroutineEnumerator(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    ///     在指定作用域中创建响应生命周期取消的 Task。
    /// </summary>
    /// <param name="scope">拥有该任务的作用域。</param>
    /// <param name="taskFactory">接收生命周期令牌的任务工厂。</param>
    /// <param name="cancellationToken">附加的外部取消令牌。</param>
    /// <returns>观察任务结果的协程句柄。</returns>
    public static CoroutineHandle StartTaskAsCoroutine(
        this CoroutineScope scope,
        Func<CancellationToken, Task> taskFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(taskFactory);
        return scope.Run(context => taskFactory(context.CancellationToken).ToCoroutineEnumerator(),
            cancellationToken: cancellationToken);
    }


    /// <summary>
    ///     将Task转换为协程枚举器
    /// </summary>
    /// <param name="task">要转换的Task</param>
    /// <returns>协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> ToCoroutineEnumerator(this Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return WaitForTaskEnumerator(task);
    }


    /// <summary>
    ///     将泛型Task转换为协程枚举器
    /// </summary>
    /// <typeparam name="T">Task返回值的类型</typeparam>
    /// <param name="task">要转换的泛型Task</param>
    /// <returns>协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> ToCoroutineEnumerator<T>(this Task<T> task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return WaitForTaskEnumerator(task);
    }

    /// <summary>
    ///     在普通手工枚举与调度器推进两种路径中都观察 Task 的真实终结结果。
    /// </summary>
    private static IEnumerator<IYieldInstruction> WaitForTaskEnumerator(Task task)
    {
        yield return new WaitForTask(task);
        task.GetAwaiter().GetResult();
    }
}
