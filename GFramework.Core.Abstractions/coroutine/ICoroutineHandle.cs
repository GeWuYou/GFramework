using System;

namespace GFramework.Core.Abstractions.coroutine;

/// <summary>
/// 协程句柄接口，用于管理和控制协程的执行状态
/// </summary>
public interface ICoroutineHandle
{
    /// <summary>
    /// 获取协程的上下文对象
    /// </summary>
    ICoroutineContext Context { get; }

    /// <summary>
    /// 获取协程是否已被取消的标志
    /// </summary>
    bool IsCancelled { get; }

    /// <summary>
    /// 获取协程是否已完成的标志
    /// </summary>
    bool IsDone { get; }

    /// <summary>
    /// 当协程完成时触发的事件
    /// </summary>
    event Action? OnComplete;

    /// <summary>
    /// 当协程发生错误时触发的事件
    /// </summary>
    event Action<Exception>? OnError;

    /// <summary>
    /// 取消协程的执行
    /// </summary>
    void Cancel();
}