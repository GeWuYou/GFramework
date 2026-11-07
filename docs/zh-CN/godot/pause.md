---
title: Godot 暂停系统
description: 将共享暂停服务接到场景树，正确处理嵌套菜单与宿主停止。
---

# Godot 暂停系统

`GodotPauseHandler` 将 `PauseStackManager` 的暂停状态应用到 Godot 场景树。管理器是普通实例服务，UI 与业务流程共享同一份状态。

## 接入

在宿主线程创建管理器并登记处理器：

```csharp
using GFramework.Core.Pause;
using GFramework.Godot.Pause;

var pauses = new PauseStackManager();
pauses.RegisterHandler(new GodotPauseHandler(GetTree()));
```

将它传给 `UiService(routes, root, dispatcher, pauses)`。标准 DI 使用 `AddGamePause`，按服务解析同一实例并连接场景树。使用 `GameRuntimeModule(dispatcher, configure)` 时，模块通过显式 dispatcher 完成原生暂停处理器的停止通知。

## 菜单暂停策略

```csharp
var pauseRoute = new UiRoute<int>("pause", new UiRouteOptions
{
    Kind = UiRouteKind.Window,
    Layer = 20,
    Modal = true,
    BlocksWorldActionInput = true,
    BlocksWorldPointerInput = true,
    PauseWhileVisible = true,
    ContinueProcessingWhenPaused = true
});
```

显示菜单时服务持有一份暂停请求。设置菜单声明相同的暂停策略后，两个窗口分别持有请求；关闭设置只释放自己的请求，暂停菜单仍保持游戏暂停。

普通 UI 可以在暂停期间继续响应输入。世界节点使用 Godot 正常可暂停的处理模式；按项目需要配置协程或其他子系统对暂停组的响应。

## 直接业务暂停

```csharp
var token = pauses.Push("Blocking gameplay flow");
try
{
    await CompleteGameplayFlowAsync();
}
finally
{
    pauses.Pop(token);
}
```

业务通过自己的令牌结束自己的请求。`ClearAll` 用于明确重置，不作为普通菜单关闭操作。

## 停止顺序

先停止 UI 和世界业务，让各自释放请求，再在 Godot 主线程等待暂停管理器 `DisposeAsync`。主线程要求由宿主共享 dispatcher 承担。随后分离原生节点并退出场景树。

## 相关主题

- [通用暂停服务](../core/pause.md)
- [Godot UI](./ui.md)
- [暂停实践](../tutorials/pause-system.md)
