---
title: "Application composition and lifecycle"
description: "Register services with standard DI and coordinate startup and shutdown with Architecture."
---

# Application composition and lifecycle {#overview}

## Register and start {#registration}

Register dependencies in `ConfigureServices`. Use standard DI registration for ordinary classes and `AddComponent` for services that need initialization or shutdown. Pass dependencies through constructors and use one registration key per component.

<<< ../../Samples/MinimalApp/OrderArchitecture.cs

## Lifecycle {#lifecycle}

1. Await `InitializeAsync` to create the provider, start components in order, and connect the host.
2. Resolve services and enable user operations when the host is ready. Create a scope for operations that use scoped services.
3. Await `DestroyAsync` on exit. Components stop in reverse order, then the host detaches and the provider is released.

`order` sorts startup callbacks from lowest to highest, with registration order breaking ties. Put work that requires a running component in a lifecycle callback.

## Ownership {#ownership}

DI-created disposable services are released with their provider or scope. If you register an existing component instance, its caller owns disposal and Architecture runs the registered shutdown work.

Related topics: [asynchronous initialization](../core/async-initialization.md) and [dependency injection](../core/ioc.md).
