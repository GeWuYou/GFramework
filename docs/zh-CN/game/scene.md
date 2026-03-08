---
title: 场景系统
description: 场景系统提供了完整的场景生命周期管理、路由导航和转换控制功能。
---

# 场景系统

## 概述

场景系统是 GFramework.Game 中用于管理游戏场景的核心组件。它提供了场景的加载、卸载、切换、暂停和恢复等完整生命周期管理，以及基于栈的场景导航机制。

通过场景系统，你可以轻松实现场景之间的平滑切换，管理场景栈（如主菜单 -> 游戏 -> 暂停菜单），并在场景转换时执行自定义逻辑。

**主要特性**：

- 完整的场景生命周期管理
- 基于栈的场景导航
- 场景转换管道和钩子
- 路由守卫（Route Guard）
- 场景工厂和行为模式
- 异步加载和卸载

## 核心概念

### 场景接口

`IScene` 定义了场景的完整生命周期：

```csharp
public interface IScene
{
    ValueTask OnLoadAsync(ISceneEnterParam? param);    // 加载资源
    ValueTask OnEnterAsync();                          // 进入场景
    ValueTask OnPauseAsync();                          // 暂停场景
    ValueTask OnResumeAsync();                         // 恢复场景
    ValueTask OnExitAsync();                           // 退出场景
    ValueTask OnUnloadAsync();                         // 卸载资源
}
```

### 场景路由

`ISceneRouter` 管理场景的导航和切换：

```csharp
public interface ISceneRouter : ISystem
{
    ISceneBehavior? Current { get; }              // 当前场景
    string? CurrentKey { get; }                   // 当前场景键
    IEnumerable<ISceneBehavior> Stack { get; }    // 场景栈
    bool IsTransitioning { get; }                 // 是否正在切换

    ValueTask ReplaceAsync(string sceneKey, ISceneEnterParam? param = null);
    ValueTask PushAsync(string sceneKey, ISceneEnterParam? param = null);
    ValueTask PopAsync();
    ValueTask ClearAsync();
}
```

### 场景行为

`ISceneBehavior` 封装了场景的具体实现和引擎集成：

```csharp
public interface ISceneBehavior
{
    string Key { get; }                    // 场景唯一标识
    IScene Scene { get; }                  // 场景实例
    ValueTask LoadAsync(ISceneEnterParam? param);
    ValueTask UnloadAsync();
}
```

## 基本用法

### 定义场景

实现 `IScene` 接口创建场景：

```csharp
using GFramework.Game.Abstractions.scene;

public class MainMenuScene : IScene
{
    public async ValueTask OnLoadAsync(ISceneEnterParam? param)
    {
        // 加载场景资源
        Console.WriteLine("加载主菜单资源");
        await Task.Delay(100); // 模拟加载
    }

    public async ValueTask OnEnterAsync()
    {
        // 进入场景
        Console.WriteLine("进入主菜单");
        // 显示 UI、播放音乐等
        await Task.CompletedTask;
    }

    public async ValueTask OnPauseAsync()
    {
        // 暂停场景
        Console.WriteLine("暂停主菜单");
        await Task.CompletedTask;
    }

    public async ValueTask OnResumeAsync()
    {
        // 恢复场景
        Console.WriteLine("恢复主菜单");
        await Task.CompletedTask;
    }

    public async ValueTask OnExitAsync()
    {
        // 退出场景
        Console.WriteLine("退出主菜单");
        // 隐藏 UI、停止音乐等
        await Task.CompletedTask;
    }

    public async ValueTask OnUnloadAsync()
    {
        // 卸载场景资源
        Console.WriteLine("卸载主菜单资源");
        await Task.Delay(50); // 模拟卸载
    }
}
```

### 注册场景

在场景注册表中注册场景：

```csharp
using GFramework.Game.Abstractions.scene;

public class GameSceneRegistry : IGameSceneRegistry
{
    private readonly Dictionary<string, Type> _scenes = new();

    public GameSceneRegistry()
    {
        // 注册场景
        Register("MainMenu", typeof(MainMenuScene));
        Register("Gameplay", typeof(GameplayScene));
        Register("Pause", typeof(PauseScene));
    }

    public void Register(string key, Type sceneType)
    {
        _scenes[key] = sceneType;
    }

    public Type? GetSceneType(string key)
    {
        return _scenes.TryGetValue(key, out var type) ? type : null;
    }
}
```

