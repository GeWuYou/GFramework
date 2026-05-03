// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Property;
using GFramework.Core.Abstractions.StateManagement;
using GFramework.Core.Events;

namespace GFramework.Core.StateManagement;

/// <summary>
///     Store 选择结果的只读绑定视图。
///     该类型将整棵状态树上的订阅转换为局部状态片段的订阅，
///     使现有依赖 IReadonlyBindableProperty 的 UI 代码能够平滑复用到 Store 场景中。
/// </summary>
/// <typeparam name="TState">源状态类型。</typeparam>
/// <typeparam name="TSelected">投影后的局部状态类型。</typeparam>
public sealed class StoreSelection<TState, TSelected> : IReadonlyBindableProperty<TSelected>
{
    /// <summary>
    ///     用于判断选择结果是否真正变化的比较器。
    /// </summary>
    private readonly IEqualityComparer<TSelected> _comparer;

    /// <summary>
    ///     当前监听器列表。
    /// </summary>
    private readonly List<SelectionListenerSubscription> _listeners = [];

#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _lock = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _lock = new();
#endif

    /// <summary>
    ///     负责从完整状态中投影出局部状态的选择器。
    /// </summary>
    private readonly Func<TState, TSelected> _selector;

    /// <summary>
    ///     源 Store。
    /// </summary>
    private readonly IReadonlyStore<TState> _store;

    /// <summary>
    ///     当前已缓存的选择结果。
    ///     该缓存仅在存在监听器时用于变化比较和事件通知，直接读取 Value 时始终以 Store 当前状态为准。
    /// </summary>
    private TSelected _currentValue = default!;

    /// <summary>
    ///     连接到底层 Store 的订阅句柄。
    ///     仅当当前存在至少一个监听器时才会建立该订阅，以减少长期闲置对象造成的引用链。
    /// </summary>
    private IUnRegister? _storeSubscription;

    /// <summary>
    ///     初始化一个新的 Store 选择视图。
    /// </summary>
    /// <param name="store">源 Store。</param>
    /// <param name="selector">状态选择器。</param>
    /// <param name="comparer">选择结果比较器；未提供时使用 <see cref="EqualityComparer{T}.Default"/>。</param>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="store"/> 或 <paramref name="selector"/> 为 <see langword="null"/> 时抛出。
    /// </exception>
    public StoreSelection(
        IReadonlyStore<TState> store,
        Func<TState, TSelected> selector,
        IEqualityComparer<TSelected>? comparer = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _comparer = comparer ?? EqualityComparer<TSelected>.Default;
    }

    /// <summary>
    ///     获取当前选择结果。
    /// </summary>
    public TSelected Value => _selector(_store.State);

    /// <summary>
    ///     将无参事件监听适配为带选择结果参数的监听。
    /// </summary>
    /// <param name="onEvent">无参事件监听器。</param>
    /// <returns>用于取消订阅的句柄。</returns>
    IUnRegister IEvent.Register(Action onEvent)
    {
        if (onEvent is null)
            throw new ArgumentNullException(nameof(onEvent));

        return Register(_ => onEvent());
    }

    /// <summary>
    ///     注册选择结果变化监听器。
    /// </summary>
    /// <param name="onValueChanged">选择结果变化时的回调。</param>
    /// <returns>用于取消订阅的句柄。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="onValueChanged"/> 为 <see langword="null"/> 时抛出。</exception>
    public IUnRegister Register(Action<TSelected> onValueChanged)
    {
        if (onValueChanged is null)
            throw new ArgumentNullException(nameof(onValueChanged));

        var subscription = new SelectionListenerSubscription(onValueChanged);
        var shouldAttach = false;

        lock (_lock)
        {
            if (_listeners.Count == 0)
            {
                _currentValue = Value;
                shouldAttach = true;
            }

            _listeners.Add(subscription);
        }

        if (shouldAttach)
        {
            AttachToStore();
        }

        return new DefaultUnRegister(() => UnRegister(subscription));
    }

    /// <summary>
    ///     注册选择结果变化监听器，并立即回放当前值。
    /// </summary>
    /// <param name="action">选择结果变化时的回调。</param>
    /// <returns>用于取消订阅的句柄。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 <see langword="null"/> 时抛出。</exception>
    public IUnRegister RegisterWithInitValue(Action<TSelected> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var subscription = new SelectionListenerSubscription(action)
        {
            IsActive = false
        };
        var currentValue = Value;
        TSelected? pendingValue = default;
        var hasPendingValue = false;

        lock (_lock)
        {
            if (_listeners.Count == 0)
            {
                _currentValue = currentValue;
            }

            _listeners.Add(subscription);
        }

        EnsureAttached();
        try
        {
            action(currentValue);
        }
        catch
        {
            UnRegister(subscription);
            throw;
        }

        lock (_lock)
        {
            if (!subscription.IsSubscribed)
            {
                return new DefaultUnRegister(() => { });
            }

            subscription.IsActive = true;
            if (subscription.HasPendingValue)
            {
                pendingValue = subscription.PendingValue;
                hasPendingValue = true;
                subscription.PendingValue = default!;
                subscription.HasPendingValue = false;
            }
        }

        if (hasPendingValue)
        {
            action(pendingValue!);
        }

        return new DefaultUnRegister(() => UnRegister(subscription));
    }

