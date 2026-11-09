---
title: 集中状态管理
description: 使用 Store、Reducer、选择器、批量通知与历史组织聚合状态。
---

# 集中状态管理

`Store<TState>` 是可选的集中状态容器：状态通过 action/reducer 更新，视图通过只读接口或选择器观察。它适合确实需要聚合更新、局部订阅、middleware 或历史的状态。

单字段展示使用 BindableProperty，流程切换使用 StateMachine。三者职责不同，不应同时成为同一份业务值的任意写入入口。

## 创建与更新

```csharp
using System;
using GFramework.Core.StateManagement;

var store = new Store<UiState>(new UiState(100));
store.RegisterReducer<Damage>((state, action) => state with
{
    Health = Math.Max(0, state.Health - action.Amount)
});
using var subscription = store.SubscribeWithInitValue(state => Console.WriteLine(state.Health));
store.Dispatch(new Damage(15));

public sealed record UiState(int Health);
public sealed record Damage(int Amount);
```

Reducer 应是纯状态转换，返回新的不可变状态；文件读取、网络、节点访问等副作用由应用用例执行。应用对象可通过构造函数接收 Store 或实际所需接口，视图只接收 `IReadonlyStore<TState>`。

## Builder 与匹配

`Store<TState>`.CreateBuilder() 支持 AddReducer、UseMiddleware、WithComparer、WithHistoryCapacity 与 WithActionMatching，最后 Build(initialState)。普通构造器和 builder 都可独立使用，无需 Architecture。

默认 ExactTypeOnly 按 action 精确类型匹配。需要基类/接口 reducer 时，显式选择 StoreActionMatchingMode.IncludeAssignableTypes，并确认多个适用 reducer 的实际顺序符合业务目标。

```csharp
using GFramework.Core.Abstractions.StateManagement;
using GFramework.Core.StateManagement;

var store = Store<int>.CreateBuilder()
    .WithHistoryCapacity(16)
    .WithActionMatching(StoreActionMatchingMode.ExactTypeOnly)
    .AddReducer<int>((state, amount) => state + amount)
    .Build(0);
```

## 订阅与选择器

Subscribe 观察后续状态；SubscribeWithInitValue 同时提供初始值。返回的 IDisposable 由视图或会话拥有，退出时释放。

```csharp
using System;
using GFramework.Core.Extensions;
using GFramework.Core.StateManagement;

var store = new Store<int>(0);
store.RegisterReducer<int>((state, amount) => state + amount);
var display = store.Select(state => $"Score: {state}");
using var subscription = display.RegisterWithInitValue(Console.WriteLine);
store.Dispatch(1);
```

Select 产生只读的 StoreSelection；选择值未改变时不通知。选择器可以传入自己的 IEqualityComparer。ToBindableProperty 提供相同的只读绑定能力，不让视图通过投影修改源状态。

## Middleware、批量与事件桥

`IStoreMiddleware<TState>`.Invoke(context, next) 包围分发，调用 next 让后续处理继续。UseMiddleware 用于持续安装；RegisterMiddleware 和 RegisterReducerHandle 返回可释放登记令牌。释放影响后续分发，不会修改已捕获的当前执行快照。

RunInBatch 合并对订阅者的状态通知；它不把外部副作用变成事务，也不提供失败后自动撤销所有操作的保证。

```csharp
using GFramework.Core.Events;
using GFramework.Core.Extensions;
using GFramework.Core.StateManagement;

var store = new Store<int>(0);
var bus = new EventBus();
using var bridge = store.BridgeToEventBus(bus);
```

BridgeToEventBus 可分别启用分发记录与状态变化通知，返回 IDisposable。事件在分发线程同步执行；桥接不提供后台队列或网络投递。

## 历史与诊断

历史容量大于零时，可使用 CanUndo、CanRedo、Undo、Redo、TimeTravelTo 和 ClearHistory。历史存储状态值，不复制可变引用对象；因此 reducer 需要按不可变状态约定工作。

IStoreDiagnostics 提供订阅数量、最近 action、分发记录、历史位置与快照等信息，便于应用调试。历史不是持久化存档，也不是通用事件溯源机制。

相关主题：[可绑定属性](./property.md)、[状态机](./state-machine.md)、[错误处理](../best-practices/error-handling.md)。

## 提交后的通知

Store 与 StoreSelection 对已提交的状态、历史跳转和选择值逐个通知直接订阅者。观察者异常不会撤销状态、破坏历史或阻断其他订阅者，可通过可选的 `observerLogger` 记录到宿主日志，默认使用 Trace；Store 创建的选择视图沿用该日志。Reducer 与 middleware 的业务失败继续传播。直接读取选择值时，选择函数的求值异常仍向调用方报告；提交后由订阅通知触发的选择求值失败在该通知边界隔离。初始化订阅回放失败仍拒绝该次订阅并清理它。
