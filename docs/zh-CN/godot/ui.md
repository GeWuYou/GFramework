---
title: Godot UI 系统
description: Godot UI 系统提供了 GFramework UI 管理与 Godot Control 节点的完整集成。
---

# Godot UI 系统

## 概述

Godot UI 系统是 GFramework.Godot 中连接框架 UI 管理与 Godot Control 节点的核心组件。它提供了 UI 页面行为封装、UI 工厂、UI
注册表等功能，支持多层级 UI 显示，让你可以在 Godot 项目中使用 GFramework 的 UI 管理系统。

通过 Godot UI 系统，你可以使用 GFramework 的 UI 路由、生命周期管理、多层级显示等功能，同时保持与 Godot UI 系统的完美兼容。

**主要特性**：

- UI 页面行为封装
- UI 工厂和注册表
- 与 Godot PackedScene 集成
- 多层级 UI 支持（Page、Overlay、Modal、Toast、Topmost）
- UI 生命周期管理
- UI 根节点管理

## 核心概念

### UI 页面行为

`CanvasItemUiPageBehaviorBase<T>` 封装了 Godot Control 节点的 UI 行为：

```csharp
public abstract class CanvasItemUiPageBehaviorBase<T> : IUiPageBehavior
    where T : CanvasItem
{
    protected readonly T Owner;
    public string Key { get; }
    public UiLayer Layer { get; }
    public bool IsReentrant { get; }
}
```

### UI 工厂

`GodotUiFactory` 负责创建 UI 实例：

```csharp
public class GodotUiFactory : IUiFactory
{
    public IUiPageBehavior Create(string uiKey);
}
```

### UI 层级行为

不同层级的 UI 有不同的行为类：

```csharp
// Page 层（栈管理）
public class PageLayerUiPageBehavior : CanvasItemUiPageBehaviorBase<Control>
{
    public override UiLayer Layer => UiLayer.Page;
    public override bool IsReentrant => false;
}

// Modal 层（模态对话框）
public class ModalLayerUiPageBehavior : CanvasItemUiPageBehaviorBase<Control>
{
    public override UiLayer Layer => UiLayer.Modal;
    public override bool IsReentrant => true;
}
```

## 基本用法

### 创建 UI 脚本

```csharp
using Godot;
using GFramework.Game.Abstractions.ui;

public partial class MainMenuPage : Control, IUiPage
{
    public void OnEnter(IUiPageEnterParam? param)
    {
        GD.Print("进入主菜单");
        Show();
    }

    public void OnExit()
    {
        GD.Print("退出主菜单");
        Hide();
    }

    public void OnPause()
    {
        GD.Print("暂停主菜单");
    }

    public void OnResume()
    {
        GD.Print("恢复主菜单");
    }

    public void OnShow()
    {
        Show();
    }

    public void OnHide()
    {
        Hide();
    }
}
```

### 实现 UI 页面行为提供者

```csharp
using Godot;
using GFramework.Game.Abstractions.ui;
using GFramework.Godot.ui;

public partial class MainMenuPage : Control, IUiPageBehaviorProvider
{
    private PageLayerUiPageBehavior _behavior;

    public override void _Ready()
    {
        _behavior = new PageLayerUiPageBehavior(this, "MainMenu");
    }

    public IUiPageBehavior GetPage()
    {
        return _behavior;
    }
}
```

### 注册 UI

```csharp
using GFramework.Godot.ui;
using Godot;

public class GameUiRegistry : GodotUiRegistry
{
    public GameUiRegistry()
    {
        // 注册 UI 资源
        Register("MainMenu", GD.Load<PackedScene>("res://ui/MainMenu.tscn"));
        Register("Settings", GD.Load<PackedScene>("res://ui/Settings.tscn"));
        Register("ConfirmDialog", GD.Load<PackedScene>("res://ui/ConfirmDialog.tscn"));
        Register("Toast", GD.Load<PackedScene>("res://ui/Toast.tscn"));
    }
}
```

### 设置 UI 系统

