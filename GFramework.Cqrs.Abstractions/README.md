# GFramework.Cqrs.Abstractions

`GFramework.Cqrs.Abstractions` 提供 GFramework CQRS 的最小契约层。它只包含消息接口、处理器接口、运行时 seam 和管道契约，不包含默认 dispatcher、处理器扫描或任何 `GFramework.Core` 运行时实现。适合以下场景：

- 你的业务程序集只需要声明 Command、Query、Notification、Stream Request 或处理器接口。
- 你希望把消息契约放在更稳定的基础层，避免直接依赖默认 runtime 实现。
- 你要为其他运行时、测试环境或自定义容器实现兼容的 CQRS 接口。

## 模块定位

- 这是 CQRS 的“协议层”。
- 目标框架为 `netstandard2.1`，用于被更上层模块稳定引用。
- 当前包不负责处理器自动注册，也不负责请求分发。

如果你需要默认消息基类、处理器基类、上下文扩展方法和运行时实现，请使用 `GeWuYou.GFramework.Cqrs`。

## 包关系

推荐按职责引用：

- `GeWuYou.GFramework.Cqrs.Abstractions`
  - 提供 `IRequest<TResponse>`、`INotification`、`IStreamRequest<TResponse>`、`IRequestHandler<,>`、`INotificationHandler<>`、`IPipelineBehavior<,>`、`ICqrsRuntime`、`ICqrsContext`、`Unit` 等基础契约。
- `GeWuYou.GFramework.Cqrs`
  - 引用本包，并提供默认 runtime、处理器注册、消息基类、处理器基类、上下文扩展方法。
- `GeWuYou.GFramework.Cqrs.SourceGenerators`
  - 可选。面向消费端程序集生成 `ICqrsHandlerRegistry` 注册表，减少冷启动反射扫描；未生成或不适用时，运行时仍会回退到反射注册。

## 子系统地图

本包当前可以分为几类契约：

- 消息契约
  - `Cqrs/IRequest.cs`
  - `Cqrs/INotification.cs`
  - `Cqrs/IStreamRequest.cs`
  - `Cqrs/Command/ICommand.cs`
  - `Cqrs/Query/IQuery.cs`
  - `Cqrs/Request/IRequestInput.cs`
  - `Cqrs/Command/ICommandInput.cs`
  - `Cqrs/Query/IQueryInput.cs`
  - `Cqrs/Notification/INotificationInput.cs`
- 处理器契约
  - `Cqrs/IRequestHandler.cs`
  - `Cqrs/INotificationHandler.cs`
  - `Cqrs/IStreamRequestHandler.cs`
- 运行时 seam
  - `Cqrs/ICqrsRuntime.cs`
  - `Cqrs/ICqrsContext.cs`
  - `Cqrs/ICqrsHandlerRegistrar.cs`
- 管道与辅助类型
  - `Cqrs/IPipelineBehavior.cs`
  - `Cqrs/MessageHandlerDelegate.cs`
  - `Cqrs/Unit.cs`

## 最小接入路径

如果你只想在基础层定义一个可被上层 runtime 消费的 Query，可以只依赖本包：

```bash
dotnet add package GeWuYou.GFramework.Cqrs.Abstractions
```

```csharp
using GFramework.Cqrs.Abstractions.Cqrs.Query;

public sealed record GetPlayerProfileInput(int PlayerId) : IQueryInput;

public sealed class GetPlayerProfileQuery : IQuery<PlayerProfileDto>
{
    public GetPlayerProfileQuery(GetPlayerProfileInput input)
    {
        Input = input;
    }

    public GetPlayerProfileInput Input { get; }
}

public sealed class GetPlayerProfileHandler
    : IRequestHandler<GetPlayerProfileQuery, PlayerProfileDto>
{
    public ValueTask<PlayerProfileDto> Handle(
        GetPlayerProfileQuery request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

这条路径适合“只声明契约”的场景。真正执行分发时，仍需要上层提供 `ICqrsRuntime` 和处理器注册流程，通常由 `GeWuYou.GFramework.Cqrs` 与 `GFramework.Core` 完成。

## 使用边界

- 只引用本包时，没有 `CommandBase<TInput, TResponse>`、`QueryBase<TInput, TResponse>`、`NotificationBase<TInput>` 等消息基类。
- 只引用本包时，没有 `AbstractCommandHandler`、`AbstractQueryHandler`、`AbstractNotificationHandler` 等处理器基类。
- `ICqrsContext` 当前是轻量 marker seam；默认 runtime 在需要向 `IContextAware` 处理器注入上下文时，仍要求传入的上下文同时实现 `IArchitectureContext`。

## 文档入口

- 运行时与整体接入说明：`docs/zh-CN/core/cqrs.md`
- 如果你需要默认实现而不是契约层，请看 `GFramework.Cqrs/README.md`
