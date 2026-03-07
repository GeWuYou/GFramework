---
title: Godot 暂停处理
description: Godot 暂停处理系统提供了 GFramework 暂停管理与 Godot SceneTree 暂停的完整集成。
---

# Godot 暂停处理

## 概述

Godot 暂停处理系统是 GFramework.Godot 中连接框架暂停管理与 Godot 引擎暂停机制的核心组件。它提供了暂停栈管理、分组暂停、嵌套暂停等功能，让你可以在
Godot 项目中使用 GFramework 的暂停系统。

通过 Godot 暂停处理系统，你可以实现精细的暂停控制，支持游戏逻辑暂停、UI 暂停、动画暂停等多种场景，同时保持与 Godot SceneTree
暂停机制的完美兼容。

**主要特性**：

- 暂停栈管理（支持嵌套暂停）
- 分组暂停（Global、Gameplay、Animation、Audio 等）
- 与 Godot SceneTree.Paused 集成
- 暂停处理器机制
- 暂停作用域（支持 using 语法）
- 线程安全的暂停管理

## 核心概念

### 暂停栈管理器

`IPauseStackManager` 管理游戏中的暂停状态：

```csharp
public interface IPauseStackManager : IContextUtility
{
    // 推入暂停请求
    PauseToken Push(string reason, PauseGroup group = PauseGroup.Global);

    // 弹出暂停请求
    bool Pop(PauseToken token);

    // 查询是否暂停
    bool IsPaused(PauseGroup group = PauseGroup.Global);

    // 获取暂停深度
    int GetPauseDepth(PauseGroup group = PauseGroup.Global);

    // 暂停状态变化事件
    event Action<PauseGroup, bool>? OnPauseStateChanged;
}
```

### 暂停组

`PauseGroup` 定义不同的暂停作用域：

```csharp
public enum PauseGroup
{
    Global = 0,      // 全局暂停（影响所有系统）
    Gameplay = 1,    // 游戏逻辑暂停（不影响 UI）
    Animation = 2,   // 动画暂停
    Audio = 3,       // 音频暂停
    Custom1 = 10,    // 自定义组 1
    Custom2 = 11,    // 自定义组 2
    Custom3 = 12     // 自定义组 3
}
```

### 暂停令牌

`PauseToken` 唯一标识一个暂停请求：

```csharp
public readonly struct PauseToken
{
    public Guid Id { get; }
    public bool IsValid { get; }
}
```

### Godot 暂停处理器

`GodotPauseHandler` 响应暂停栈状态变化，控制 SceneTree.Paused：

```csharp
public class GodotPauseHandler : IPauseHandler
{
    public int Priority => 0;

    public void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        // 只有 Global 组影响 Godot 的全局暂停
        if (group == PauseGroup.Global)
        {
            _tree.Paused = isPaused;
        }
    }
}
```

## 基本用法

### 设置暂停系统

```csharp
using GFramework.Godot.architecture;
using GFramework.Godot.pause;
using GFramework.Core.pause;

public class GameArchitecture : AbstractArchitecture
{
    protected override void InstallModules()
    {
        // 注册暂停栈管理器
        var pauseManager = new PauseStackManager();
        RegisterUtility<IPauseStackManager>(pauseManager);

        // 注册 Godot 暂停处理器
        var pauseHandler = new GodotPauseHandler(GetTree());
        pauseManager.RegisterHandler(pauseHandler);
    }
}
```

### 基本暂停和恢复

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class PauseMenu : Control
{
    private PauseToken _pauseToken;

    public void ShowPauseMenu()
    {
        // 暂停游戏
        var pauseManager = this.GetUtility<IPauseStackManager>();
        _pauseToken = pauseManager.Push("Pause menu opened");

        Show();
    }

    public void HidePauseMenu()
    {
        // 恢复游戏
        var pauseManager = this.GetUtility<IPauseStackManager>();
        pauseManager.Pop(_pauseToken);

        Hide();
    }
}
```

### 使用暂停作用域

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class DialogBox : Control
{
    public async void ShowDialog()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();

        // 使用 using 语法自动管理暂停
        using (pauseManager.PauseScope("Dialog shown"))
        {
            Show();
            await ToSignal(GetTree().CreateTimer(3.0f), "timeout");
            Hide();
        } // 自动恢复
    }
}
```

