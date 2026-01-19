using System;
using System.Collections;

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程作用域接口，定义协程作用域的生命周期和管理功能
/// </summary>
/// <remarks>
///     协程作用域是协程的生命周期边界，所有在作用域内启动的协程都会随着作用域的销毁而自动取消
///     类似于Kotlin的CoroutineScope概念
/// </remarks>
public interface ICoroutineScope : IDisposable
{
    /// <summary>
    ///     获取作用域是否处于活跃状态
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    ///     获取作用域的名称
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     获取作用域内正在运行的协程数量
    /// </summary>
    int RunningCount { get; }

    /// <summary>
    ///     在此作用域中启动一个新的协程
    /// </summary>
    /// <param name="routine">协程的迭代器</param>
    /// <param name="priority">协程优先级，使用Inherit继承作用域默认优先级</param>
    /// <returns>协程句柄，用于控制协程</returns>
    ICoroutineHandle Launch(IEnumerator routine, CoroutinePriority priority = CoroutinePriority.Inherit);

    /// <summary>
    ///     取消作用域及其所有协程
    /// </summary>
    /// <remarks>
    ///     取消后，作用域将不再活跃，所有协程都会被停止
    ///     后续的Launch调用将抛出InvalidOperationException
    /// </remarks>
    void Cancel();

    /// <summary>
    ///     暂停作用域内的所有协程
    /// </summary>
    void Pause();

    /// <summary>
    ///     恢复作用域内的所有协程
    /// </summary>
    void Resume();
}