---
title: Godot 场景系统
description: Godot 场景系统提供了 GFramework 场景管理与 Godot 场景树的完整集成。
---

# Godot 场景系统

## 概述

Godot 场景系统是 GFramework.Godot 中连接框架场景管理与 Godot 场景树的核心组件。它提供了场景行为封装、场景工厂、场景注册表等功能，让你可以在
Godot 项目中使用 GFramework 的场景管理系统。

通过 Godot 场景系统，你可以使用 GFramework 的场景路由、生命周期管理等功能，同时保持与 Godot 场景系统的完美兼容。

**主要特性**：

- 场景行为封装（SceneBehavior）
- 场景工厂和注册表
- 与 Godot PackedScene 集成
- 多种场景行为类型（Node2D、Node3D、Control）
- 场景生命周期管理
- 场景根节点管理

## 核心概念

### 场景行为

`SceneBehaviorBase<T>` 封装了 Godot 节点的场景行为：

```csharp
public abstract class SceneBehaviorBase<T> : ISceneBehavior
    where T : Node
{
    protected readonly T Owner;
    public string Key { get; }
    public IScene Scene { get; }
}
```

### 场景工厂

`GodotSceneFactory` 负责创建场景实例：

```csharp
public class GodotSceneFactory : ISceneFactory
{
    public ISceneBehavior Create(string sceneKey);
}
```

### 场景注册表

`IGodotSceneRegistry` 管理场景资源：

```csharp
public interface IGodotSceneRegistry
{
    void Register(string key, PackedScene scene);
    PackedScene Get(string key);
}
```

## 基本用法

### 创建场景脚本

```csharp
using Godot;
using GFramework.Game.Abstractions.scene;

public partial class MainMenuScene : Control, IScene
{
    public async ValueTask OnLoadAsync(ISceneEnterParam? param)
    {
        GD.Print("加载主菜单资源");
        await Task.CompletedTask;
    }

    public async ValueTask OnEnterAsync()
    {
        GD.Print("进入主菜单");
        Show();
        await Task.CompletedTask;
    }

    public async ValueTask OnPauseAsync()
    {
        GD.Print("暂停主菜单");
        await Task.CompletedTask;
    }

    public async ValueTask OnResumeAsync()
    {
        GD.Print("恢复主菜单");
        await Task.CompletedTask;
    }

    public async ValueTask OnExitAsync()
    {
        GD.Print("退出主菜单");
        Hide();
        await Task.CompletedTask;
    }

    public async ValueTask OnUnloadAsync()
    {
        GD.Print("卸载主菜单资源");
        await Task.CompletedTask;
    }
}
```

### 注册场景

```csharp
using GFramework.Godot.scene;
using Godot;

public class GameSceneRegistry : GodotSceneRegistry
{
    publieneRegistry()
    {
        // 注册场景资源
        Register("MainMenu", GD.Load<PackedScene>("res://scenes/MainMenu.tscn"));
        Register("Gameplay", GD.Load<PackedScene>("res://scenes/Gameplay.tscn"));
        Register("Pause", GD.Load<PackedScene>("res://scenes/Pause.tscn"));
    }
}
```

### 设置场景系统

```csharp
using GFramework.Godot.architecture;
using GFramework.Godot.scene;

public class GameArchitecture : AbstractArchitecture
{
    protected override void InstallModules()
    {
        // 注册场景注册表
        var sceneRegistry = new GameSceneRegistry();
        RegisterUtility<IGodotSceneRegistry>(sceneRegistry);

        // 注册场景工厂
        var sceneFactory = new GodotSceneFactory();
        RegisterUtility<ISceneFactory>(sceneFactory);

        // 注册场景路由
        var sceneRouter = new GodotSceneRouter();
        RegisterSystem<ISceneRouter>(sceneRouter);
    }
}
```

### 使用场景路由

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class GameController : Node
{
    public override void _Ready()
    {
        // 切换到主菜单
        SwitchToMainMenu();
    }

    private async void SwitchToMainMenu()
    {
        var sceneRouter = this.GetSystem<ISceneRouter>();
        await sceneRouter.ReplaceAsync("MainMenu");
    }

    private async void StartGame()
    {
        var sceneRouter = this.GetSystem<ISceneRouter>();
        await sceneRouter.ReplaceAsync("Gameplay");
    }

    private async void ShowPause()
    {
        var sceneRouter = this.GetSystem<ISceneRouter>();
        await sceneRouter.PushAsync("Pause");
    }
}
```

## 高级用法

### 使用场景行为提供者

```csharp
using Godot;
using GFramework.Game.Abstractions.scene;
using GFramework.Godot.scene;

public partial class GameplayScene : Node2D, ISceneBehaviorProvider
{
    private GameplaySceneBehavior _behavior;

    public override void _Ready()
    {
        _behavior = new GameplaySceneBehavior(this, "Gameplay");
    }

