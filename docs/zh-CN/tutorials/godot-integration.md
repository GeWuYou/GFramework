---
title: "接入 Godot"
description: "在最小 Godot 场景中启动服务、打开页面并完成对话框。"
---

# 接入 Godot {#overview}

## 构建与运行 {#run}

使用 Godot 4.7.2 .NET 和仓库指定的 SDK，从仓库根目录运行：

```bash
dotnet build Samples/GodotIntegration/GodotIntegration.csproj -c Debug
godot --headless --editor --path Samples/GodotIntegration --import
godot --path Samples/GodotIntegration res://GettingStarted/QuickStart.tscn
```

窗口显示按键加载的资源标题。打开确认框并操作，状态栏显示 `Confirmed` 或 `Cancelled`。

## 装配服务与视图 {#composition}

在场景根准备导出的资源与视图，在组合根登记资源组件、原生挂载根和类型化路由。资源先完成初始化，然后由页面工厂取得资源并配置视图。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartArchitecture.cs{csharp}

## 返回结果与关闭 {#dialog}

由 `RegisterGodotDialog` 登记对话框工厂，将动作和上下文传入视图。确认按钮完成 `bool` 结果，关闭按钮结束当前对话框。宿主退出时先停止 UI 再退出树。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartConfirm.cs{csharp}

## 自检 {#smoke}

```bash
godot --headless --path Samples/GodotIntegration res://GettingStarted/QuickStart.tscn -- --quickstart-smoke
```

观察测试是否完成打开、确认、取消和关闭。接下来可[运行完整小游戏](../tutorials/godot-complete-project.md)。
