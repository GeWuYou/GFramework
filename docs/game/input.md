---
title: "Input bindings and UI actions"
description: "Persist rebindable actions and translate input into semantic UI actions."
---

# Input bindings and UI actions {#overview}

Use `InputBindingDescriptor` to describe a device and input code. Use `IInputBindingStore` to query bindings, replace the primary binding, or restore the defaults. Save an `InputBindingSnapshot` with settings. Let the player choose how to resolve a binding conflict.

Map logical actions to `UiInputAction` values such as confirm and cancel. Send mapped actions to the UI with `IUiInputDispatcher`. For Godot, see the [InputMap adapter](../godot/input.md).
