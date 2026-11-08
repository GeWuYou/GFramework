---
title: 写操作请求
description: 说明修改状态的操作请求如今如何通过 CQRS runtime 的 request 模型表达与分发。
---

# 写操作请求

修改状态的业务操作通过 [CQRS runtime](./cqrs.md) 的 request 模型表达：请求对象实现 `IRequest<TResponse>`，处理逻辑放在对应的 `IRequestHandler<,>` 中，按需要返回结果；不关心返回值时使用 `IRequest<Unit>` 或无返回值的扩展入口。

## 最小示例

```csharp
using GFramework.Cqrs.Abstractions.Cqrs;

public sealed record RestoreHealthRequest(int PlayerId) : IRequest<Unit>;

public sealed class RestoreHealthHandler(PlayerModel playerModel)
    : IRequestHandler<RestoreHealthRequest, Unit>
{
    public ValueTask<Unit> Handle(
        RestoreHealthRequest request,
        CancellationToken cancellationToken)
    {
        playerModel.Health.Value = playerModel.MaxHealth.Value;
        return ValueTask.FromResult(Unit.Value);
    }
}
```

## 发送方式

本例的 `PlayerModel` 是应用提供的业务模型。处理器通过构造函数获取模型，在组合根登记相同依赖：

```csharp
services.AddSingleton<PlayerModel>();
services.AddSingleton<IRequestHandler<RestoreHealthRequest, Unit>, RestoreHealthHandler>();
services.AddCqrsRuntime();
```

在 `IContextAware` 对象（如生成过上下文注入的 Controller、Model、System）内：

```csharp
using GFramework.Cqrs.Extensions;

await this.SendAsync(new RestoreHealthRequest(playerId));
```

在组合根、非 `IContextAware` 对象或测试中，从容器解析 `ICqrsRuntime` 后直接分发：

```csharp
var runtime = serviceProvider.GetRequiredService<ICqrsRuntime>();
await runtime.SendAsync(new RestoreHealthRequest(playerId));
```

## 相关主题

- 请求、通知与流式请求的完整模型：[CQRS 运行时](./cqrs.md)
- 上下文入口与 `IContextAware` 扩展：[Context 上下文](./context.md)
