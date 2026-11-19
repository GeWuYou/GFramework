---
title: "Generate node lookups"
description: "Declare Godot node lookups on fields of a partial node type."
---

# Generate node lookups {#overview}

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

public partial class TopBar : HBoxContainer
{
    [GetNode]
    private HBoxContainer _leftContainer = null!;
}
```

`_leftContainer` looks up `%LeftContainer` by default. Enable a unique name for the node in the scene or provide an explicit attribute path. Use `Required = false` for an optional node and handle null.

If the class implements `_Ready()`, call `__InjectGetNodes_Generated()` before using the fields. The generator supplies `_Ready()` when the class has no override. For events, continue with [generated signal connections](../source-generators/bind-node-signal-generator.md).
