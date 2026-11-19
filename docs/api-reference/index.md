---
title: "API reference"
description: "Find public GFramework types, contracts, and generators by task."
---

# API reference {#overview}

| Task | Public entry points | Guide |
| --- | --- | --- |
| Register, start, and stop services | `Architecture`, `AddComponent` | [Lifecycle](../core/architecture.md) |
| Dispatch requests, notifications, and streams | `ICqrsRuntime` | [CQRS](../core/cqrs.md) |
| Subscribe or hold resources | `IEventBus`, `IResourceHandle<T>` | [Events](../core/events.md) and [resources](../core/resource.md) |
| Read configuration | `ConfigRegistry`, `YamlConfigLoader` | [Typed configuration](../game/config-system.md) |
| Save game data | `SaveRepository<T>` | [Versioned saves](../game/data.md) |
| Navigate scenes and UI | `SceneRouter`, `UiService` | [Scenes](../game/scene.md) and [UI](../game/ui.md) |
| Integrate Godot and ECS | `GodotUiRoot`, `IArchRuntime` | [Godot](../godot/index.md) and [Arch](../ecs/arch.md) |

Use the IDE to find parameter, return value, and lifecycle details for these types. See [abstractions](../abstractions/index.md) for interface packages and [source generators](../source-generators/index.md) for compile-time inputs and diagnostics.
