---
title: "生成节点查找"
description: "在 partial 节点类型中为字段声明 Godot 节点查找。"
---

# 生成节点查找 {#overview}

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

public partial class TopBar : HBoxContainer
{
    [GetNode]
    private HBoxContainer _leftContainer = null!;
}
```

`_leftContainer` 默认查找 `%LeftContainer`。在场景中给节点启用唯一名称，或在特性中提供显式路径。可选节点用 `Required = false` 并处理空值。

如果类已实现 `_Ready()`，在使用字段之前调用 `__InjectGetNodes_Generated()`。类没有 `_Ready()` 时，生成器补齐该入口。绑定事件时继续[生成信号连接](../source-generators/bind-node-signal-generator.md)。
