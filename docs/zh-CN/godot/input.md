---
title: "适配 Godot InputMap"
description: "在原生动作表上实现重绑定、恢复和持久化。"
---

# 适配 Godot InputMap {#overview}

在 `project.godot` 定义默认动作。用 `GodotInputBindingStore` 读取和更新 `InputMap`，将导出的 `InputBindingSnapshot` 交给设置服务保存。游戏加载时导入快照，恢复默认时使用存储的重置入口。

如需避免手写动作名，用[项目元数据生成器](../source-generators/godot-project-generator.md)生成 `InputActions`。把打开菜单、确认和返回等动作接到[UI 输入分发](../godot/ui.md)。
