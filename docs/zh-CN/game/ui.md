---
title: UI 系统
description: 使用强类型路由、显式宿主和异步操作组织页面、窗口、对话框与暂停。
---

# UI 系统

`UiService` 管理页面历史、窗口顺序、输入仲裁和视图释放。它通过构造函数接收 `UiRegistry`、`IUiHost`、`IGameDispatcher`，可直接采用，也可通过标准 DI 或可选 Architecture 模块接入。

## 路由定义

路由定义由注册和调用方共享，参数由编译器检查：

```csharp
using GFramework.Game.Abstractions.UI;

public sealed record MenuArgs(string Title);
public sealed record SettingsArgs(float Volume);

public static class Routes
{
    public static readonly UiRoute<MenuArgs> Menu = new("menu");
    public static readonly UiRoute<SettingsArgs> Settings = new("settings", new UiRouteOptions
    {
        Kind = UiRouteKind.Window,
        Layer = 20,
        Modal = true,
        BlocksWorldPointerInput = true,
        BlocksWorldActionInput = true,
        PauseWhileVisible = true
    });
    public static readonly UiDialogRoute<string, bool> Confirm = new("confirm", new UiRouteOptions
    {
        Kind = UiRouteKind.Window,
        Layer = 30,
        Modal = true,
        BlocksWorldPointerInput = true,
        BlocksWorldActionInput = true
    });
}
```

`Kind` 决定页面或独立窗口。`Layer` 只决定显示顺序；`Modal`、世界输入阻断、暂停和多实例分别声明。窗口默认单实例，再次显示会返回已有句柄，沿用原有参数；需要新参数时关闭后重新打开。`AllowMultiple = true` 显式开启多个窗口实例。

页面的 `PushAsync` 每次创建历史条目。`ReplaceAsync` 替换页面历史，独立窗口继续存在。`AllowBack` 控制默认返回是否能关闭该视图。

## 注册与构造

在组合根创建一次注册表，登记项目自己的视图工厂：

```csharp
using System.Threading.Tasks;
using GFramework.Game.UI;

var routes = new UiRegistry();
routes.Register(Routes.Menu, (args, context, token) =>
    Task.FromResult<IUiView>(new MenuView(args, context)));
routes.Register(Routes.Settings, (args, context, token) =>
    Task.FromResult<IUiView>(new SettingsView(args, context)));
routes.Register(Routes.Confirm, (question, context, token) =>
    Task.FromResult<IUiView>(new ConfirmView(question, context)));

await using var ui = new UiService(routes, host, dispatcher, pauseManager);
await ui.ReplaceAsync(Routes.Menu, new MenuArgs("Dodge the Creeps!"));
```

`MenuView`、`SettingsView` 和 `ConfirmView` 是项目提供的 `IUiView`；`host` 和 `dispatcher` 由宿主提供。注册使用相同的路由定义实例，服务构造时复制注册表。重复 key 在登记时失败。带 `PauseWhileVisible` 的路由需要传入暂停服务。

纯托管宿主可使用 `InlineGameDispatcher`，原生宿主使用共享的线程调度器。Godot 的普通 `Control` 接入见 [Godot UI](../godot/ui.md)。可构建工程见 [官方素材小游戏](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps)。

## 日常操作

```csharp
await ui.PushAsync(Routes.Menu, new MenuArgs("Another page"), cancellationToken);
var opened = await ui.ShowAsync(Routes.Settings, new SettingsArgs(0.7f), cancellationToken);
if (opened.Handle is { } handle)
    await ui.CloseAsync(handle, cancellationToken);
await ui.BackAsync(cancellationToken);
```

- `PushAsync`、`ReplaceAsync` 操作页面。
- `ShowAsync` 操作窗口、HUD 和提示等独立 UI。
- `CloseAsync` 通过句柄关闭准确实例，无需重复传层级。
- `BackAsync` 先处理最上层窗口；没有窗口时处理最上层页面。禁用返回的顶层视图会阻止默认返回继续穿透。
- `ClearAsync` 在全部关闭守卫接受后关闭页面和窗口。

普通操作返回 `UiOperationResult`，其中 `Status` 为 `Applied`、`Rejected` 或 `NoOp`，`Handle` 是相关实例。跨服务的句柄会被拒绝。取消使用标准取消异常，运行失败通过异常返回。

## 对话框结果

```csharp
var answer = await ui.ShowDialogAsync(Routes.Confirm, "Return to menu?", cancellationToken);
if (answer.Accepted && answer.Value == true)
    await ReturnToMenuAsync();
```

工厂收到 `UiDialogContext<TResult>`。确认按钮调用 `CompleteAsync(result)`，取消按钮调用 `CloseAsync()`。结果在视图关闭和清理完成后返回；`Accepted` 区分业务提交与未接受结果；用户关闭或打开守卫拒绝时返回未接受结果。

等待用户操作不占用修改队列，因此仍可打开其他 UI 或关闭宿主。调用方取消会关闭本次对话框，完成清理后取消等待任务。

## 视图生命周期与所有权

`IUiView` 是服务拥有的视图租约：

| 成员 | 责任 |
| --- | --- |
| `OpenAsync` | 在已挂载且隐藏时准备视图、订阅业务事件 |
| `SetVisible` | 修改可见性 |
| `SetActive` | 修改交互资格 |
| `CloseAsync` | 取消业务订阅，容忍部分打开状态 |
| `DisposeAsync` | 释放租约拥有的视图资源 |

业务参数在工厂中绑定，`UiViewContext` 提供本实例句柄和关闭操作。普通按钮通过它关闭自己，无需查找全局服务。宿主 `IUiHost` 负责挂载、顺序和焦点，释放仍由视图租约承担。

可见、可交互和游戏暂停分别管理。页面被覆盖可保留历史；关闭默认释放，不保留隐藏缓存。工厂在返回租约之前失败时，负责回收自己创建的部分资源。

## 输入、守卫与诊断

`DispatchActionAsync` 把 `Cancel`、`Confirm` 等语义动作交给最高优先级的合格视图。视图可实现 `IUiActionTarget`；未处理的 `Cancel` 使用正常返回规则。控件级键盘导航和点击由宿主原生 UI 处理。

通过构造参数登记 `IUiGuard`，分别检查打开和关闭。拒绝保留原状态。`Snapshot` 提供不可变的视图、忙碌和停止状态，以及 `BlocksWorldPointerInput`、`BlocksWorldActionInput`；世界输入流程应查询对应阻断状态。

所有修改操作串行执行，工厂、生命周期和宿主操作经过同一调度器。生命周期回调不能等待同一服务的嵌套导航。正常事件回调可以等待自己上下文的关闭或对话框完成。

## 取消与停止

令牌可取消排队和准备；已开始的关闭与释放会被完整等待。打开失败清理目标并尽可能恢复旧视图。`StopAsync` 立即关闭接纳、等待在途操作，再绕过用户守卫清理所有视图。重复停止共享同一结果；一个清理错误不会跳过其他视图。

先等待 UI 停止，再释放原生 root、调度器和暂停服务。`DisposeAsync` 连接相同的停止过程。

## 迁移

旧页面行为提供者、层级行为工厂和路由子类改为 `UiRegistry`、普通视图及默认宿主。定义一次路由策略，调用时使用强类型参数和实例句柄。页面业务不再需要生成页面包装方法。

## 相关主题

- [Godot UI](../godot/ui.md)
- [场景系统](./scene.md)
- [暂停系统](../core/pause.md)
