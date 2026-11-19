---
title: "通用扩展方法"
description: "使用集合、数值、异步和状态辅助扩展。"
---

# 通用扩展方法 {#overview}

`GFramework.Core.Extensions` 提供集合与 Span 操作、临时数组、数值显示、异步辅助和 Store 选择器。为每个调用确认所有权和分配成本。

```csharp
using GFramework.Core.Extensions;

string compact = 12345.ToCompactString();
string display = 0.00123.ToDisplayString();
```

数值格式化适用于显示。将精确数值保留在领域模型或存档中。使用 ArrayPool 时，在归还后结束对借用数组的所有访问。
