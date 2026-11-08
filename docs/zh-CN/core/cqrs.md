---
title: CQRS 运行时
description: Cqrs 模块的独立采用、架构集成、请求模型、处理器注册与生成器协作。
---

# CQRS 运行时

CQRS 模块族提供统一的 request / notification / stream 消息模型，包含三个直接相关的消费模块：

- `GFramework.Cqrs`
- `GFramework.Cqrs.Abstractions`
- `GFramework.Cqrs.SourceGenerators`

runtime 运行在标准 `Microsoft.Extensions.DependencyInjection` 容器上。CQRS-only 应用不依赖 `GFramework.Core` 也能使用；`GFramework.Core` 也不再传递包含 CQRS，需要消息模型的项目显式安装。

## 模块族边界

| 模块 | 角色 | 何时安装 |
| --- | --- | --- |
| `GeWuYou.GFramework.Cqrs.Abstractions` | 纯契约层，定义 request、notification、stream、handler、pipeline、runtime seam | 需要把消息契约放到更稳定的共享层，或只依赖接口做解耦 |
| `GeWuYou.GFramework.Cqrs` | 默认 runtime，提供 dispatcher、notification publisher、处理器基类、注册入口和上下文扩展 | 大多数直接消费 CQRS 的业务模块 |
| `GeWuYou.GFramework.Cqrs.SourceGenerators` | 编译期生成 `ICqrsHandlerRegistry`，让运行时先走生成注册器，再只对剩余 handler 做定向 fallback | handler 较多，想把注册映射前移到编译期 |

安装组合：

```bash
dotnet add package GeWuYou.GFramework.Cqrs
dotnet add package GeWuYou.GFramework.Cqrs.Abstractions
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators   # 可选
```

## 独立采用（标准 DI）

不使用 `Architecture` 时，在任意标准 DI 容器上注册 runtime。注册入口位于 `GFramework.Cqrs` 命名空间：

```csharp
using GFramework.Cqrs;

var services = new ServiceCollection();

services.AddCqrsRuntime();
services.AddCqrsHandlersFromAssembly(typeof(CreatePlayerHandler).Assembly);
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

await using var provider = services.BuildServiceProvider();
var runtime = provider.GetRequiredService<ICqrsRuntime>();
```

`AddCqrsRuntime` 提供 `AddCqrsRuntime()`、`AddCqrsRuntime(ILoggerFactory?)`（提供标准 `ILoggerFactory` 时注册进 `IServiceCollection`）与 `AddCqrsRuntime(ILogger)` 三种重载，runtime 单例在 provider 构建完成后惰性创建。`LoggingBehavior`、`PerformanceBehavior` 等 pipeline 行为通过构造注入 `ILogger<T>` 获取日志。

默认单例只解析根 provider 可用的依赖。请求宿主需要 Scoped handler 时，使用 `AddScopedCqrsRuntime()`，按业务需要登记 Scoped handler 与依赖，并从 `scope.ServiceProvider` 解析 `ICqrsRuntime`；三个分发入口均遵循同一作用域。跨作用域不能共享该 runtime，也不能与单例注册混用。

从 `ICqrsRuntime` 分发（全部为异步入口）：

- `SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken, ICqrsContext?)`
- `PublishAsync<TNotification>(TNotification notification, CancellationToken, ICqrsContext?)`
- `CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken, ICqrsContext?)`

