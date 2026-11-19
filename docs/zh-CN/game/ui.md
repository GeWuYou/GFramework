---
title: "UI 路由与对话框"
description: "用强类型参数打开页面、窗口与对话框。"
---

# UI 路由与对话框 {#overview}

## 定义路由与注册视图 {#routes}

用 `UiRoute<TArgs>` 定义页面和窗口，用 `UiDialogRoute<TArgs, TResult>` 定义带返回结果的对话框。用 `UiRouteOptions` 定义层级、模态、世界输入拦截和暂停。将路由与视图工厂登记到 `UiRegistry`。

<<< ../../../Samples/DodgeTheCreeps/DodgeRoutes.cs{csharp}

## 打开、关闭与输入 {#navigation}

向 `UiService` 提供 registry、`IUiHost` 和 `IGameDispatcher`。打开时传入路由和参数。视图用 `UiViewContext` 取得本次打开的句柄或关闭自身。将世界输入的放行判断接到 `Snapshot` 的拦截状态，语义动作通过 `DispatchActionAsync` 交给 UI。

## 释放 {#shutdown}

让工厂创建视图租约并负责释放视图资源。在生命周期回调外发起后续导航。退出时等待 `StopAsync`，然后再释放挂载根、调度器和暂停服务。Godot 示例见[挂载原生 UI](../godot/ui.md)。
