---
title: CQRS
description: Cqrs 模块族的运行时、契约层、生成器入口，以及源码与 API 阅读链路。
---

# CQRS

`Cqrs` 栏目对应三个直接相关的消费模块：

- `GFramework.Cqrs`
- `GFramework.Cqrs.Abstractions`
- `GFramework.Cqrs.SourceGenerators`

如果你在写新功能，优先使用这套请求模型，而不是继续扩展 `GFramework.Core.Command` / `Query` 的兼容层。

如果你在查找 `ICqrsRuntime`，请把 `GFramework.Core.Abstractions.Cqrs.ICqrsRuntime` 理解为旧命名空间下保留的
legacy compatibility alias。新代码应直接依赖 `GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime`。

## 模块族边界

| 模块 | 角色 | 何时安装 |
| --- | --- | --- |
| `GeWuYou.GFramework.Cqrs.Abstractions` | 纯契约层，定义 request、notification、stream、handler、pipeline、runtime seam | 需要把消息契约放到更稳定的共享层，或只依赖接口做解耦 |
| `GeWuYou.GFramework.Cqrs` | 默认 runtime，提供 dispatcher、notification publisher seam、handler 基类、上下文扩展和程序集注册流程 | 大多数直接消费 CQRS 的业务模块 |
| `GeWuYou.GFramework.Cqrs.SourceGenerators` | 编译期生成 `ICqrsHandlerRegistry`，让运行时先走生成注册器，再只对剩余 handler 做定向 fallback | handler 较多，想把注册映射前移到编译期 |

## 最小接入路径

Core 当前已经传递包含 CQRS runtime 与契约，标准架构会自动注册运行时。直接使用 CQRS API 的项目可显式声明以下依赖；仅声明消息契约可只引用 Abstractions。默认运行时工厂仍接受 Core 的 IIocContainer，不应把拆包理解为脱离 Core 契约的独立标准 DI 接入。

显式引用组合：

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

如果你在协程驱动的调用链里工作，`GFramework.Core` 还提供了 `CqrsCoroutineExtensions.SendCommandCoroutine(...)`
这类桥接入口，用来把 CQRS 调度接回协程系统。

## 统一请求模型

这套 runtime 不只处理 command，也统一处理：

- Query
  - 读路径请求
- Notification
  - 一对多广播
- Stream Request
  - 返回 `IAsyncEnumerable<T>`

新代码通常不需要再分别设计“命令总线”“查询总线”和另一套通知分发语义。

当前通知分发默认仍保持顺序语义：

- 零处理器时静默完成
- 已解析处理器按容器顺序逐个执行
- 首个处理器抛出异常时立即停止后续分发
- 如果容器在 runtime 创建前已显式注册 `INotificationPublisher`，默认 runtime 会复用该策略；未注册时回退到内置 `SequentialNotificationPublisher`
- 默认 runtime 只消费一个 `INotificationPublisher`；如果容器里已经存在该注册，再调用 `UseNotificationPublisher*` 系列扩展会直接报错，而不是按“后注册覆盖前注册”处理

如果你需要在组合根里明确表达“为什么选这条策略”，可以按下面的矩阵判断：

| 策略 | 适用场景 | 顺序语义 | 失败语义 | 备注 |
| --- | --- | --- | --- | --- |
| `UseSequentialNotificationPublisher()` | 需要保持容器顺序，且希望首个失败立即停止 | 保证按容器顺序执行 | 首个处理器异常会中断后续处理器 | 这也是默认回退策略 |
| `UseTaskWhenAllNotificationPublisher()` | 需要让全部处理器并行完成，再统一观察异常或取消 | 不保证顺序 | 不会在首个失败时中断其余处理器；全部结束后统一暴露结果 | 更适合语义补齐，不是性能优化开关 |
| `UseNotificationPublisher(...)` / `UseNotificationPublisher<TPublisher>()` | 需要接入自定义或第三方 publisher 策略 | 取决于实现 | 取决于实现 | 前者复用现成实例，后者让容器负责单例生命周期；两者都要求容器此前尚未注册 `INotificationPublisher` |

如果你想在组合根里显式保留默认顺序语义，也可以直接写成：

```csharp
using GFramework.Cqrs.Extensions;

container.UseSequentialNotificationPublisher();
```

如果你需要等待所有通知处理器并行完成，而不是沿用默认顺序语义，可以显式切换到内置
`TaskWhenAllNotificationPublisher`：

```csharp
using GFramework.Cqrs.Extensions;

container.UseTaskWhenAllNotificationPublisher();
```

这条策略的边界也需要明确：

