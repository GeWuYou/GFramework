---
title: ECS 集成
description: 按需采用原生 Arch 系统、宿主帧循环与可选生命周期编排。
---

# ECS 集成

ECS 用于实体数据和批量系统更新，普通游戏服务无需通过 ECS。GFramework 保留 Arch 的 World、系统与 Group，让宿主显式决定更新和暂停时机。

| 包 | 职责 |
| --- | --- |
| `GFramework.Ecs.Arch` | `ArchRuntime`、配置模块、可选适配基类和 DI 登记 |
| `GFramework.Ecs.Arch.Abstractions` | `IArchRuntime`、唯一的 `ArchOptions` 和不可变统计快照 |

运行时可以直接构造，也可以用 `services.AddArch(...)` 接入 Architecture。系统按原生 `ISystem<float>` 登记，不强制继承框架基类。

- [Arch ECS 接入](./arch.md)：完整接入、帧阶段、资源责任和统计。
- [ECS 契约](../abstractions/ecs-arch-abstractions.md)：宿主循环与只读快照。
