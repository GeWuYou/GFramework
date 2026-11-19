---
title: "Coordinate game state transitions"
description: "Use a state machine for a menu, gameplay, and back navigation."
---

# Coordinate game state transitions {#overview}

1. Define menu and playing states and inject their dependencies through constructors.
2. Register the states, enter the menu, and request a transition when gameplay starts.
3. Check exit conditions before leaving a state and give observers the successful change to update the view.
4. Verify that a rejected exit keeps the current state. Await `StopAsync` when the game exits.

See [asynchronous state machines](../core/state-machine.md) for a minimal example and fault handling.
