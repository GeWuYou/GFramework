---
title: "Handle read requests"
description: "Return read-only snapshots from application queries."
---

# Handle read requests {#overview}

Return the projection or snapshot the caller needs. To use a shared pipeline, define an `IRequest<TResponse>`, inject read dependencies into its handler, and obtain the result through `ICqrsRuntime.SendAsync`.

Reads and writes can share the same domain model. See [CQRS setup](../core/cqrs.md) for registration and scopes.
