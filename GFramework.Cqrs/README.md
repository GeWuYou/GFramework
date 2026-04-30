# GFramework.Cqrs

`GFramework.Cqrs` 是 GFramework 的默认 CQRS runtime 包。它在 `GFramework.Cqrs.Abstractions` 之上提供请求分发、通知发布、流式请求处理、处理器注册、上下文扩展方法，以及消息/处理器基类，面向直接使用 GFramework CQRS 的业务模块。

## 模块定位

- 这是 CQRS 的“默认实现层”。
- 包内同时承载运行时基础设施和面向业务代码的便捷基类。
- 它依赖 `GFramework.Cqrs.Abstractions` 与 `GFramework.Core.Abstractions`。
- 在标准 GFramework 架构启动路径中，`GFramework.Core` 的 `CqrsRuntimeModule` 会把默认 runtime、处理器注册器与注册服务自动接入容器。

如果你只需要声明跨模块共享的消息契约，而不想依赖默认 runtime，请改为引用 `GeWuYou.GFramework.Cqrs.Abstractions`。

## 包关系

推荐的依赖关系如下：

- `GeWuYou.GFramework.Cqrs.Abstractions`
  - 最小 CQRS 契约层。
- `GeWuYou.GFramework.Cqrs`
  - 默认 runtime 与业务侧常用基类。
- `GeWuYou.GFramework.Cqrs.SourceGenerators`
  - 可选。为消费端程序集生成 `ICqrsHandlerRegistry`，运行时优先走生成注册表；只有缺失、不适用，或 fallback 仍需补齐剩余 handler 时，才继续进入反射路径。
- `GFramework.Core`
  - 架构上下文中实际调用 `ICqrsRuntime`，并在模块初始化时注册 CQRS 基础设施。

## 子系统地图

本包当前主要由以下子系统组成：

- 消息基类
  - `Command/CommandBase.cs`
  - `Query/QueryBase.cs`
  - `Request/RequestBase.cs`
  - `Notification/NotificationBase.cs`
- 处理器基类
  - `Cqrs/CqrsContextAwareHandlerBase.cs`
  - `Cqrs/Command/AbstractCommandHandler.cs`
  - `Cqrs/Query/AbstractQueryHandler.cs`
  - `Cqrs/Notification/AbstractNotificationHandler.cs`
  - `Cqrs/Request/AbstractRequestHandler.cs`
  - `Cqrs/Request/AbstractStreamRequestHandler.cs`
  - `Cqrs/Command/AbstractStreamCommandHandler.cs`
  - `Cqrs/Query/AbstractStreamQueryHandler.cs`
- 请求管道
  - `Cqrs/Behaviors/LoggingBehavior.cs`
  - `Cqrs/Behaviors/PerformanceBehavior.cs`
  - 管道契约定义在 `GFramework.Cqrs.Abstractions` 的 `IPipelineBehavior<,>`
- 默认 runtime 与注册入口
  - `CqrsRuntimeFactory.cs`
  - `Internal/CqrsDispatcher.cs`
  - `Notification/INotificationPublisher.cs`
  - `Internal/CqrsHandlerRegistrar.cs`
  - `Internal/DefaultCqrsHandlerRegistrar.cs`
  - `Internal/DefaultCqrsRegistrationService.cs`
- 生成注册表协作接口
  - `ICqrsHandlerRegistry.cs`
  - `CqrsHandlerRegistryAttribute.cs`
  - `CqrsReflectionFallbackAttribute.cs`
- 业务侧扩展入口
  - `Extensions/ContextAwareCqrsExtensions.cs`
  - `Extensions/ContextAwareCqrsCommandExtensions.cs`
  - `Extensions/ContextAwareCqrsQueryExtensions.cs`

## 最小接入路径

在标准 GFramework 架构中，最小接入通常是“安装包 + 定义消息 + 定义处理器 + 通过上下文发送”：

```bash
dotnet add package GeWuYou.GFramework.Cqrs
dotnet add package GeWuYou.GFramework.Cqrs.Abstractions
```

