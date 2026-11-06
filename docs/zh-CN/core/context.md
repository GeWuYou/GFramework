---
title: 上下文（Context）
description: 说明 IArchitectureContext 与 ArchitectureContext 的统一上下文入口和当前推荐用法。
---

# 上下文（Context）

`IArchitectureContext` 是框架的统一上下文入口。

当前版本的上下文不再以“公开属性总线”作为主要模型，而是以一组明确的方法承载：

- 组件获取
- 事件系统
- 环境访问

默认实现类型是 `ArchitectureContext`。消息分发（request / notification / stream）不在上下文接口上；它通过 [CQRS 运行时](./cqrs.md)提供的扩展方法与 `ICqrsRuntime` 完成。

## 当前上下文约束

Architecture 激活组件时显式注入自己的上下文。ContextAwareBase 和生成的 ContextAware 实现都不会回退到 GameContext；未绑定对象调用 GetContext 会抛出 InvalidOperationException。

同类型或不同类型 Architecture 可以同时存在，各自持有服务、上下文与日志配置。配置阶段禁止服务解析；停止开始后外部保留的 Context 拒绝新操作。调用方应等待 InitializeAsync 后使用服务，并在使用完成后等待 DisposeAsync。

GetService 和组件获取方法遵循正式容器生命周期，不额外缓存 Transient。根上下文不能代替业务 scope；Scoped 服务通过显式 scope.ServiceProvider 解析。

## 公开总线入口

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

public sealed class PlayerDamageHandler : AbstractSystem
{
    protected override void OnInit()
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

## 事件入口

框架事件系统由上下文统一暴露：

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

## 消息分发入口

`IArchitectureContext` 本身不声明消息分发成员。request、notification 与 stream 的分发通过两类显式入口完成：

- `GFramework.Cqrs.Extensions` 里的上下文扩展（仅异步）：`SendRequestAsync`、`SendAsync`、`PublishAsync`、`CreateStream`
- 从容器解析 `ICqrsRuntime` 后直接调用（不依赖 Architecture 也可用）

```csharp
using GFramework.Cqrs.Extensions;

var playerId = await architecture.Context.SendRequestAsync<int>(
    new CreatePlayerRequest("Alice"));
```

```csharp
using GFramework.Cqrs.Extensions;

var playerId = await this.SendAsync<int>(new CreatePlayerRequest("Alice"));
```

前提是架构安装了 CQRS runtime 模块（`architecture.UseCqrsRuntime()` 或 `RegisterServiceModule(new CqrsRuntimeModule(...))`）；接入方式见 [CQRS 运行时](./cqrs.md)。

## `GameContext` 现在是什么角色

GameContext 与 GameContextProvider 仍是显式使用的兼容工具；Architecture 不再自动绑定或解绑它们。它们不会为未绑定的 ContextAware 对象提供隐式上下文。

新代码通过以下方式建立调用链：

- 让 Architecture 激活组件并注入实例上下文
- 使用生成的 ContextAware 实例接口，显式调用 SetContext
- 在组合根直接从 architecture.Context 发起调用

## 什么时候需要手动拿 `architecture.Context`

以下场景适合直接使用 `architecture.Context`：

- 组合根或启动代码
- 非 `IContextAware` 对象
- 测试中显式驱动请求和事件

## 继续阅读

- 架构入口：[architecture](./architecture.md)
- 生命周期：[lifecycle](./lifecycle.md)
- CQRS runtime：[cqrs](./cqrs.md)
