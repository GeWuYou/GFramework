---
title: "Pause requests"
description: "Combine menu, cutscene, and application pauses with independent tokens."
---

# Pause requests {#overview}

```csharp
using GFramework.Core.Pause;

await using var pauses = new PauseStackManager();
var menu = pauses.Push("Pause menu");
var settings = pauses.Push("Settings");
pauses.Pop(settings);
pauses.Pop(menu);
```

Each token represents one request. Its group resumes when the last request ends. For a short operation, use `PauseScope` to end the request automatically. Register an `IPauseHandler` with `RegisterHandler` to apply the first pause and last resume to the relevant subsystem.

In Godot, use the [engine pause adapter](../godot/pause.md) and give each UI route a pause policy.