    /// <summary>
    ///     取消注册选择结果变化监听器。
    /// </summary>
    /// <param name="onValueChanged">需要移除的监听器。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="onValueChanged"/> 为 <see langword="null"/> 时抛出。</exception>
    public void UnRegister(Action<TSelected> onValueChanged)
    {
        if (onValueChanged is null)
            throw new ArgumentNullException(nameof(onValueChanged));

        SelectionListenerSubscription? subscriptionToRemove = null;

        lock (_lock)
        {
            var index = _listeners.FindIndex(subscription => subscription.Listener == onValueChanged);
            if (index < 0)
            {
                return;
            }

            subscriptionToRemove = _listeners[index];
        }

        if (subscriptionToRemove != null)
        {
            UnRegister(subscriptionToRemove);
        }
    }

    /// <summary>
    ///     确保当前选择视图已连接到底层 Store。
    /// </summary>
    private void EnsureAttached()
    {
        var shouldAttach = false;

        lock (_lock)
        {
            shouldAttach = _listeners.Count > 0 && _storeSubscription == null;
        }

        if (shouldAttach)
        {
            AttachToStore();
        }
    }

    /// <summary>
    ///     取消注册一个精确的选择结果监听器。
    /// </summary>
    /// <param name="subscriptionToRemove">需要移除的订阅对象。</param>
    private void UnRegister(SelectionListenerSubscription subscriptionToRemove)
    {
        IUnRegister? storeSubscription = null;

        lock (_lock)
        {
            subscriptionToRemove.IsSubscribed = false;
            _listeners.Remove(subscriptionToRemove);
            if (_listeners.Count == 0 && _storeSubscription != null)
            {
                storeSubscription = _storeSubscription;
                _storeSubscription = null;
            }
        }

        storeSubscription?.UnRegister();
    }

    /// <summary>
    ///     将当前选择视图连接到底层 Store。
    /// </summary>
    private void AttachToStore()
    {
        var subscription = _store.Subscribe(OnStoreChanged);
        Action<TSelected>[] listenersSnapshot = Array.Empty<Action<TSelected>>();
        var latestValue = Value;
        var shouldNotify = false;

        lock (_lock)
        {
            // 如果在建立底层订阅期间所有监听器都已被移除，则立即释放刚刚建立的订阅，
            // 避免选择视图在无人监听时继续被 Store 保持引用。
            if (_listeners.Count == 0)
            {
                subscription.UnRegister();
                return;
            }

            if (_storeSubscription != null)
            {
                subscription.UnRegister();
                return;
            }

            _storeSubscription = subscription;
            if (!_comparer.Equals(_currentValue, latestValue))
            {
                _currentValue = latestValue;
                foreach (var listener in _listeners)
                {
                    if (!listener.IsSubscribed)
                    {
                        continue;
                    }

                    if (listener.IsActive)
                    {
                        continue;
                    }

                    listener.PendingValue = latestValue;
                    listener.HasPendingValue = true;
                }

                listenersSnapshot = _listeners
                    .Where(listener => listener.IsSubscribed && listener.IsActive)
                    .Select(listener => listener.Listener)
                    .ToArray();
                shouldNotify = listenersSnapshot.Length > 0;
            }
        }

        if (!shouldNotify)
        {
            return;
        }

        foreach (var listener in listenersSnapshot)
        {
            listener(latestValue);
        }
    }

    /// <summary>
    ///     响应底层 Store 的状态变化，并在选中片段真正变化时通知监听器。
    /// </summary>
    /// <param name="state">新的完整状态。</param>
    private void OnStoreChanged(TState state)
    {
        var selectedValue = _selector(state);
        Action<TSelected>[] listenersSnapshot = Array.Empty<Action<TSelected>>();

        lock (_lock)
        {
            if (_listeners.Count == 0 || _comparer.Equals(_currentValue, selectedValue))
            {
                return;
            }

            _currentValue = selectedValue;
            foreach (var listener in _listeners)
            {
                if (!listener.IsSubscribed)
                {
                    continue;
                }

                if (listener.IsActive)
                {
                    continue;
                }

                listener.PendingValue = selectedValue;
                listener.HasPendingValue = true;
            }

            listenersSnapshot = _listeners
                .Where(listener => listener.IsSubscribed && listener.IsActive)
                .Select(listener => listener.Listener)
                .ToArray();
        }

        foreach (var listener in listenersSnapshot)
        {
            listener(selectedValue);
        }
    }

    /// <summary>
    ///     表示一个选择结果监听订阅。
    ///     该对象用于保证 RegisterWithInitValue 在初始化回放与后续状态变化之间不会漏掉最近一次更新。
    /// </summary>
    private sealed class SelectionListenerSubscription(Action<TSelected> listener)
    {
        /// <summary>
        ///     获取订阅回调。
        /// </summary>
        public Action<TSelected> Listener { get; } = listener;

        /// <summary>
        ///     获取或设置订阅是否已激活。
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        ///     获取或设置订阅是否仍然有效。
        /// </summary>
        public bool IsSubscribed { get; set; } = true;

        /// <summary>
        ///     获取或设置是否存在待补发的局部状态值。
        /// </summary>
        public bool HasPendingValue { get; set; }

        /// <summary>
        ///     获取或设置初始化阶段积累的最新局部状态值。
        /// </summary>
        public TSelected PendingValue { get; set; } = default!;
    }
}
