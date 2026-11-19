---
title: "Generate a CQRS handler registry"
description: "Generate directly registerable handlers in a consuming assembly."
---

# Generate a CQRS handler registry {#overview}

Install `GeWuYou.GFramework.Cqrs.SourceGenerators`. Implement `IRequestHandler<,>`, `INotificationHandler<>`, or `IStreamRequestHandler<,>` in the project that declares the handlers.

```csharp
services.AddCqrsHandlers(
    GFramework.Generated.Cqrs.CqrsRegistration.CreateRegistry());
```

Handlers that can be expressed statically go into the generated registry. For an assembly discovered at runtime, call `AddCqrsHandlersFromAssembly` explicitly. Check build diagnostics and make sure handler declarations match the registration scope. Run the [CQRS pipeline example](../core/cqrs.md#pipeline) to verify dispatch and scoping.
