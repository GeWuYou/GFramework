---
title: "Dependency injection"
description: "Use standard Microsoft DI with lifetimes that fit application services."
---

# Dependency injection {#overview}

Register services through `IServiceCollection` in the composition root and use constructor injection in application code. Choose `AddSingleton`, `AddScoped`, or `AddTransient` according to how long the service is used.

Create a scope through `IServiceScopeFactory` for scoped services, resolve them there, and dispose the scope when the operation completes. The container owns disposable instances it creates. Keep responsibility for disposal when you register an existing instance.

[MinimalApp](../getting-started/quick-start.md) shows startup with both a standard container and Architecture.
