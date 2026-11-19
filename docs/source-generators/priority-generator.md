---
title: "Generate a priority property"
description: "Implement IPrioritized on a regular type with an attribute."
---

# Generate a priority property {#overview}

```csharp
using GFramework.Core.SourceGenerators.Abstractions.Bases;

[Priority(10)]
public partial class ScheduledWork;
```

Install `GeWuYou.GFramework.Core.SourceGenerators` and declare the type `partial`. The sorter that consumes `IPrioritized` defines what the generated value means. Set architecture order with `AddComponent(order: ...)` and event order with subscription priorities.
