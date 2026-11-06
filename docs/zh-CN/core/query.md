---
title: 读操作请求
description: 说明读取数据的查询操作如今如何通过 CQRS runtime 的 request 模型表达与分发。
---

# 读操作请求

读取数据的查询通过 [CQRS runtime](./cqrs.md) 的 request 模型表达：请求对象实现 `IRequest<TResponse>` 并返回读取结果，处理逻辑放在对应的 `IRequestHandler<,>` 中。查询和写操作请求、通知、流式请求共用同一套分发入口、pipeline 与注册路径。

## 最小示例

```csharp
using GFramework.Cqrs.Abstractions.Cqrs;

public sealed record GetPlayerHealthRequest(int PlayerId) : IRequest<int>;

public sealed class GetPlayerHealthHandler
    : IRequestHandler<GetPlayerHealthRequest, int>
{
    public ValueTask<int> Handle(
        GetPlayerHealthRequest request,
        CancellationToken cancellationToken)
    {
        var playerModel = Context.GetModel<PlayerModel>();
        return ValueTask.FromResult(playerModel.Health.Value);
    }
}
```

## 发送方式

在 `IContextAware` 对象内：

```csharp
using GFramework.Cqrs.Extensions;

var health = await this.SendRequestAsync<int>(new GetPlayerHealthRequest(playerId));
```

在组合根、非 `IContextAware` 对象或测试中，从容器解析 `ICqrsRuntime` 后直接分发：

```csharp
var runtime = serviceProvider.GetRequiredService<ICqrsRuntime>();
var health = await runtime.SendAsync(new GetPlayerHealthRequest(playerId));
```

查询保持纯粹：只读取数据，不修改状态。需要返回 `IAsyncEnumerable<T>` 的读取路径使用 `IStreamRequest<TResponse>` 与对应的流式处理器。

## 相关主题

- 请求、通知与流式请求的完整模型：[CQRS 运行时](./cqrs.md)
- 上下文入口与 `IContextAware` 扩展：[Context 上下文](./context.md)