如果你希望减少处理器注册时的反射扫描，再额外安装：

```bash
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators
```

示例：

```csharp
using GFramework.Cqrs.Command;
using GFramework.Cqrs.Cqrs.Command;
using GFramework.Cqrs.Abstractions.Cqrs.Command;

public sealed record CreatePlayerInput(string Name) : ICommandInput;

public sealed class CreatePlayerCommand : CommandBase<CreatePlayerInput, int>
{
    public CreatePlayerCommand(CreatePlayerInput input) : base(input)
    {
    }
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

在 `IContextAware` 对象内发送命令：

```csharp
using GFramework.Cqrs.Extensions;

var playerId = await this.SendAsync(new CreatePlayerCommand(new CreatePlayerInput("Alice")));
```

在 `ArchitectureContext` 上也可以直接使用统一 CQRS 入口，例如 `SendRequestAsync`、`SendQueryAsync`、`PublishAsync` 和 `CreateStream`。

## 运行时行为

- 请求分发
  - `CqrsDispatcher` 按请求实际类型解析 `IRequestHandler<,>`，未找到处理器会抛出异常。
- 通知分发
  - 通知会分发给所有已注册 `INotificationHandler<>`；零处理器时默认静默完成。
  - 默认通知发布器会按容器解析顺序逐个执行处理器，并在首个处理器抛出异常时立即停止后续分发。
  - 若容器在 runtime 创建前已显式注册 `INotificationPublisher`，默认 runtime 会复用该策略；未注册时回退到内置顺序发布器。
- 流式请求
  - 通过 `IStreamRequest<TResponse>` 和 `IStreamRequestHandler<,>` 返回 `IAsyncEnumerable<TResponse>`。
- 上下文注入
  - 处理器基类继承 `CqrsContextAwareHandlerBase`，runtime 会在分发前注入当前 `IArchitectureContext`。
  - 如果处理器或行为需要上下文注入，而当前 `ICqrsContext` 不是 `IArchitectureContext`，默认实现会抛出异常。
- 管道行为
  - 所有已注册 `IPipelineBehavior<TRequest, TResponse>` 会包裹请求处理器执行。
  - 当前包内提供了 `LoggingBehavior` 和 `PerformanceBehavior` 两个可复用行为。

## 处理器注册与程序集接入

默认注册流程由 `ICqrsRegistrationService.RegisterHandlers(IEnumerable<Assembly>)` 协调，语义是：

- 同一程序集按稳定键去重，避免重复注册。
- 优先尝试消费端程序集上的 `ICqrsHandlerRegistry` 生成注册器。
- 生成注册器不可用或元数据损坏时，记录告警并回退到反射扫描。
- 当程序集声明了 `CqrsReflectionFallbackAttribute` 时，运行时会先执行生成注册器，再只补它未覆盖的 handler。
- `CqrsReflectionFallbackAttribute` 现在可以多次声明，并同时承载 `Type[]` 与 `string[]` 两类 fallback 清单。
- 运行时会优先复用 fallback 特性里直接提供的 `Type` 条目，只对字符串条目执行定向 `Assembly.GetType(...)` 查找；只有旧版空 marker 才会退回整程序集扫描。
- 处理器以 transient 方式注册，避免上下文感知处理器在并发请求间共享可变上下文。

如果你走标准 `GFramework.Core` 架构初始化路径，这些步骤通常由框架自动完成；裸容器或测试环境则需要显式补齐 runtime 与注册入口。

## 适用边界

- 这个包是默认实现，不是“纯契约包”。
- 处理器基类依赖 runtime 在分发前注入上下文，不适合脱离 dispatcher 直接手动实例化后调用。
- README 中的消息基类和 handler 基类位于 `GFramework.Cqrs`，接口契约位于 `GFramework.Cqrs.Abstractions`；最小示例通常需要同时引入这两个命名空间层级。

## 文档入口

- 总体文档：[CQRS 栏目](../docs/zh-CN/core/cqrs.md)
- 契约层说明：[CQRS 抽象层说明](../GFramework.Cqrs.Abstractions/README.md)
