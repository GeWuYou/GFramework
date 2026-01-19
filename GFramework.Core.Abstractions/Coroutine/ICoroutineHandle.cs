using System;

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程句柄接口，用于控制和监视单个协程的执行
/// </summary>
/// <remarks>
///     协程句柄提供了对协程的精细控制能力，包括取消、暂停、恢复和设置优先级
///     同时提供事件通知机制，用于监听协程的完成和错误状态
/// </remarks>
public interface ICoroutineHandle
{
    /// <summary>
    ///     获取协程是否已完成
    /// </summary>
    bool IsDone { get; }

    /// <summary>
    ///     获取协程是否已被取消
    /// </summary>
    bool IsCancelled { get; }

    /// <summary>
    ///     获取协程是否处于暂停状态
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    ///     获取协程的状态
    /// </summary>
    CoroutineStatus Status { get; }

    /// <summary>
    ///     获取协程的优先级
    /// </summary>
    CoroutinePriority Priority { get; }

    /// <summary>
    ///     获取协程的名称
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     获取协程的上下文
    /// </summary>
    CoroutineContext Context { get; }

    /// <summary>
    ///     取消协程的执行
    /// </summary>
    /// <remarks>
    ///     取消后协程将立即停止，不会再执行后续的MoveNext
    ///     已取消的协程会触发OnError事件（使用OperationCanceledException）
    /// </remarks>
    void Cancel();

    /// <summary>
    ///     暂停协程的执行
    /// </summary>
    /// <remarks>
    ///     暂停后协程不会执行MoveNext，直到调用Resume
    /// </remarks>
    void Pause();

    /// <summary>
    ///     恢复协程的执行
    /// </summary>
    void Resume();

    /// <summary>
    ///     设置协程的优先级
    /// </summary>
    /// <param name="priority">新的优先级</param>
    /// <remarks>
    ///     优先级更改将在下一帧调度时生效
    /// </remarks>
    void SetPriority(CoroutinePriority priority);

    /// <summary>
    ///     协程完成事件
    /// </summary>
    /// <remarks>
    ///     当协程正常完成、取消或出错时触发
    /// </remarks>
    event Action<ICoroutineHandle> OnComplete;

    /// <summary>
    ///     协程错误事件
    /// </summary>
    /// <remarks>
    ///     当协程执行过程中抛出异常时触发
    /// </remarks>
    event Action<ICoroutineHandle, Exception> OnError;
}