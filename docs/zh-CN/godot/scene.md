---
title: "挂载 Godot 场景"
description: "把 PackedScene 接到通用场景路由器。"
---

# 挂载 Godot 场景 {#overview}

在 Godot 主线程上把 `PackedScene` 注册到 `GodotSceneRegistry`。用 `GodotSceneFactory` 实例化场景，把工厂、宿主的 `ISceneRoot` 和主线程调度器交给 `SceneRouter`。根对象负责添加和移除节点。

```csharp
var registry = new GodotSceneRegistry();
registry.Register("play", GD.Load<PackedScene>("res://scenes/play.tscn"));
var factory = new GodotSceneFactory(registry);
await using var scenes = new SceneRouter(factory, sceneRoot, dispatcher);
await scenes.ReplaceAsync("play", cancellationToken: cancellationToken);
```

`sceneRoot` 与 `dispatcher` 由宿主提供。停机时先等待路由器关闭，再释放场景树中的根。操作说明见[场景路由](../game/scene.md)。
