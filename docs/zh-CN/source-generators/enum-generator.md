---
title: "生成枚举扩展"
description: "为枚举生成判断与转换辅助。"
---

# 生成枚举扩展 {#overview}

```csharp
using GFramework.Core.SourceGenerators.Abstractions.Enums;

[GenerateEnumExtensions]
public enum RunMode { Menu, Playing, Ended }
```

在消费项目安装 `GeWuYou.GFramework.Core.SourceGenerators`，给枚举加 `GenerateEnumExtensions`。可用 `GenerateIsMethods` 和 `GenerateIsInMethod` 控制对应判断方法。扩展位于枚举的命名空间；全局命名空间枚举的扩展位于 `EnumExtensionsGenerated`。
