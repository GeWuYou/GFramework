# GFramework.Cqrs

`GFramework.Cqrs` 提供基于标准 Microsoft DI 的请求、通知和异步流分发。处理器与管道行为通过构造函数声明依赖，可脱离 Architecture 独立运行。

## 包关系与能力

- `GeWuYou.GFramework.Cqrs.Abstractions`：消息、处理器、runtime 和管道契约。
- `GeWuYou.GFramework.Cqrs`：dispatcher、注册扩展、通知发布策略及可选消息/处理器基类。
- `GeWuYou.GFramework.Cqrs.SourceGenerators`：可选的消费端注册表及 request/stream 调用元数据生成。
- Core 不传递引用 CQRS；需要消息分发时显式安装本包。

主要目录包括 `Cqrs/Request` 与 `Cqrs/Notification` 的处理器基类、`Cqrs/Behaviors` 的日志和耗时行为、`Notification` 的发布策略，以及 `Internal` 的注册与分发实现。处理器可直接实现接口，基类不提供容器访问或可变的分发上下文。

## 最小接入

```bash
dotnet add package GeWuYou.GFramework.Cqrs
```

下面的组合根使用标准注册并发送请求：

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCqrsRuntime();
services.AddCqrsHandlersFromAssembly(typeof(GetLengthHandler).Assembly);

await using var provider = services.BuildServiceProvider(
    new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
var runtime = provider.GetRequiredService<ICqrsRuntime>();
Console.WriteLine(await runtime.SendAsync(new GetLength("Alice")));

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

业务处理器需要仓库、规则服务或日志时，将对应类型加入构造函数并在组合根注册。手动调用处理器时遵守同一依赖和取消契约。

## 可选 Architecture 模块

配置型模块只组织同一份 `IServiceCollection`，不引入另一套生命周期：

```csharp
using GFramework.Core.Architectures;
using GFramework.Cqrs;
using Microsoft.Extensions.DependencyInjection;

public sealed class ApplicationArchitecture : Architecture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddModule(new CqrsRuntimeModule(typeof(GetLengthHandler).Assembly));
    }
}
```

宿主启动完成后可在组合根通过 `architecture.Services.GetRequiredService<ICqrsRuntime>()` 取得 runtime；业务对象直接注入 `ICqrsRuntime`。模块显式接收处理器程序集，不自动扫描架构所在程序集。

## 管道与通知

- `AddCqrsPipelineBehavior<TBehavior>()` 注册 request 管道；Type 重载支持开放泛型。默认 Transient，可显式传入 Scoped 或 Singleton；`AddCqrsStreamPipelineBehavior<TBehavior>()` 注册 stream 管道，具有相同的 Type 重载和生命周期选择。
- 注册的管道按登记顺序包裹处理器。内置 `LoggingBehavior`、`PerformanceBehavior` 服务 request；stream 行为由应用实现。
- 通知分发解析全部 `INotificationHandler<T>`。没有处理器时成功完成。
- 默认 `SequentialNotificationPublisher` 按解析顺序逐个调用，首个异常停止后续调用。
- `TaskWhenAllNotificationPublisher` 启动全部处理器并等待完成，不保证完成顺序。失败和取消通过异步结果传播；它使用 `Task.WhenAll`，不能将一次 `await` 抛出的异常视为所有失败的完整清单。
- 自定义策略实现 `INotificationPublisher`；处理器之间需要并发安全，容器线程安全不代表共享依赖可以并发修改。

在构建 provider 前选择一个通知策略：

```csharp
using GFramework.Cqrs.Extensions;

services.UseTaskWhenAllNotificationPublisher();
```

也可选择 `UseSequentialNotificationPublisher()`、`UseNotificationPublisher(instance)` 或 `UseNotificationPublisher<TPublisher>()`。这些入口互斥，重复策略注册明确失败。实例重载保留调用者的资源所有权。

## 请求作用域与流

`AddCqrsRuntime()` 注册根单例 runtime。Scoped 处理器或依赖应使用 `AddScopedCqrsRuntime()`，并在业务操作作用域中解析 runtime：

```csharp
var services = new ServiceCollection();
services.AddScopedCqrsRuntime();
services.AddScoped<IRequestHandler<GetLength, int>, GetLengthHandler>();

await using var provider = services.BuildServiceProvider(
    new ServiceProviderOptions { ValidateScopes = true });
await using var scope = provider.CreateAsyncScope();
var runtime = scope.ServiceProvider.GetRequiredService<ICqrsRuntime>();
var length = await runtime.SendAsync(new GetLength("Alice"));
```

同一作用域复用 runtime，不同作用域隔离 scoped 依赖。两种 runtime 注册模式不能混用；注册 runtime 不会自动改变处理器生命周期。需要自行组合 provider 时可使用 `CqrsRuntimeFactory.CreateRuntime(provider)`。

`CreateStream` 接收 `IStreamRequest<T>` 并返回 `IAsyncEnumerable<T>`，通过 `IStreamRequestHandler<,>` 处理。runtime 在建流时解析处理器，stream 管道包裹单次建流；调用方必须让作用域覆盖完整枚举和清理过程，并将取消令牌传入分发与枚举。

## 生成注册与反射回退

安装消费端生成器后，优先调用 `services.AddCqrsHandlers(GFramework.Generated.Cqrs.CqrsRegistration.CreateRegistry())`。注册器直接构造，调用描述符通过 `Create<TResponse>` 接收静态强类型委托；相同注册器类型重复登记会去重。需要 fallback 的生成结果会在创建入口明确拒绝，要求选择显式程序集路径。

`AddCqrsHandlersFromAssembly` / `AddCqrsHandlersFromAssemblies` 优先使用消费程序集提供的 `ICqrsHandlerRegistry`，没有可用注册表时执行反射回退；处理器默认注册为 transient。同一程序集按稳定键去重。

request/stream 的生成调用元数据可用于缓存分发绑定；通知继续使用实际解析的处理器集合和通知发布策略。`CqrsReflectionFallbackAttribute` 的类型与名称清单用于补齐未覆盖处理器，不改变其依赖或生命周期。生成器只安装到消费项目。

## 相关文档

- [CQRS 采用指南](../docs/zh-CN/core/cqrs.md)
- [CQRS 契约包](../GFramework.Cqrs.Abstractions/README.md)
- [生成处理器注册表](../docs/zh-CN/source-generators/cqrs-handler-registry-generator.md)

完整开放泛型与 Scoped 依赖示例见 [CQRS 管道工程](../Samples/CqrsPipelineDemo/README.md)。
