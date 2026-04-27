---
title: Godot UI 系统
description: 说明 GFramework.Godot 当前如何把 Game UI 契约接到 PackedScene、页面行为和层级 UI 接入路径上。
---

# Godot UI 系统

`GFramework.Godot.UI` 当前负责的是把 `GFramework.Game` 的 UI 路由契约接到 `Control` / `CanvasLayer` /
`PackedScene` 上，而不是再提供一套 Godot 专用路由器类型。

当前真正参与这条链路的核心类型是：

- `IGodotUiRegistry` / `GodotUiRegistry`
- `GodotUiFactory`
- `CanvasItemUiPageBehaviorBase<T>`
- `UiPageBehaviorFactory`
- `Page` / `Overlay` / `Modal` / `Toast` / `Topmost` 五类 layer behavior
- 项目侧实现的 `IUiRoot`
- 项目侧继承 `UiRouterBase` 的路由器

## 当前公开入口

### `IGodotUiRegistry`

Godot 侧 UI 资源表，底层是 `IAssetRegistry<PackedScene>`。它只负责：

- `uiKey -> PackedScene` 映射
- 让 `GodotUiFactory` 可以按 key 实例化 UI 页面

框架当前不会自动扫描 `.tscn`、不会自动根据类型名补全注册表。

### `GodotUiFactory`

`GodotUiFactory.Create(string uiKey)` 的当前行为比场景工厂更严格：

1. 从 `IGodotUiRegistry` 取出 `PackedScene`
2. 调用 `Instantiate()`
3. 节点必须实现 `IUiPageBehaviorProvider`
4. 返回 `provider.GetPage()`

如果实例化得到的节点没有实现 `IUiPageBehaviorProvider`，当前实现会直接抛 `InvalidCastException`。这也是 UI 页面文档必须强调
`GetPage()` / `[AutoUiPage]` 的原因。

### `CanvasItemUiPageBehaviorBase<T>`

Godot runtime 的页面行为包装基类。它把 `IUiPageBehavior` 的这些语义接到 `CanvasItem` 上：

- `Key`
- `Layer`
- `Handle`
- `IsAlive`
- `IsVisible`
- `InteractionProfile`
- `OnEnter` / `OnExit`
- `OnPause` / `OnResume`
- `OnShow` / `OnHide`
- `TryHandleUiAction(UiInputAction action)`

如果 owner 同时实现了 `IUiPage`、`IUiInteractionProfileProvider`、`IUiActionHandler`，这些契约都会被页面行为继续利用。

### `UiPageBehaviorFactory`

当前 layer 到 behavior 的映射来自运行时代码本身：

- `UiLayer.Page` -> `PageLayerUiPageBehavior<T>`
- `UiLayer.Overlay` -> `OverlayLayerUiPageBehavior<T>`
- `UiLayer.Modal` -> `ModalLayerUiPageBehavior<T>`
- `UiLayer.Toast` -> `ToastLayerUiPageBehavior<T>`
- `UiLayer.Topmost` -> `TopmostLayerUiPageBehavior<T>`

几个容易被旧文档写偏的默认语义如下：

- `Page`
  - 不可重入，阻断输入
- `Overlay`
  - 可重入，非模态，不阻断输入；暂停时不会停掉节点处理
- `Modal`
  - 可重入，模态，阻断输入
- `Toast`
  - 可重入，非模态，不阻断输入
- `Topmost`
  - 不可重入，模态，阻断输入

## 最小接入路径

### 1. 继续在项目层保留自己的路由器

仓库当前不存在 `GodotUiRouter` 类型。实际做法仍然是项目侧继承 `GFramework.Game.UI.UiRouterBase`。

最小形态通常就是：

```csharp
using LoggingTransitionHandler = GFramework.Game.UI.Handler.LoggingTransitionHandler;

namespace GameProject.UI;

[Log]
public partial class UiRouter : UiRouterBase
{
    protected override void RegisterHandlers()
    {
        _log.Debug("Registering default transition handlers");
        RegisterHandler(new LoggingTransitionHandler());
    }
}
```

Godot runtime 自身并不接管这层路由器定义。

### 2. 注册 `IGodotUiRegistry` 与 `IUiFactory`

最小 wiring 需要显式注册 UI 资源表和工厂：

```csharp
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.UI;
using Godot;

public sealed class GameUiRegistry : GodotUiRegistry
{
    public GameUiRegistry()
    {
        Register(nameof(UiKey.MainMenu), GD.Load<PackedScene>("res://ui/main_menu.tscn"));
        Register(nameof(UiKey.PauseMenu), GD.Load<PackedScene>("res://ui/pause_menu.tscn"));
        Register(nameof(UiKey.OptionsMenu), GD.Load<PackedScene>("res://ui/options_menu.tscn"));
    }
}

architecture.RegisterUtility<IGodotUiRegistry>(new GameUiRegistry());
architecture.RegisterUtility<IUiFactory>(new GodotUiFactory());
architecture.RegisterSystem(new UiRouter());
```

### 3. 提供 `IUiRoot`

`UiRouterBase` 只负责页面栈、layer UI、输入仲裁和暂停语义；真正把页面挂到 Godot 容器的是项目自己的 `IUiRoot`。

CoreGrid 当前的 `UiRoot` 做法和源码契约一致：

- 继承 `CanvasLayer`
- 为每个 `UiLayer` 创建一个 `Control` 容器
- 在 `_Ready()` 时调用 `_uiRouter.BindRoot(this)`
- 在 `AddUiPage` / `RemoveUiPage` 中处理 `CanvasItem` 挂载与释放

