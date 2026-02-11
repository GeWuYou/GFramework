# 场景管理

> GFramework.Game 模块提供的场景管理功能，实现游戏场景的加载、切换和状态管理

## 概述

GFramework.Game 提供了统一的场景管理系统，支持场景的异步加载、切换动画、状态保持等功能。通过场景管理，您可以优雅地处理游戏中的各种场景转换，如主菜单、游戏界面、设置菜单等。

## 核心特性

- **异步加载**：支持后台异步加载场景，避免卡顿
- **场景切换动画**：内置淡入淡出等切换效果
- **状态保持**：场景切换时保持必要的游戏状态
- **场景栈**：支持场景叠加（如弹出菜单）
- **资源管理**：自动管理场景资源的加载和卸载

## 基本用法

### 定义场景

```csharp
using GFramework.Game.scene;

public class GameScene : GameSceneBase
{
    protected override void OnLoad()
    {
        // 场景加载时的初始化
    }

    protected override void OnUnload()
    {
        // 场景卸载时的清理
    }

    public override void OnEnter()
    {
        // 进入场景
    }

    public override void OnExit()
    {
        // 退出场景
    }
}
```

### 场景管理器

```csharp
using GFramework.Game.scene;

[ContextAware]
public partial class SceneManager : Node
{
    private ISceneManager _sceneManager;

    public override void _Ready()
    {
        _sceneManager = Context.GetSystem<ISceneManager>();
        
        // 初始化场景管理器
        _sceneManager.Initialize();
    }

    public async Task SwitchToGameScene()
    {
        await _sceneManager.SwitchSceneAsync<GameScene>();
    }
}
```

## 场景加载

### 同步加载

适用于小场景的快速切换：

```csharp
public void LoadMainMenu()
{
    _sceneManager.SwitchScene<MainMenuScene>();
}
```

### 异步加载

大场景建议使用异步加载：

```csharp
public async Task LoadGameLevel(string levelName)
{
    // 显示加载界面
    loadingUI.Show();
    
    try
    {
        // 异步加载场景
        await _sceneManager.SwitchSceneAsync<GameLevelScene>(scene =>
        {
            // 可以在这里传递场景参数
            var gameScene = scene as GameLevelScene;
            gameScene.LevelName = levelName;
        });
        
        GD.Print("场景加载完成");
    }
    catch (Exception ex)
    {
        GD.PrintErr($"场景加载失败: {ex.Message}");
        loadingUI.ShowError();
    }
    finally
    {
        loadingUI.Hide();
    }
}
```

### 加载进度

监听加载进度：

```csharp
public async Task LoadSceneWithProgress<TScene>() where TScene : IScene
{
    loadingUI.Show();
    
    var progress = new Progress<float>(value =>
    {
        loadingUI.UpdateProgress(value * 100);
    });
    
    await _sceneManager.SwitchSceneAsync<TScene>(progress: progress);
    
    loadingUI.Hide();
}
```

## 场景切换动画

### 内置切换效果

```csharp
public async Task SwitchWithFade()
{
    // 使用淡入淡出效果
    await _sceneManager.SwitchSceneAsync<GameScene>(
        transition: new FadeTransition(
            duration: 0.5f,
            fadeColor: Colors.Black
        )
    );
}

public async Task SlideTransition()
{
    // 使用滑动效果
    await _sceneManager.SwitchSceneAsync<GameScene>(
        transition: new SlideTransition(
            direction: Vector2.Left,
            duration: 0.3f
        )
    );
}
```

### 自定义切换动画

```csharp
public class CustomTransition : ISceneTransition
{
    public float Duration { get; } = 1.0f;

    public async Task PlayAsync(Node fromScene, Node toScene)
    {
        // 自定义动画逻辑
        var tween = CreateTween();
        
        // 旧场景缩小消失
        tween.TweenProperty(fromScene, "scale", Vector2.Zero, Duration / 2)
            .SetTrans(Tween.TransitionType.BackIn);
        
        await ToSignal(tween, Tween.SignalName.Finished);
        
        fromScene.Visible = false;
        toScene.Visible = true;
        toScene.Scale = Vector2.Zero;
        
        // 新场景放大出现
        tween = CreateTween();
        tween.TweenProperty(toScene, "scale", Vector2.One, Duration / 2)
            .SetTrans(Tween.TransitionType.BackOut);
        
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}
```

## 场景栈

### 推送场景

将新场景推入栈中：

```csharp
public void PushPauseMenu()
{
    _sceneManager.PushScene<PauseMenuScene>();
}
```

### 弹出场景

弹出栈顶场景：

```csharp
public void PopScene()
{
    _sceneManager.PopScene();
}
```

### 示例：带返回的导航

```csharp
public async Task NavigateToSettings()
{
    // 保存当前场景引用
    var previousScene = _sceneManager.CurrentScene;
    
    // 推入设置场景
    await _sceneManager.PushSceneAsync<SettingsScene>();
    
    // 设置场景有返回按钮
    await _sceneManager.PushSceneAsync<ConfirmDialog>(
        data: new ConfirmData
        {
            Message = "是否保存设置？",
            OnConfirm = () => _sceneManager.PopScene(), // 不保存，直接返回
            OnCancel = () =>
            {
                // 保存设置后返回
                SaveSettings();
                _sceneManager.PopScene();
            }
        }
    );
}
```