    public ISceneBehavior GetScene()
    {
        return _behavior;
    }
}

// 自定义场景行为
public class GameplaySceneBehavior : Node2DSceneBehavior
{
    public GameplaySceneBehavior(Node2D owner, string key) : base(owner, key)
    {
    }

    protected override async ValueTask OnLoadInternalAsync(ISceneEnterParam? param)
    {
        GD.Print("加载游戏场景");
        // 加载游戏资源
        await Task.CompletedTask;
    }

    protected override async ValueTask OnEnterInternalAsync()
    {
        GD.Print("进入游戏场景");
        Owner.Show();
        await Task.CompletedTask;
    }
}
```

### 不同类型的场景行为

```csharp
// Node2D 场景
public class Node2DSceneBehavior : SceneBehaviorBase<Node2D>
{
    public Node2DSceneBehavior(Node2D owner, string key) : base(owner, key)
    {
    }
}

// Node3D 场景
public class Node3DSceneBehavior : SceneBehaviorBase<Node3D>
{
    public Node3DSceneBehavior(Node3D owner, string key) : base(owner, key)
    {
    }
}

// Control 场景（UI）
public class ControlSceneBehavior : SceneBehaviorBase<Control>
{
    public ControlSceneBehavior(Control owner, string key) : base(owner, key)
    {
    }
}
```

### 场景根节点管理

```csharp
using Godot;
using GFramework.Godot.scene;

public partial class SceneRoot : Node, ISceneRoot
{
    private Node _currentSceneNode;

    public void AttachScene(Node sceneNode)
    {
        // 移除旧场景
        if (_currentSceneNode != null)
        {
            RemoveChild(_currentSceneNode);
            _currentSceneNode.QueueFree();
        }

        // 添加新场景
        _currentSceneNode = sceneNode;
        AddChild(_currentSceneNode);
    }

    public void DetachScene(Node sceneNode)
    {
        if (_currentSceneNode == sceneNode)
        {
            RemoveChild(_currentSceneNode);
            _currentSceneNode = null;
        }
    }
}
```

### 场景参数传递

```csharp
// 定义场景参数
public class GameplayEnterParam : ISceneEnterParam
{
    public int Level { get; set; }
    public string Difficulty { get; set; }
}

// 在场景中接收参数
public partial class GameplayScene : Node2D, IScene
{
    private int _level;
    private string _difficulty;

    public async ValueTask OnLoadAsync(ISceneEnterParam? param)
    {
        if (param is GameplayEnterParam gameplayParam)
        {
            _level = gameplayParam.Level;
            _difficulty = gameplayParam.Difficulty;
            GD.Print($"加载关卡 {_level}，难度: {_difficulty}");
        }

        await Task.CompletedTask;
    }

    // ... 其他生命周期方法
}

// 切换场景时传递参数
var sceneRouter = this.GetSystem<ISceneRouter>();
await sceneRouter.ReplaceAsync("Gameplay", new GameplayEnterParam
{
    Level = 1,
    Difficulty = "Normal"
});
```

### 场景预加载

```csharp
public partial class LoadingScene : Control
{
    public override async void _Ready()
    {
        // 预加载下一个场景
        await PreloadNextScene();

        // 切换到预加载的场景
        var sceneRouter = this.GetSystem<ISceneRouter>();
        await sceneRouter.ReplaceAsync("Gameplay");
    }

    private async Task PreloadNextScene()
    {
        var sceneFactory = this.GetUtility<ISceneFactory>();
        var sceneBehavior = sceneFactory.Create("Gameplay");

        // 预加载场景资源
        await sceneBehavior.LoadAsync(null);

        GD.Print("场景预加载完成");
    }
}
```

### 场景转换动画

```csharp
using Godot;
using GFramework.Game.Abstractions.scene;

public class FadeTransitionHandler : ISceneTransitionHandler
{
    private ColorRect _fadeRect;

    public FadeTransitionHandler(ColorRect fadeRect)
    {
        _fadeRect = fadeRect;
    }

    public async ValueTask OnBeforeExitAsync(SceneTransitionEvent @event)
    {
        // 淡出动画
        var tween = _fadeRect.CreateTween();
        tween.TweenProperty(_fadeRect, "modulate:a", 1.0f, 0.3f);
        await tween.ToSignal(tween, Tween.SignalName.Finished);
    }

    public async ValueTask OnAfterEnterAsync(SceneTransitionEvent @event)
    {
        // 淡入动画
        var tween = _fadeRect.CreateTween();
        tween.TweenProperty(_fadeRect, "modulate:a", 0.0f, 0.3f);
        await tween.ToSignal(tween, Tween.SignalName.Finished);
    }

    // ... 其他方法
}
```

### 场景间通信

```csharp
// 通过事件通信
public partial class GameplayScene : Node2D, IScene
{
    public async ValueTask OnEnterAsync()
    {
        // 发送场景进入事件
        this.SendEvent(new GameplaySceneEnteredEvent());
        await Task.CompletedTask;
    }
}

