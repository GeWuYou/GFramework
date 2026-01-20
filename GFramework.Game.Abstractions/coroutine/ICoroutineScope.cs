using System.Collections;

namespace GFramework.Game.Abstractions.coroutine;

/// <summary>
/// 协程作用域接口，用于管理协程的生命周期和执行
/// </summary>
public interface ICoroutineScope
{
    /// <summary>
    /// 获取协程作用域是否处于活动状态
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 取消当前协程作用域，停止所有正在运行的协程
    /// </summary>
    void Cancel();

    /// <summary>
    /// 启动一个新的协程
    /// </summary>
    /// <param name="routine">要执行的协程迭代器</param>
    /// <returns>协程句柄，用于控制和监控协程的执行</returns>
    ICoroutineHandle Launch(IEnumerator routine);
}