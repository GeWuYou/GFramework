---
title: 上下文（Context）
description: 说明 IArchitectureContext 与 ArchitectureContext 的统一上下文入口和当前推荐用法。
---

# 上下文（Context）

`IArchitectureContext` 是框架的统一上下文入口。

当前版本的上下文不再以“公开属性总线”作为主要模型，而是以一组明确的方法同时承载：

- 组件获取
- 事件系统
- 旧 Command / Query 兼容入口
- 新 CQRS runtime 入口

默认实现类型是 `ArchitectureContext`。

## 先记住一个事实

如果你正在寻找这些属性式总线入口：

- `CommandBus`
- `QueryBus`
- `EventBus`
- `Container`

当前公开入口是方法，不是这些属性式总线。

## 组件访问

`IArchitectureContext` 直接提供按类型获取组件的方法：

```csharp
var context = architecture.Context;

var model = context.GetModel<PlayerModel>();
var system = context.GetSystem<CombatSystem>();
var utility = context.GetUtility<SaveUtility>();
var service = context.GetService<IMyService>();
```

也支持批量获取和按优先级获取：

- `GetModels<T>()`
- `GetSystems<T>()`
- `GetUtilities<T>()`
- `GetServices<T>()`
- `GetModelsByPriority<T>()`
- `GetSystemsByPriority<T>()`
- `GetUtilitiesByPriority<T>()`
- `GetServicesByPriority<T>()`

## 在 `IContextAware` 对象里怎么用

大多数业务代码不会手动把 `architecture.Context` 传来传去，而是通过 `IContextAware` 扩展方法访问上下文：

```csharp
using GFramework.Core.Extensions;

public sealed class DamagePlayerCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var playerModel = this.GetModel<PlayerModel>();
        playerModel.Health.Value -= 10;
    }
}
```

常用扩展包括：

- `GetModel<T>()`
- `GetSystem<T>()`
- `GetUtility<T>()`
- `GetService<T>()`
- `SendEvent(...)`
- `RegisterEvent(...)`
- `SendCommand(...)`
- `SendQuery(...)`

## 事件入口

框架事件系统仍然由上下文统一暴露：

```csharp
context.SendEvent(new PlayerDiedEvent());

var unRegister = context.RegisterEvent<PlayerDiedEvent>(static e =>
{
    Console.WriteLine("Player died.");
});
```

在 `IContextAware` 对象里也可以直接用扩展：

```csharp
this.SendEvent(new PlayerDiedEvent());
```

## 旧 Command / Query 兼容入口

当前上下文仍保留旧命令 / 查询体系：

- `SendCommand(ICommand)`
- `SendCommand<TResult>(ICommand<TResult>)`
- `SendCommandAsync(IAsyncCommand)`
- `SendCommandAsync<TResult>(IAsyncCommand<TResult>)`
- `SendQuery<TResult>(IQuery<TResult>)`
- `SendQueryAsync<TResult>(IAsyncQuery<TResult>)`

这部分入口主要用于兼容存量代码。新功能优先看 [cqrs](./cqrs.md)。

## 新 CQRS 入口

`IArchitectureContext` 也是当前 CQRS runtime 的主入口。最重要的方法是：

- `SendRequestAsync(...)`
- `SendRequest(...)`
- `SendAsync(...)`
- `PublishAsync(...)`
- `CreateStream(...)`
- `SendCommandAsync(...)` / `SendQueryAsync(...)` 的 CQRS 重载

示例：

```csharp
var playerId = await architecture.Context.SendRequestAsync(
    new CreatePlayerCommand(new CreatePlayerInput("Alice")));
```

如果你在 `IContextAware` 对象内部，通常直接用 `GFramework.Cqrs.Extensions` 里的扩展：

```csharp
using GFramework.Cqrs.Extensions;

var playerId = await this.SendAsync(
    new CreatePlayerCommand(new CreatePlayerInput("Alice")));
```

## `GameContext` 现在是什么角色

`GameContext` 仍然存在，但已经退到兼容和回退路径。

`ContextAwareBase` 在实例未显式注入上下文时，会回退到 `GameContext.GetFirstArchitectureContext()`。这个入口现在表示“当前活动上下文”，不再依赖全局注册表里的任意首项。这能保证部分旧代码继续工作，但它不是新代码的首选接法。

新代码更推荐：

- 让对象通过框架流程注入 `IArchitectureContext`
- 或使用 `[ContextAware]` 生成路径
- 或显式从 `architecture.Context` 启动调用链

## 什么时候需要手动拿 `architecture.Context`

以下场景适合直接使用 `architecture.Context`：

- 组合根或启动代码
- 非 `IContextAware` 对象
- 测试中显式驱动请求和事件
- 你要清楚地区分“旧 Command / Query 兼容入口”和“新 CQRS 入口”

## 继续阅读

- 架构入口：[architecture](./architecture.md)
- 生命周期：[lifecycle](./lifecycle.md)
- 旧命令系统：[command](./command.md)
- 旧查询系统：[query](./query.md)
- 新 CQRS runtime：[cqrs](./cqrs.md)
