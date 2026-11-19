---
title: "Generate node signal bindings"
description: "Declare Godot CLR signal callbacks and lifecycle wiring."
---

# Generate node signal bindings {#overview}

Apply `[BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]` to a callback. Point the field at the actual node and match the callback signature to the signal event.

1. Prepare node references in `_Ready()`. With `[GetNode]`, call `__InjectGetNodes_Generated()` first.
2. Call `__BindNodeSignals_Generated()` to subscribe.
3. Call `__UnbindNodeSignals_Generated()` in `_ExitTree()` to release subscriptions.

Check generator diagnostics for missing calls in custom lifecycle overrides. For runtime connections, see [Godot signals](../godot/signal.md).