// 在其他地方监听
public partial class HUD : Control
{
    public override void _Ready()
    {
        this.RegisterEvent<GameplaySceneEnteredEvent>(OnGameplayEntered);
    }

    private void OnGameplayEntered(GameplaySceneEnteredEvent evt)
    {
        GD.Print("游戏场景已进入，显示 HUD");
        Show();
    }
}
```

## 最佳实践

1. **场景脚本实现 IScene 接口**：获得完整的生命周期管理
   ```csharp
   ✓ public partial class MyScene : Node2D, IScene { }
   ✗ public partial class MyScene : Node2D { } // 无生命周期管理
   ```

2. **使用场景注册表管理场景资源**：集中管理所有场景
   ```csharp
   public class GameSceneRegistry : GodotSceneRegistry
   {
       public GameSceneRegistry()
       {
           Register("MainMenu", GD.Load<PackedScene>("res://scenes/MainMenu.tscn"));
           Register("Gameplay", GD.Load<PackedScene>("res://scenes/Gameplay.tscn"));
       }
   }
   ```

3. **在 OnLoadAsync 中加载资源**：避免场景切换卡顿
   ```csharp
   public async ValueTask OnLoadAsync(ISceneEnterParam? param)
   {
       // 异步加载资源
       await LoadTexturesAsync();
       await LoadAudioAsync();
   }
   ```

4. **使用场景根节点管理场景树**：保持场景树结构清晰
   ```csharp
   // 创建场景根节点
   var sceneRoot = new Node { Name = "SceneRoot" };
   AddChild(sceneRoot);

   // 绑定到场景路由
   sceneRouter.BindRoot(sceneRoot);
   ```

5. **正确清理场景资源**：在 OnUnloadAsync 中释放资源
   ```csharp
   public async ValueTask OnUnloadAsync()
   {
       // 释放资源
       _texture?.Dispose();
       _audioStream?.Dispose();
       await Task.CompletedTask;
   }
   ```

6. **使用场景参数传递数据**：避免使用全局变量
   ```csharp
   ✓ await sceneRouter.ReplaceAsync("Gameplay", new GameplayEnterParam { Level = 1 });
   ✗ GlobalData.CurrentLevel = 1; // 避免全局状态
   ```

## 常见问题

### 问题：如何在 Godot 场景中使用 GFramework？

**解答**：
场景脚本实现 `IScene` 接口：

```csharp
public partial class MyScene : Node2D, IScene
{
    public async ValueTask OnLoadAsync(ISceneEnterParam? param) { }
    public async ValueTask OnEnterAsync() { }
    // ... 实现其他方法
}
```

### 问题：场景切换时节点如何管理？

**解答**：
使用场景根节点管理：

```csharp
// 场景路由会自动管理节点的添加和移除
await sceneRouter.ReplaceAsync("NewScene");
// 旧场景节点会被移除，新场景节点会被添加
```

### 问题：如何实现场景预加载？

**解答**：
使用场景工厂提前创建场景：

```csharp
var sceneFactory = this.GetUtility<ISceneFactory>();
var sceneBehavior = sceneFactory.Create("NextScene");
await sceneBehavior.LoadAsync(null);
```

### 问题：场景生命周期方法的调用顺序是什么？

**解答**：

- 进入场景：`OnLoadAsync` -> `OnEnterAsync` -> `OnShow`
- 暂停场景：`OnPause` -> `OnHide`
- 恢复场景：`OnShow` -> `OnResume`
- 退出场景：`OnHide` -> `OnExitAsync` -> `OnUnloadAsync`

### 问题：如何在场景中访问架构组件？

**解答**：
使用扩展方法：

```csharp
public partial class MyScene : Node2D, IScene
{
    public async ValueTask OnEnterAsync()
    {
        var playerModel = this.GetModel<PlayerModel>();
        var gameSystem = this.GetSystem<GameSystem>();
        await Task.CompletedTask;
    }
}
```

### 问题：场景切换时如何显示加载界面？

**解答**：
使用场景转换处理器：

```csharp
public class LoadingScreenHandler : ISceneTransitionHandler
{
    public async ValueTask OnBeforeLoadAsync(SceneTransitionEvent @event)
    {
        // 显示加载界面
        ShowLoadingScreen();
        await Task.CompletedTask;
    }

    public async ValueTask OnAfterEnterAsync(SceneTransitionEvent @event)
    {
        // 隐藏加载界面
        HideLoadingScreen();
        await Task.CompletedTask;
    }
}
```

## 相关文档

- [场景系统](/zh-CN/game/scene) - 核心场景管理
- [Godot 架构集成](/zh-CN/godot/architecture) - Godot 架构基础
- [Godot UI 系统](/zh-CN/godot/ui) - Godot UI 集成
- [Godot 扩展](/zh-CN/godot/extensions) - Godot 扩展方法
