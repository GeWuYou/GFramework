---
title: "异步初始化"
description: "等待文件、内容或其他首载操作完成后开放应用。"
---

# 异步初始化 {#overview}

实现 `IAsyncInitializable` 并用 `AddComponent` 登记。将首载 I/O 放进 `InitializeAsync`，把取消令牌传给实际等待的操作。

在宿主调用并等待 `Architecture.InitializeAsync`。启动完成后再启用 UI 或请求入口。组件应能清理部分初始化完成的状态，启动失败或取消时架构会尝试停止已经开始的组件。将宿主停止请求放在初始化回调之外。

继续阅读[启动和停止顺序](../core/architecture.md#lifecycle)。
