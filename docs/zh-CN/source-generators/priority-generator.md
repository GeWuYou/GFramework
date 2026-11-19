---
title: "生成优先级属性"
description: "用特性为普通类型实现 IPrioritized。"
---

# 生成优先级属性 {#overview}

```csharp
using GFramework.Core.SourceGenerators.Abstractions.Bases;

[Priority(10)]
public partial class ScheduledWork;
```

安装 `GeWuYou.GFramework.Core.SourceGenerators` 并将类型声明为 `partial`。生成的 `IPrioritized` 值由消费它的排序器解释。架构启动顺序使用 `AddComponent(order: ...)`，事件顺序使用订阅优先级。
