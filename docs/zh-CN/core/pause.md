---
title: "暂停请求"
description: "用独立令牌组合菜单、过场和业务暂停。"
---

# 暂停请求 {#overview}

```csharp
using GFramework.Core.Pause;

await using var pauses = new PauseStackManager();
var menu = pauses.Push("Pause menu");
var settings = pauses.Push("Settings");
pauses.Pop(settings);
pauses.Pop(menu);
```

每个令牌对应一次请求。最后一个请求结束时恢复该组。短操作可用 `PauseScope` 自动结束请求。通过 `RegisterHandler` 注册 `IPauseHandler`，把首次暂停和最终恢复应用到相应子系统。

在 Godot 中使用[引擎暂停适配](../godot/pause.md)，并为每个 UI 路由指定暂停策略。