### 切换场景

使用场景路由进行导航：

```csharp
using GFramework.Core.Abstractions.controller;
using GFramework.SourceGenerators.Abstractions.rule;

[ContextAware]
public partial class GameController : IController
{
    public async Task StartGame()
    {
        var sceneRouter = this.GetSystem<ISceneRouter>();

        // 替换当前场景（清空场景栈）
        await sceneRouter.ReplaceAsync("Gameplay");
    }

    public async Task ShowPauseMenu()
    {
        var sceneRouter = this.GetSystem<ISceneRouter>();

        // 压入新场景（保留当前场景）
        await sceneRouter.PushAsync("Pause");
    }

    public async Task ClosePauseMenu()
    {
        var sceneRouter = this.GetSystem<ISceneRouter>();

        // 弹出当前场景（恢复上一个场景）
        await sceneRouter.PopAsync();
    }
}
```

## 高级用法

### 场景参数传递

通过 `ISceneEnterParam` 传递数据：

```csharp
// 定义场景参数
public class GameplayEnterParam : ISceneEnterParam
{
    public int Level { get; set; }
    public string Difficulty { get; set; }
}

// 在场景中接收参数
public class GameplayScene : IScene
{
    private int _level;
    private string _difficulty;

    public async ValueTask OnLoadAsync(ISceneEnterParam? param)
    {
        if (param is GameplayEnterParam gameplayParam)
        {
            _level = gameplayParam.Level;
            _difficulty = gameplayParam.Difficulty;
            Console.WriteLine($"加载关卡 {_level}，难度: {_difficulty}");
        }

        await Task.CompletedTask;
    }

    // ... 其他生命周期方法
}

// 切换场景时传递参数
await sceneRouter.ReplaceAsync("Gameplay", new GameplayEnterParam
{
    Level = 1,
    Difficulty = "Normal"
});
```

### 路由守卫

使用路由守卫控制场景切换：

```csharp
using GFramework.Game.Abstractions.scene;

public class SaveGameGuard : ISceneRouteGuard
{
    public async ValueTask<bool> CanLeaveAsync(
        ISceneBehavior from,
        string toKey,
        ISceneEnterParam? param)
    {
        // 离开游戏场景前检查是否需要保存
        if (from.Key == "Gameplay")
        {
            var needsSave = CheckIfNeedsSave();
            if (needsSave)
            {
                await SaveGameAsync();
            }
        }

        return true; // 允许离开
    }

    public async ValueTask<bool> CanEnterAsync(
        string toKey,
        ISceneEnterParam? param)
    {
        // 进入场景前的验证
        if (toKey == "Gameplay")
        {
            // 检查是否满足进入条件
            var canEnter = CheckGameplayRequirements();
            return canEnter;
        }

        return true;
    }

    private bool CheckIfNeedsSave() => true;
    private async Task SaveGameAsync() => await Task.Delay(100);
    private bool CheckGameplayRequirements() => true;
}

// 注册守卫
sceneRouter.AddGuard(new SaveGameGuard());
```

### 场景转换处理器

自定义场景转换逻辑：

```csharp
using GFramework.Game.Abstractions.scene;

public class FadeTransitionHandler : ISceneTransitionHandler
{
    public async ValueTask OnBeforeLoadAsync(SceneTransitionEvent @event)
    {
        Console.WriteLine($"准备加载场景: {@event.ToKey}");
        // 显示加载画面
        await ShowLoadingScreen();
    }

    public async ValueTask OnAfterLoadAsync(SceneTransitionEvent @event)
    {
        Console.WriteLine($"场景加载完成: {@event.ToKey}");
        await Task.CompletedTask;
    }

    public async ValueTask OnBeforeEnterAsync(SceneTransitionEvent @event)
    {
        Console.WriteLine($"准备进入场景: {@event.ToKey}");
        // 播放淡入动画
        await PlayFadeIn();
    }

    public async ValueTask OnAfterEnterAsync(SceneTransitionEvent @event)
    {
        Console.WriteLine($"已进入场景: {@event.ToKey}");
        // 隐藏加载画面
        await HideLoadingScreen();
    }

    public async ValueTask OnBeforeExitAsync(SceneTransitionEvent @event)
    {
        Console.WriteLine($"准备退出场景: {@event.FromKey}");
        // 播放淡出动画
        await PlayFadeOut();
    }

    public async ValueTask OnAfterExitAsync(SceneTransitionEvent @event)
    {
        Console.WriteLine($"已退出场景: {@event.FromKey}");
        await Task.CompletedTask;
    }

    private async Task ShowLoadingScreen() => await Task.Delay(100);
    private async Task HideLoadingScreen() => await Task.Delay(100);
    private async Task PlayFadeIn() => await Task.Delay(200);
    private async Task PlayFadeOut() => await Task.Delay(200);
}

// 注册转换处理器
sceneRouter.AddTransitionHandler(new FadeTransitionHandler());
```

