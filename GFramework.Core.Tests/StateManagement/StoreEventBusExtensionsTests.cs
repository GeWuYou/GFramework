// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Events;

namespace GFramework.Core.Tests.StateManagement;

/// <summary>
///     Store 到 EventBus 桥接扩展的单元测试。
///     这些测试验证旧模块兼容桥接能够正确转发 dispatch 和状态变化事件，并支持运行时拆除。
/// </summary>
[TestFixture]
public class StoreEventBusExtensionsTests
{
    /// <summary>
    ///     测试桥接会发布每次 dispatch 事件，并对批处理后的状态变化只发送一次最终状态事件。
    /// </summary>
    [Test]
    public void BridgeToEventBus_Should_Publish_Dispatches_And_Collapsed_State_Changes()
    {
        var eventBus = new EventBus();
        var store = CreateStore();
        var dispatchedEvents = new List<StoreDispatchedEvent<CounterState>>();
        var stateChangedEvents = new List<StoreStateChangedEvent<CounterState>>();

        eventBus.Register<StoreDispatchedEvent<CounterState>>(dispatchedEvents.Add);
        eventBus.Register<StoreStateChangedEvent<CounterState>>(stateChangedEvents.Add);

        store.BridgeToEventBus(eventBus);

        store.Dispatch(new IncrementAction(1));
        store.RunInBatch(() =>
        {
            store.Dispatch(new IncrementAction(1));
            store.Dispatch(new IncrementAction(1));
        });

        Assert.That(dispatchedEvents.Count, Is.EqualTo(3));
        Assert.That(dispatchedEvents[0].DispatchRecord.NextState.Count, Is.EqualTo(1));
        Assert.That(dispatchedEvents[2].DispatchRecord.NextState.Count, Is.EqualTo(3));

        Assert.That(stateChangedEvents.Count, Is.EqualTo(2));
        Assert.That(stateChangedEvents[0].State.Count, Is.EqualTo(1));
        Assert.That(stateChangedEvents[1].State.Count, Is.EqualTo(3));
    }

    /// <summary>
    ///     测试桥接句柄注销后不会再继续向 EventBus 发送事件。
    /// </summary>
    [Test]
    public void BridgeToEventBus_UnRegister_Should_Stop_Future_Publications()
    {
        var eventBus = new EventBus();
        var store = CreateStore();
        var dispatchedEvents = new List<StoreDispatchedEvent<CounterState>>();
        var stateChangedEvents = new List<StoreStateChangedEvent<CounterState>>();

        eventBus.Register<StoreDispatchedEvent<CounterState>>(dispatchedEvents.Add);
        eventBus.Register<StoreStateChangedEvent<CounterState>>(stateChangedEvents.Add);

        var bridge = store.BridgeToEventBus(eventBus);

        store.Dispatch(new IncrementAction(1));
        bridge.UnRegister();
        store.Dispatch(new IncrementAction(1));

        Assert.That(dispatchedEvents.Count, Is.EqualTo(1));
        Assert.That(stateChangedEvents.Count, Is.EqualTo(1));
        Assert.That(stateChangedEvents[0].State.Count, Is.EqualTo(1));
    }

    /// <summary>
    ///     创建一个带基础 reducer 的测试 Store。
    /// </summary>
    /// <returns>测试用 Store 实例。</returns>
    private static Store<CounterState> CreateStore()
    {
        var store = new Store<CounterState>(new CounterState(0, "Player"));
        store.RegisterReducer<IncrementAction>((state, action) => state with { Count = state.Count + action.Amount });
        return store;
    }

    /// <summary>
    ///     用于桥接测试的状态类型。
    /// </summary>
    /// <param name="Count">当前计数值。</param>
    /// <param name="Name">当前名称。</param>
    private sealed record CounterState(int Count, string Name);

    /// <summary>
    ///     用于桥接测试的计数 action。
    /// </summary>
    /// <param name="Amount">要增加的数量。</param>
    private sealed record IncrementAction(int Amount);
}