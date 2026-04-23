---
title: CQRS
description: Cqrs 模块族的运行时、契约层、生成器入口，以及 XML / API 阅读链路。
---

# CQRS

`Cqrs` 栏目对应三个直接相关的消费模块：

- `GFramework.Cqrs`
- `GFramework.Cqrs.Abstractions`
- `GFramework.Cqrs.SourceGenerators`

如果你在写新功能，优先使用这套请求模型，而不是继续扩展 `GFramework.Core.Command` / `Query` 的兼容层。

## 模块族边界

| 模块 | 角色 | 何时安装 |
| --- | --- | --- |
| `GeWuYou.GFramework.Cqrs.Abstractions` | 纯契约层，定义 request、notification、stream、handler、pipeline、runtime seam | 需要把消息契约放到更稳定的共享层，或只依赖接口做解耦 |
| `GeWuYou.GFramework.Cqrs` | 默认 runtime，提供 dispatcher、handler 基类、上下文扩展和程序集注册流程 | 大多数直接消费 CQRS 的业务模块 |
| `GeWuYou.GFramework.Cqrs.SourceGenerators` | 编译期生成 `ICqrsHandlerRegistry`，缩小运行时反射扫描范围 | handler 较多，想把注册映射前移到编译期 |

## 最小接入路径

最小安装组合是：

```bash
dotnet add package GeWuYou.GFramework.Cqrs
dotnet add package GeWuYou.GFramework.Cqrs.Abstractions
```

如果你希望消费端程序集在编译期生成 handler registry，再额外安装：

```bash
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators
```

## 最小示例

消息基类和处理器基类在不同命名空间：

- 消息基类：`GFramework.Cqrs.Command` / `Query` / `Notification`
- 处理器基类：`GFramework.Cqrs.Cqrs.Command` / `Query` / `Notification`

```csharp
using GFramework.Cqrs.Abstractions.Cqrs.Command;
using GFramework.Cqrs.Command;
using GFramework.Cqrs.Cqrs.Command;

public sealed record CreatePlayerInput(string Name) : ICommandInput;

public sealed class CreatePlayerCommand(CreatePlayerInput input)
    : CommandBase<CreatePlayerInput, int>(input)
{
}

public sealed class CreatePlayerCommandHandler
    : AbstractCommandHandler<CreatePlayerCommand, int>
{
    public override ValueTask<int> Handle(
        CreatePlayerCommand command,
        CancellationToken cancellationToken)
    {
        var playerModel = Context.GetModel<PlayerModel>();
        var playerId = playerModel.Create(command.Input.Name);
        return ValueTask.FromResult(playerId);
    }
}
```

如果你在 `IContextAware` 对象内部发送请求：

```csharp
using GFramework.Cqrs.Extensions;

var playerId = await this.SendAsync(
    new CreatePlayerCommand(new CreatePlayerInput("Alice")));
```

如果你在组合根或测试里发送请求：

```csharp
var playerId = await architecture.Context.SendRequestAsync(
    new CreatePlayerCommand(new CreatePlayerInput("Alice")));
```

最常用的上下文入口有：

- `SendRequestAsync(...)`
- `SendAsync(...)`
- `SendQueryAsync(...)`
- `PublishAsync(...)`
- `CreateStream(...)`

## 统一请求模型

这套 runtime 不只处理 command，也统一处理：

- Query
  - 读路径请求
- Notification
  - 一对多广播
- Stream Request
  - 返回 `IAsyncEnumerable<T>`

新代码通常不需要再分别设计“命令总线”“查询总线”和另一套通知分发语义。

## 处理器注册与生成器协作

在标准 `Architecture` 启动路径中，CQRS runtime 会自动接入基础设施。你通常只需要在 `OnInitialize()` 里追加行为或额外程序集：

```csharp
protected override void OnInitialize()
{
    RegisterCqrsPipelineBehavior<LoggingBehavior<,>>();
    RegisterCqrsPipelineBehavior<PerformanceBehavior<,>>();

    RegisterCqrsHandlersFromAssemblies(
    [
        typeof(InventoryCqrsMarker).Assembly,
        typeof(BattleCqrsMarker).Assembly
    ]);
}
```

