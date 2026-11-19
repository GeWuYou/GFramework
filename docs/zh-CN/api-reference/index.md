---
title: "API 参考"
description: "按任务定位 GFramework 的公共类型、契约与生成器入口。"
---

# API 参考 {#overview}

| 任务 | 公开入口 | 说明 |
| --- | --- | --- |
| 注册服务、启动和停止 | `Architecture`、`AddComponent` | [生命周期](../core/architecture.md) |
| 分发请求、通知和流 | `ICqrsRuntime` | [CQRS](../core/cqrs.md) |
| 订阅或持有资源 | `IEventBus`、`IResourceHandle<T>` | [事件](../core/events.md) · [资源](../core/resource.md) |
| 读取配置 | `ConfigRegistry`、`YamlConfigLoader` | [类型化配置](../game/config-system.md) |
| 保存游戏数据 | `SaveRepository<T>` | [版本化存档](../game/data.md) |
| 导航场景与 UI | `SceneRouter`、`UiService` | [场景](../game/scene.md) · [UI](../game/ui.md) |
| 接入 Godot 与 ECS | `GodotUiRoot`、`IArchRuntime` | [Godot](../godot/index.md) · [Arch](../ecs/arch.md) |

在 IDE 中查看这些类型的参数、返回值和生命周期说明。接口包见[抽象接口](../abstractions/index.md)，编译期输入与诊断见[源码生成器](../source-generators/index.md)。
