---
title: "依赖注入"
description: "使用标准 Microsoft DI 为应用服务选择合适的生命周期。"
---

# 依赖注入 {#overview}

在组合根用 `IServiceCollection` 注册服务，对应用逻辑使用构造注入。按依赖的使用范围选 `AddSingleton`、`AddScoped` 或 `AddTransient`。

需要 scoped 服务时，用 `IServiceScopeFactory` 建立作用域，从该作用域解析并在操作结束时释放。容器拥有其创建的可释放实例。登记已有实例时，由原调用者继续负责释放。

[MinimalApp](../getting-started/quick-start.md) 展示标准容器和 Architecture 的启动方式。
