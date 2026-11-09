---
title: CQRS 运行时
description: 使用标准 DI 组合请求、通知、流和显式生命周期管道。
---

# CQRS 运行时

CQRS runtime 提供异步 request、notification 和 stream 分发，独立于 Core Architecture。它适合需要统一处理管线、处理器注册与分发的部分功能；普通方法调用仍可直接使用。

## 接入

在组合根调用 services.AddCqrsRuntime()，使用消费端生成器时，通过 `services.AddCqrsHandlers(GFramework.Generated.Cqrs.CqrsRegistration.CreateRegistry())` 直接登记处理器。动态程序集或需要 fallback 的项目显式使用 `AddCqrsHandlersFromAssembly`。普通 DI 消费者通过构造函数接收 ICqrsRuntime。Architecture 使用同一 IServiceCollection，不需要额外上下文。

IRequestHandler&lt;TRequest, TResponse&gt; 的 Handle 方法接收请求和取消令牌。处理器通过构造函数接收领域对象与基础设施能力。调用 runtime.SendAsync、PublishAsync 或 CreateStream，并传递取消令牌。

## 管道与作用域

request 和 stream 管道均可通过泛型或 Type 重载登记，默认 Transient。指定 Scoped 时在同一调用方 scope 内复用；Singleton 只适合可共享且不依赖 scoped 服务的行为。注册顺序决定外层到内层顺序。

```csharp
services.AddCqrsPipelineBehavior(typeof(LoggingBehavior<,>));
services.AddCqrsPipelineBehavior(typeof(ScopeBehavior<,>), ServiceLifetime.Scoped);
```

开放泛型必须按请求、响应的顺序映射两个类型参数。抽象类型、缺少目标接口或反转类型参数的映射在登记阶段失败。容器生命周期不改变处理器及其依赖的线程安全约束。

下列完整组合根来自 CqrsPipelineDemo，展示生成注册表、开放泛型管道和 Scoped 依赖：

<<< ../../../Samples/CqrsPipelineDemo/Program.cs#scoped-pipeline

```bash
dotnet run --project Samples/CqrsPipelineDemo -c Release
```

流式行为通过 `AddCqrsStreamPipelineBehavior` 使用相同生命周期规则。作用域必须覆盖整个枚举与清理过程；调用方传递取消令牌，runtime 不隐式创建或提前释放 scope。

## 边界

runtime 使用所属标准 provider 解析处理器与管线。Scoped 处理器需在显式 scope 中消费，不能让根单例捕获。分发通知与本地 EventBus 是不同能力：前者可等待异步处理链，后者同步调用当前实例的订阅者。

请求分发不要求数据库读写分离，也不要求将每帧移动包装成请求。独立包及其 XML 契约给出完整的处理器、管线和流生命周期说明。

相关主题：[应用服务](./system.md)、[读请求](./query.md)、[写请求](./command.md)、[生成处理器注册表](../source-generators/cqrs-handler-registry-generator.md)。

直接注册与程序集注册在同一 `IServiceCollection` 中按注册器类型共同去重，混用两种入口或改变调用顺序不会重复执行生成注册。程序集入口仍会补齐该程序集声明的 fallback handlers；不同 service collection 分别维护注册状态。
