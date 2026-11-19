---
title: "集中状态"
description: "用 action 和 reducer 更新不可变快照。"
---

# 集中状态 {#overview}

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

将副作用留在应用服务中，reducer 返回新状态。向视图提供 `IReadonlyStore<TState>` 和所需状态片段。需要定制匹配、比较、历史或 middleware 时，从 `Store<TState>.CreateBuilder()` 开始。默认按 action 的具体类型匹配。
