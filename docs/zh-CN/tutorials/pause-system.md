---
title: "实现嵌套暂停菜单"
description: "验证暂停页和设置窗独立持有暂停请求。"
---

# 实现嵌套暂停菜单 {#overview}

1. 在宿主创建一个 `PauseStackManager`，登记 `GodotPauseHandler`。
2. 给暂停页和设置窗口设置对应的 `PauseWhileVisible`。使用同一实例将 UI 策略应用到场景树。
3. 开始游戏，打开暂停页，再打开设置。关闭设置时确认玩法保持暂停。
4. 返回游戏后确认场景恢复。退出时先停止 UI，再释放暂停服务。

<<< ../../../Samples/DodgeTheCreeps/DodgeRoutes.cs{csharp}

宿主装配见[Godot 暂停](../godot/pause.md)。
