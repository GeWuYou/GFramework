---
title: "同步事件"
description: "发布进程内通知并管理订阅寿命。"
---

# 同步事件 {#overview}

```csharp
using GFramework.Core.Events;

var events = new EventBus();
using var subscriptions = new SubscriptionScope();
subscriptions.Add(events.Subscribe<int>(score => Console.WriteLine(score)));
events.Publish(42);
```

## 投递 {#delivery}

`Publish` 同步调用订阅快照中的回调，优先级较高的先执行，同值按订阅顺序。使用 `EventPropagation.Highest` 投递到最高优先级组，或用 `UntilHandled` 配合 `MarkAsHandled` 提前停止。回调异常向发布者传播。

## 释放与线程 {#ownership}

让视图或组件拥有订阅令牌，退出时释放。回调在发布线程运行；更新 Godot 节点前切到引擎线程。需要等待处理结果时，用服务方法或[CQRS 请求](../core/cqrs.md)。
