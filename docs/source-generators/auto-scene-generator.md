---
title: "Generate a scene wrapper"
description: "Use AutoScene to generate a keyed scene helper for a node."
---

# Generate a scene wrapper {#overview}

In a project that uses scene behavior wrappers, apply `[AutoScene("gameplay")]` to a `partial` scene root. The attribute is in `GFramework.Godot.SourceGenerators.Abstractions.UI`. Call the generated `GetScene()` to get the wrapper and use it with the registered scene key.

For the default factory and navigation, see [mount Godot scenes](../godot/scene.md).
