---
title: CQRS 运行时
---

# CQRS 运行时

CQRS runtime 提供异步 request、notification 和 stream 分发，独立于 Core Architecture。它适合需要统一处理管线、处理器注册与分发的部分功能；普通方法调用仍可直接使用。

## 接入

在组合根调用 services.AddCqrsRuntime()，通过 AddCqrsHandlersFromAssembly 等入口登记处理器。普通 DI 消费者通过构造函数接收 ICqrsRuntime。Architecture 使用同一 IServiceCollection，不需要额外上下文。

IRequestHandler&lt;TRequest, TResponse&gt; 的 Handle 方法接收请求和取消令牌。处理器通过构造函数接收领域对象与基础设施能力。调用 runtime.SendAsync、PublishAsync 或 CreateStream，并传递取消令牌。

## 边界

runtime 使用所属标准 provider 解析处理器与管线。Scoped 处理器需在显式 scope 中消费，不能让根单例捕获。分发通知与本地 EventBus 是不同能力：前者可等待异步处理链，后者同步调用当前实例的订阅者。

请求分发不要求数据库读写分离，也不要求将每帧移动包装成请求。独立包及其 XML 契约给出完整的处理器、管线和流生命周期说明。

相关主题：[应用服务](./system.md)、[读请求](./query.md)、[写请求](./command.md)、[生成处理器注册表](../source-generators/cqrs-handler-registry-generator.md)。
