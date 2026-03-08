---
title: UI 系统
description: UI 系统提供了完整的 UI 页面管理、路由导航和多层级显示功能。
---

# UI 系统

## 概述

UI 系统是 GFramework.Game 中用于管理游戏 UI 界面的核心组件。它提供了 UI 页面的生命周期管理、基于栈的导航机制，以及多层级的
UI 显示系统（Page、Overlay、Modal、Toast、Topmost）。

通过 UI 系统，你可以轻松实现 UI 页面之间的切换，管理 UI 栈（如主菜单 -> 设置 -> 关于），以及在不同层级显示各种类型的
UI（对话框、提示、加载界面等）。

**主要特性**：

- 完整的 UI 生命周期管理
- 基于栈的 UI 导航
- 多层级 UI 显示（5 个层级）
- UI 转换管道和钩子
- 路由守卫（Route Guard）
- UI 工厂和行为模式

## 核心概念

### UI 页面接口

`IUiPage` 定义了 UI 页面的生命周期：

```csharp
public interface IUiPage
{
    void OnEnter(IUiPageEnterParam? param);  // 进入页面
    void OnExit();                           // 退出页面
    void OnPause();                          // 暂停页面
    void OnResume();                         // 恢复页面
    void OnShow();                           // 显示页面
    void OnHide();                           // 隐藏页面
}
```

### UI 路由

`IUiRouter` 管理 UI 的导航和切换：

```csharp
public interface IUiRouter : ISystem
{
    int Count { get; }                    // UI 栈深度
    IUiPageBehavior? Peek();              // 栈顶 UI

    ValueTask PushAsync(string uiKey, IUiPageEnterParam? param = null);
    ValueTask PopAsync(UiPopPolicy policy = UiPopPolicy.Destroy);
    ValueTask ReplaceAsync(string uiKey, IUiPageEnterParam? param = null);
    ValueTask ClearAsync();
}
```

### UI 层级

UI 系统支持 5 个显示层级：

```csharp
public enum UiLayer
{
    Page,      // 页面层（栈管理，不可重入）
    Overlay,   // 浮层（可重入，对话框等）
    Modal,     // 模态层（可重入，带遮罩）
    Toast,     // 提示层（可重入，轻量提示）
    Topmost    // 顶层（不可重入，系统级）
}
```

## 基本用法

### 定义 UI 页面

实现 `IUiPage` 接口创建 UI 页面：

```csharp
using GFramework.Game.Abstractions.ui;

public class MainMenuPage : IUiPage
{
    public void OnEnter(IUiPageEnterParam? param)
    {
        Console.WriteLine("进入主菜单");
        // 初始化 UI、绑定事件
    }

    public void OnExit()
    {
        Console.WriteLine("退出主菜单");
        // 清理资源、解绑事件
    }

    public void OnPause()
    {
        Console.WriteLine("暂停主菜单");
        // 暂停动画、停止交互
    }

    public void OnResume()
    {
        Console.WriteLine("恢复主菜单");
        // 恢复动画、启用交互
    }

    public void OnShow()
    {
        Console.WriteLine("显示主菜单");
        // 显示 UI 元素
    }

    public void OnHide()
    {
        Console.WriteLine("隐藏主菜单");
        // 隐藏 UI 元素
    }
}
```

### 切换 UI 页面

使用 UI 路由进行导航：

```csharp
using GFramework.Core.Abstractions.controller;
using GFramework.SourceGenerators.Abstractions.rule;

[ContextAware]
public partial class UiController : IController
{
    public async Task ShowSettings()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // 压入设置页面（保留当前页面）
        await uiRouter.PushAsync("Settings");
    }

    public async Task CloseSettings()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // 弹出当前页面（返回上一页）
        await uiRouter.PopAsync();
    }

    public async Task ShowMainMenu()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // 替换所有页面（清空 UI 栈）
        await uiRouter.ReplaceAsync("MainMenu");
    }
}
```

### 显示不同层级的 UI

```csharp
[ContextAware]
public partial class UiController : IController
{
    public void ShowDialog()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // 在 Modal 层显示对话框
        var handle = uiRouter.Show("ConfirmDialog", UiLayer.Modal);
    }

    public void ShowToast(string message)
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // 在 Toast 层显示提示
        var handle = uiRouter.Show("ToastMessage", UiLayer.Toast,
            new ToastParam { Message = message });
    }

    public void ShowLoading()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // 在 Topmost 层显示加载界面
        var handle = uiRouter.Show("LoadingScreen", UiLayer.Topmost);
    }
}
```