```csharp
using GFramework.Godot.architecture;
using GFramework.Godot.ui;

public class GameArchitecture : AbstractArchitecture
{
    protected override void InstallModules()
    {
        // 注册 UI 注册表
        var uiRegistry = new GameUiRegistry();
        RegisterUtility<IGodotUiRegistry>(uiRegistry);

        // 注册 UI 工厂
        var uiFactory = new GodotUiFactory();
        RegisterUtility<IUiFactory>(uiFactory);

        // 注册 UI 路由
        var uiRouter = new GodotUiRouter();
        RegisterSystem<IUiRouter>(uiRouter);
    }
}
```

### 使用 UI 路由

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class GameController : Node
{
    public override void _Ready()
    {
        ShowMainMenu();
    }

    private async void ShowMainMenu()
    {
        var uiRouter = this.GetSystem<IUiRouter>();
        await uiRouter.PushAsync("MainMenu");
    }

    private async void ShowSettings()
    {
        var uiRouter = this.GetSystem<IUiRouter>();
        await uiRouter.PushAsync("Settings");
    }

    private void ShowDialog()
    {
        var uiRouter = this.GetSystem<IUiRouter>();
        uiRouter.Show("ConfirmDialog", UiLayer.Modal);
    }

    private void ShowToast(string message)
    {
        var uiRouter = this.GetSystem<IUiRouter>();
        uiRouter.Show("Toast", UiLayer.Toast, new ToastParam { Message = message });
    }
}
```

## 高级用法

### 不同层级的 UI 行为

```csharp
// Page 层 UI（栈管理，不可重入）
public partial class MainMenuPage : Control, IUiPageBehaviorProvider
{
    public IUiPageBehavior GetPage()
    {
        return new PageLayerUiPageBehavior(this, "MainMenu");
    }
}

// Overlay 层 UI（浮层，可重入）
public partial class InfoPanel : Control, IUiPageBehaviorProvider
{
    public IUiPageBehavior GetPage()
    {
        return new OverlayLayerUiPageBehavior(this, "InfoPanel");
    }
}

// Modal 层 UI（模态对话框，可重入）
public partial class ConfirmDialog : Control, IUiPageBehaviorProvider
{
    public IUiPageBehavior GetPage()
    {
        return new ModalLayerUiPageBehavior(this, "ConfirmDialog");
    }
}

// Toast 层 UI（提示，可重入）
public partial class ToastMessage : Control, IUiPageBehaviorProvider
{
    public IUiPageBehavior GetPage()
    {
        return new ToastLayerUiPageBehavior(this, "Toast");
    }
}

// Topmost 层 UI（顶层，不可重入）
public partial class LoadingScreen : Control, IUiPageBehaviorProvider
{
    public IUiPageBehavior GetPage()
    {
        return new TopmostLayerUiPageBehavior(this, "Loading");
    }
}
```

### UI 参数传递

```csharp
// 定义 UI 参数
public class ConfirmDialogParam : IUiPageEnterParam
{
    public string Title { get; set; }
    public string Message { get; set; }
    public Action OnConfirm { get; set; }
    public Action OnCancel { get; set; }
}

// 在 UI 中接收参数
public partial class ConfirmDialog : Control, IUiPage
{
    private Label _titleLabel;
    private Label _messageLabel;
    private Action _onConfirm;
    private Action _onCancel;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("Title");
        _messageLabel = GetNode<Label>("Message");

        GetNode<Button>("ConfirmButton").Pressed += OnConfirmPressed;
        GetNode<Button>("CancelButton").Pressed += OnCancelPressed;
    }

    public void OnEnter(IUiPageEnterParam? param)
    {
        if (param is ConfirmDialogParam dialogParam)
        {
            _titleLabel.Text = dialogParam.Title;
            _messageLabel.Text = dialogParam.Message;
            _onConfirm = dialogParam.OnConfirm;
            _onCancel = dialogParam.OnCancel;
        }

        Show();
    }

    private void OnConfirmPressed()
    {
        _onConfirm?.Invoke();
        CloseDialog();
    }

    private void OnCancelPressed()
    {
        _onCancel?.Invoke();
        CloseDialog();
    }

    private void CloseDialog()
    {
        var uiRouter = this.GetSystem<IUiRouter>();
        if (Handle.HasValue)
        {
            uiRouter.Hide(Handle.Value, UiLayer.Modal, destroy: true);
        }
    }

    // ... 其他生命周期方法
}

