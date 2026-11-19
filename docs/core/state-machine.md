---
title: "Asynchronous state machines"
description: "Run state entry, exit, back navigation, and cleanup in order."
---

# Asynchronous state machines {#overview}

```csharp
using GFramework.Core.State;

await using var machine = new StateMachine(maxHistorySize: 10);
machine.Register(new MenuState()).Register(new PlayingState());
await machine.ChangeToAsync<MenuState>();
await machine.ChangeToAsync<PlayingState>();

public sealed class MenuState : StateBase;
public sealed class PlayingState : StateBase;
```

## Transitions {#transitions}

Register all states before the first transition. A transition checks the exit condition, exits the old state, enters the new state, and records the successful change. `GoBackAsync` also checks the exit condition. Trigger follow-up transitions outside state callbacks.

## Faults and shutdown {#shutdown}

Read a state callback failure from `Fault`. Await `StopAsync` to finish exit and application cleanup before creating a replacement machine. The caller or DI container disposes the state instances. Start operations involving Godot nodes on the engine thread.
