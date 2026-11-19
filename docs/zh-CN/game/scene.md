---
title: "场景路由"
description: "通过宿主管理场景栈、导航和生命周期。"
---

# 场景路由 {#overview}

## 装配路由器 {#setup}

向 `SceneRouter` 提供 `ISceneFactory`、`ISceneRoot` 和 `IGameDispatcher`。纯托管宿主可选 `InlineGameDispatcher`；Godot 使用[引擎适配](../godot/scene.md)和同一主线程调度器。

## 导航 {#navigation}

| 操作 | 调用 |
| --- | --- |
| 替换整个场景栈 | `ReplaceAsync` |
| 保留当前场景并叠加新场景 | `PushAsync` |
| 退出栈顶并恢复下层 | `PopAsync` |

使用返回结果处理守卫拒绝。将后续导航安排在生命周期回调之外。宿主退出时等待路由器停止，再释放场景根与调度器。