可选的 `ICqrsContext` 参数只在处理链包含 `IContextAware` 对象时需要；未提供时按[上下文注入规则](#上下文注入规则)处理。

## 在 Architecture 中集成

使用 `GFramework.Core` 的 `Architecture` 时，有两种接入方式：

```csharp
using GFramework.Cqrs.Extensions;

public sealed class GameArchitecture : Architecture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        this.UseCqrsRuntime(typeof(InventoryModule).Assembly);
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.UseTaskWhenAllNotificationPublisher();
    }
}

```

配置完成后创建架构并调用 `InitializeAsync()`。模块必须在 `ConfigureServices` 或其配置回调窗口中安装；不能在启动前直接调用 `architecture.UseCqrsRuntime()`。

或者在配置窗口中显式登记服务模块：

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
    RegisterServiceModule(new CqrsRuntimeModule(typeof(InventoryModule).Assembly));
}
```

`CqrsRuntimeModule` 的构造函数接受 `params Assembly[] handlerAssemblies`；`UseCqrsRuntime(params Assembly[] additionalHandlerAssemblies)` 会在其中自动补充架构所在程序集。`CqrsRuntimeModule` 不再内置到 Core 的默认模块列表，未安装它的架构没有 CQRS 分发能力。

## 统一请求模型

| 消息类型 | 契约 | 分发入口 | 语义 |
| --- | --- | --- | --- |
| Request | `IRequest<TResponse>` | `SendAsync` / `SendRequestAsync` | 单个处理器，返回结果 |
| Notification | `INotification` | `PublishAsync` | 零到多个处理器，一对多广播 |
| Stream Request | `IStreamRequest<TResponse>` | `CreateStream` | 返回 `IAsyncEnumerable<TResponse>` |

命名习惯上，写操作请求常以 `...Request` / 命令语义命名，读操作请求以查询语义命名；runtime 不区分两者，处理方式完全一致。

## 最小示例

```csharp
using GFramework.Cqrs.Abstractions.Cqrs;

public sealed record CreatePlayerRequest(string Name) : IRequest<int>;

public sealed class CreatePlayerHandler(PlayerModel playerModel)
    : IRequestHandler<CreatePlayerRequest, int>
{
    public ValueTask<int> Handle(
        CreatePlayerRequest request,
        CancellationToken cancellationToken)
    {
        var playerId = playerModel.Create(request.Name);
        return ValueTask.FromResult(playerId);
    }
}
```

`PlayerModel` 是应用提供的业务模型；在组合根登记 `services.AddSingleton<PlayerModel>()`，让标准 DI 注入处理器。裸 `runtime.SendAsync()` 不为处理器创建 Architecture 上下文。

在 `IContextAware` 对象内部发送请求：

```csharp
using GFramework.Cqrs.Extensions;

var playerId = await this.SendAsync<int>(new CreatePlayerRequest("Alice"));
```

在组合根或测试里通过架构上下文发送：

```csharp
var playerId = await architecture.Context.SendRequestAsync<int>(
    new CreatePlayerRequest("Alice"));
```

上下文扩展（命名空间 `GFramework.Cqrs.Extensions`）只有异步入口：

| 扩展方法 | 作用对象 | 对应 runtime 调用 |
| --- | --- | --- |
| `SendRequestAsync<TResponse>(request, ct)` | `IArchitectureContext` / `IContextAware` | `SendAsync` |
| `SendAsync<TCommand>(command, ct)` / `SendAsync<TResponse>(request, ct)` | `IArchitectureContext` / `IContextAware` | `SendAsync` |
| `PublishAsync(notification, ct)` | `IArchitectureContext` / `IContextAware` | `PublishAsync` |
| `CreateStream<TResponse>(request, ct)` | `IArchitectureContext` / `IContextAware` | `CreateStream` |

这些扩展把当前架构上下文绑定为分发上下文，让 `IContextAware` 处理器能拿到 `IArchitectureContext`。

## 消息与处理器基类

业务侧可复用的基类分布在不同命名空间：

| 基类 | 命名空间 | 用途 |
| --- | --- | --- |
| `RequestBase<TInput, TResponse>` | `GFramework.Cqrs.Request` | 带输入与返回值的请求基类 |
| `NotificationBase<TInput>` | `GFramework.Cqrs.Notification` | 通知基类 |
| `AbstractRequestHandler<TRequest, TResponse>` | `GFramework.Cqrs.Cqrs.Request` | 请求处理器基类，支持上下文注入 |
| `AbstractStreamRequestHandler<TRequest, TResponse>` | `GFramework.Cqrs.Cqrs.Request` | 流式请求处理器基类 |
| `AbstractNotificationHandler<TNotification>` | `GFramework.Cqrs.Cqrs.Notification` | 通知处理器基类 |

不需要基类时，直接实现 `IRequestHandler<,>`、`INotificationHandler<>`、`IStreamRequestHandler<,>` 也可以。

## 上下文注入规则

处理器与 pipeline behavior 可以实现 `IContextAware`，runtime 会在分发前注入上下文：

- 在 `Architecture` 中运行时，上下文扩展会把当前 `IArchitectureContext` 绑定为分发上下文，处理器内可直接使用 `GetModel<T>()`、`GetSystem<T>()` 等方法
- 独立采用（无 Architecture）时，处理器通过构造函数注入获取依赖，不要依赖上下文注入
- 如果处理链包含 `IContextAware` 对象，而传入的 `ICqrsContext` 为空或不是 `IArchitectureContext`，注入会抛出 `InvalidOperationException`

## 通知发布策略

当前通知分发默认保持顺序语义：

- 零处理器时静默完成
- 已解析处理器按容器顺序逐个执行
- 首个处理器抛出异常时立即停止后续分发
- 如果容器在 runtime 创建前已显式注册 `INotificationPublisher`，默认 runtime 会复用该策略；未注册时回退到内置 `SequentialNotificationPublisher`
- 默认 runtime 只消费一个 `INotificationPublisher`；如果容器里已经存在该注册，再调用 `UseNotificationPublisher*` 系列扩展会直接报错，而不是按“后注册覆盖前注册”处理

| 策略 | 适用场景 | 顺序语义 | 失败语义 | 备注 |
| --- | --- | --- | --- | --- |
| `UseSequentialNotificationPublisher()` | 需要保持容器顺序，且希望首个失败立即停止 | 保证按容器顺序执行 | 首个处理器异常会中断后续处理器 | 这也是默认回退策略 |
| `UseTaskWhenAllNotificationPublisher()` | 需要让全部处理器并行完成，再统一观察异常或取消 | 不保证顺序 | 不会在首个失败时中断其余处理器；全部结束后统一暴露结果 | 更适合语义补齐，不是性能优化开关 |
| `UseNotificationPublisher(...)` / `UseNotificationPublisher<TPublisher>()` | 需要接入自定义或第三方 publisher 策略 | 取决于实现 | 取决于实现 | 前者复用现成实例，后者让容器负责单例生命周期；两者都要求容器此前尚未注册 `INotificationPublisher` |

这些扩展都作用于 `IServiceCollection`：

```csharp
using GFramework.Cqrs.Extensions;