## 场景状态管理

### 保存状态

```csharp
public class GameScene : GameSceneBase
{
    private GameState _state;

    protected override void OnLoad()
    {
        // 从存档加载状态
        _state = LoadGameState();
    }

    protected override void OnUnload()
    {
        // 保存状态
        SaveGameState(_state);
    }

    public override void OnExit()
    {
        // 清理工作
        Cleanup();
    }
}
```

### 跨场景数据传递

```csharp
// 传递数据到新场景
await _sceneManager.SwitchSceneAsync<GameScene>(data: new GameData
{
    Level = 5,
    Difficulty = Difficulty.Hard,
    ContinueToken = saveToken
});

// 在目标场景中接收数据
public class GameScene : GameSceneBase
{
    public override void OnEnter(object data)
    {
        var gameData = data as GameData;
        InitializeLevel(gameData.Level, gameData.Difficulty);
    }
}
```

## 预加载策略

### 预加载资源

```csharp
public void PreloadCommonResources()
{
    _sceneManager.PreloadScene<MainMenuScene>();
    _sceneManager.PreloadScene<GameLevelScene>(count: 3);
}
```

### 预加载配置

```csharp
var config = new ScenePreloadConfig
{
    PreloadCount = 5,
    Priority = ResourceLoader.CacheMode.Cache,
    Timeout = 30.0f
};

_sceneManager.ConfigurePreload(config);
```

## 资源管理

### 自动卸载

```csharp
// 配置自动卸载策略
var unloadConfig = new SceneUnloadConfig
{
    UnloadDelay = 30.0f,      // 30秒后卸载
    KeepReferences = true,    // 保持引用
    ForceUnloadOnMemoryPressure = true  // 内存紧张时强制卸载
};

_sceneManager.ConfigureUnload(unloadConfig);
```

### 手动卸载

```csharp
// 卸载指定场景
_sceneManager.UnloadScene<MainMenuScene>();

// 卸载所有场景
_sceneManager.UnloadAllScenes();
```

## 最佳实践

### 1. 场景设计原则

```csharp
// 推荐：每个场景职责单一
public class MainMenuScene : GameSceneBase { /* 主菜单 */ }
public class SettingsScene : GameSceneBase { /* 设置菜单 */ }
public class GameHUDScene : GameSceneBase { /* 游戏HUD */ }

// 避免：场景职责过多
public class MegaScene : GameSceneBase 
{ 
    /* 包含菜单、设置、HUD、存档管理... 不要这样设计 */
}
```

### 2. 场景切换优化

```csharp
// 推荐：使用异步加载
public async Task LoadGame()
{
    loadingUI.Show("正在加载游戏...");
    await _sceneManager.SwitchSceneAsync<GameScene>();
}

// 避免：同步加载大场景
public void LoadGame()
{
    // 这会导致游戏卡顿
    _sceneManager.SwitchScene<GameScene>();
}
```

### 3. 内存管理

```csharp
public class GameScene : GameSceneBase
{
    private Texture2D[] _largeTextures;

    protected override void OnLoad()
    {
        // 加载资源
        _largeTextures = LoadLargeTextures();
    }

    protected override void OnUnload()
    {
        // 及时释放大资源
        foreach (var texture in _largeTextures)
        {
            texture.Dispose();
        }
        _largeTextures = null;
    }
}
```

### 4. 错误处理

```csharp
public async Task SafeLoadScene<TScene>() where TScene : IScene
{
    try
    {
        await _sceneManager.SwitchSceneAsync<TScene>();
    }
    catch (SceneNotFoundException)
    {
        Logger.Error($"场景 {typeof(TScene)} 未找到");
        // 回退到默认场景
        await _sceneManager.SwitchSceneAsync<FallbackScene>();
    }
    catch (ResourceLoadException ex)
    {
        Logger.Error($"资源加载失败: {ex.Message}");
        await ShowRetryDialog();
    }
}
```

## 与其他模块集成

### 与 Game 模块集成

```csharp
public class GameLevelScene : GameSceneBase
{
    private GameManager _gameManager;

    public override void OnEnter()
    {
        // 初始化游戏管理器
        _gameManager = Context.GetSystem<GameManager>();
        _gameManager.StartGame();
    }

    public override void OnExit()
    {
        _gameManager.EndGame();
    }
}
```

### 与存档系统集成

```csharp
public class GameScene : GameSceneBase
{
    private ISaveSystem _saveSystem;

    protected override void OnLoad()
    {
        _saveSystem = Context.GetUtility<ISaveSystem>();
        
        // 检查是否有自动存档
        if (_saveSystem.HasAutoSave())
        {
            var dialog = Context.GetSystem<IDialogSystem>();
            await dialog.ShowAsync("发现自动存档，是否继续？",
                onConfirm: () => LoadAutoSave(),
                onCancel: () => StartNewGame()
            );
        }
    }

    private void LoadAutoSave()
    {
        var saveData = _saveSystem.LoadAutoSave();
        RestoreGameState(saveData);
    }

    public override void OnExit()
    {
        // 自动保存
        _saveSystem.SaveAutoSave(CreateSaveData());
    }
}
```

---

**相关文档**：

- [Game 概述](./overview)
- [游戏设置](./setting)
- [存储系统](./storage)
- [存档系统](../game/storage)
