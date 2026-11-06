# GFramework.Cqrs

`GFramework.Cqrs` 是 GFramework 的默认 CQRS runtime 包。它在 `GFramework.Cqrs.Abstractions` 之上提供请求分发、通知发布、流式请求处理、处理器注册、上下文扩展方法，以及请求/通知基类，面向直接使用 GFramework CQRS 的业务模块。

runtime 运行在标准 `Microsoft.Extensions.DependencyInjection` 容器上，不要求使用 `GFramework.Core` 的 Architecture。

## 模块定位

- 这是 CQRS 的“默认实现层”。
- 包内同时承载运行时基础设施和面向业务代码的便捷基类。
- 它依赖 `GFramework.Cqrs.Abstractions` 与 `GFramework.Core.Abstractions`。
- `GFramework.Core` 不再传递依赖本包；需要消息模型的项目显式安装，并通过 `CqrsRuntimeModule` 或组合根扩展安装 runtime。

如果你只需要声明跨模块共享的消息契约，而不想依赖默认 runtime，请改为引用 `GeWuYou.GFramework.Cqrs.Abstractions`。

## 包关系

推荐的依赖关系如下：

- `GeWuYou.GFramework.Cqrs.Abstractions`
  - 最小 CQRS 契约层。
- `GeWuYou.GFramework.Cqrs`
  - 默认 runtime 与业务侧常用基类。
- `GeWuYou.GFramework.Cqrs.SourceGenerators`
  - 可选。为消费端程序集生成 `ICqrsHandlerRegistry`，并在可用时补充 generated request / stream invoker provider 元数据；运行时会优先消费这些编译期元数据，只有缺失、不适用，或 fallback 仍需补齐剩余 handler 时，才继续进入反射路径。

## 子系统地图

本包当前主要由以下子系统组成：

- 消息基类
  - `Request/RequestBase.cs`
  - `Notification/NotificationBase.cs`
- 处理器基类
  - `Cqrs/CqrsContextAwareHandlerBase.cs`
  - `Cqrs/Request/AbstractRequestHandler.cs`
  - `Cqrs/Request/AbstractStreamRequestHandler.cs`
  - `Cqrs/Notification/AbstractNotificationHandler.cs`
- 请求管道
  - `Cqrs/Behaviors/LoggingBehavior.cs`
  - `Cqrs/Behaviors/PerformanceBehavior.cs`
  - 管道契约定义在 `GFramework.Cqrs.Abstractions` 的 `IPipelineBehavior<,>`
- 默认 runtime 与注册入口
  - `CqrsRuntimeModule.cs`
  - `CqrsServiceCollectionExtensions.cs`
  - `CqrsRuntimeFactory.cs`
  - `Internal/CqrsDispatcher.cs`
  - `Notification/INotificationPublisher.cs`
  - `Notification/SequentialNotificationPublisher.cs`
  - `Notification/TaskWhenAllNotificationPublisher.cs`
  - `Internal/CqrsHandlerRegistrar.cs`
  - `Internal/DefaultCqrsHandlerRegistrar.cs`
  - `Internal/DefaultCqrsRegistrationService.cs`
- 生成注册表协作接口
  - `ICqrsHandlerRegistry.cs`
  - `CqrsHandlerRegistryAttribute.cs`
  - `CqrsReflectionFallbackAttribute.cs`
- 架构集成与业务侧扩展入口
  - `Extensions/ArchitectureCqrsExtensions.cs`
  - `Extensions/ArchitectureContextCqrsExtensions.cs`
  - `Extensions/ContextAwareCqrsExtensions.cs`
  - `Extensions/NotificationPublisherRegistrationExtensions.cs`
  - `Integration/ArchitectureCqrsDispatchContext.cs`

## 最小接入路径

标准 DI 上的最小接入是“安装包 + 注册 runtime + 定义消息 + 定义处理器 + 分发”：

```bash
dotnet add package GeWuYou.GFramework.Cqrs
dotnet add package GeWuYou.GFramework.Cqrs.Abstractions
```

如果你希望把可静态表达的 handler 注册与 request / stream invoker 元数据前移到编译期，再额外安装：

```bash
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators
```

示例：

