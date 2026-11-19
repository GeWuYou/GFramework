---
title: "在场景间管理资源"
description: "按使用者管理租约并验证切换后的释放。"
---

# 在场景间管理资源 {#overview}

由场景所有者调用 `AcquireAsync<T>` 获取资源。若下一个场景仍会使用，切换前用 `Retain()` 创建它自己的租约。退出旧场景时释放旧租约，并在新场景退出后结束剩余租约。

校验加载器的调用次数、取消后的租约状态和最终卸载。接入步骤见[Core 资源租约](../core/resource.md)，项目资源索引见[Godot 资源](../godot/resource.md)。