### 查询暂停状态

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class GameController : Node
{
    public override void _Process(double delta)
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();

        // 检查是否暂停
        if (pauseManager.IsPaused())
        {
            GD.Print("游戏已暂停");
            return;
        }

        // 游戏逻辑
        UpdateGame(delta);
    }

    private void UpdateGame(double delta)
    {
        // 游戏更新逻辑
    }
}
```

## 高级用法

### 分组暂停

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class GameManager : Node
{
    private PauseToken _gameplayPauseToken;
    private PauseToken _animationPauseToken;

    // 只暂停游戏逻辑，UI 仍然可以交互
    public void PauseGameplay()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();
        _gameplayPauseToken = pauseManager.Push("Gameplay paused", PauseGroup.Gameplay);
    }

    public void ResumeGameplay()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();
        pauseManager.Pop(_gameplayPauseToken);
    }

    // 只暂停动画
    public void PauseAnimations()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();
        _animationPauseToken = pauseManager.Push("Animations paused", PauseGroup.Animation);
    }

    public void ResumeAnimations()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();
        pauseManager.Pop(_animationPauseToken);
    }

    // 检查特定组的暂停状态
    public bool IsGameplayPaused()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();
        return pauseManager.IsPaused(PauseGroup.Gameplay);
    }
}
```

### 嵌套暂停

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class GameScene : Node
{
    public async void ShowNestedDialogs()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();

        // 第一层暂停
        using (pauseManager.PauseScope("First dialog"))
        {
            GD.Print($"暂停深度: {pauseManager.GetPauseDepth()}"); // 输出: 1
            ShowDialog("第一个对话框");
            await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

            // 第二层暂停
            using (pauseManager.PauseScope("Second dialog"))
            {
                GD.Print($"暂停深度: {pauseManager.GetPauseDepth()}"); // 输出: 2
                ShowDialog("第二个对话框");
                await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
            }

            GD.Print($"暂停深度: {pauseManager.GetPauseDepth()}"); // 输出: 1
        }

        GD.Print($"暂停深度: {pauseManager.GetPauseDepth()}"); // 输出: 0
    }

    private void ShowDialog(string message)
    {
        GD.Print(message);
    }
}
```

### 自定义暂停处理器

```csharp
using GFramework.Core.Abstractions.pause;
using Godot;

// 自定义动画暂停处理器
public class AnimationPauseHandler : IPauseHandler
{
    private readonly AnimationPlayer _animationPlayer;

    public AnimationPauseHandler(AnimationPlayer animationPlayer)
    {
        _animationPlayer = animationPlayer;
    }

    public int Priority => 10;

    public void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        // 只响应 Animation 组
        if (group == PauseGroup.Animation)
        {
            if (isPaused)
            {
                _animationPlayer.Pause();
                GD.Print("动画已暂停");
            }
            else
            {
                _animationPlayer.Play();
                GD.Print("动画已恢复");
            }
        }
    }
}

// 注册自定义处理器
public partial class GameController : Node
{
    public override void _Ready()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();
        var animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        var animationHandler = new AnimationPauseHandler(animationPlayer);
        pauseManager.RegisterHandler(animationHandler);
    }
}
```

### 音频暂停处理器

```csharp
using GFramework.Core.Abstractions.pause;
using Godot;

public class AudioPauseHandler : IPauseHandler
{
    private readonly AudioStreamPlayer _musicPlayer;
    private readonly AudioStreamPlayer _sfxPlayer;

    public AudioPauseHandler(AudioStreamPlayer musicPlayer, AudioStreamPlayer sfxPlayer)
    {
        _musicPlayer = musicPlayer;
        _sfxPlayer = sfxPlayer;
    }

    public int Priority => 20;

