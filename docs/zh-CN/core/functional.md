---
title: "Option 与 Result"
description: "显式表示缺席和失败，并组合成功路径。"
---

# Option 与 Result {#overview}

```csharp
using GFramework.Core.Functional;

Option<string> nickname = Option<string>.Some("Player");
string label = nickname.Match(name => name, () => "Guest");
Result<int> count = Result<int>.Success(3);
Result<int> doubled = count.Map(value => value * 2);
```

使用 `Option<T>` 表示值可能缺席，缺席用 `None`。用 `Result<T>` 表示成功值或错误，无值操作使用 `Result`。用 `Map` 转换已有值，用 `Bind` 串联本身返回 Option 或 Result 的操作。用 `Match` 消费两种分支。

可能返回空引用的 Option 转换应显式选择 `Some` 或 `None`。在调用边界用 `Failure(error)` 保存可处理的错误。
