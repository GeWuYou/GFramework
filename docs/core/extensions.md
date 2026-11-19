---
title: "General extensions"
description: "Use collection, numeric, asynchronous, and state helpers."
---

# General extensions {#overview}

`GFramework.Core.Extensions` provides collection and span operations, temporary arrays, numeric formatting, asynchronous helpers, and Store selectors. Check ownership and allocation costs for the calls you use.

```csharp
using GFramework.Core.Extensions;

string compact = 12345.ToCompactString();
string display = 0.00123.ToDisplayString();
```

Use formatted numbers for display and keep exact values in the domain model or save data. When using ArrayPool, end all access to the rented array before returning it.
