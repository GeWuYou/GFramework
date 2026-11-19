---
title: "Arch ECS contracts"
description: "Declare runtime dependencies shared between the host, ECS integration, and modules."
---

# Arch ECS contracts {#overview}

`GeWuYou.GFramework.Ecs.Arch.Abstractions` provides `IArchRuntime`, `IArchSystemAdapter<T>`, `ArchOptions`, and statistics snapshots. The host owns update ordering and shutdown timing. For composition, see [run Arch ECS systems](../ecs/arch.md).
