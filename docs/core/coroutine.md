---
title: "Coroutine scheduling"
description: "Advance frame-based game flows from a host loop."
---

# Coroutine scheduling {#overview}

Use `IEnumerator<IYieldInstruction>` for a frame-based flow. Start it with `CoroutineScheduler.Run` and call `Update` in the host loop. `Run` executes to the first wait and returns a handle.

## Waits and stages {#stages}

Declare the `CoroutineExecutionStage` values the host advances. Use scaled time for game timing and real time for waits that span a pause. Start, control, and advance the scheduler on its driving thread. Pass cross-thread cancellation through a token so the driving thread can clean up.

## Observe completion {#completion}

Keep the handle to observe completion, cancellation, or failure. Give grouped flows a scope with a clear shutdown lifetime. See the [runnable coroutine example](../tutorials/coroutine-tutorial.md) and [Godot stage integration](../godot/coroutine.md).
