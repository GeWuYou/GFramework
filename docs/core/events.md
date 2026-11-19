---
title: "Synchronous events"
description: "Publish in-process notifications and manage subscription lifetimes."
---

# Synchronous events {#overview}

```csharp
using GFramework.Core.Events;

var events = new EventBus();
using var subscriptions = new SubscriptionScope();
subscriptions.Add(events.Subscribe<int>(score => Console.WriteLine(score)));
events.Publish(42);
```

## Delivery {#delivery}

`Publish` calls the current subscription snapshot synchronously. Higher priorities run first and ties follow subscription order. Use `EventPropagation.Highest` for the highest-priority group, or `UntilHandled` with `MarkAsHandled` to stop delivery early. Callback exceptions propagate to the publisher.

## Disposal and threading {#ownership}

Give subscription tokens to the view or component that owns them and dispose them on exit. Callbacks run on the publishing thread. Dispatch to the engine thread before updating Godot nodes. Use a service method or a [CQRS request](../core/cqrs.md) when the caller needs an awaited result.
