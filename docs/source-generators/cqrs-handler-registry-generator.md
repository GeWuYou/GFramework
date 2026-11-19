---
title: "Generate a CQRS handler registry"
description: "Install the runtime and generator in the handler project and register handlers from the host."
---

# Generate a CQRS handler registry {#overview}

## Install packages {#install}

Run both commands from the project that declares the handlers:

```bash
dotnet add package GeWuYou.GFramework.Cqrs
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators
```

`GFramework.Cqrs` supplies the registry interface and attribute the generator needs. It also brings in `GFramework.Cqrs.Abstractions`, which defines `IRequestHandler<,>`, `INotificationHandler<>`, and `IStreamRequestHandler<,>`. A shared project that declares only messages can reference the abstractions package directly.

## Register handlers {#register}

When the host and handlers are in the same project, use the generated factory in the composition root:

```csharp
services.AddCqrsHandlers(
    GFramework.Generated.Cqrs.CqrsRegistration.CreateRegistry());
```

## Register from a separate library {#separate-library}

Add a project reference from the host to the handler library. The generated factory is internal to the handler assembly. Expose a public method in that library to register its handlers:

```csharp
using GFramework.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace MyGame.Handlers;

public static class HandlerRegistration
{
    public static IServiceCollection AddHandlers(IServiceCollection services)
        => services.AddCqrsHandlers(
            GFramework.Generated.Cqrs.CqrsRegistration.CreateRegistry());
}
```

Import `MyGame.Handlers` in the host. Call `services.AddCqrsRuntime()` and `HandlerRegistration.AddHandlers(services)` in the host before building the provider. The handler library's reference to `GFramework.Cqrs` supplies the runtime transitively to the host.

Use `AddCqrsHandlersFromAssembly` for dynamically loaded assemblies or a generated registry that requires reflection fallback. Run the [CQRS pipeline example](../core/cqrs.md#pipeline) to verify dispatch and scoping.
