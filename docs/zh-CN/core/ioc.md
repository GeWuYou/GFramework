---
title: 依赖注入
---

# 依赖注入

使用 Microsoft.Extensions.DependencyInjection 的 IServiceCollection、IServiceProvider 和 IServiceScopeFactory。框架采用标准注册、构造注入及资源所有权语义。

```csharp
var services = new ServiceCollection();
services.AddSingleton<OrderService>();
await using var provider = services.BuildServiceProvider(
    new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
var orders = provider.GetRequiredService<OrderService>();
```

调用 provider 的地方集中在组合根。应用服务构造函数直接接收 OrderService 或真实需要的接口；接口用于隔离基础设施或可替换策略，不要求每个类都有对应接口。

容器创建的可释放对象由容器释放。AddSingleton(instance) 借用外部实例，调用者负责释放。Scoped 服务在显式 scope 内使用，不能由根单例捕获。普通 DI 注册不会自动初始化对象，需要编排时使用 [生命周期组件](./architecture.md)。

参考 [Microsoft DI 指南](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines)。
