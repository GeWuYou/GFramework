---
title: "在 Godot 中运行协程"
description: "通过节点寿命和引擎阶段驱动协程。"
---

# 在 Godot 中运行协程 {#overview}

在节点就绪后使用 `this.RunCoroutine(...)` 启动节点拥有的流程，节点退出树时会请求取消。保留句柄以便观察结果或主动停止。

| 阶段 | 对应引擎时机 |
| --- | --- |
| `Update` | Godot 的帧处理 |
| `FixedUpdate` | Godot 的物理处理 |
| `Deferred` | Godot 延迟回调 |

需要跨阶段时显式等待对应阶段。把原生节点操作安排在正确的引擎阶段。参见[协程示例](../tutorials/coroutine-tutorial.md)。
