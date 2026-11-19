---
title: "抽象接口"
description: "为共享契约、适配器和测试替身选择接口包。"
---

# 抽象接口 {#overview}

| 使用场景 | 包 |
| --- | --- |
| 生命周期、事件和资源契约 | [Core.Abstractions](../abstractions/core-abstractions.md) |
| 存储、场景、UI 和模组契约 | [Game.Abstractions](../abstractions/game-abstractions.md) |
| ECS 宿主和系统契约 | [Ecs.Arch.Abstractions](../abstractions/ecs-arch-abstractions.md) |
| 消息和处理器契约 | [Cqrs.Abstractions](../core/cqrs.md) |

运行时包会带入其所用的公开契约。在只需要共享接口的项目中，直接引用对应抽象包。