### 场景栈管理

```csharp
using GFramework.Core.Abstractions.controller;
using GFramework.SourceGenerators.Abstractions.rule;

[ContextAware]
public partial class SceneNavigationController : IController
{
    public async Task NavigateToSettings()
    {
        var sceneRouter = this.GetSystem<ISceneRouter>();

        // 检查场景是否已在栈中
        if (sceneRouter.Contains("Settings"))
        {
            Console.WriteLine("设置场景已打开");
            return;
        }

        // 压入设置场景
        await sceneRouter.PushAsync("Settings");
    }

    public void ShowSceneStack()
    {
        var sceneRouter = this.GetSystem<ISceneRouter>();

        Console.WriteLine("当前场景栈:");
        foreach (var scene in sceneRouter.Stack)
        {
            Console.WriteLine($"- {scene.Key}");
        }
    }

    public async Task ReturnToMainMenu()
    {
        var sceneRouter = this.GetSystem<ISceneRouter>();

        // 清空所有场景并加载主菜单
        await sceneRouter.ClearAsync();
        await sceneRouter.ReplaceAsync("MainMenu");
    }
}
```

### 场景加载进度

```csharp
public class GameplayScene : IScene
{
    public async ValueTask OnLoadAsync(ISceneEnterParam? param)
    {
        var resourceManager = GetResourceManager();

        // 加载多个资源并报告进度
        var resources = new[]
        {
            "textures/player.png",
            "textures/enemy.png",
            "audio/bgm.mp3",
            "models/level.obj"
        };

        for (int i = 0; i < resources.Length; i++)
        {
            await resourceManager.LoadAsync<object>(resources[i]);

            // 报告进度
            var progress = (i + 1) / (float)resources.Length;
            ReportProgress(progress);
        }
    }

    private void ReportProgress(float progress)
    {
        // 发送进度事件
        Console.WriteLine($"加载进度: {progress * 100:F0}%");
    }

    // ... 其他生命周期方法
}
```

### 场景预加载

```csharp
using GFramework.Core.Abstractions.controller;
using GFramework.SourceGenerators.Abstractions.rule;

[ContextAware]
public partial class PreloadController : IController
{
    public async Task PreloadNextLevel()
    {
        var sceneFactory = this.GetUtility<ISceneFactory>();

        // 预加载下一关场景
        var scene = sceneFactory.Create("Level2");
        await scene.OnLoadAsync(null);

        Console.WriteLine("下一关预加载完成");
    }
}
```

## 最佳实践

1. **在 OnLoad 中加载资源，在 OnUnload 中释放**：保持资源管理清晰
   ```csharp
   public async ValueTask OnLoadAsync(ISceneEnterParam? param)
   {
       _texture = await LoadTextureAsync("player.png");
   }

   public async ValueTask OnUnloadAsync()
   {
       _texture?.Dispose();
       _texture = null;
   }
   ```

2. **使用 Push/Pop 管理临时场景**：如暂停菜单、设置界面
   ```csharp
   // 打开暂停菜单（保留游戏场景）
   await sceneRouter.PushAsync("Pause");

   // 关闭暂停菜单（恢复游戏场景）
   await sceneRouter.PopAsync();
   ```

3. **使用 Replace 切换主要场景**：如从菜单到游戏
   ```csharp
   // 开始游戏（清空场景栈）
   await sceneRouter.ReplaceAsync("Gameplay");
   ```

4. **在 OnPause/OnResume 中管理状态**：暂停和恢复游戏逻辑
   ```csharp
   public async ValueTask OnPauseAsync()
   {
       // 暂停游戏逻辑
       _gameTimer.Pause();
       _audioSystem.PauseBGM();
   }

   public async ValueTask OnResumeAsync()
   {
       // 恢复游戏逻辑
       _gameTimer.Resume();
       _audioSystem.ResumeBGM();
   }
   ```

