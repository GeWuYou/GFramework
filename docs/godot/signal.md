---
title: "Connect Godot signals"
description: "Connect native signals with the fluent wrapper."
---

# Connect Godot signals {#overview}

Start a connection from a `GodotObject` and `StringName` with `SignalFluentExtensions.Signal`. Configure the callback and connection options through `SignalBuilder`, then connect through Godot's signal mechanism.

Manage the connection with the lifecycle that owns the node. For static scene-node bindings, use the [BindNodeSignal generator](../source-generators/bind-node-signal-generator.md).
