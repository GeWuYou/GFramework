---
title: 暂停系统实践
description: 通过共享暂停服务和声明式 UI 策略实现可组合的暂停与嵌套菜单。
---

# 暂停系统实践

暂停由多个独立原因共同决定：玩家菜单、设置窗口或阻塞业务都可以持有自己的请求。`PauseStackManager` 负责记录请求，宿主处理器负责把状态应用到实际游戏。

## 创建共享服务

```csharp
using GFramework.Core.Pause;
using GFramework.Godot.Pause;

var pauses = new PauseStackManager();
pauses.RegisterHandler(new GodotPauseHandler(GetTree()));
```

这是组合根中长期存活的实例。将相同管理器传入 `UiService` 和需要暂停的业务服务，原生状态变化在宿主线程上执行。

## 声明菜单策略

暂停菜单与设置菜单分别使用 Window 路由，在 `UiRouteOptions` 中声明 `PauseWhileVisible = true`、`ContinueProcessingWhenPaused = true`，并按业务需要声明模态和输入阻断。

可编译样例中的路由定义如下：

<<< ../../../Samples/DodgeTheCreeps/DodgeRoutes.cs{csharp}

打开暂停菜单后再打开设置，两者各持有一个请求。`BackAsync` 关闭顶层设置，暂停菜单仍然持有请求；再关闭暂停菜单，游戏恢复。页面无需自行跟踪嵌套深度或复原整个场景树状态。

## 业务请求

```csharp
var token = pauses.Push("Blocking gameplay flow");
try
{
    await RunBlockingGameplayStepAsync();
}
finally
{
    pauses.Pop(token);
}
```

每个调用方只释放自己的令牌。短作用域可使用 `PauseScope`。需要独立暂停子系统时使用明确的 `PauseGroup`，让对应处理器决定受影响对象。

## 观察和关闭

`IsPaused`、`GetPauseDepth`、`GetPauseReasons` 与标准事件提供观察入口。UI 状态通过 `IUiService.Snapshot` 检查，输入阻断和游戏暂停是各自独立的策略。

关闭时先停止 UI 和业务请求，再在宿主线程等待暂停管理器释放。使用标准 DI 和 Architecture 时，通过 `GameRuntimeModule(dispatcher, configure)` 将此顺序接入宿主生命周期。

## 采用验收

运行 [小游戏工程](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps)，依次执行开始、暂停、打开设置、关闭设置、恢复游戏。检查世界移动和计分在暂停期间停止，设置仍能操作，关闭顶层窗口保持下层暂停。

## 相关主题

- [通用暂停服务](../core/pause.md)
- [Godot 暂停](../godot/pause.md)
- [UI 系统](../game/ui.md)
