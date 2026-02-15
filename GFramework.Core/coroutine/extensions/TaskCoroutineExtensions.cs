using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;

namespace GFramework.Core.coroutine.extensions;

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
    ///     在调度器中启动一个Task并等待其完成
    /// </summary>
    /// <param name="scheduler">协程调度器</param>
    /// <param name="task">要等待的Task</param>
    /// <returns>协程句柄</returns>
    public static CoroutineHandle StartTaskAsCoroutine(this CoroutineScheduler scheduler, Task task)
    {
        return scheduler.Run(task.ToCoroutineEnumerator());
    }

    /// <summary>
    ///     在调度器中启动一个泛型Task并等待其完成
    /// </summary>
    /// <typeparam name="T">Task返回值的类型</typeparam>
    /// <param name="scheduler">协程调度器</param>
    /// <param name="task">要等待的Task</param>
    /// <returns>协程句柄</returns>
    public static CoroutineHandle StartTaskAsCoroutine<T>(this CoroutineScheduler scheduler, Task<T> task)
    {
        return scheduler.Run(task.ToCoroutineEnumerator());
    }


    /// <summary>
    ///     将Task转换为协程枚举器
    /// </summary>
    /// <param name="task">要转换的Task</param>
    /// <returns>协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> ToCoroutineEnumerator(this Task task)
    {
        yield return task.AsCoroutineInstruction();
    }


    /// <summary>
    ///     将泛型Task转换为协程枚举器
    /// </summary>
    /// <typeparam name="T">Task返回值的类型</typeparam>
    /// <param name="task">要转换的泛型Task</param>
    /// <returns>协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> ToCoroutineEnumerator<T>(this Task<T> task)
    {
        yield return task.AsCoroutineInstruction();
    }
}