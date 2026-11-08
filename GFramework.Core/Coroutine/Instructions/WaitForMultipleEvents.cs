// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.ExceptionServices;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     等待两个事件中的首个事件，原子地保留胜出的数据与事件编号。
/// </summary>
/// <typeparam name="TEvent1">第一种事件。</typeparam>
/// <typeparam name="TEvent2">第二种事件。</typeparam>
/// <remarks>
///     回调只捕获数据；注册和注销由创建或驱动线程完成，不从事件回调进入调度器。
///     调用方须将该指令交给调度器或主动 Dispose。
/// </remarks>
public sealed class WaitForMultipleEvents<TEvent1, TEvent2> : IYieldInstruction, IDisposable
{
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private bool _disposed;
    private volatile bool _done;
    private TEvent1? _firstEventData;
    private TEvent2? _secondEventData;
    private int _triggeredBy;
    private IUnRegister? _unRegister1;
    private IUnRegister? _unRegister2;

    /// <summary>
    ///     订阅两种事件；第二个注册失败时也释放第一个注册。
    /// </summary>
    /// <param name="eventBus">事件总线。</param>
    public WaitForMultipleEvents(IEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        _unRegister1 = eventBus.Register<TEvent1>(OnFirstEvent);
        try
        {
            _unRegister2 = eventBus.Register<TEvent2>(OnSecondEvent);
        }
        catch (Exception registrationException)
        {
            var registration = _unRegister1;
            _unRegister1 = null;
            try
            {
                registration?.UnRegister();
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(registrationException, cleanupException);
            }

            throw;
        }
    }

    /// <summary>
    ///     获取第一种事件的数据；仅第一种事件胜出时有值。
    /// </summary>
    public TEvent1? FirstEventData
    {
        get
        {
            lock (_sync)
            {
                return _firstEventData;
            }
        }
    }

    /// <summary>
    ///     获取第二种事件的数据；仅第二种事件胜出时有值。
    /// </summary>
    public TEvent2? SecondEventData
    {
        get
        {
            lock (_sync)
            {
                return _secondEventData;
            }
        }
    }

    /// <summary>
    ///     获取首次事件编号：0 为尚未触发，1 或 2 为胜出的事件。
    /// </summary>
    public int TriggeredBy
    {
        get
        {
            lock (_sync)
            {
                return _triggeredBy;
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
    ///     原子地停止接收事件，并尝试释放两项订阅；重复释放不重复注销。
    /// </summary>
    public void Dispose()
    {
        IUnRegister? first;
        IUnRegister? second;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            first = _unRegister1;
            second = _unRegister2;
            _unRegister1 = null;
            _unRegister2 = null;
        }

        Exception? firstFailure = null;
        try
        {
            first?.UnRegister();
        }
        catch (Exception exception)
        {
            firstFailure = exception;
        }

        try
        {
            second?.UnRegister();
        }
        catch (Exception exception)
        {
            if (firstFailure != null)
            {
                throw new AggregateException(firstFailure, exception);
            }

            throw;
        }

        if (firstFailure != null)
        {
            ExceptionDispatchInfo.Capture(firstFailure).Throw();
        }
    }

    /// <summary>
    ///     在首次事件获胜后发布第一种事件数据。
    /// </summary>
    private void OnFirstEvent(TEvent1 eventData)
    {
        lock (_sync)
        {
            if (_disposed || _done)
            {
                return;
            }

            _firstEventData = eventData;
            _triggeredBy = 1;
            _done = true;
        }
    }

    /// <summary>
    ///     在首次事件获胜后发布第二种事件数据。
    /// </summary>
    private void OnSecondEvent(TEvent2 eventData)
    {
        lock (_sync)
        {
            if (_disposed || _done)
            {
                return;
            }

            _secondEventData = eventData;
            _triggeredBy = 2;
            _done = true;
        }
    }
}
