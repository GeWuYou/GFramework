// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     等待首次事件，并在调度线程推进或释放时注销监听器。
/// </summary>
/// <typeparam name="TEvent">事件数据类型。</typeparam>
/// <remarks>
///     事件回调允许来自其他线程，仅发布数据；它不操作调度器或注销事件总线。
///     Dispose 与事件竞争时，由先取得锁的一方决定是否接收数据。
/// </remarks>
public sealed class WaitForEvent<TEvent> : IYieldInstruction, IDisposable
{
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private bool _disposed;
    private volatile bool _done;
    private TEvent? _eventData;
    private IDisposable? _unRegister;

    /// <summary>
    ///     订阅事件。指令从创建起拥有订阅，调用方须交给调度器或主动释放。
    /// </summary>
    /// <param name="eventBus">负责该订阅的事件总线。</param>
    public WaitForEvent(IEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        _unRegister = eventBus.Subscribe<TEvent>(OnEventTriggered);
    }

    /// <summary>
    ///     获取首次收到的事件数据；未收到事件时为默认值。
    /// </summary>
    public TEvent? EventData
    {
        get
        {
            lock (_sync)
            {
                return _eventData;
            }
        }
    }

    /// <inheritdoc />
    public bool IsDone => _done;

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        if (_done)
        {
            Dispose();
        }
    }

    /// <summary>
    ///     释放订阅并阻止后续数据写入；多次调用只注销一次。
    /// </summary>
    public void Dispose()
    {
        IDisposable? registration;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            registration = _unRegister;
            _unRegister = null;
        }

        // 不在状态锁内调用外部总线，避免注销回调重入形成死锁。
        registration?.Dispose();
    }

    /// <summary>
    ///     关闭尚未完成的等待，供超时包装器与事件回调原子地竞争结果。
    /// </summary>
    /// <returns>若关闭时尚未收到事件，则返回 true。</returns>
    internal bool StopIfPending()
    {
        Dispose();
        return !_done;
    }

    /// <summary>
    ///     只发布首次事件；volatile 完成位使读取方能观察完整的数据写入。
    /// </summary>
    private void OnEventTriggered(TEvent eventData)
    {
        lock (_sync)
        {
            if (_disposed || _done)
            {
                return;
            }

            _eventData = eventData;
            _done = true;
        }
    }
}
