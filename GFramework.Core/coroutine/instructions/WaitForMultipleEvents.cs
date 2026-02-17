using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.Abstractions.events;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     等待多个事件中的任意一个触发的指令
///     实现了 IDestroyable 接口，支持资源释放
/// </summary>
/// <typeparam name="TEvent1">第一个事件类型</typeparam>
/// <typeparam name="TEvent2">第二个事件类型</typeparam>
public sealed class WaitForMultipleEvents<TEvent1, TEvent2> : IYieldInstruction, IDisposable
{
    private bool _disposed;
    private volatile bool _done;
    private IUnRegister? _unRegister1;
    private IUnRegister? _unRegister2;

    /// <summary>
    ///     初始化 WaitForMultipleEvents 实例
    /// </summary>
    /// <param name="eventBus">事件总线实例</param>
    public WaitForMultipleEvents(IEventBus eventBus)
    {
        var eventBus1 = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        // 注册两个事件的监听器
        _unRegister1 = eventBus1.Register<TEvent1>(OnFirstEvent);
        _unRegister2 = eventBus1.Register<TEvent2>(OnSecondEvent);
    }

    /// <summary>
    ///     获取第一个事件的数据（如果已触发）
    /// </summary>
    public TEvent1? FirstEventData { get; private set; }

    /// <summary>
    ///     获取第二个事件的数据（如果已触发）
    /// </summary>
    public TEvent2? SecondEventData { get; private set; }

    /// <summary>
    ///     获取是哪个事件先触发（1表示第一个事件，2表示第二个事件）
    /// </summary>
    public int TriggeredBy { get; private set; }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _unRegister1?.UnRegister();
        _unRegister2?.UnRegister();
        _unRegister1 = null;
        _unRegister2 = null;
        _disposed = true;
    }

    /// <summary>
    ///     获取等待是否已完成
    /// </summary>
    public bool IsDone => _done;

    /// <summary>
    ///     更新方法
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        if (!_done || (_unRegister1 == null && _unRegister2 == null)) return;

        _unRegister1?.UnRegister();
        _unRegister2?.UnRegister();
        _unRegister1 = null;
        _unRegister2 = null;
    }

    /// <summary>
    ///     第一个事件触发处理
    /// </summary>
    private void OnFirstEvent(TEvent1 eventData)
    {
        // 如果已经完成或者被释放，则直接返回
        if (_done || _disposed) return;

        FirstEventData = eventData;
        TriggeredBy = 1;
        _done = true;

        // 立即注销事件监听器
        _unRegister1?.UnRegister();
        _unRegister2?.UnRegister();
        _unRegister1 = null;
        _unRegister2 = null;
    }

    /// <summary>
    ///     第二个事件触发处理
    /// </summary>
    private void OnSecondEvent(TEvent2 eventData)
    {
        // 如果已经完成或者被释放，则直接返回
        if (_done || _disposed) return;

        SecondEventData = eventData;
        TriggeredBy = 2;
        _done = true;

        // 立即注销事件监听器
        _unRegister1?.UnRegister();
        _unRegister2?.UnRegister();
        _unRegister1 = null;
        _unRegister2 = null;
    }
}