---
title: Priority 生成器
---

# Priority 生成器

Priority 为普通 partial 类型实现 IPrioritized。该值的含义由使用它的排序器决定；Architecture 组件启动顺序通过 AddComponent 的 order 显式配置，事件优先级通过 Subscribe 的 priority 配置。

```csharp
using GFramework.Core.SourceGenerators.Abstractions.Bases;

[Priority(10)]
public partial class ScheduledWork;
```

生成器输入与诊断以特性契约和测试为准。运行时项目不使用生成器特性。