services.UseSequentialNotificationPublisher();
// 或
services.UseTaskWhenAllNotificationPublisher();
// 或
services.UseNotificationPublisher(new TaskWhenAllNotificationPublisher());
services.UseNotificationPublisher<MyCustomNotificationPublisher>();
```

`TaskWhenAllNotificationPublisher` 不保证处理器执行顺序、不会在首个处理器失败时立即停止其余处理器，并会在全部处理器结束后统一暴露异常或取消结果。当前 fixed `4 handler` fan-out benchmark 中，它的 steady-state 成本也高于默认顺序发布器；因此它更适合“我要并行语义”，而不是“我要更快的 publish”。

## Pipeline Behavior

围绕请求处理流程插入横切逻辑：

```csharp
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

适合的场景包括日志、性能统计、校验、审计、重试或统一异常封装。

围绕 `CreateStream(...)` 的建流过程插入横切逻辑时，实现 `IStreamPipelineBehavior<TRequest, TResponse>` 并注册：

```csharp
services.AddSingleton(typeof(IStreamPipelineBehavior<,>), typeof(MyStreamBehavior<,>));
```

两个边界需要明确：

- `AddCqrsPipelineBehavior<TBehavior>()`
  - 只作用于 `SendAsync` / `SendRequestAsync` 这类单次请求分发
- `AddCqrsStreamPipelineBehavior<TBehavior>()`
  - 作用于 `CreateStream(...)` 的建流阶段
  - 默认实现围绕单次建流调用编排行为链，不会自动把行为扩展成“每个流元素都单独拦截一次”

## 处理器注册与生成器协作

handler 通过程序集注册：

```csharp
services.AddCqrsHandlersFromAssembly(typeof(GameHandlers).Assembly);
// 或一次注册多个程序集
services.AddCqrsHandlersFromAssemblies(
[
    typeof(InventoryMarker).Assembly,
    typeof(BattleMarker).Assembly
]);
```

在 `Architecture` 集成路径中，把同样的程序集传给 `UseCqrsRuntime(...)` 或 `CqrsRuntimeModule(...)` 即可，不需要再单独调用注册扩展。

默认注册流程当前遵循这些语义：