// 显示对话框
var uiRouter = this.GetSystem<IUiRouter>();
uiRouter.Show("ConfirmDialog", UiLayer.Modal, new ConfirmDialogParam
{
    Title = "确认",
    Message = "确定要退出吗？",
    OnConfirm = () => GD.Print("确认"),
    OnCancel = () => GD.Print("取消")
});
```

### UI 根节点管理

```csharp
using Godot;
using GFramework.Godot.ui;

public partial class UiRoot : CanvasLayer, IUiRoot
{
    private Control _pageLayer;
    private Control _overlayLayer;
    private Control _modalLayer;
    private Control _toastLayer;
    private Control _topmostLayer;

    public override void _Ready()
    {
        // 创建各层级容器
        _pageLayer = new Control { Name = "PageLayer" };
        _overlayLayer = new Control { Name = "OverlayLayer" };
        _modalLayer = new Control { Name = "ModalLayer" };
        _toastLayer = new Control { Name = "ToastLayer" };
        _topmostLayer = new Control { Name = "TopmostLayer" };

        AddChild(_pageLayer);
        AddChild(_overlayLayer);
        AddChild(_modalLayer);
        AddChild(_toastLayer);
        AddChild(_topmostLayer);
    }

    public void AttachPage(Control page, UiLayer layer)
    {
        var container = GetLayerContainer(layer);
        container.AddChild(page);
    }

    public void DetachPage(Control page, UiLayer layer)
    {
        var container = GetLayerContainer(layer);
        container.RemoveChild(page);
    }

    private Control GetLayerContainer(UiLayer layer)
    {
        return layer switch
        {
            UiLayer.Page => _pageLayer,
            UiLayer.Overlay => _overlayLayer,
            UiLayer.Modal => _modalLayer,
            UiLayer.Toast => _toastLayer,
            UiLayer.Topmost => _topmostLayer,
            _ => _pageLayer
        };
    }
}
```

### UI 动画和过渡

```csharp
public partial class AnimatedPage : Control, IUiPage
{
    public void OnEnter(IUiPageEnterParam? param)
    {
        // 淡入动画
        Modulate = new Color(1, 1, 1, 0);
        Show();

        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.3f);
    }

    public void OnExit()
    {
        // 淡出动画
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(Hide));
    }

    public void OnShow()
    {
        Show();
    }

    public void OnHide()
    {
        Hide();
    }

    // ... 其他方法
}
```

### UI 句柄管理

```csharp
public partial class DialogManager : Node
{
    private UiHandle? _currentDialog;

    public void ShowDialog(string dialogKey)
    {
        // 关闭当前对话框
        CloseCurrentDialog();

        // 显示新对话框
        var uiRouter = this.GetSystem<IUiRouter>();
        _currentDialog = uiRouter.Show(dialogKey, UiLayer.Modal);
    }

    public void CloseCurrentDialog()
    {
        if (_currentDialog.HasValue)
        {
            var uiRouter = this.GetSystem<IUiRouter>();
            uiRouter.Hide(_currentDialog.Value, UiLayer.Modal, destroy: true);
            _currentDialog = null;
        }
    }
}
```

### 多个 Toast 显示

```csharp
public partial class ToastManager : Node
{
    private readonly List<UiHandle> _activeToasts = new();

    public void ShowToast(string message, float duration = 3.0f)
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // Toast 层支持重入，可以同时显示多个
        var handle = uiRouter.Show("Toast", UiLayer.Toast, new ToastParam
        {
            Message = message
        });

        _activeToasts.Add(handle);