- 不保证处理器执行顺序
- 不会在首个处理器失败时立即停止其余处理器
- 会在全部处理器结束后统一暴露异常或取消结果
- 当前 fixed `4 handler` fan-out benchmark 中，它的 steady-state 成本也高于默认顺序发布器；因此它更适合“我要并行语义”，而不是“我要更快的 publish”

如果你需要显式提供自定义 publisher 实例，而不是直接采用内置 `TaskWhenAll` 策略，也可以在组合根里写成：

```csharp
using GFramework.Cqrs.Extensions;
using GFramework.Cqrs.Notification;

container.UseNotificationPublisher(new TaskWhenAllNotificationPublisher());
```

如果你的自定义 publisher 需要继续由容器构造和托管，也可以改用泛型注册入口：

```csharp
using GFramework.Cqrs.Extensions;

container.UseNotificationPublisher<MyCustomNotificationPublisher>();
```

## Request 与流式变体

除了最常见的 `Command` / `Query` / `Notification`，当前公开面还覆盖两类容易被忽略的入口：

### 普通 Request

如果你的请求不想再被读者预设成“命令”或“查询”，可以直接使用：

- `RequestBase<TInput, TResponse>`
- `AbstractRequestHandler<TRequest, TResponse>`

它们仍然走统一的 `SendRequestAsync(...)` 调度入口，只是把语义保持在更中性的 `Request` 层。

### 流式 Command / Query

如果你需要返回 `IAsyncEnumerable<T>`，除了通用的 `IStreamRequest<TResponse>`，当前也提供更明确的专用契约：

- `IStreamCommand<TResponse>`
- `IStreamQuery<TResponse>`
- `AbstractStreamCommandHandler<TCommand, TResponse>`
- `AbstractStreamQueryHandler<TQuery, TResponse>`

这几类处理器最终仍然通过 `CreateStream(...)` 进入统一的 CQRS runtime，而不是另一套独立流式总线。

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
3. 当生成注册器同时暴露 generated request invoker provider 时，runtime 会把 request/response 类型对对应的 descriptor 预先接线到 dispatcher 缓存，后续请求分发优先消费这些 generated request invoker 元数据
4. 当生成注册器同时暴露 generated stream invoker provider 时，runtime 会以同样方式优先消费 stream request 对应的 generated stream invoker descriptor；只有当前类型对未命中时，才回退到既有反射 stream binding
5. generated invoker 只覆盖 request 与 stream 两类单次分发元数据；notification handler 仍通过已注册的 `INotificationHandler<>` 集合和选定的 `INotificationPublisher` 参与分发，不存在对应的 generated notification invoker 通道
6. 生成注册器不可用时记录告警并回退到反射路径；只有“未命中 generated descriptor”才会走反射 binding 创建，已成功登记到缓存的类型对不会再回退到另一条 generated 通道
7. 当生成注册器携带 `CqrsReflectionFallbackAttribute` 元数据时，运行时会先完成生成注册器注册，再补剩余 handler
8. `CqrsReflectionFallbackAttribute` 可以同时携带 `Type[]` 和 `string[]` 两类清单；运行时会优先复用直接 `Type` 条目，只对名称条目做定向 `Assembly.GetType(...)` 查找
9. 只有 fallback 元数据为空、仍是旧版空 marker 语义，或生成注册器整体不可用时，才会回到整程序集反射扫描
10. 同一程序集按稳定键去重，避免重复注册

换句话说，声明 fallback 特性本身不等于“整包反射扫描”。当前推荐理解是：生成注册器负责能静态表达的部分，fallback 只补它覆盖不到的 handler。

如果你在阅读 dispatcher 行为，可以把这部分理解成两组并列能力：

- request invoker provider / descriptor
  - 面向 `SendRequestAsync(...)`、`SendAsync(...)`、`SendQueryAsync(...)` 这类单次请求分发
- stream invoker provider / descriptor
  - 面向 `CreateStream(...)` 触发的流式请求分发

两者的共同点都是“优先消费 generated invoker 元数据，未命中时保留既有反射绑定作为兜底”，而不是要求业务侧切换到另一套 runtime 入口。通知发布不在这组 generated invoker 能力里；它始终沿用 runtime 解析出的 handler 集合与当前 publisher 策略。

对接入方来说，更关键的 reader-facing 语义是：安装 `Cqrs.SourceGenerators` 后，不要求“所有 handler 都能被生成代码直接引用”才有收益。
即使仍有 fallback，runtime 也会先消费 generated registry，再只对剩余 handler 做定向补扫；只有旧版 marker 语义或空 fallback 元数据才会退回整程序集扫描。
`Type` fallback、按名称恢复的 fallback，以及 mixed fallback 只影响补扫精度，不改变
`RegisterCqrsHandlersFromAssembly(...)` 或 `RegisterCqrsHandlersFromAssemblies(...)` 的接法。

