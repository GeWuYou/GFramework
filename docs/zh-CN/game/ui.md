---
title: UI 系统
description: 说明 GFramework.Game UI 路由当前的页面栈、层级 UI、输入语义与项目侧接入方式。
---

# UI 系统

`GFramework.Game` 的 UI 系统不是单纯的“页面栈”。按当前实现，它同时覆盖：

- `UiLayer.Page` 的页面导航
- `Overlay` / `Modal` / `Toast` / `Topmost` 的层级 UI
- UI 语义动作捕获与分发
- World 输入阻断
- 由 UI 可见性驱动的暂停语义

因此，新的接入文档不应再把它写成“只有 Push/Pop 的传统页面管理器”。

## 当前公开入口

### `IUiPage`

最轻量的页面生命周期契约，暴露：

- `OnEnter`
- `OnExit`
- `OnPause`
- `OnResume`
- `OnShow`
- `OnHide`

如果你的页面逻辑只想表达这些生命周期阶段，停留在 `IUiPage` 就够了。

### `IUiPageBehavior`

路由器真正操作的运行时页面行为。相比 `IUiPage`，它还携带：

- `Key`
- `Layer`
- `Handle`
- `View`
- `IsAlive`
- `IsVisible`
- `IsModal`
- `BlocksInput`
- `InteractionProfile`
- `TryHandleUiAction(UiInputAction action)`

也就是说，页面栈和层级 UI 都是围绕 `IUiPageBehavior` 工作的，而不是只围绕 `IUiPage`。

### `IUiRouter`

当前最常用的入口分成两组。

页面栈：

- `PushAsync(...)`
- `ReplaceAsync(...)`
- `PopAsync(...)`
- `ClearAsync()`
- `Peek()`
- `PeekKey()`

层级 UI：

- `Show(...)`
- `Hide(...)`
- `Resume(...)`
- `ClearLayer(...)`
- `HideByKey(...)`
- `GetAllFromLayer(...)`

输入与阻断：

- `GetUiActionOwner(UiInputAction action)`
- `TryDispatchUiAction(UiInputAction action)`
- `BlocksWorldPointerInput()`
- `BlocksWorldActionInput()`

### `UiLayer`

当前层级语义如下：

- `Page`
  - 页面栈层。请用 `PushAsync` / `ReplaceAsync`，不要用 `Show(...)`。
- `Overlay`
  - 可叠加的浮层。
- `Modal`
  - 默认阻断下层输入的模态层。
- `Toast`
  - 轻量提示层。
- `Topmost`
  - 最顶层的系统级 UI。

### `UiTransitionPolicy` 与 `UiPopPolicy`

页面栈的两个关键策略：

- `UiTransitionPolicy.Exclusive`
  - 新页面独占显示，下层页面会 `Pause + Hide`
- `UiTransitionPolicy.Overlay`
  - 新页面覆盖显示，下层页面只 `Pause`
- `UiPopPolicy.Destroy`
  - 弹出时直接销毁页面实例
- `UiPopPolicy.Suspend`
  - 弹出时保留页面实例，供后续恢复

## UI 路由的真实语义

### 页面栈和层级 UI 是两套入口

当前源码里：

- `Page` 层属于栈语义，用 `PushAsync` / `ReplaceAsync` / `PopAsync`
- `Overlay`、`Modal`、`Toast`、`Topmost` 属于层级语义，用 `Show` / `Hide` / `Resume`

`Show(..., UiLayer.Page)` 在当前实现里会直接抛异常，因此旧文档里那种“所有 UI 都统一通过 Show 进入”的写法不再准确。

### 输入不是页面自己抢，而是 router 先仲裁

`UiInteractionProfile` 用来描述页面的交互契约，例如：

- 捕获哪些 `UiInputAction`
- 是否阻断 World 指针输入
- 是否阻断 World 语义动作输入
- 页面可见时是否推动暂停栈

输入层先把设备输入映射成 `UiInputAction`，再交给 `IUiRouter.TryDispatchUiAction(...)`。最终谁拥有动作捕获权，由当前可见页面和层级顺序决定。

### 页面可见性会影响暂停与阻断

这也是 UI 系统和普通页面栈最不同的地方之一。当前实现里：

- `Modal` / `Topmost` 默认具有更强的输入阻断语义
- 页面的 `InteractionProfile` 可以驱动暂停栈
- `BlocksWorldPointerInput()` 与 `BlocksWorldActionInput()` 是给项目输入层做统一判断的

如果你的项目有“打开设置页后暂停世界”“Modal 打开时地图点击失效”这类需求，优先接这个契约，而不是每个页面自己散落地写输入屏蔽逻辑。

## 最小接入路径

推荐按下面的顺序接入。

### 推荐目录与文件约定（项目侧）

UI 系统的接入文件建议按“路由、工厂、根节点、页面行为、入参”拆开。这样可以让 `UiRouterBase` 只承担编排职责，
把引擎节点创建和页面业务逻辑留在项目侧。

```text
Game/UI/
  GameUiRouter.cs
  GameUiFactory.cs
  UiRoot.cs
  Pages/
    MainMenuPageBehavior.cs
    SettingsPageBehavior.cs
  Params/
    SettingsEnterParam.cs
  Views/
    MainMenuView.cs
```

推荐约定如下：

