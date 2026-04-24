---
title: 事件（Events）
description: 说明 GFramework.Core.Events 的轻量广播模型、安装方式与常用事件入口。
---

# 事件（Events）

`GFramework.Core.Events` 是架构内的轻量广播层。它适合表达“某件事已经发生”的运行时信号、模块间松耦合通知，
以及为旧模块保留 `EventBus` 语义；如果你需要请求/响应、pipeline behavior 或 handler registry，优先使用
[cqrs](./cqrs.md)。

## 安装方式

```bash
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions
```

事件实现位于 `GFramework.Core`，抽象接口位于 `GFramework.Core.Abstractions`。

## 最常用入口

如果你已经在 `ArchitectureContext` 或任何 `IContextAware` 对象里，最常见的入口仍然是：

- `SendEvent<TEvent>()`
- `SendEvent(eventData)`
- `RegisterEvent(Action<TEvent>)`
- `UnRegisterEvent(Action<TEvent>)`

示例：

```csharp
using GFramework.Core.Extensions;
using GFramework.Core.System;

public sealed record PlayerDiedEvent(int PlayerId);

public sealed class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnPlayerDied(PlayerDiedEvent @event)
    {
        Logger.Info("Player died: {0}", @event.PlayerId);
    }

    public void KillPlayer(int playerId)
    {
        this.SendEvent(new PlayerDiedEvent(playerId));
    }
}
```

如果你在架构外单独使用，也可以直接构造 `EventBus`。

## EventBus 与 EnhancedEventBus

默认实现是 `EventBus`，提供类型化发送与订阅：

```csharp
using GFramework.Core.Events;

var eventBus = new EventBus();

eventBus.Register<PlayerJoinedEvent>(e =>
{
    Console.WriteLine(e.Name);
});

eventBus.Send(new PlayerJoinedEvent("Alice"));
```

如果你还需要统计、过滤或弱引用订阅，可以改用 `EnhancedEventBus`。它在 `EventBus` 基础上额外提供：

- `Statistics`
- `SendFilterable(...)` / `RegisterFilterable(...)`
- `SendWeak(...)` / `RegisterWeak(...)`

这类能力更适合工具层、编辑器层或长生命周期对象，不必默认扩散到每个业务事件。

## 优先级、传播与上下文事件

当事件处理顺序或“是否继续传播”本身就是语义的一部分时，使用优先级入口：

```csharp
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Events;

public sealed record InputCommand(string Name);

var eventBus = new EventBus();

eventBus.RegisterWithContext<InputCommand>(ctx =>
{
    if (ctx.Data.Name == "Pause")
    {
        Console.WriteLine("Pause handled");
        ctx.MarkAsHandled();
    }
}, priority: 10);

eventBus.Send(new InputCommand("Pause"), EventPropagation.UntilHandled);
```

当前公开语义是：

- `Register<T>(handler, priority)`：按优先级订阅
- `RegisterWithContext<T>(...)`：拿到 `EventContext<T>`
- `EventPropagation.All`：广播给全部监听器
- `EventPropagation.UntilHandled`：直到上下文事件被标记为 handled
- `EventPropagation.Highest`：只执行最高优先级层

## 局部事件对象

如果事件只在一个对象或一个小模块内部流动，不必一定挂到 `EventBus`。当前仍可直接使用：

- `EasyEvent`
- `Event<T>`
- `Event<T1, T2>`
- `OrEvent`
- `EventListenerScope<TEvent>`

这类类型更适合局部组合和 UI/工具层内聚逻辑，不适合作为全局消息总线的替代品。

## 与 Store / CQRS 的边界

- 轻量运行时广播：`EventBus`
- 聚合状态演进：`Store<TState>`，必要时用 `BridgeToEventBus(...)` 兼容旧事件消费者
- 新业务请求模型：`GFramework.Cqrs`

一个简单判断规则是：如果你关心“谁来处理、是否有返回值、是否要挂 pipeline”，用 CQRS；如果你只是广播
“这件事发生了”，事件系统更直接。
