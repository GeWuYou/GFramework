---
title: Godot UI 系统
description: 用普通 Control、强类型注册和默认原生宿主接入 GFramework UI。
---

# Godot UI 系统

Godot 接入由 `GodotUiRoot`、`GodotUiView`、注册扩展和 `GodotUiInputBridge` 组成。页面继续使用普通 `Control` 场景，导航和实例所有权交给 [通用 UI 服务](../game/ui.md)。

## 创建宿主

在 Godot 主线程、场景树就绪后创建并挂载 root：

```csharp
using System;
using System.Threading;
using GFramework.Core.Pause;
using GFramework.Game.Runtime;
using GFramework.Game.UI;
using GFramework.Godot.UI;

var root = new GodotUiRoot();
AddChild(root);
var context = SynchronizationContext.Current
    ?? throw new InvalidOperationException("Create UI on the Godot thread.");
var dispatcher = new SynchronizationContextGameDispatcher(context);
var pauses = new PauseStackManager();
var routes = new UiRegistry();
```

root 为每个显示层提供原生容器，处理模态遮罩、挂载顺序、焦点保存和恢复。宿主保持 root 存活，直到 UI 停止完成。

## 注册普通场景

```csharp
using GFramework.Game.Abstractions.UI;
using Godot;

var settingsRoute = new UiRoute<float>("settings", new UiRouteOptions
{
    Kind = UiRouteKind.Window,
    Layer = 20,
    Modal = true,
    BlocksWorldPointerInput = true,
    BlocksWorldActionInput = true,
    PauseWhileVisible = true
});
var settingsScene = GD.Load<PackedScene>("res://ui/settings.tscn");
routes.RegisterGodotScene(settingsRoute, settingsScene, (control, volume, viewContext) =>
{
    var settings = (SettingsPanel)control;
    settings.Bind(volume, viewContext);
});
var ui = new UiService(routes, root, dispatcher, pauses);
await ui.ShowAsync(settingsRoute, 0.7f);
```

`SettingsPanel` 是项目的普通 `Control` 脚本，`Bind` 是项目自己的参数接收方法。场景根必须是 `Control`。参数绑定发生在挂载前，访问子节点的工作放在 `_Ready` 或打开生命周期中。

代码构造的 UI 可使用 `RegisterGodot(route, (args, context) => control)`。每次工厂调用返回一个未挂载实例，所有权转交给服务；创建函数在返回前抛错时应释放已创建节点。

## 对话框

```csharp
var confirm = new UiDialogRoute<string, bool>("confirm");
routes.RegisterGodotDialog(confirm, (question, dialog) =>
{
    var panel = new VBoxContainer();
    panel.AddChild(new Label { Text = question });
    var yes = new Button { Text = "Confirm" };
    var cancel = new Button { Text = "Cancel" };
    yes.Pressed += () => RunAction(() => dialog.CompleteAsync(true));
    cancel.Pressed += () => RunAction(() => dialog.CloseAsync());
    panel.AddChild(yes);
    panel.AddChild(cancel);
    return panel;
});
```

注册发生在创建 `UiService` 前。`RunAction` 是宿主观察异步按钮操作的入口：保存任务，展示失败，并在退出时等待在途工作。完整实现见 [官方素材小游戏](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps)。

```csharp
var result = await ui.ShowDialogAsync(confirm, "Return to menu?", cancellationToken);
if (result.Accepted && result.Value == true)
    await ReturnToMenuAsync();
```

对话框的显示策略可像普通窗口一样显式配置。需要阻断世界输入或暂停时，在路由选项中声明并提供共享暂停管理器。

## 可选生命周期

普通场景无需实现框架接口。需要业务订阅时实现 `IGodotUiLifecycle`：

```csharp
public Task OpenAsync(CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    SubscribeToInventory();
    RefreshInventory();
    return Task.CompletedTask;
}

public Task CloseAsync(CancellationToken cancellationToken = default)
{
    UnsubscribeFromInventory();
    return Task.CompletedTask;
}
```

打开时节点已挂载但尚未展示；关闭时取消业务订阅。服务通过租约回收节点，页面脚本无需在关闭回调中释放自己。需要交互状态通知时实现 `IGodotUiActivation` 的 `OnActivated` 和 `OnDeactivated`。

`GodotUiView` 保存并恢复控件已有的鼠标和焦点设置，处理暂停期间继续交互。视图自己的处理开关保持由项目脚本管理。

## 输入接入

`GodotUiInputBridge` 在原生 GUI 处理后接收未处理的取消动作：

```csharp
var bridge = new GodotUiInputBridge
{
    Service = ui,
    CancelAction = "ui_cancel",
    RunAction = RunAction,
    UnhandledCancelAsync = OpenPauseMenuAsync
};
AddChild(bridge);
```

按键、焦点邻居和控件导航继续使用 Godot 设置。世界帧更新或快捷键流程查询 `ui.Snapshot` 中的对应输入阻断状态。共享暂停管理器通过 `GodotPauseHandler` 连接场景树，见 [Godot 暂停](./pause.md)。

## 标准 DI 与 Architecture

```csharp
services.AddGamePause();
services.AddGameUi(routes, root, dispatcher);
```

`AddGameUi` 创建容器拥有的单例服务，宿主 root 和 dispatcher 的所有权保留在调用方。需要 Architecture 编排时安装 `GameRuntimeModule(dispatcher, configure)`，在其注册回调中执行相同登记。服务先停止，宿主随后分离。

## 迁移

将旧 UI registry、工厂和路由派生类替换为一处强类型注册。移除页面包装生成特性和空生命周期方法。打开页面使用 `PushAsync`／`ReplaceAsync`，打开窗口使用 `ShowAsync`，关闭使用返回句柄或视图上下文。

## 相关主题

- [通用 UI 服务](../game/ui.md)
- [Godot 场景](./scene.md)
- [Godot 暂停](./pause.md)
- [运行时集成示例](./runtime-sample.md)

## 可编译的路由示例

以下定义直接引用小游戏源码，注册和调用复用同一份定义：

<<< ../../../Samples/DodgeTheCreeps/DodgeRoutes.cs{csharp}
