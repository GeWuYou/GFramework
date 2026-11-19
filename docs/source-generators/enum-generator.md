---
title: "Generate enum extensions"
description: "Generate tests and conversion helpers for an enum."
---

# Generate enum extensions {#overview}

```csharp
using GFramework.Core.SourceGenerators.Abstractions.Enums;

[GenerateEnumExtensions]
public enum RunMode { Menu, Playing, Ended }
```

Install `GeWuYou.GFramework.Core.SourceGenerators` in the consuming project and apply `GenerateEnumExtensions` to the enum. Control the corresponding tests with `GenerateIsMethods` and `GenerateIsInMethod`. Extensions use the enum's namespace. For an enum in the global namespace, import `EnumExtensionsGenerated`.