`Cqrs.SourceGenerators` 的专题入口见[CQRS Handler Registry 生成器](../source-generators/cqrs-handler-registry-generator.md)。

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

如果你需要围绕 `CreateStream(...)` 的建流过程插入横切逻辑，实现 `IStreamPipelineBehavior<TRequest, TResponse>`，并在初始化阶段注册：

```csharp
RegisterCqrsStreamPipelineBehavior<MyStreamBehavior<,>>();
```

这里有两个需要明确的边界：

- `RegisterCqrsPipelineBehavior<TBehavior>()`
  - 只作用于 `SendRequestAsync(...)` / `SendAsync(...)` / `SendQueryAsync(...)` 这类单次请求分发
- `RegisterCqrsStreamPipelineBehavior<TBehavior>()`
  - 作用于 `CreateStream(...)` 的建流阶段
  - 默认实现围绕单次建流调用编排行为链，不会自动把行为扩展成“每个流元素都单独拦截一次”

## 和旧 Command / Query 的关系

当前仓库同时存在两套路径：

- 旧路径
  - `GFramework.Core.Command`
  - `GFramework.Core.Query`
- 新路径
  - `GFramework.Cqrs`

`IArchitectureContext` 仍然兼容旧入口，但新代码应优先使用 CQRS runtime。

这里有两个边界需要分开理解：

- 旧 `Command` / `Query` 入口仍可用于维护历史调用链
- 标准 `Architecture` 启动路径下，旧入口现在会通过内部 bridge request 复用同一个 `ICqrsRuntime`
- 旧命名空间下的 `ICqrsRuntime` 只是为了兼容既有引用而保留的 alias；面向新代码时，应直接使用
  `GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime`

一个简单判断规则：

- 在维护历史代码：允许继续使用旧 Command / Query
- 在写新功能或新模块：优先使用 CQRS

相对 `ai-libs/Mediator`，当前 `GFramework.Cqrs` 仍有几类能力差距尚未完全吸收：

- `IMediator` / `ISender` / `IPublisher` 风格的一等 facade 公开入口
- telemetry / tracing / metrics 的运行时与生成器配置面
- 更丰富的 notification publisher 策略
- 更强的生成器配置与诊断公开面
- 生命周期 / 缓存策略的显式公开配置面

## 源码阅读入口

如果你需要直接回到源码确认 CQRS 契约，建议按下面这几组入口阅读：

| 类型族 | 代表类型 | 建议先确认什么 |
| --- | --- | --- |
| `GFramework.Cqrs.Abstractions/Cqrs/` | `ICqrsRuntime`、`ICqrsHandlerRegistrar`、`IPipelineBehavior<,>`、`IRequestHandler<,>`、`Unit` | 请求、处理器和 runtime seam 的最小契约 |
| `GFramework.Cqrs/Command` `Query` `Notification` `Request` `Extensions` | `CommandBase<TInput, TResponse>`、`QueryBase<TInput, TResponse>`、`NotificationBase<TInput>`、`RequestBase<TInput, TResponse>`、`ContextAwareCqrsExtensions` | 业务侧常用基类和上下文发送入口 |
| `GFramework.Cqrs/Cqrs/` | `AbstractCommandHandler<,>`、`AbstractQueryHandler<,>`、`AbstractRequestHandler<,>`、`AbstractStreamCommandHandler<,>`、`AbstractStreamQueryHandler<,>`、`LoggingBehavior<,>` | 默认处理器基类、上下文注入、流式处理与行为管道 |
| 运行时入口与内部协作层 | `CqrsRuntimeFactory`、`ICqrsHandlerRegistry`、`CqrsHandlerRegistryAttribute`、`CqrsReflectionFallbackAttribute`、`ICqrsRequestInvokerProvider`、`ICqrsStreamInvokerProvider` | runtime 创建入口、generated-registry 优先级、request / stream invoker provider 协作点、targeted fallback 语义和程序集去重规则 |
| `GFramework.Cqrs.SourceGenerators/Cqrs/` | `CqrsHandlerRegistryGenerator`、`RuntimeTypeReferenceSpec`、`OrderedRegistrationKind` | 生成注册器、可直接引用类型判定、mixed fallback 发射与诊断边界 |

## 继续阅读

- 架构入口：[架构与上下文](./architecture.md)
- 上下文入口：[Context 上下文](./context.md)
- 生成器专题：[CQRS Handler Registry 生成器](../source-generators/cqrs-handler-registry-generator.md)
- 协程接法：[协程系统](./coroutine.md)
- 选包与入口：[入门指南](../getting-started/index.md)
