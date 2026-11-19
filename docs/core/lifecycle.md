---
title: "Lifecycle components"
description: "Give services an explicit startup and shutdown lifecycle."
---

# Lifecycle components {#overview}

Register lifecycle components with `AddComponent` and implement the interfaces each service needs.

| Capability | Interface |
| --- | --- |
| Synchronous initialization | `IInitializable` |
| Asynchronous initialization | `IAsyncInitializable` |
| Synchronous shutdown | `IDestroyable` |
| Asynchronous shutdown | `IAsyncDestroyable` |

Implement `IDisposable` or `IAsyncDisposable` for resource cleanup and let the instance owner call it. Wait for components at startup and finish application work before disposal. See [application composition](../core/architecture.md) for ordering and ownership.
