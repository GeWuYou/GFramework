# GFramework.Cqrs.Abstractions

`GFramework.Cqrs.Abstractions` 提供请求、通知、异步流、处理器、runtime 和管道的契约，目标为 `netstandard2.1`。它不包含 dispatcher、处理器扫描或默认运行时实现，也不要求 Architecture。

## 包关系

业务契约程序集可只引用 `GeWuYou.GFramework.Cqrs.Abstractions`。需要实际注册和分发时，应用组合根引用 `GeWuYou.GFramework.Cqrs`；需要编译期注册表时，消费项目再引用可选的 CQRS 生成器。

契约集中在 `Cqrs` 目录：

| 能力 | 契约 |
| --- | --- |
| 请求与响应 | `IRequest<TResponse>`、`IRequestHandler<TRequest, TResponse>`、`Unit` |
| 通知 | `INotification`、`INotificationHandler<TNotification>` |
| 异步流 | `IStreamRequest<TResponse>`、`IStreamRequestHandler<TRequest, TResponse>` |
| 分发 | `ICqrsRuntime` |
| 请求管道 | `IPipelineBehavior<,>`、`MessageHandlerDelegate<,>` |
| 流管道 | `IStreamPipelineBehavior<,>`、`StreamMessageHandlerDelegate<,>` |
| 注册与输入协作 | `ICqrsHandlerRegistrar`、`IRequestInput`、`INotificationInput` |

## 声明消息与处理器

```bash
dotnet add package GeWuYou.GFramework.Cqrs.Abstractions
```

```csharp
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;

public sealed record GetLength(string Value) : IRequest<int>;

public sealed class GetLengthHandler : IRequestHandler<GetLength, int>
{
    public ValueTask<int> Handle(GetLength request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(request.Value.Length);
    }
}
```

处理器用构造函数接收具体需要的依赖。`ICqrsRuntime` 的 `SendAsync`、`PublishAsync` 和 `CreateStream` 接收消息与取消令牌，不向处理器注入服务定位上下文。

## 生命周期边界

接口本身不选择处理器的 singleton、scoped 或 transient 生命周期。默认 runtime 通过标准 DI 解析；应用需要 scoped 依赖时，显式创建作用域并让其覆盖操作，异步流需要覆盖整个枚举过程。

只引用契约包时没有 `RequestBase`、`NotificationBase` 或 `AbstractRequestHandler` 等便利基类。实现接口即可声明消息与处理器；是否采用基类由应用决定。

## 相关文档

- [CQRS 采用指南](../docs/zh-CN/core/cqrs.md)
- [CQRS 运行时包](../GFramework.Cqrs/README.md)
