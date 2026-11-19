---
title: "CQRS requests, notifications, and streams"
description: "Register handlers, dispatch messages, and compose pipeline behaviors."
---

# CQRS requests, notifications, and streams {#overview}

## Set up the runtime {#setup}

Install `GeWuYou.GFramework.Cqrs` and call `AddCqrsRuntime` in the composition root. Inject dependencies into handlers and resolve `ICqrsRuntime` in consumers.

| Task | Call |
| --- | --- |
| Request one response | `SendAsync` |
| Notify multiple handlers | `PublishAsync` |
| Read multiple asynchronous results | `CreateStream` |

To [generate a registry](../source-generators/cqrs-handler-registry-generator.md#install), install the runtime and source generator in the project that declares the handlers. For assemblies discovered at runtime, use `AddCqrsHandlersFromAssembly`. Pass the caller's cancellation token.

## Pipelines and scopes {#pipeline}

Register pipeline behaviors in order from outermost to innermost. Register behaviors that depend on scoped services with a scoped lifetime and resolve the runtime from the same caller scope. For streams, keep the scope open throughout enumeration.

<<< ../../Samples/CqrsPipelineDemo/Program.cs#scoped-pipeline

```bash
dotnet run --project Samples/CqrsPipelineDemo -c Release
```

`CQRS_PIPELINE_PASS` confirms that the handler returned its response and the scope released its dependency. See also the [handler registry generator](../source-generators/cqrs-handler-registry-generator.md).