## 高级用法

### UI 参数传递

```csharp
// 定义 UI 参数
public class SettingsEnterParam : IUiPageEnterParam
{
    public string Category { get; set; }
}

// 在 UI 中接收参数
public class SettingsPage : IUiPage
{
    private string _category;

    public void OnEnter(IUiPageEnterParam? param)
    {
        if (param is SettingsEnterParam settingsParam)
        {
            _category = settingsParam.Category;
            Console.WriteLine($"打开设置分类: {_category}");
        }
    }

    // ... 其他生命周期方法
}

// 传递参数
await uiRouter.PushAsync("Settings", new SettingsEnterParam
{
    Category = "Audio"
});
```

### 路由守卫

```csharp
using GFramework.Game.Abstractions.ui;

public class UnsavedChangesGuard : IUiRouteGuard
{
    public async ValueTask<bool> CanLeaveAsync(
        IUiPageBehavior from,
        string toKey,
        IUiPageEnterParam? param)
    {
        // 检查是否有未保存的更改
        if (from.Key == "Settings" && HasUnsavedChanges())
        {
            var confirmed = await ShowConfirmDialog();
            return confirmed;
        }

        return true;
    }

    public async ValueTask<bool> CanEnterAsync(
        string toKey,
        IUiPageEnterParam? param)
    {
        // 进入前的验证
        return true;
    }

    private bool HasUnsavedChanges() => true;
    private async Task<bool> ShowConfirmDialog() => await Task.FromResult(true);
}

// 注册守卫
uiRouter.AddGuard(new UnsavedChangesGuard());
```

### UI 转换处理器

```csharp
using GFramework.Game.Abstractions.ui;

public class FadeTransitionHandler : IUiTransitionHandler
{
    public async ValueTask OnBeforeEnterAsync(UiTransitionEvent @event)
    {
        Console.WriteLine($"准备进入 UI: {@event.ToKey}");
        await PlayFadeIn();
    }

    public async ValueTask OnAfterEnterAsync(UiTransitionEvent @event)
    {
        Console.WriteLine($"已进入 UI: {@event.ToKey}");
    }

    public async ValueTask OnBeforeExitAsync(UiTransitionEvent @event)
    {
        Console.WriteLine($"准备退出 UI: {@event.FromKey}");
        await PlayFadeOut();
    }

    public async ValueTask OnAfterExitAsync(UiTransitionEvent @event)
    {
        Console.WriteLine($"已退出 UI: {@event.FromKey}");
    }

    private async Task PlayFadeIn() => await Task.Delay(200);
    private async Task PlayFadeOut() => await Task.Delay(200);
}

// 注册转换处理器
uiRouter.RegisterHandler(new FadeTransitionHandler());
```

### UI 句柄管理

```csharp
using GFramework.Core.Abstractions.controller;
using GFramework.SourceGenerators.Abstractions.rule;

[ContextAware]
public partial class DialogController : IController
{
    private UiHandle? _dialogHandle;

    public void ShowDialog()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // 显示对话框并保存句柄
        _dialogHandle = uiRouter.Show("ConfirmDialog", UiLayer.Modal);
    }

    public void CloseDialog()
    {
        if (_dialogHandle.HasValue)
        {
            var uiRouter = this.GetSystem<IUiRouter>();

            // 使用句柄关闭对话框
            uiRouter.Hide(_dialogHandle.Value, UiLayer.Modal, destroy: true);
            _dialogHandle = null;
        }
    }
}
```

### UI 栈管理

```csharp
using GFramework.Core.Abstractions.controller;
using GFramework.SourceGenerators.Abstractions.rule;

[ContextAware]
public partial class NavigationController : IController
{
    public void ShowUiStack()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        Console.WriteLine($"UI 栈深度: {uiRouter.Count}");

        var current = uiRouter.Peek();
        if (current != null)
        {
            Console.WriteLine($"当前 UI: {current.Key}");
        }
    }

    public bool IsSettingsOpen()
    {
        var uiRouter = this.GetSystem<IUiRouter>();
        return uiRouter.Contains("Settings");
    }

    public bool IsTopPage(string uiKey)
    {
        var uiRouter = this.GetSystem<IUiRouter>();
        return uiRouter.IsTop(uiKey);
    }
}
```

