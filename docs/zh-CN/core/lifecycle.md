---
title: "生命周期组件"
description: "为服务声明初始化与业务停止。"
---

# 生命周期组件 {#overview}

通过 `AddComponent` 登记生命周期组件，并在服务中实现所需能力。

| 能力 | 接口 |
| --- | --- |
| 同步初始化 | `IInitializable` |
| 异步初始化 | `IAsyncInitializable` |
| 同步停止 | `IDestroyable` |
| 异步停止 | `IAsyncDestroyable` |

把资源释放放在 `IDisposable` 或 `IAsyncDisposable`，由实例的拥有者执行。启动时等待组件就绪，停止时先结束业务工作。顺序和所有权见[应用装配](../core/architecture.md)。
