---
title: "输入绑定与 UI 动作"
description: "保存可重绑的动作，并将输入转换为语义 UI 动作。"
---

# 输入绑定与 UI 动作 {#overview}

用 `InputBindingDescriptor` 描述设备和键码，用 `IInputBindingStore` 查询、替换主绑定或恢复默认值。将 `InputBindingSnapshot` 随设置保存。出现绑定冲突时，由界面让玩家选择处理方式。

将逻辑动作映射到 `UiInputAction`，例如确认和返回。使用 `IUiInputDispatcher` 将已映射的操作交给 UI。Godot 项目见[InputMap 适配](../godot/input.md)。