5. **使用路由守卫处理业务逻辑**：如保存检查、权限验证
   ```csharp
   public async ValueTask<bool> CanLeaveAsync(...)
   {
       if (HasUnsavedChanges())
       {
           var confirmed = await ShowSaveDialog();
           if (confirmed)
           {
               await SaveAsync();
           }
           return confirmed;
       }
       return true;
   }
   ```

6. **避免在场景切换时阻塞**：使用异步操作
   ```csharp
   ✓ await sceneRouter.ReplaceAsync("Gameplay");
   ✗ sceneRouter.ReplaceAsync("Gameplay").Wait(); // 可能死锁
   ```

## 常见问题

### 问题：Replace、Push、Pop 有什么区别？

**解答**：

- **Replace**：清空场景栈，加载新场景（用于主要场景切换）
- **Push**：压入新场景，暂停当前场景（用于临时场景）
- **Pop**：弹出当前场景，恢复上一个场景（用于关闭临时场景）

```csharp
// 场景栈示例
await sceneRouter.ReplaceAsync("MainMenu");  // [MainMenu]
await sceneRouter.PushAsync("Settings");     // [MainMenu, Settings]
await sceneRouter.PushAsync("About");        // [MainMenu, Settings, About]
await sceneRouter.PopAsync();                // [MainMenu, Settings]
await sceneRouter.PopAsync();                // [MainMenu]
```

### 问题：如何在场景之间传递数据？

**解答**：
有几种方式：

1. **通过场景参数**：

```csharp
await sceneRouter.ReplaceAsync("Gameplay", new GameplayEnterParam
{
    Level = 5
});
```

2. **通过 Model**：

```csharp
var gameModel = this.GetModel<GameModel>();
gameModel.CurrentLevel = 5;
await sceneRouter.ReplaceAsync("Gameplay");
```

3. **通过事件**：

```csharp
this.SendEvent(new LevelSelectedEvent { Level = 5 });
await sceneRouter.ReplaceAsync("Gameplay");
```

### 问题：场景切换时如何显示加载画面？

**解答**：
使用场景转换处理器：

```csharp
public class LoadingScreenHandler : ISceneTransitionHandler
{
    public async ValueTask OnBeforeLoadAsync(SceneTransitionEvent @event)
    {
        await ShowLoadingScreen();
    }

    public async ValueTask OnAfterEnterAsync(SceneTransitionEvent @event)
    {
        await HideLoadingScreen();
    }

    // ... 其他方法
}
```

### 问题：如何防止用户在场景切换时操作？

**解答**：
检查 `IsTransitioning` 状态：

```csharp
public async Task ChangeScene(string sceneKey)
{
    var sceneRouter = this.GetSystem<ISceneRouter>();

    if (sceneRouter.IsTransitioning)
    {
        Console.WriteLine("场景正在切换中，请稍候");
        return;
    }

    await sceneRouter.ReplaceAsync(sceneKey);
}
```

### 问题：场景切换失败怎么办？

**解答**：
使用 try-catch 捕获异常：

```csharp
try
{
    await sceneRouter.ReplaceAsync("Gameplay");
}
catch (Exception ex)
{
    Console.WriteLine($"场景切换失败: {ex.Message}");
    // 回退到安全场景
    await sceneRouter.ReplaceAsync("MainMenu");
}
```

### 问题：如何实现场景预加载？

**解答**：
在后台预先加载场景资源：

```csharp
// 在当前场景中预加载下一个场景
var factory = this.GetUtility<ISceneFactory>();
var nextScene = factory.Create("NextLevel");
await nextScene.OnLoadAsync(null);

// 稍后快速切换
await sceneRouter.ReplaceAsync("NextLevel");
```

## 相关文档

- [UI 系统](/zh-CN/game/ui) - UI 页面管理
- [资源管理系统](/zh-CN/core/resource) - 场景资源加载
- [状态机系统](/zh-CN/core/state-machine) - 场景状态管理
- [Godot 场景系统](/zh-CN/godot/scene) - Godot 引擎集成
- [存档系统实现教程](/zh-CN/tutorials/save-system) - 场景切换时保存数据
