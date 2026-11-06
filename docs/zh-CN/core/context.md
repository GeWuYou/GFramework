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

## 当前上下文约束

Architecture 激活组件时显式注入自己的上下文。ContextAwareBase 和生成的 ContextAware 实现都不会回退到 GameContext；未绑定对象调用 GetContext 会抛出 InvalidOperationException。

同类型或不同类型 Architecture 可以同时存在，各自持有服务、上下文与日志配置。配置阶段禁止服务解析；停止开始后外部保留的 Context 拒绝新操作。调用方应等待 InitializeAsync 后使用服务，并在使用完成后等待 DisposeAsync。

GetService 和组件获取方法遵循正式容器生命周期，不额外缓存 Transient。根上下文不能代替业务 scope；Scoped 服务通过显式 scope.ServiceProvider 解析。CQRS 单例 runtime 的 Lazy 仍保留，但每次调用都会先检查上下文访问窗口，销毁后不能凭借缓存继续分发。

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

在标准 `Architecture` 初始化路径里，这些旧入口现在会复用同一个 `ICqrsRuntime`：
旧 `SendCommand(...)` / `SendQuery(...)` 仍保持原有调用方式，但会经过统一的 request pipeline 与上下文注入链路。
只有在你直接 `new CommandExecutor()`、`new QueryExecutor()` 或 `new AsyncQueryExecutor()` 做隔离测试，且没有提供 `ICqrsRuntime` 时，才会回退到 legacy 直接执行；此时 `SendQueryAsync(...)` 兼容入口也会沿用同样的 legacy 路径，而不会进入统一 runtime pipeline。

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
- 你要清楚地区分“旧 Command / Query 兼容入口”和“新 CQRS 入口”

## 继续阅读

- 架构入口：[architecture](./architecture.md)
- 生命周期：[lifecycle](./lifecycle.md)
- 旧命令系统：[command](./command.md)
- 旧查询系统：[query](./query.md)
- 新 CQRS runtime：[cqrs](./cqrs.md)
