---
title: "应用 Godot 设置"
description: "在引擎线程应用音频、图形和语言偏好。"
---

# 应用 Godot 设置 {#overview}

创建[设置模型](../game/setting.md)和位置提供者，使用宿主的调度器装配模型与 `SettingsSystem`。根据实际功能登记 `GodotAudioSettings`、`GodotGraphicsSettings` 或 `GodotLocalizationSettings`。

先等待模型加载，再应用到引擎。通过设置 UI 改变数据，在玩家确认时保存。验证项目中实际存在的 Audio Bus、窗口配置及本地化服务。

游戏代码见[暂停和设置示例](../tutorials/godot-complete-project.md#pause-settings)。
