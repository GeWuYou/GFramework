---
title: "生成 CQRS 处理器注册表"
description: "在消费程序集生成可直接注册的 CQRS 处理器表。"
---

# 生成 CQRS 处理器注册表 {#overview}

安装 `GeWuYou.GFramework.Cqrs.SourceGenerators`。在定义处理器的项目中实现 `IRequestHandler<,>`、`INotificationHandler<>` 或 `IStreamRequestHandler<,>`。

```csharp
services.AddCqrsHandlers(
    GFramework.Generated.Cqrs.CqrsRegistration.CreateRegistry());
```

静态可表达的处理器进入生成注册表。使用运行时发现的程序集时，显式调用 `AddCqrsHandlersFromAssembly`。检查构建诊断，确认处理器声明与注册范围匹配。运行[CQRS 管道示例](../core/cqrs.md#pipeline)验证实际处理与作用域。
