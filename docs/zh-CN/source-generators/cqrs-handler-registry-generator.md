---
title: "生成 CQRS 处理器注册表"
description: "在处理器项目安装运行时和生成器，并从宿主完成处理器注册。"
---

# 生成 CQRS 处理器注册表 {#overview}

## 安装包 {#install}

在声明处理器的项目目录执行这两条命令：

```bash
dotnet add package GeWuYou.GFramework.Cqrs
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators
```

`GFramework.Cqrs` 提供生成器需要的注册表接口和特性，同时带入定义 `IRequestHandler<,>`、`INotificationHandler<>` 和 `IStreamRequestHandler<,>` 的 `GFramework.Cqrs.Abstractions`。仅声明消息的共享项目可以直接引用契约包。

## 注册处理器 {#register}

宿主与处理器在同一个项目时，在组合根调用生成的工厂：

```csharp
services.AddCqrsHandlers(
    GFramework.Generated.Cqrs.CqrsRegistration.CreateRegistry());
```

## 注册独立处理器库 {#separate-library}

让宿主引用处理器库。生成的工厂是处理器程序集的内部类型，在该库中开放一个公开方法注册处理器：

```csharp
using GFramework.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace MyGame.Handlers;

public static class HandlerRegistration
{
    public static IServiceCollection AddHandlers(IServiceCollection services)
        => services.AddCqrsHandlers(
            GFramework.Generated.Cqrs.CqrsRegistration.CreateRegistry());
}
```

在宿主引入 `MyGame.Handlers`，并在构建服务提供程序前调用 `services.AddCqrsRuntime()` 和 `HandlerRegistration.AddHandlers(services)`。宿主通过处理器库的包引用取得 `GFramework.Cqrs` 运行时。

动态加载程序集或生成的注册表需要反射回退时，使用 `AddCqrsHandlersFromAssembly`。运行 [CQRS 管道示例](../core/cqrs.md#pipeline) 验证消息分发与作用域。