```csharp
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Cqrs.Request;

public sealed record CreatePlayerRequest(string Name) : IRequest<int>;

public sealed class CreatePlayerHandler
    : AbstractRequestHandler<CreatePlayerRequest, int>
{
    public override ValueTask<int> Handle(
        CreatePlayerRequest request,
        CancellationToken cancellationToken)
    {
        // 通过构造函数注入获取依赖；上下文感知处理器才使用 Context
        var playerModel = Context.GetModel<PlayerModel>();
        var playerId = playerModel.Create(request.Name);
        return ValueTask.FromResult(playerId);
    }
}

var services = new ServiceCollection();
services.AddCqrsRuntime();
services.AddCqrsHandlersFromAssembly(typeof(CreatePlayerHandler).Assembly);

await using var provider = services.BuildServiceProvider();
var runtime = provider.GetRequiredService<ICqrsRuntime>();
var playerId = await runtime.SendAsync(new CreatePlayerRequest("Alice"));
```

## 在 Architecture 中集成

走标准 `GFramework.Core` 架构启动路径时，在启动前安装 runtime 模块：

```csharp
using GFramework.Cqrs.Extensions;

var architecture = new GameArchitecture();
architecture.UseCqrsRuntime(); // 自动包含架构所在程序集的 handler
await architecture.InitializeAsync();
```

或显式登记服务模块：

```csharp
architecture.RegisterServiceModule(new CqrsRuntimeModule(typeof(InventoryModule).Assembly));
```

pipeline 行为、通知发布策略与额外 handler 程序集通过 `ConfigureServices(...)` 里的 `IServiceCollection` 扩展登记（`AddCqrsPipelineBehavior`、`UseNotificationPublisher*`、`AddCqrsHandlersFromAssembly`）。`CqrsRuntimeModule` 不再内置到 Core 的默认模块列表，未安装它的架构没有 CQRS 分发能力。

在 `IContextAware` 对象内发送请求：

```csharp
using GFramework.Cqrs.Extensions;

var playerId = await this.SendAsync<int>(new CreatePlayerRequest("Alice"));
```

在 `ArchitectureContext` 上也可以直接使用统一 CQRS 入口，例如 `SendRequestAsync`、`SendAsync`、`PublishAsync` 和 `CreateStream`。

## 运行时行为

- 请求分发
  - `CqrsDispatcher` 按请求实际类型解析 `IRequestHandler<,>`，若当前程序集提供 generated request invoker provider，则会先复用对应 descriptor 中的处理器服务类型与 invoker 元数据；未命中时仍回退到既有反射 request binding 创建路径。
  - 未找到处理器会抛出异常。
  - 分发入口只有异步版本（`SendAsync` / `PublishAsync` / `CreateStream`）。
- 通知分发
  - 通知会分发给所有已注册 `INotificationHandler<>`；零处理器时默认静默完成。
  - 若容器在 runtime 创建前已显式注册 `INotificationPublisher`，默认 runtime 会复用该策略；未注册时回退到内置 `SequentialNotificationPublisher`。
  - notification publish 不存在 generated invoker 通道；它始终基于当前已注册的 `INotificationHandler<>` 集合和选定的 `INotificationPublisher` 策略执行。
  - 默认 runtime 只消费一个 `INotificationPublisher`；如果容器里已经存在该注册，再调用 `UseNotificationPublisher(...)`、`UseNotificationPublisher<TPublisher>()`、`UseSequentialNotificationPublisher()` 或 `UseTaskWhenAllNotificationPublisher()` 会直接报错，而不是按“后注册覆盖前注册”处理。
  - 内置 notification publisher 的推荐选择如下：

  | 策略 | 推荐场景 | 执行顺序 | 失败语义 | 备注 |
  | --- | --- | --- | --- | --- |
  | `SequentialNotificationPublisher` | 需要保持容器顺序，且希望首个失败立即停止后续分发 | 保证按容器解析顺序逐个执行 | 首个处理器抛出异常时立即停止 | 也是默认回退策略 |
  | `TaskWhenAllNotificationPublisher` | 需要让全部处理器并行完成，并在结束后统一观察失败或取消 | 不保证顺序 | 不会在首个失败时停止其余处理器；会聚合最终异常或取消结果 | 更适合语义补齐，不是性能开关 |
  | `UseNotificationPublisher(...)` / `UseNotificationPublisher<TPublisher>()` | 需要接入仓库外的自定义策略或第三方策略 | 取决于具体实现 | 取决于具体实现 | 前者复用现成实例，后者让容器负责单例生命周期 |

  - 若只是为了降低 fixed fan-out publish 的 steady-state 成本，当前 benchmark 并不表明 `TaskWhenAllNotificationPublisher` 会优于默认顺序发布器；它更适合你需要“等待全部处理器完成并统一观察失败”的场景。

这些 publisher 策略都作用于 `IServiceCollection`：

```csharp
using GFramework.Cqrs.Extensions;

services.UseSequentialNotificationPublisher();
// 或
services.UseTaskWhenAllNotificationPublisher();
// 或
services.UseNotificationPublisher(new TaskWhenAllNotificationPublisher());
services.UseNotificationPublisher<MyCustomNotificationPublisher>();
```

