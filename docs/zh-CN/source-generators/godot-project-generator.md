---
title: "生成 Godot 项目元数据"
description: "从 project.godot 生成 Autoload 和 InputMap 的 C# 入口。"
---

# 生成 Godot 项目元数据 {#overview}

安装 `GeWuYou.GFramework.Godot.SourceGenerators`，让根目录的 `project.godot` 作为 `AdditionalFiles` 参与编译。NuGet 包自动添加根目录文件；仓库内的项目引用可显式指定。

`[autoload]` 生成 `GFramework.Godot.Generated.AutoLoads`，`[input]` 生成 `GFramework.Godot.Generated.InputActions`。用生成的动作名接入[输入绑定](../godot/input.md)。添加或重命名 Godot 动作后重新构建。
