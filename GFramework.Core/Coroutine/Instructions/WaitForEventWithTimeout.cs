// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     等待事件或超时，并拥有所包装事件订阅的释放责任。
/// </summary>
/// <typeparam name="TEvent">事件数据类型。</typeparam>
/// <param name="waitForEvent">转交给该包装器管理的事件等待。</param>
/// <param name="timeout">超时时间（秒）；非正值在首次推进时立即超时。</param>
/// <remarks>Update 和 Dispose 应由同一调度线程调用；事件回调可以来自其他线程。</remarks>
public sealed class WaitForEventWithTimeout<TEvent>(WaitForEvent<TEvent> waitForEvent, float timeout)
    : IYieldInstruction, IDisposable
{
    private readonly WaitForEvent<TEvent> _waitForEvent =
        waitForEvent ?? throw new ArgumentNullException(nameof(waitForEvent));
    private double _elapsedTime;
    private bool _disposed;
    private bool _timedOut;

    /// <summary>
    ///     获取事件尚未到达时是否达到超时。
    /// </summary>
    public bool IsTimeout => _timedOut;

    /// <summary>
    ///     获取接收到的事件数据。
    /// </summary>
    public TEvent? EventData => _waitForEvent.EventData;

    /// <inheritdoc />
    public bool IsDone => _waitForEvent.IsDone || IsTimeout;

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        if (_disposed)
        {
            return;
        }

        if (!_waitForEvent.IsDone)
        {
            _elapsedTime += deltaTime;
            if (_elapsedTime >= timeout)
            {
                // 通过内部事件等待的状态锁决定事件与超时的唯一赢家。
                _timedOut = _waitForEvent.StopIfPending();
            }
        }

        _waitForEvent.Update(deltaTime);
        if (IsDone)
        {
            Dispose();
        }
    }

    /// <summary>
    ///     在完成、超时、取消或外部释放时解除内部事件订阅。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _waitForEvent.Dispose();
    }
}
