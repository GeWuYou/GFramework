---
title: "宿主环境"
description: "把路径、平台和运行环境传给需要它们的服务。"
---

# 宿主环境 {#overview}

在 .NET Host 中从 `IHostEnvironment` 获取环境与内容根。在 Godot 中由宿主解析资源位置和用户数据目录，再向适配器传入路径或专用路径服务。

把环境解析集中在组合根。参见[应用配置](../core/configuration.md)和[Godot 存储](../godot/storage.md)。