- `GameUiRouter.cs`：项目侧 router，继承 `UiRouterBase`，只注册 UI transition handler 与 guard
- `GameUiFactory.cs`：实现 `IUiFactory`，负责 `uiKey -> IUiPageBehavior` 的映射与实例创建
- `UiRoot.cs`：实现 `IUiRoot`，负责按 `UiLayer` 把页面行为挂到真实 UI 容器
- `Pages/*PageBehavior.cs`：放实现 `IUiPageBehavior` 的页面行为；使用 Godot 生成器时可由 `AutoUiPage` 相关样板补齐
- `Params/*EnterParam.cs`：放实现 `IUiPageEnterParam` 的页面入参
- `Views/*`：放项目引擎层视图包装或节点引用，不建议把导航决策写在视图里

最小 wiring 通常是：

```csharp
architecture.RegisterUtility<IUiFactory>(new GameUiFactory());
architecture.RegisterSystem(new GameUiRouter());
```

随后在 `UiRoot` 的引擎生命周期就绪点调用 `_uiRouter.BindRoot(this)`。如果项目已经按功能域组织 UI 文件，也可以保留
原目录；关键是让 `*Router` 只做编排、`*Factory` 只做映射与创建、`*Root` 只做容器挂载，页面行为只表达页面自身语义。

### 1. 提供项目自己的 router

```csharp
using GFramework.Game.UI;
using LoggingTransitionHandler = GFramework.Game.UI.Handler.LoggingTransitionHandler;

public sealed class GameUiRouter : UiRouterBase
{
    protected override void RegisterHandlers()
    {
        RegisterHandler(new LoggingTransitionHandler());
    }
}
```

### 2. 提供 `IUiFactory`

`UiRouterBase` 会通过 `IUiFactory.Create(string uiKey)` 获取页面行为实例，因此项目需要自己决定：

- `uiKey` 如何映射到页面行为
- 页面行为如何包裹具体引擎视图
- 预挂载节点、调试节点或动态实例化页面如何接入

如果你在 Godot 项目里使用 `AutoUiPage` 相关生成器，它可以帮你减少部分行为样板，但 factory / root / 实际页面注册仍然是项目职责。

### 3. 提供 `IUiRoot`

`IUiRoot` 负责把页面行为挂进真实 UI 容器：

- `AddUiPage(IUiPageBehavior child)`
- `AddUiPage(IUiPageBehavior child, UiLayer layer, int orderInLayer = 0)`
- `RemoveUiPage(IUiPageBehavior child)`

当前 `ai-libs/` 的参考实现就是在项目自己的 `CanvasLayer` 上为每个 `UiLayer` 建独立容器，再在 `_Ready()` 时执行
`_uiRouter.BindRoot(this)`。

### 4. 装配 router 与 factory

```csharp
architecture.RegisterUtility<IUiFactory>(new GameUiFactory());
architecture.RegisterSystem(new GameUiRouter());
```

### 5. 在 root 就绪后绑定

```csharp
public sealed class UiRoot : CanvasLayer, IUiRoot
{
    [GetSystem] private IUiRouter _uiRouter = null!;

    public override void _Ready()
    {
        __InjectContextBindings_Generated();
        _uiRouter.BindRoot(this);
    }

    public void AddUiPage(IUiPageBehavior child)
    {
        AddUiPage(child, UiLayer.Page);
    }

    public void AddUiPage(IUiPageBehavior child, UiLayer layer, int orderInLayer = 0)
    {
        // 项目侧决定如何把 child.View 挂到具体容器
    }

    public void RemoveUiPage(IUiPageBehavior child)
    {
        // 项目侧决定如何移除并释放视图
    }
}
```

### 6. 从业务代码区分两类入口

页面栈：

```csharp
await uiRouter.ReplaceAsync("MainMenu");
await uiRouter.PushAsync("Settings", new SettingsEnterParam());
await uiRouter.PopAsync(UiPopPolicy.Destroy);
```

层级 UI：

```csharp
var modalHandle = uiRouter.Show(
    "ConfirmExit",
    UiLayer.Modal,
    new ConfirmExitParam());

uiRouter.Hide(modalHandle, UiLayer.Modal);
```

## 扩展点

### 路由守卫

如果你要在进入或离开页面前做业务检查，实现 `IUiRouteGuard`：

- `CanEnterAsync(string uiKey, IUiPageEnterParam? param)`
- `CanLeaveAsync(string uiKey)`

适合放：

- 未保存设置拦截
- 新手引导期间禁用某些页面跳转
- 多层弹窗切换前的业务确认

### 过渡处理器

`IUiRouter` 当前公开的是：

- `RegisterHandler(IUiTransitionHandler handler, UiTransitionHandlerOptions? options = null)`
- `UnregisterHandler(IUiTransitionHandler handler)`

适合放：

- UI 转场动画
- 统一日志
- 栈变化埋点

### 输入适配层

如果项目已经有自己的输入系统，推荐把它适配成：

1. 设备输入 -> `UiInputAction`
2. `IUiRouter.TryDispatchUiAction(...)`
3. 若未被 UI 捕获，再决定是否把输入继续交给 World

这样可以直接复用当前路由器的动作捕获与阻断语义。

## 与旧写法的边界

以下说法不再适合作为默认指导：

- “所有 UI 都统一通过一个 Show API 管理”
- “UI 系统只有页面栈，不涉及输入阻断和暂停语义”
- “Modal / Topmost 只是视觉层级，不影响交互”

当前更准确的理解是：

- 页面栈和层级 UI 是两套入口
- 页面行为不仅有生命周期，还有输入、阻断、暂停契约
- router 是 UI 语义仲裁中心，项目输入层应主动接入它

## 推荐阅读

1. [Game 模块总览](./index.md)
2. [场景系统](./scene.md)
3. [AutoUiPage 生成器](../source-generators/auto-ui-page-generator.md)
4. [Game 抽象层说明](../abstractions/game-abstractions.md)
5. [API 参考](../api-reference/index.md)
