---
title: "暂停 Godot 场景树"
description: "将嵌套暂停请求应用到 Godot 场景树。"
---

# 暂停 Godot 场景树 {#overview}

```csharp
using GFramework.Core.Pause;
using GFramework.Godot.Pause;

var pauses = new PauseStackManager();
pauses.RegisterHandler(new GodotPauseHandler(GetTree()));
```

将 `pauses` 与 UI 服务共享，并在需要暂停的路由上设置 `PauseWhileVisible`。需要在暂停时响应的原生界面应配置相应 `ProcessMode`。直接业务暂停通过令牌或作用域管理。

关闭时先结束 UI 和场景请求，再释放暂停管理器。一起验证主菜单、设置和确认窗的嵌套行为，见[暂停演练](../tutorials/pause-system.md)。
