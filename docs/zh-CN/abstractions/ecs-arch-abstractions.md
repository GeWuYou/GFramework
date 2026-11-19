---
title: "Arch ECS 接口"
description: "在宿主、ECS 集成和共享模块间声明运行时依赖。"
---

# Arch ECS 接口 {#overview}

`GeWuYou.GFramework.Ecs.Arch.Abstractions` 提供 `IArchRuntime`、`IArchSystemAdapter<T>`、`ArchOptions` 与统计快照。由宿主负责更新顺序和停止时机。实际装配见[运行 Arch ECS 系统](../ecs/arch.md)。