### 多层级 UI 管理

```csharp
using GFramework.Core.Abstractions.controller;
using GFramework.SourceGenerators.Abstractions.rule;

[ContextAware]
public partial class LayerController : IController
{
    public void ShowMultipleToasts()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // Toast 层支持重入，可以同时显示多个
        uiRouter.Show("Toast1", UiLayer.Toast);
        uiRouter.Show("Toast2", UiLayer.Toast);
        uiRouter.Show("Toast3", UiLayer.Toast);
    }

    public void ClearAllToasts()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // 清空 Toast 层的所有 UI
        uiRouter.ClearLayer(UiLayer.Toast, destroy: true);
    }

    public void HideAllDialogs()
    {
        var uiRouter = this.GetSystem<IUiRouter>();

        // 隐藏 Modal 层的所有对话框
        uiRouter.HideByKey("ConfirmDialog", UiLayer.Modal, hideAll: true);
    }
}
```

## 最佳实践

1. **使用合适的层级**：根据 UI 类型选择正确的层级
   ```csharp
   ✓ Page: 主要页面（主菜单、设置、游戏界面）
   ✓ Overlay: 浮层（信息面板、小窗口）
   ✓ Modal: 模态对话框（确认框、输入框）
   ✓ Toast: 轻量提示（消息、通知）
   ✓ Topmost: 系统级（加载界面、全屏遮罩）
   ```

2. **使用 Push/Pop 管理临时 UI**：如设置、帮助页面
   ```csharp
   // 打开设置（保留当前页面）
   await uiRouter.PushAsync("Settings");

   // 关闭设置（返回上一页）
   await uiRouter.PopAsync();
   ```

3. **使用 Replace 切换主要页面**：如从菜单到游戏
   ```csharp
   // 开始游戏（清空 UI 栈）
   await uiRouter.ReplaceAsync("Gameplay");
   ```

4. **在 OnEnter/OnExit 中管理资源**：保持资源管理清晰
   ```csharp
   public void OnEnter(IUiPageEnterParam? param)
   {
       // 加载资源、绑定事件
       BindEvents();
   }

   public void OnExit()
   {
       // 清理资源、解绑事件
       UnbindEvents();
   }
   ```

5. **使用句柄管理非栈 UI**：对于 Overlay、Modal、Toast 层
   ```csharp
   // 保存句柄
   var handle = uiRouter.Show("Dialog", UiLayer.Modal);

   // 使用句柄关闭
   uiRouter.Hide(handle, UiLayer.Modal, destroy: true);
   ```

6. **避免在 UI 切换时阻塞**：使用异步操作
   ```csharp
   ✓ await uiRouter.PushAsync("Settings");
   ✗ uiRouter.PushAsync("Settings").Wait(); // 可能死锁
   ```

## 常见问题

### 问题：Push、Pop、Replace 有什么区别？

**解答**：

- **Push**：压入新 UI，暂停当前 UI（用于临时页面）
- **Pop**：弹出当前 UI，恢复上一个 UI（用于关闭临时页面）
- **Replace**：清空 UI 栈，加载新 UI（用于主要页面切换）

### 问题：什么时候使用不同的 UI 层级？

**解答**：

- **Page**：主要页面，使用栈管理
- **Overlay**：浮层，可叠加显示
- **Modal**：模态对话框，阻挡下层交互
- **Toast**：轻量提示，不阻挡交互
- **Topmost**：系统级，最高优先级

### 问题：如何在 UI 之间传递数据？

**解答**：

1. 通过 UI 参数
2. 通过 Model
3. 通过事件

### 问题：UI 切换时如何显示过渡动画？

**解答**：
使用 UI 转换处理器在 `OnBeforeEnter`/`OnAfterExit` 中播放动画。

### 问题：如何防止用户在 UI 切换时操作？

**解答**：
在转换处理器中显示遮罩或禁用输入。

## 相关文档

- [场景系统](/zh-CN/game/scene) - 场景管理
- [Godot UI 系统](/zh-CN/godot/ui) - Godot 引擎集成
- [事件系统](/zh-CN/core/events) - UI 事件通信
- [状态机系统](/zh-CN/core/state-machine) - UI 状态管理