1. 优先读取消费端程序集上的 `CqrsHandlerRegistryAttribute`
2. 存在生成注册器时优先使用 `ICqrsHandlerRegistry`
3. 当生成注册器同时暴露 generated request invoker provider 时，runtime 会把 request/response 类型对对应的 descriptor 预先接线到 dispatcher 缓存，后续请求分发优先消费这些 generated request invoker 元数据
4. 当生成注册器同时暴露 generated stream invoker provider 时，runtime 会以同样方式优先消费 stream request 对应的 generated stream invoker descriptor；只有当前类型对未命中时，才回退到既有反射 stream binding
5. generated invoker 只覆盖 request 与 stream 两类单次分发元数据；notification handler 仍通过已注册的 `INotificationHandler<>` 集合和选定的 `INotificationPublisher` 参与分发，不存在对应的 generated notification invoker 通道
6. 生成注册器不可用时记录告警并回退到反射路径；只有“未命中 generated descriptor”才会走反射 binding 创建，已成功登记到缓存的类型对不会再回退到另一条 generated 通道
7. 当生成注册器携带 `CqrsReflectionFallbackAttribute` 元数据时，运行时会先完成生成注册器注册，再补剩余 handler
8. `CqrsReflectionFallbackAttribute` 可以同时携带 `Type[]` 和 `string[]` 两类清单；运行时会优先复用直接 `Type` 条目，只对名称条目做定向 `Assembly.GetType(...)` 查找
9. 只有 fallback 元数据为空、仍是旧版空 marker 语义，或生成注册器整体不可用时，才会回到整程序集反射扫描
10. 同一程序集按稳定键去重，避免重复注册

换句话说，声明 fallback 特性本身不等于“整包反射扫描”。生成注册器负责能静态表达的部分，fallback 只补它覆盖不到的 handler。安装 `Cqrs.SourceGenerators` 后，不要求“所有 handler 都能被生成代码直接引用”才有收益。

`Cqrs.SourceGenerators` 的专题入口见[CQRS Handler Registry 生成器](../source-generators/cqrs-handler-registry-generator.md)。

## 源码阅读入口

| 类型族 | 代表类型 | 建议先确认什么 |
| --- | --- | --- |
| `GFramework.Cqrs.Abstractions/Cqrs/` | `ICqrsRuntime`、`ICqrsHandlerRegistrar`、`IPipelineBehavior<,>`、`IRequestHandler<,>`、`Unit` | 请求、处理器和 runtime seam 的最小契约 |
| `GFramework.Cqrs/Request/` `Notification/` | `RequestBase<TInput, TResponse>`、`NotificationBase<TInput>` | 业务侧常用消息基类 |
| `GFramework.Cqrs/Cqrs/` | `AbstractRequestHandler<,>`、`AbstractStreamRequestHandler<,>`、`AbstractNotificationHandler<>`、`LoggingBehavior<,>` | 默认处理器基类、上下文注入与行为管道 |
| `GFramework.Cqrs/Extensions/` | `CqrsServiceCollectionExtensions`、`ArchitectureCqrsExtensions`、`ArchitectureContextCqrsExtensions`、`ContextAwareCqrsExtensions` | DI 注册入口、架构集成与上下文分发入口 |
| 运行时入口与内部协作层 | `CqrsRuntimeModule`、`CqrsRuntimeFactory`、`ICqrsHandlerRegistry`、`CqrsHandlerRegistryAttribute`、`CqrsReflectionFallbackAttribute`、`ICqrsRequestInvokerProvider`、`ICqrsStreamInvokerProvider` | runtime 安装与创建入口、generated-registry 优先级、request / stream invoker provider 协作点、targeted fallback 语义和程序集去重规则 |
| `GFramework.Cqrs.SourceGenerators/Cqrs/` | `CqrsHandlerRegistryGenerator`、`RuntimeTypeReferenceSpec`、`OrderedRegistrationKind` | 生成注册器、可直接引用类型判定、mixed fallback 发射与诊断边界 |

## 继续阅读

- 架构入口：[架构与上下文](./architecture.md)
- 上下文入口：[Context 上下文](./context.md)
- 生成器专题：[CQRS Handler Registry 生成器](../source-generators/cqrs-handler-registry-generator.md)
- 选包与入口：[入门指南](../getting-started/index.md)
