---
title: "Run coroutines in Godot"
description: "Drive coroutines with node lifetimes and engine phases."
---

# Run coroutines in Godot {#overview}

After a node is ready, call `this.RunCoroutine(...)` to start a flow owned by the node. Leaving the tree requests cancellation. Keep the handle to observe completion or stop the coroutine.

| Stage | Engine timing |
| --- | --- |
| `Update` | Godot frame processing |
| `FixedUpdate` | Godot physics processing |
| `Deferred` | Godot deferred callback |

Wait explicitly for the next stage when a flow crosses phases. Schedule native node work at the stage it needs. See the [coroutine example](../tutorials/coroutine-tutorial.md).
