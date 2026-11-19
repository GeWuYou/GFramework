---
title: "加载 Godot 资源"
description: "用类型化资源仓储按业务键访问 Godot 资源。"
---

# 加载 Godot 资源 {#overview}

通过 `GodotResourceRepository<TKey, TResource>` 管理 `Resource` 资产。为需要按 ID 读取的资源定义稳定业务键，在 Godot 可访问资源的阶段准备仓储，再把仓储传给使用方。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartResources.cs{csharp}

以仓储处理项目资源的索引。在需要跨使用方跟踪获取和卸载时，使用[Core 资源租约](../core/resource.md)及相应加载器。
