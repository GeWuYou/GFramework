---
title: "Godot 读写路径"
description: "为导出资源和玩家文件选择正确的存储路径。"
---

# Godot 读写路径 {#overview}

用 `GodotFileStorage` 把游戏数据仓库接到 Godot 的路径约定。打包资源使用 `res://`；玩家设置、进度和备份写入 `user://`。为不同子系统指定相对键或使用 `ScopedStorage` 分配前缀。

对存档槽位使用[版本化存档仓库](../game/data.md)。调试读写失败时检查键、路径根和导出预设。