如果直接调用 `CqrsRuntimeFactory.CreateRuntime(...)`，也可以显式传入 publisher 实例。

- 流式请求
  - 通过 `IStreamRequest<TResponse>` 和 `IStreamRequestHandler<,>` 返回 `IAsyncEnumerable<TResponse>`。
  - 当消费端程序集提供 generated stream invoker provider / descriptor 后，runtime 会优先消费这组 stream invoker 元数据；未命中时仍回退到既有反射 stream binding 创建路径。
  - 所有已注册 `IStreamPipelineBehavior<TRequest, TResponse>` 会在建流阶段包裹对应 stream handler；默认实现不拦截每个元素，而是围绕单次 `CreateStream(...)` 调用编排行为链。
- 上下文注入
  - 处理器基类继承 `CqrsContextAwareHandlerBase`，runtime 会在分发前注入当前 `IArchitectureContext`。
  - 如果处理器或行为需要上下文注入，而当前 `ICqrsContext` 不是 `IArchitectureContext`，默认实现会抛出异常。
  - 独立采用（无 Architecture）时，处理器通过构造函数注入获取依赖，不要依赖上下文注入。
- 管道行为
  - 所有已注册 `IPipelineBehavior<TRequest, TResponse>` 会包裹请求处理器执行。
  - 所有已注册 `IStreamPipelineBehavior<TRequest, TResponse>` 会包裹流式请求处理器执行。
  - 注册入口分别为 `services.AddCqrsPipelineBehavior<TBehavior>()` 与 `services.AddCqrsStreamPipelineBehavior<TBehavior>()`。
  - 当前包内提供了 `LoggingBehavior` 和 `PerformanceBehavior` 两个可复用 request 行为；stream 行为需要按业务需求自行实现。

## 处理器注册与程序集接入

默认注册流程由 `ICqrsRegistrationService.RegisterHandlers(IEnumerable<Assembly>)` 协调，语义是：

- 同一程序集按稳定键去重，避免重复注册。
- 优先尝试消费端程序集上的 `ICqrsHandlerRegistry` 生成注册器。
- 当生成注册器同时暴露 generated request invoker provider 或 generated stream invoker provider 时，registrar 会把对应 descriptor 元数据接线到 runtime 缓存。
- 生成注册器不可用或元数据损坏时，记录告警并回退到反射扫描。
- generated invoker 只覆盖 request 与 stream 两类单次分发元数据；`INotificationHandler<>` 仍然只参与 registry / fallback 注册，通知分发本身继续由 runtime 解析出的 handler 集合和 `INotificationPublisher` 策略决定。
- 当程序集声明了 `CqrsReflectionFallbackAttribute` 时，运行时会先执行生成注册器，再只补它未覆盖的 handler。
- `CqrsReflectionFallbackAttribute` 可以多次声明，并同时承载 `Type[]` 与 `string[]` 两类 fallback 清单。
- 运行时会优先复用 fallback 特性里直接提供的 `Type` 条目，只对字符串条目执行定向 `Assembly.GetType(...)` 查找；只有旧版空 marker、空 fallback 元数据，或生成注册器整体不可用时，才会退回整程序集扫描。
- 处理器以 transient 方式注册，避免上下文感知处理器在并发请求间共享可变上下文。

程序集通过 `services.AddCqrsHandlersFromAssembly(...)` / `AddCqrsHandlersFromAssemblies(...)` 登记；Architecture 集成路径中把程序集交给 `UseCqrsRuntime(...)` 或 `CqrsRuntimeModule(...)` 即可。

## 适用边界

- 这个包是默认实现，不是“纯契约包”；如果你只需要共享请求/处理器契约，请停在 `GeWuYou.GFramework.Cqrs.Abstractions`。
- 处理器基类依赖 runtime 在分发前注入上下文，不适合脱离 dispatcher 直接手动实例化后调用。
- README 中的消息基类和 handler 基类位于 `GFramework.Cqrs`，接口契约位于 `GFramework.Cqrs.Abstractions`；最小示例通常需要同时引入这两个命名空间层级。
- 不使用 Architecture 时，处理器通过构造函数注入获取依赖；只有把架构上下文绑定为分发上下文（上下文扩展或 `ArchitectureCqrsDispatchContext`）时，上下文感知处理器才能工作。

## 文档入口

- 总体文档：[CQRS 运行时](../docs/zh-CN/core/cqrs.md)
- 契约层说明：[CQRS 抽象层说明](../GFramework.Cqrs.Abstractions/README.md)
