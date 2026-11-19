---
title: "生成场景包装"
description: "用 AutoScene 为场景节点生成带键的场景辅助。"
---

# 生成场景包装 {#overview}

在使用场景行为包装的项目中，将 `[AutoScene("gameplay")]` 添加到 `partial` 场景根。属性位于 `GFramework.Godot.SourceGenerators.Abstractions.UI`。调用生成的 `GetScene()` 取得包装并与已登记的场景键一起使用。

接入默认工厂与导航时，参见[挂载 Godot 场景](../godot/scene.md)。
