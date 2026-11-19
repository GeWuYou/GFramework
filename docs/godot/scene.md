---
title: "Mount Godot scenes"
description: "Connect PackedScene assets to the scene router."
---

# Mount Godot scenes {#overview}

Register `PackedScene` assets in a `GodotSceneRegistry` on the Godot main thread. Instantiate scenes with `GodotSceneFactory` and provide the factory, the host's `ISceneRoot`, and the dispatcher to `SceneRouter`. The root adds and removes native nodes.

```csharp
var registry = new GodotSceneRegistry();
registry.Register("play", GD.Load<PackedScene>("res://scenes/play.tscn"));
var factory = new GodotSceneFactory(registry);
await using var scenes = new SceneRouter(factory, sceneRoot, dispatcher);
await scenes.ReplaceAsync("play", cancellationToken: cancellationToken);
```

The host supplies `sceneRoot` and `dispatcher`. On shutdown, await the router before releasing the scene tree root. See [scene routing](../game/scene.md) for navigation operations.
