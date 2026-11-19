---
title: "Bindable properties"
description: "Observe a value and release its binding when a view closes."
---

# Bindable properties {#overview}

```csharp
using GFramework.Core.Property;

var score = new BindableProperty<int>(0);
using var subscription = score.RegisterWithInitValue(Console.WriteLine);
score.Value = 10;
```

`RegisterWithInitValue` sends the current value first. `Register` observes later changes. A view can receive `IReadonlyBindableProperty<T>` while application use cases own changes. Let the binding owner dispose the subscription.

Callbacks run on the thread that sets the value. Update properties that drive native UI on the engine thread. See [shared state](../core/state-management.md) when several fields need to change as one snapshot.
