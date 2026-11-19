---
title: "Adapt Godot InputMap"
description: "Rebind, reset, and persist native actions."
---

# Adapt Godot InputMap {#overview}

Define default actions in `project.godot`. Read and update `InputMap` through `GodotInputBindingStore` and save the exported `InputBindingSnapshot` with settings. Import it on load and use the store's reset operation to restore defaults.

Generate `InputActions` with the [project metadata generator](../source-generators/godot-project-generator.md) for named constants. Map actions such as open menu, confirm, and cancel to [UI input dispatch](../godot/ui.md).
