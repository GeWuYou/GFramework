---
title: "Godot 宿主装配"
description: "在场景树中启动应用服务，并按顺序停止原生组件。"
---

# Godot 宿主装配 {#overview}

在场景树就绪的主线程上建立宿主调度器。需要将架构绑定到树时继承 `AbstractArchitecture`，在 `InstallModules` 注册服务或通过 `AddModule` 登记模块。配置阶段只登记服务；在宿主启动后挂载场景中需要的节点。

将具体依赖传给视图。准备退出时，等待场景、UI、暂停及其他已注册组件完成停止，再释放它们需要的根节点和调度器。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartArchitecture.cs{csharp}

继续[接入教程](../tutorials/godot-integration.md)。