最小形态可以写成：

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
        if (child.View is not CanvasItem item)
            throw new InvalidOperationException("UIPage View must be a Godot Node");

        AddChild(item);
        item.ZIndex = (int)layer * 100 + orderInLayer;
    }

    public void RemoveUiPage(IUiPageBehavior child)
    {
        if (child.View is Node node && node.GetParent() == this)
            RemoveChild(node);
    }
}
```

### 4. 让页面节点提供 `GetPage()`

因为 `GodotUiFactory` 不会自动回退到默认 behavior，页面节点必须显式提供 `GetPage()`。

#### 方式 A：手写 `IUiPageBehaviorProvider`

```csharp
public partial class PauseMenu : Control, IUiPage, IUiPageBehaviorProvider
{
    private IUiPageBehavior? _page;

    public IUiPageBehavior GetPage()
    {
        return _page ??= UiPageBehaviorFactory.Create(this, nameof(UiKey.PauseMenu), UiLayer.Modal);
    }

    public void OnEnter(IUiPageEnterParam? param)
    {
    }

    public void OnExit()
    {
    }

    public void OnPause()
    {
    }

    public void OnResume()
    {
    }

    public void OnShow()
    {
    }

    public void OnHide()
    {
    }
}
```

#### 方式 B：用 `[AutoUiPage]` 让生成器补样板

当前更贴近真实消费者 wiring 的方式，是让生成器产出 `UiKeyStr` 和 `GetPage()`：

```csharp
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.SourceGenerators.Abstractions.UI;
using Godot;

[AutoUiPage(nameof(UiKey.MainMenu), nameof(UiLayer.Page))]
public partial class MainMenu : Control, IUiPageBehaviorProvider, IUiPage
{
    public void OnEnter(IUiPageEnterParam? param)
    {
    }

    public void OnExit()
    {
    }

    public void OnPause()
    {
    }

    public void OnResume()
    {
    }

    public void OnShow()
    {
    }

    public void OnHide()
    {
    }
}
```

当前生成器补出的核心样板与源码一致：

```csharp
public IUiPageBehavior GetPage()
{
    return __autoUiPageBehavior_Generated ??=
        UiPageBehaviorFactory.Create(this, UiKeyStr, UiLayer.Page);
}
```

要注意两点：

- `[AutoUiPage]` 不会替你自动补 `: IUiPageBehaviorProvider`
- UI 层级是生成器输入的一部分；`Page` / `Modal` / `Overlay` 语义不是后面再猜出来的

### 5. 按 layer 选择正确入口

Godot runtime 只是落地 `UiRouterBase` 的语义，因此入口仍然和 `GFramework.Game` 一致：

页面栈：

```csharp
await uiRouter.ReplaceAsync(nameof(UiKey.MainMenu));
await uiRouter.PushAsync(nameof(UiKey.Settings));
await uiRouter.PopAsync();
```

层级 UI：

```csharp
var handle = uiRouter.Show(nameof(UiKey.PauseMenu), UiLayer.Modal);
uiRouter.Hide(handle, UiLayer.Modal);
```

当前实现里，`Show(..., UiLayer.Page)` 会直接抛异常；`Page` 层必须走 `PushAsync` / `ReplaceAsync`。

## 输入与暂停语义

如果页面只实现 `IUiPage`，它只有基础生命周期。

如果还需要更强的输入仲裁或暂停语义，可以像 CoreGrid 的 `PauseMenu` 一样继续实现：

- `IUiInteractionProfileProvider`
- `IUiActionHandler`

当前这条链路是成立的：

1. 页面行为从 owner 读取 `UiInteractionProfile`
2. router 根据 profile 判断动作捕获、世界输入阻断和暂停策略
3. 如果页面实现了 `IUiActionHandler`，`TryHandleUiAction(...)` 会继续下沉到页面

这也是为什么 `PauseMenu` 一类 modal 页面可以声明：

- 捕获 `Cancel`
- 阻断 World pointer / action input
- 在可见时持有暂停
- 即使在暂停状态也继续处理节点逻辑

## 当前边界

### 没有 `GodotUiRouter`

仓库当前没有这个类型。旧文档把它写成默认入口是不准确的；真实入口仍然是项目侧的 `UiRouterBase` 派生类。

### UI 工厂不会自动补 behavior

和 `GodotSceneFactory` 不同，`GodotUiFactory` 当前不会按节点类型自动创建 behavior。节点不实现
`IUiPageBehaviorProvider` 时会直接失败。

### `Page` 层不是 `Show(...)` 的适用对象

`UiLayer.Page` 代表页面栈语义，而不是普通 layer UI。当前实现明确要求：

- `Page` 用 `PushAsync` / `ReplaceAsync`
- `Overlay` / `Modal` / `Toast` / `Topmost` 用 `Show` / `Hide`

### root 仍然由项目控制

`IUiRoot` 决定：

- 每个 layer 是否拆独立容器
- 层内排序怎么算
- 页面移除时如何释放节点

Godot runtime 不会替项目自动生成统一 UI 根节点。

## 继续阅读

1. [Godot 运行时集成](./index.md)
2. [Game UI 系统](../game/ui.md)
3. [AutoUiPage 生成器](../source-generators/auto-ui-page-generator.md)
4. [Godot 架构集成](./architecture.md)
