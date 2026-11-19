---
title: "Resource leases"
description: "Load shared resources and return them to their loader after the last lease."
---

# Resource leases {#overview}

## Acquire and release {#leases}

1. Register an `IResourceLoader<T>` for the resource type.
2. Load the resource and obtain an `IResourceHandle<T>` through `AcquireAsync<T>`.
3. Access the object through `Resource` and dispose the handle when finished.
4. Use `Retain()` to create a separate lease for another component.

The cache identifies resources by requested type and path. The loader handles unloading. Return leases before disposing the manager asynchronously.

## Concurrent loads {#loading}

Concurrent asynchronous requests for the same type and path share one load and receive separate leases. Pass the caller's token to `AcquireAsync` to cancel that caller's wait. Use the async entry point when a shared load for the same resource is pending.

For engine projects, see [load Godot resources](../godot/resource.md).
