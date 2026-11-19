---
title: "挂载 Godot UI"
description: "在原生 Control 场景中打开页面、传参和确认结果。"
---

# 挂载 Godot UI {#overview}

## 创建宿主 {#host}

在引擎线程上将 `GodotUiRoot` 添加到场景树。通过同一个 `IGameDispatcher` 创建 `UiService`，将路由和原生视图工厂登记到 `UiRegistry`。需要暂停菜单时传入同一个暂停管理器。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartArchitecture.cs{csharp}

## 打开页面与对话框 {#navigation}

使用普通 `Control` 场景制作视图。页面接收参数和动作，通过 `UiViewContext` 关闭自己。对话框用其上下文完成带类型的结果。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartConfirm.cs{csharp}

## 视图生命周期 {#view-lifecycle}

<span id="可选生命周期"></span>

需要业务订阅时，实现 `IGodotUiLifecycle`，在 `OpenAsync` 准备状态，在 `CloseAsync` 解除订阅。打开时视图处于已挂载、隐藏状态。节点由视图租约释放。

将 Godot 动作交给 `GodotUiInputBridge` 做语义分发。在宿主退出时先等待 UI 停止，再移除挂载根。完整操作见[Godot 接入教程](../tutorials/godot-integration.md)。
