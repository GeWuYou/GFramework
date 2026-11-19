---
title: "Core 接口"
description: "为生命周期、事件、资源和状态声明引擎无关契约。"
---

# Core 接口 {#overview}

`GeWuYou.GFramework.Core.Abstractions` 提供 `IInitializable`、`IEventBus`、`IResourceLoader<T>` 和只读状态接口等契约。需要在共享库中定义生命周期参与者或实现适配器时可直接引用。使用框架的默认实现时，参见[Core 指南](../core/index.md)。