        // 自动隐藏
        GetTree().CreateTimer(duration).Timeout += () =>
        {
            uiRouter.Hide(handle, UiLayer.Toast, destroy: true);
            _activeToasts.Remove(handle);
        };
    }

    public void ClearAllToasts()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        foreach (var handle in _activeToasts)
        {
            uiRouter.Hide(handle, UiLayer.Toast, destroy: true);
        }

        _activeToasts.Clear();
    }
}
```

## 最佳实践

1. **UI 脚本实现 IUiPage 接口**：获得完整的生命周期管理
   ```csharp
   ✓ public partial class MyPage : Control, IUiPage { }
   ✗ public partial class MyPage : Control { } // 无生命周期管理
   ```

2. **使用正确的 UI 层级**：根据 UI 类型选择合适的层级
   ```csharp
   ✓ Page: 主要页面（主菜单、设置）
   ✓ Overlay: 浮层（信息面板）
   ✓ Modal: 模态对话框（确认框）
   ✓ Toast: 提示消息
   ✓ Topmost: 系统级（加载界面）
   ```

3. **在 OnEnter 中显示 UI**：确保 UI 正确显示
   ```csharp
   public void OnEnter(IUiPageEnterParam? param)
   {
       Show(); // 显示 UI
       // 初始化 UI 状态
   }
   ```

4. **在 OnExit 中隐藏 UI**：确保 UI 正确隐藏
   ```csharp
   public void OnExit()
   {
       Hide(); // 隐藏 UI
       // 清理 UI 状态
   }
   ```

5. **使用 UI 句柄管理非栈 UI**：对于 Modal、Toast 等层级
   ```csharp
   var handle = uiRouter.Show("Dialog", UiLayer.Modal);
   // 保存句柄以便后续关闭
   uiRouter.Hide(handle, UiLayer.Modal, destroy: true);
   ```

6. **使用 UI 参数传递数据**：避免使用全局变量
   ```csharp
   ✓ uiRouter.Show("Dialog", UiLayer.Modal, new DialogParam { ... });
   ✗ GlobalData.DialogMessage = "..."; // 避免全局状态
   ```

## 常见问题

### 问题：如何在 Godot UI 中使用 GFramework？

**解答**：
UI 脚本实现 `IUiPage` 和 `IUiPageBehaviorProvider` 接口：

```csharp
public partial class MyPage : Control, IUiPage, IUiPageBehaviorProvider
{
    public void OnEnter(IUiPageEnterParam? param) { }
    public IUiPageBehavior GetPage() { return new PageLayerUiPageBehavior(this, "MyPage"); }
}
```

### 问题：UI 层级有什么区别？

**解答**：

- **Page**：栈管理，不可重入，用于主要页面
- **Overlay**：可重入，用于浮层
- **Modal**：可重入，带遮罩，用于对话框
- **Toast**：可重入，轻量提示
- **Topmost**：不可重入，最高优先级

### 问题：如何实现 UI 动画？

**解答**：
在生命周期方法中使用 Godot Tween：

```csharp
public void OnEnter(IUiPageEnterParam? param)
{
    var tween = CreateTween();
    tween.TweenProperty(this, "modulate:a", 1.0f, 0.3f);
}
```

### 问题：如何在 UI 中访问架构组件？

**解答**：
使用扩展方法：

```csharp
public partial class MyPage : Control, IUiPage
{
    public void OnEnter(IUiPageEnterParam? param)
    {
        var playerModel = this.GetModel<PlayerModel>();
        var gameSystem = this.GetSystem<GameSystem>();
    }
}
```

### 问题：如何关闭 Modal 或 Toast？

**解答**：
使用 UI 句柄：

```csharp
// 显示时保存句柄
var handle = uiRouter.Show("Dialog", UiLayer.Modal);

// 关闭时使用句柄
uiRouter.Hide(handle, UiLayer.Modal, destroy: true);
```

### 问题：UI 生命周期方法的调用顺序是什么？

**解答**：

- 进入：`OnEnter` -> `OnShow`
- 暂停：`OnPause` -> `OnHide`
- 恢复：`OnShow` -> `OnResume`
- 退出：`OnHide` -> `OnExit`

## 相关文档

- [UI 系统](/zh-CN/game/ui) - 核心 UI 管理
- [Godot 架构集成](/zh-CN/godot/architecture) - Godot 架构基础
- [Godot 场景系统](/zh-CN/godot/scene) - Godot 场景集成
- [Godot 扩展](/zh-CN/godot/extensions) - Godot 扩展方法
