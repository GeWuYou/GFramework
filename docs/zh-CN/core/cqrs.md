---
title: CQRS
description: 当前推荐的新请求模型，统一覆盖 command、query、notification、stream request 和 pipeline behaviors。
---

# CQRS

`GFramework.Cqrs` 是当前推荐的新请求模型 runtime。

如果你在写新功能，优先使用这套模型，而不是继续扩展 `GFramework.Core.Command` / `Query` 的兼容层。

## 安装方式

```bash
dotnet add package GeWuYou.GFramework.Cqrs
dotnet add package GeWuYou.GFramework.Cqrs.Abstractions
```

如果你希望消费端程序集在编译期生成 handler registry，再额外安装：

```bash
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators
```

## 先理解分层

- `GFramework.Cqrs.Abstractions`
  - 纯契约层，定义请求、处理器、行为等接口
- `GFramework.Cqrs`
  - 默认 runtime、dispatcher、处理器基类和上下文扩展
- `GFramework.Cqrs.SourceGenerators`
  - 可选生成器，为消费端程序集生成 `ICqrsHandlerRegistry`

## 最小示例

消息基类和处理器基类在不同命名空间：

- 消息基类：`GFramework.Cqrs.Command` / `Query` / `Notification`
- 处理器基类：`GFramework.Cqrs.Cqrs.Command` / `Query` / `Notification`

示例：

```csharp
using GFramework.Cqrs.Command;
using GFramework.Cqrs.Cqrs.Command;
using GFramework.Cqrs.Abstractions.Cqrs.Command;

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

## 发送请求

如果你在 `IContextAware` 对象内部：

```csharp
using GFramework.Cqrs.Extensions;

var playerId = await this.SendAsync(
    new CreatePlayerCommand(new CreatePlayerInput("Alice")));
```

如果你在组合根或测试里：

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

## 查询、通知和流

这套 runtime 不只处理 command，也统一处理：

- Query
  - 读路径请求
- Notification
  - 一对多广播
- Stream Request
  - 返回 `IAsyncEnumerable<T>`

也就是说，新代码通常不需要再分别设计“命令总线”“查询总线”和另一套通知分发语义。

## 注册处理器

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

默认逻辑会：

1. 优先使用消费端程序集上的生成注册器
2. 生成注册器不可用时回退到反射扫描
3. 对同一程序集去重，避免重复注册

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

旧的 `Mediator` 兼容别名入口已经移除；当前公开入口只有 `RegisterCqrsPipelineBehavior<TBehavior>()`。

## 和旧 Command / Query 的关系

当前仓库同时存在两套路径：

- 旧路径
  - `GFramework.Core.Command`
  - `GFramework.Core.Query`
- 新路径
  - `GFramework.Cqrs`

`IArchitectureContext` 仍然会兼容旧入口，但新代码应优先使用 CQRS runtime。

一个简单判断规则：

- 在维护历史代码：允许继续使用旧 Command / Query
- 在写新功能或新模块：优先使用 CQRS

## 继续阅读

- 架构入口：[architecture](./architecture.md)
- 上下文入口：[context](./context.md)
- 模块 README：`GFramework.Cqrs/README.md`
