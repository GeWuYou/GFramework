---
title: "Option and Result"
description: "Represent missing values and failures, and compose the success path."
---

# Option and Result {#overview}

```csharp
using GFramework.Core.Functional;

Option<string> nickname = Option<string>.Some("Player");
string label = nickname.Match(name => name, () => "Guest");
Result<int> count = Result<int>.Success(3);
Result<int> doubled = count.Map(value => value * 2);
```

Use `Option<T>` for a possible value and `None` for absence. Use `Result<T>` for a value or an error and `Result` for operations without a return value. Use `Map` to transform a value and `Bind` to chain an operation that returns its own Option or Result. Use `Match` to handle both outcomes.

For an Option transformation that can produce null, explicitly choose `Some` or `None`. Use `Failure(error)` at an operation boundary to preserve an error the caller can handle.