    public void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        if (group == PauseGroup.Audio || group == PauseGroup.Global)
        {
            if (isPaused)
            {
                _musicPlayer.StreamPaused = true;
                _sfxPlayer.StreamPaused = true;
            }
            else
            {
                _musicPlayer.StreamPaused = false;
                _sfxPlayer.StreamPaused = false;
            }
        }
    }
}
```

### 节点暂停模式控制

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class GameNode : Node
{
    public override void _Ready()
    {
        // 设置节点在暂停时的行为

        // 暂停时停止处理
        ProcessMode = ProcessModeEnum.Pausable;

        // 暂停时继续处理（用于 UI）
        // ProcessMode = ProcessModeEnum.Always;

        // 暂停时停止，且子节点也停止
        // ProcessMode = ProcessModeEnum.Inherit;
    }

    public override void _Process(double delta)
    {
        // 当 SceneTree.Paused = true 且 ProcessMode = Pausable 时
        // 此方法不会被调用
        UpdateGameLogic(delta);
    }

    private void UpdateGameLogic(double delta)
    {
        // 游戏逻辑
    }
}
```

### UI 在暂停时继续工作

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class PauseMenuUI : Control
{
    public override void _Ready()
    {
        // UI 在游戏暂停时仍然可以交互
        ProcessMode = ProcessModeEnum.Always;

        GetNode<Button>("ResumeButton").Pressed += OnResumePressed;
        GetNode<Button>("QuitButton").Pressed += OnQuitPressed;
    }

    private void OnResumePressed()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();

        // 获取所有暂停原因
        var reasons = pauseManager.GetPauseReasons();
        GD.Print($"当前暂停原因: {string.Join(", ", reasons)}");

        // 清空所有暂停
        pauseManager.ClearAll();

        Hide();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
```

### 监听暂停状态变化

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class PauseIndicator : Label
{
    public override void _Ready()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();

        // 订阅暂停状态变化事件
        pauseManager.OnPauseStateChanged += OnPauseStateChanged;
    }

    public override void _ExitTree()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();
        pauseManager.OnPauseStateChanged -= OnPauseStateChanged;
    }

    private void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        if (group == PauseGroup.Global)
        {
            Text = isPaused ? "游戏已暂停" : "游戏运行中";
            Visible = isPaused;
        }
    }
}
```

### 调试暂停状态

```csharp
using Godot;
using GFramework.Godot.extensions;

public partial class PauseDebugger : Node
{
    public override void _Ready()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();
        pauseManager.OnPauseStateChanged += OnPauseStateChanged;
    }

    private void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();

        GD.Print($"=== 暂停状态变化 ===");
        GD.Print($"组: {group}");
        GD.Print($"状态: {(isPaused ? "暂停" : "恢复")}");
        GD.Print($"深度: {pauseManager.GetPauseDepth(group)}");

        var reasons = pauseManager.GetPauseReasons(group);
        if (reasons.Count > 0)
        {
            GD.Print($"原因:");
            foreach (var reason in reasons)
            {
                GD.Print($"  - {reason}");
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        // 按 F12 显示所有暂停状态
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.F12)
        {
            PrintAllPauseStates();
        }
    }

    private void PrintAllPauseStates()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();

        GD.Print("=== 所有暂停状态 ===");

        foreach (PauseGroup group in Enum.GetValues(typeof(PauseGroup)))
        {
            var isPaused = pauseManager.IsPaused(group);
            var depth = pauseManager.GetPauseDepth(group);

            if (depth > 0)
            {
                GD.Print($"{group}: 暂停 (深度: {depth})");
                var reasons = pauseManager.GetPauseReasons(group);
                foreach (var reason in reasons)
                {
                    GD.Print($"  - {reason}");
                }
            }
        }
    }
}
```

## 最佳实践

1. **使用暂停作用域管理生命周期**：避免忘记恢复
   ```csharp
   ✓ using (pauseManager.PauseScope("Dialog")) { ... }
   ✗ var token = pauseManager.Push("Dialog"); // 可能忘记 Pop
   ```

2. **为暂停提供清晰的原因**：便于调试
   ```csharp
   ✓ pauseManager.Push("Inventory opened");
   ✗ pauseManager.Push("pause"); // 原因不明确
   ```

3. **使用正确的暂停组**：避免影响不该暂停的系统
   ```csharp
   ✓ pauseManager.Push("Menu", PauseGroup.Gameplay); // 只暂停游戏逻辑
   ✗ pauseManager.Push("Menu", PauseGroup.Global); // 暂停所有系统包括 UI
   ```

4. **UI 节点设置 ProcessMode.Always**：确保 UI 在暂停时可用
   ```csharp
   public override void _Ready()
   {
       ProcessMode = ProcessModeEnum.Always;
   }
   ```

5. **游戏逻辑节点设置 ProcessMode.Pausable**：确保暂停时停止
   ```csharp
   public override void _Ready()
   {
       ProcessMode = ProcessModeEnum.Pausable;
   }
   ```

6. **保存暂停令牌以便恢复**：确保能正确恢复暂停
   ```csharp
   private PauseToken _pauseToken;

   public void Pause()
   {
       _pauseToken = pauseManager.Push("Paused");
   }

   public void Resume()
   {
       pauseManager.Pop(_pauseToken);
   }
   ```

7. **使用事件监听暂停状态**：实现响应式 UI
   ```csharp
   pauseManager.OnPauseStateChanged += (group, isPaused) =>
   {
       UpdateUI(isPaused);
   };
   ```

8. **清理时注销事件监听**：避免内存泄漏
   ```csharp
   public override void _ExitTree()
   {
       pauseManager.OnPauseStateChanged -= OnPauseStateChanged;
   }
   ```

## 常见问题

### 问题：如何暂停游戏但保持 UI 可交互？

**解答**：
使用 `PauseGroup.Gameplay` 而不是 `PauseGroup.Global`：

```csharp
// 只暂停游戏逻辑
pauseManager.Push("Menu opened", PauseGroup.Gameplay);

// UI 节点设置为 Always
public override void _Ready()
{
    ProcessMode = ProcessModeEnum.Always;
}
```

### 问题：嵌套暂停如何工作？

**解答**：
暂停栈支持嵌套，需要所有 Pop 才能完全恢复：

```csharp
var token1 = pauseManager.Push("First");  // 深度: 1, 暂停
var token2 = pauseManager.Push("Second"); // 深度: 2, 仍然暂停

pauseManager.Pop(token1); // 深度: 1, 仍然暂停
pauseManager.Pop(token2); // 深度: 0, 恢复
```

### 问题：如何实现自定义暂停行为？

**解答**：
实现 `IPauseHandler` 接口并注册：

```csharp
public class CustomPauseHandler : IPauseHandler
{
    public int Priority => 0;

    public void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        // 自定义暂停逻辑
    }
}

pauseManager.RegisterHandler(new CustomPauseHandler());
```

### 问题：暂停处理器的优先级如何工作？

**解答**：
数值越小优先级越高，按优先级顺序调用：

```csharp
handler1.Priority = 0;  // 最先调用
handler2.Priority = 10; // 其次调用
handler3.Priority = 20; // 最后调用
```

### 问题：如何清空所有暂停？

**解答**：
使用 `ClearAll()` 或 `ClearGroup()`：

```csharp
// 清空所有组的暂停
pauseManager.ClearAll();

// 只清空特定组
pauseManager.ClearGroup(PauseGroup.Gameplay);
```

### 问题：暂停系统是线程安全的吗？

**解答**：
是的，`PauseStackManager` 使用 `ReaderWriterLockSlim` 确保线程安全：

```csharp
// 可以在多个线程中安全调用
Task.Run(() => pauseManager.Push("Thread 1"));
Task.Run(() => pauseManager.Push("Thread 2"));
```

### 问题：如何调试暂停问题？

**解答**：
使用暂停状态查询方法：

```csharp
// 检查是否暂停
bool isPaused = pauseManager.IsPaused(PauseGroup.Global);

// 获取暂停深度
int depth = pauseManager.GetPauseDepth(PauseGroup.Global);

// 获取所有暂停原因
var reasons = pauseManager.GetPauseReasons(PauseGroup.Global);
foreach (var reason in reasons)
{
    GD.Print($"暂停原因: {reason}");
}
```

## 相关文档

- [Godot 架构集成](/zh-CN/godot/architecture) - Godot 架构基础
- [Godot 场景系统](/zh-CN/godot/scene) - Godot 场景集成
- [Godot UI 系统](/zh-CN/godot/ui) - Godot UI 集成
- [Godot 扩展](/zh-CN/godot/extensions) - Godot 扩展方法
