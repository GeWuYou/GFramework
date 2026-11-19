---
title: "Pause the Godot scene tree"
description: "Apply nested pause requests to the Godot scene tree."
---

# Pause the Godot scene tree {#overview}

```csharp
using GFramework.Core.Pause;
using GFramework.Godot.Pause;

var pauses = new PauseStackManager();
pauses.RegisterHandler(new GodotPauseHandler(GetTree()));
```

Share `pauses` with the UI service and set `PauseWhileVisible` for routes that should pause gameplay. Set a suitable `ProcessMode` on native UI that must respond while paused. Use tokens or scopes for direct gameplay pauses.

On shutdown, finish UI and scene requests before disposing the pause manager. See the [pause exercise](../tutorials/pause-system.md) to verify nested menus, settings, and confirmation dialogs.
