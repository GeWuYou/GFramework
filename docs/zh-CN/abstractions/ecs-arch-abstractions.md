---
title: ECS 运行契约
description: 通过最小契约驱动原生 ECS 运行时，并共享不可变统计。
---

# ECS 运行契约

引用 `GeWuYou.GFramework.Ecs.Arch.Abstractions`，宿主即可面向运行时契约编程，而不依赖 Arch 原生类型。

| 类型 | 职责 |
| --- | --- |
| `IArchRuntime` | Initialize、Update、Stop 与 GetStatistics |
| `ArchOptions` | 唯一的 World 容量、统计开关与编排顺序配置 |
| `ArchStatisticsSnapshot` | 实体数、顶层系统数、帧计数和最近帧耗时 |
| `ArchSystemStatistics`／`ArchUpdatePhase` | 顶层系统的阶段耗时与失败信息 |
| `IArchSystemAdapter<T>` | 可选适配器的业务生命周期，不是原生系统接入要求 |

```csharp
using GFramework.Ecs.Arch.Abstractions;

public sealed class GameLoop(IArchRuntime runtime)
{
    public void Tick(float deltaTime) => runtime.Update(deltaTime);
}
```

运行时必须在宿主拥有线程启动、更新、取快照和停止，不允许回调重入。快照为不可变副本，可以交给其他线程。统计默认关闭，禁用时返回 null。

配置模块使用 `IArchitectureModule`，不混合更新操作。实现、资源所有权、失败和清理语义见 [Arch ECS 接入](../ecs/arch.md)。
