---
title: "Handle write requests"
description: "Keep write rules in use cases and add CQRS dispatch when useful."
---

# Handle write requests {#overview}

Perform a write in a service method or a CQRS handler. When you need a pipeline, define an `IRequest<TResponse>` and implement its `IRequestHandler<TRequest, TResponse>`. Pass cancellation through to persistence and other asynchronous work.

Return the operation result after a successful write and notify observers as needed. See [CQRS setup](../core/cqrs.md) for registration.
