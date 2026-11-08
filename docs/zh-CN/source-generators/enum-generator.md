---
title: 枚举扩展生成器
---

# 枚举扩展生成器

GenerateEnumExtensions 为消费项目的枚举生成判断与转换辅助。

```csharp
using GFramework.Core.SourceGenerators.Abstractions.Enums;

[GenerateEnumExtensions]
public enum RunMode { Menu, Playing, Ended }
```

GenerateIsMethods 和 GenerateIsInMethod 控制对应判断方法。生成行为、flags 和边界值以生成器 XML 及快照测试为准。此功能属于消费项目的编译期能力，不使运行时依赖生成器。

枚举位于命名空间时，扩展类位于同一命名空间；全局命名空间的枚举使用 EnumExtensionsGenerated，调用扩展方法时导入该命名空间。特性源码随 Core.SourceGenerators NuGet 包提供。
