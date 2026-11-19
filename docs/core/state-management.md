---
title: "Shared state"
description: "Update immutable snapshots with actions and reducers."
---

# Shared state {#overview}

```csharp
using GFramework.Core.StateManagement;

var store = new Store<UiState>(new UiState(100));
store.RegisterReducer<Damage>((state, action) =>
    state with { Health = Math.Max(0, state.Health - action.Amount) });
using var subscription = store.SubscribeWithInitValue(state => Console.WriteLine(state.Health));
store.Dispatch(new Damage(15));

public sealed record UiState(int Health);
public sealed record Damage(int Amount);
```

Keep side effects in application services and return new state from reducers. Give views an `IReadonlyStore<TState>` and select the state they need. Start with `Store<TState>.CreateBuilder()` when you need custom matching, comparisons, history, or middleware. The default matches the action's concrete type.
