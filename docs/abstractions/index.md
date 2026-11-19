---
title: "Abstractions"
description: "Choose interface packages for shared contracts, adapters, and test doubles."
---

# Abstractions {#overview}

| Use case | Package |
| --- | --- |
| Lifecycle, event, and resource contracts | [Core.Abstractions](../abstractions/core-abstractions.md) |
| Storage, scene, UI, and mod contracts | [Game.Abstractions](../abstractions/game-abstractions.md) |
| ECS host and system contracts | [Ecs.Arch.Abstractions](../abstractions/ecs-arch-abstractions.md) |
| Message and handler contracts | [Cqrs.Abstractions](../core/cqrs.md) |

Runtime packages bring in the public contracts they use. Reference an abstractions package directly when a project only needs to share its interfaces.