默认注册流程当前遵循这些语义：

1. 优先读取消费端程序集上的 `CqrsHandlerRegistryAttribute`
2. 存在生成注册器时优先使用 `ICqrsHandlerRegistry`
3. 生成注册器不可用或元数据异常时记录告警并回退到反射路径
4. 如果程序集带有 `CqrsReflectionFallbackAttribute`，只补扫剩余 handler
5. 同一程序集按稳定键去重，避免重复注册

`Cqrs.SourceGenerators` 的专题入口见 [../source-generators/cqrs-handler-registry-generator.md](../source-generators/cqrs-handler-registry-generator.md)。

## Pipeline Behavior

如果你需要围绕请求处理流程插入横切逻辑，使用：

```csharp
RegisterCqrsPipelineBehavior<LoggingBehavior<,>>();
```

适合的场景包括：

- 日志
- 性能统计
- 校验
- 审计
- 重试或统一异常封装

当前公开入口只有 `RegisterCqrsPipelineBehavior<TBehavior>()`。

## 和旧 Command / Query 的关系

当前仓库同时存在两套路径：

- 旧路径
  - `GFramework.Core.Command`
  - `GFramework.Core.Query`
- 新路径
  - `GFramework.Cqrs`

`IArchitectureContext` 仍然兼容旧入口，但新代码应优先使用 CQRS runtime。

一个简单判断规则：

- 在维护历史代码：允许继续使用旧 Command / Query
- 在写新功能或新模块：优先使用 CQRS

## XML 覆盖基线

下面这份 inventory 记录的是 `2026-04-22` 对 `Cqrs` 家族做的一轮轻量 XML 盘点结果：只统计当前运行时、契约层和生成器入口中的类型声明级 XML 覆盖，用来校对 README、landing page 与 API 入口，不把它表述成成员级契约全审计。

| 类型族 | 基线状态 | 代表类型 | 阅读重点 |
| --- | --- | --- | --- |
| `GFramework.Cqrs.Abstractions/Cqrs/` | `20/20` 个类型声明已带 XML 注释 | `ICqrsRuntime`、`ICqrsHandlerRegistrar`、`IPipelineBehavior<,>`、`IRequestHandler<,>`、`Unit` | 先看请求、处理器和 runtime seam 的最小契约 |
| `GFramework.Cqrs/Command` `Query` `Notification` `Request` `Extensions` | `7/7` 个类型声明已带 XML 注释 | `CommandBase<TInput, TResponse>`、`QueryBase<TInput, TResponse>`、`NotificationBase<TInput>`、`ContextAwareCqrsExtensions` | 看业务侧常用基类和上下文发送入口 |
| `GFramework.Cqrs/Cqrs/` | `12/12` 个类型声明已带 XML 注释 | `AbstractCommandHandler<,>`、`AbstractQueryHandler<,>`、`AbstractNotificationHandler<>`、`LoggingBehavior<,>` | 看默认处理器基类、上下文注入与行为管道 |
| `GFramework.Cqrs` 根入口与 `Internal/` | `19/19` 个类型声明已带 XML 注释 | `CqrsRuntimeFactory`、`ICqrsHandlerRegistry`、`CqrsHandlerRegistryAttribute`、`CqrsReflectionFallbackAttribute`、`DefaultCqrsRegistrationService` | 看 runtime 创建入口、registry 协议、fallback 语义和程序集去重规则 |
| `GFramework.Cqrs.SourceGenerators/Cqrs/` | `3/3` 个类型声明已带 XML 注释 | `CqrsHandlerRegistryGenerator`、`RuntimeTypeReferenceSpec`、`OrderedRegistrationKind` | 看生成注册器、精确 type lookup 和 fallback 诊断边界 |

## 继续阅读

- 架构入口：[architecture](./architecture.md)
- 上下文入口：[context](./context.md)
- 生成器专题：[../source-generators/cqrs-handler-registry-generator.md](../source-generators/cqrs-handler-registry-generator.md)
- 模块 README：[`GFramework.Cqrs/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Cqrs/README.md)
