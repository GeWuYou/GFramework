---
title: "Scene routing"
description: "Manage scene stacks, navigation, and lifecycle through a host."
---

# Scene routing {#overview}

## Set up the router {#setup}

Provide `ISceneFactory`, `ISceneRoot`, and `IGameDispatcher` to `SceneRouter`. A managed host can use `InlineGameDispatcher`. In Godot, use the [engine adapter](../godot/scene.md) and the host's main-thread dispatcher.

## Navigate {#navigation}

| Operation | Call |
| --- | --- |
| Replace the scene stack | `ReplaceAsync` |
| Keep the current scene under a new one | `PushAsync` |
| Exit the top scene and resume the one beneath it | `PopAsync` |

Use the result to handle a guard rejecting navigation. Schedule follow-up navigation outside lifecycle callbacks. On exit, await router shutdown before releasing the scene root and dispatcher.
