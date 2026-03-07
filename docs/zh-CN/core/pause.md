# 暂停管理系统使用说明

## 概述

暂停管理系统（Pause System）提供了一套完整的游戏暂停控制机制，支持多层嵌套暂停、分组暂停、以及灵活的暂停处理器扩展。该系统基于栈结构实现，能够优雅地处理复杂的暂停场景，如菜单叠加、对话框弹出等。

暂停系统是 GFramework 架构中的核心工具（Utility），与其他系统协同工作，为游戏提供统一的暂停管理能力。

**主要特性：**

- **嵌套暂停**：支持多层暂停请求，只有所有请求都解除后才恢复
- **分组管理**：不同系统可以独立暂停（游戏逻辑、动画、音频等）
- **线程安全**：使用读写锁保证并发安全
- **作用域管理**：支持 `using` 语法自动管理暂停生命周期
- **事件通知**：状态变化时通知所有注册的处理器
- **优先级控制**：处理器按优先级顺序执行

## 核心概念

### 暂停栈（Pause Stack）

暂停系统使用栈结构管理暂停请求。每次调用 `Push` 会将暂停请求压入栈中，调用 `Pop` 会从栈中移除对应的请求。只有当栈为空时，游戏才会恢复运行。

```
栈深度 3: [暂停原因: "库存界面"]
栈深度 2: [暂停原因: "对话框"]
栈深度 1: [暂停原因: "暂停菜单"]
```

### 暂停组（Pause Group）

暂停组允许不同系统独立控制暂停状态。例如，打开菜单时可以暂停游戏逻辑但保持 UI 动画运行。

**预定义组：**

- `Global` - 全局暂停（影响所有系统）
- `Gameplay` - 游戏逻辑暂停（不影响 UI）
- `Animation` - 动画暂停
- `Audio` - 音频暂停
- `Custom1/2/3` - 自定义组

### 暂停令牌（Pause Token）

每次暂停请求都会返回一个唯一的令牌，用于后续恢复操作。令牌基于 GUID 实现，确保唯一性。

```csharp
public readonly struct PauseToken
{
    public Guid Id { get; }
    public bool IsValid => Id != Guid.Empty;
}
```

### 暂停处理器（Pause Handler）

处理器实现具体的暂停/恢复逻辑，如控制物理引擎、音频系统等。处理器按优先级顺序执行。

```csharp
public interface IPauseHandler
{
    int Priority { get; }  // 优先级（数值越小越高）
    void OnPauseStateChanged(PauseGroup group, bool isPaused);
}
```

## 核心接口

### IPauseStackManager

暂停栈管理器接口，提供暂停控制的所有功能。

**核心方法：**

```csharp
// 推入暂停请求
PauseToken Push(string reason, PauseGroup group = PauseGroup.Global);

// 弹出暂停请求
bool Pop(PauseToken token);

// 查询暂停状态
bool IsPaused(PauseGroup group = PauseGroup.Global);

// 获取暂停深度
int GetPauseDepth(PauseGroup group = PauseGroup.Global);

// 获取暂停原因列表
IReadOnlyList<string> GetPauseReasons(PauseGroup group = PauseGroup.Global);

// 创建暂停作用域
IDisposable PauseScope(string reason, PauseGroup group = PauseGroup.Global);

// 清空指定组
void ClearGroup(PauseGroup group);

// 清空所有组
void ClearAll();

// 注册/注销处理器
void RegisterHandler(IPauseHandler handler);
void UnregisterHandler(IPauseHandler handler);

// 状态变化事件
event Action<PauseGroup, bool>? OnPauseStateChanged;
```

## 基本用法

### 1. 获取暂停管理器

```csharp
public class GameController : IController
{
    private IPauseStackManager _pauseManager;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void Initialize()
    {
        // 从架构中获取暂停管理器
        _pauseManager = this.GetUtility<IPauseStackManager>();
    }
}
```

### 2. 简单的暂停/恢复

```csharp
public class PauseMenuController : IController
{
    private IPauseStackManager _pauseManager;
    private PauseToken _pauseToken;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void Initialize()
    {
        _pauseManager = this.GetUtility<IPauseStackManager>();
    }

    public void OpenPauseMenu()
    {
        // 暂停游戏
        _pauseToken = _pauseManager.Push("暂停菜单");

        Console.WriteLine($"游戏已暂停，深度: {_pauseManager.GetPauseDepth()}");
    }

    public void ClosePauseMenu()
    {
        // 恢复游戏
        if (_pauseToken.IsValid)
        {
            _pauseManager.Pop(_pauseToken);
            Console.WriteLine("游戏已恢复");
        }
    }
}
```

### 3. 使用作用域自动管理

```csharp
public class DialogController : IController
{
    private IPauseStackManager _pauseManager;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void ShowDialog(string message)
    {
        // 使用 using 语法，自动管理暂停生命周期
        using (_pauseManager.PauseScope("对话框"))
        {
            Console.WriteLine($"显示对话框: {message}");
            // 对话框显示期间游戏暂停
            WaitForUserInput();
        }
        // 离开作用域后自动恢复
    }
}
```

### 4. 查询暂停状态

```csharp
public class GameplaySystem : AbstractSystem
{
    private IPauseStackManager _pauseManager;

    protected override void OnInit()
    {
        _pauseManager = this.GetUtility<IPauseStackManager>();
    }

    public void Update(float deltaTime)
    {
        // 检查是否暂停
        if (_pauseManager.IsPaused(PauseGroup.Gameplay))
        {
            return;  // 暂停时跳过更新
        }

        // 正常游戏逻辑
        UpdateGameLogic(deltaTime);
    }
}
```

## 高级用法

### 1. 嵌套暂停

```csharp
public class UIManager : IController
{
    private IPauseStackManager _pauseManager;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void ShowNestedMenus()
    {
        // 第一层：主菜单
        var token1 = _pauseManager.Push("主菜单");
        Console.WriteLine($"深度: {_pauseManager.GetPauseDepth()}");  // 输出: 1

        // 第二层：设置菜单
        var token2 = _pauseManager.Push("设置菜单");
        Console.WriteLine($"深度: {_pauseManager.GetPauseDepth()}");  // 输出: 2

        // 第三层：确认对话框
        var token3 = _pauseManager.Push("确认对话框");
        Console.WriteLine($"深度: {_pauseManager.GetPauseDepth()}");  // 输出: 3

        // 关闭对话框
        _pauseManager.Pop(token3);
        Console.WriteLine($"仍然暂停: {_pauseManager.IsPaused()}");  // 输出: True

        // 关闭设置菜单
        _pauseManager.Pop(token2);
        Console.WriteLine($"仍然暂停: {_pauseManager.IsPaused()}");  // 输出: True

        // 关闭主菜单
        _pauseManager.Pop(token1);
        Console.WriteLine($"已恢复: {!_pauseManager.IsPaused()}");  // 输出: True
    }
}
```

### 2. 分组暂停

```csharp
public class GameManager : IController
{
    private IPauseStackManager _pauseManager;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void OpenInventory()
    {
        // 只暂停游戏逻辑，UI 和音频继续运行
        var token = _pauseManager.Push("库存界面", PauseGroup.Gameplay);

        Console.WriteLine($"游戏逻辑暂停: {_pauseManager.IsPaused(PauseGroup.Gameplay)}");
        Console.WriteLine($"音频暂停: {_pauseManager.IsPaused(PauseGroup.Audio)}");
        Console.WriteLine($"全局暂停: {_pauseManager.IsPaused(PauseGroup.Global)}");
    }

    public void OpenPauseMenu()
    {
        // 全局暂停，影响所有系统
        var token = _pauseManager.Push("暂停菜单", PauseGroup.Global);

        Console.WriteLine($"所有系统已暂停");
    }

    public void MuteAudio()
    {
        // 只暂停音频
        var token = _pauseManager.Push("静音", PauseGroup.Audio);
    }
}
```

### 3. 自定义暂停处理器

```csharp
// 物理引擎暂停处理器
public class PhysicsPauseHandler : IPauseHandler
{
    private readonly PhysicsWorld _physicsWorld;

    public PhysicsPauseHandler(PhysicsWorld physicsWorld)
    {
        _physicsWorld = physicsWorld;
    }

    // 高优先级，确保物理引擎最先暂停
    public int Priority => 10;

    public void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        // 只响应游戏逻辑和全局暂停
        if (group == PauseGroup.Gameplay || group == PauseGroup.Global)
        {
            _physicsWorld.Enabled = !isPaused;
            Console.WriteLine($"物理引擎 {(isPaused ? "已暂停" : "已恢复")}");
        }
    }
}

// 音频系统暂停处理器
public class AudioPauseHandler : IPauseHandler
{
    private readonly AudioSystem _audioSystem;

    public AudioPauseHandler(AudioSystem audioSystem)
    {
        _audioSystem = audioSystem;
    }

    public int Priority => 20;

    public void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        // 响应音频和全局暂停
        if (group == PauseGroup.Audio || group == PauseGroup.Global)
        {
            if (isPaused)
            {
                _audioSystem.PauseAll();
            }
            else
            {
                _audioSystem.ResumeAll();
            }
        }
    }
}

// 注册处理器
public class GameInitializer
{
    public void Initialize()
    {
        var pauseManager = architecture.GetUtility<IPauseStackManager>();
        var physicsWorld = GetPhysicsWorld();
        var audioSystem = GetAudioSystem();

        // 注册处理器
        pauseManager.RegisterHandler(new PhysicsPauseHandler(physicsWorld));
        pauseManager.RegisterHandler(new AudioPauseHandler(audioSystem));
    }
}
```

### 4. 监听暂停状态变化

```csharp
public class PauseIndicator : IController
{
    private IPauseStackManager _pauseManager;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void Initialize()
    {
        _pauseManager = this.GetUtility<IPauseStackManager>();

        // 订阅状态变化事件
        _pauseManager.OnPauseStateChanged += OnPauseStateChanged;
    }

    private void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        Console.WriteLine($"暂停状态变化: 组={group}, 暂停={isPaused}");

        if (group == PauseGroup.Global)
        {
            if (isPaused)
            {
                ShowPauseIndicator();
            }
            else
            {
                HidePauseIndicator();
            }
        }
    }

    public void Cleanup()
    {
        _pauseManager.OnPauseStateChanged -= OnPauseStateChanged;
    }
}
```

### 5. 调试暂停状态

```csharp
public class PauseDebugger : IController
{
    private IPauseStackManager _pauseManager;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void PrintPauseStatus()
    {
        Console.WriteLine("=== 暂停状态 ===");

        foreach (PauseGroup group in Enum.GetValues(typeof(PauseGroup)))
        {
            var isPaused = _pauseManager.IsPaused(group);
            var depth = _pauseManager.GetPauseDepth(group);
            var reasons = _pauseManager.GetPauseReasons(group);

            Console.WriteLine($"\n组: {group}");
            Console.WriteLine($"  状态: {(isPaused ? "暂停" : "运行")}");
            Console.WriteLine($"  深度: {depth}");

            if (reasons.Count > 0)
            {
                Console.WriteLine("  原因:");
                foreach (var reason in reasons)
                {
                    Console.WriteLine($"    - {reason}");
                }
            }
        }
    }
}
```

### 6. 紧急恢复

```csharp
public class EmergencyController : IController
{
    private IPauseStackManager _pauseManager;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void ForceResumeAll()
    {
        // 清空所有暂停请求（谨慎使用）
        _pauseManager.ClearAll();
        Console.WriteLine("已强制恢复所有系统");
    }

    public void ForceResumeGameplay()
    {
        // 只清空游戏逻辑组
        _pauseManager.ClearGroup(PauseGroup.Gameplay);
        Console.WriteLine("已强制恢复游戏逻辑");
    }
}
```

## Godot 集成

### GodotPauseHandler

GFramework.Godot 提供了 Godot 引擎的暂停处理器实现：

```csharp
public class GodotPauseHandler : IPauseHandler
{
    private readonly SceneTree _tree;

    public GodotPauseHandler(SceneTree tree)
    {
        _tree = tree;
    }

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

### 在 Godot 中使用

```csharp
public partial class GameRoot : Node
{
    private IPauseStackManager _pauseManager;

    public override void _Ready()
    {
        // 获取暂停管理器
        _pauseManager = architecture.GetUtility<IPauseStackManager>();

        // 注册 Godot 处理器
        var godotHandler = new GodotPauseHandler(GetTree());
        _pauseManager.RegisterHandler(godotHandler);
    }

    public void OnPauseButtonPressed()
    {
        // 暂停游戏
        _pauseManager.Push("玩家暂停", PauseGroup.Global);
    }
}
```

### 配合 ProcessMode

```csharp
public partial class PauseMenu : Control
{
    public override void _Ready()
    {
        // 设置为 Always 模式，暂停时仍然处理输入
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            if (pauseManager.IsPaused())
            {
                // 恢复游戏
                ResumeGame();
            }
            else
            {
                // 暂停游戏
                PauseGame();
            }
        }
    }
}
```

## 最佳实践

### 1. 使用作用域管理

优先使用 `PauseScope` 而不是手动 `Push/Pop`，避免忘记恢复：

```csharp
// ✅ 推荐
public void ShowDialog()
{
    using (_pauseManager.PauseScope("对话框"))
    {
        // 对话框逻辑
    }
    // 自动恢复
}

// ❌ 不推荐
public void ShowDialog()
{
    var token = _pauseManager.Push("对话框");
    // 对话框逻辑
    _pauseManager.Pop(token);  // 容易忘记
}
```

### 2. 提供清晰的暂停原因

暂停原因用于调试，应该清晰描述暂停来源：

```csharp
// ✅ 推荐
_pauseManager.Push("主菜单 - 设置页面");
_pauseManager.Push("过场动画 - 关卡加载");
_pauseManager.Push("教程对话框 - 第一关");

// ❌ 不推荐
_pauseManager.Push("pause");
_pauseManager.Push("menu");
```

### 3. 合理选择暂停组

根据实际需求选择合适的暂停组：

```csharp
// 打开库存：只暂停游戏逻辑
_pauseManager.Push("库存界面", PauseGroup.Gameplay);

// 打开暂停菜单：全局暂停
_pauseManager.Push("暂停菜单", PauseGroup.Global);

// 播放过场动画：暂停游戏逻辑和输入
_pauseManager.Push("过场动画", PauseGroup.Gameplay);
```

### 4. 处理器优先级设计

合理设置处理器优先级，确保正确的执行顺序：

```csharp
// 物理引擎：高优先级（10），最先暂停
public class PhysicsPauseHandler : IPauseHandler
{
    public int Priority => 10;
}

// 音频系统：中优先级（20）
public class AudioPauseHandler : IPauseHandler
{
    public int Priority => 20;
}

// UI 动画：低优先级（30），最后暂停
public class UiAnimationPauseHandler : IPauseHandler
{
    public int Priority => 30;
}
```

### 5. 避免在处理器中抛出异常

处理器异常会被捕获并记录，但不会中断其他处理器：

```csharp
public class SafePauseHandler : IPauseHandler
{
    public int Priority => 0;

    public void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        try
        {
            // 可能失败的操作
            RiskyOperation();
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出
            Console.WriteLine($"暂停处理失败: {ex.Message}");
        }
    }
}
```

### 6. 线程安全考虑

暂停管理器是线程安全的，但处理器回调在主线程执行：

```csharp
public class ThreadSafeUsage
{
    private IPauseStackManager _pauseManager;

    public void WorkerThread()
    {
        // ✅ 可以从任何线程调用
        Task.Run(() =>
        {
            var token = _pauseManager.Push("后台任务");
            // 执行任务
            _pauseManager.Pop(token);
        });
    }
}
```

### 7. 清理资源

在组件销毁时注销处理器和事件：

```csharp
public class ProperCleanup : IController
{
    private IPauseStackManager _pauseManager;
    private IPauseHandler _customHandler;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void Initialize()
    {
        _pauseManager = this.GetUtility<IPauseStackManager>();
        _customHandler = new CustomPauseHandler();

        _pauseManager.RegisterHandler(_customHandler);
        _pauseManager.OnPauseStateChanged += OnPauseChanged;
    }

    public void Cleanup()
    {
        _pauseManager.UnregisterHandler(_customHandler);
        _pauseManager.OnPauseStateChanged -= OnPauseChanged;
    }

    private void OnPauseChanged(PauseGroup group, bool isPaused) { }
}
```

## 常见问题

### Q1: 为什么调用 Pop 后游戏还是暂停？

A: 暂停系统使用栈结构，只有当栈为空时才会恢复。检查是否有其他暂停请求：

```csharp
// 调试暂停状态
var depth = _pauseManager.GetPauseDepth();
var reasons = _pauseManager.GetPauseReasons();

Console.WriteLine($"当前暂停深度: {depth}");
Console.WriteLine("暂停原因:");
foreach (var reason in reasons)
{
    Console.WriteLine($"  - {reason}");
}
```

### Q2: 如何实现"暂停时显示菜单"？

A: 使用 Godot 的 `ProcessMode` 或监听暂停事件：

```csharp
public partial class PauseMenu : Control
{
    public override void _Ready()
    {
        // 方案 1: 设置为 Always 模式
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        // 方案 2: 监听暂停事件
        var pauseManager = this.GetUtility<IPauseStackManager>();
        pauseManager.OnPauseStateChanged += (group, isPaused) =>
        {
            if (group == PauseGroup.Global)
            {
                Visible = isPaused;
            }
        };
    }
}
```

### Q3: 可以在暂停期间执行某些逻辑吗？

A: 可以，通过检查暂停状态或使用不同的暂停组：

```csharp
public class SelectiveSystem : AbstractSystem
{
    protected override void OnInit() { }

    public void Update(float deltaTime)
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();

        // 方案 1: 检查特定组
        if (!pauseManager.IsPaused(PauseGroup.Gameplay))
        {
            UpdateGameplay(deltaTime);
        }

        // UI 始终更新（不检查暂停）
        UpdateUI(deltaTime);
    }
}
```

### Q4: 如何实现"慢动作"效果？

A: 暂停系统控制是否执行，时间缩放需要使用时间系统：

```csharp
public class SlowMotionController : IController
{
    private ITimeProvider _timeProvider;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void EnableSlowMotion()
    {
        // 使用时间缩放而不是暂停
        _timeProvider.TimeScale = 0.3f;
    }

    public void DisableSlowMotion()
    {
        _timeProvider.TimeScale = 1.0f;
    }
}
```

### Q5: 暂停管理器的性能如何？

A: 暂停管理器使用读写锁优化并发性能：

- 查询操作（`IsPaused`）使用读锁，支持并发
- 修改操作（`Push/Pop`）使用写锁，互斥执行
- 事件通知在锁外执行，避免死锁
- 适合频繁查询、偶尔修改的场景

### Q6: 可以动态添加/移除暂停组吗？

A: 暂停组是枚举类型，不支持动态添加。可以使用自定义组：

```csharp
// 使用预定义的自定义组
_pauseManager.Push("特殊效果", PauseGroup.Custom1);
_pauseManager.Push("天气系统", PauseGroup.Custom2);
_pauseManager.Push("AI 系统", PauseGroup.Custom3);
```

### Q7: 如何处理异步操作中的暂停？

A: 使用 `PauseScope` 配合 `async/await`：

```csharp
public class AsyncPauseExample : IController
{
    private IPauseStackManager _pauseManager;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public async Task ShowAsyncDialog()
    {
        using (_pauseManager.PauseScope("异步对话框"))
        {
            await Task.Delay(1000);
            Console.WriteLine("对话框显示中...");
            await WaitForUserInput();
        }
        // 自动恢复
    }
}
```

## 架构集成

### 在架构中注册

```csharp
public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void OnRegisterUtility()
    {
        // 注册暂停管理器
        RegisterUtility<IPauseStackManager>(new PauseStackManager());
    }

    protected override void OnInit()
    {
        // 注册默认处理器
        var pauseManager = GetUtility<IPauseStackManager>();

        // Godot 处理器
        if (Engine.IsEditorHint() == false)
        {
            var tree = (GetTree() as SceneTree)!;
            pauseManager.RegisterHandler(new GodotPauseHandler(tree));
        }
    }
}
```

### 与其他系统协同

```csharp
// 与事件系统配合
public class PauseEventBridge : AbstractSystem
{
    protected override void OnInit()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();

        pauseManager.OnPauseStateChanged += (group, isPaused) =>
        {
            // 发送暂停事件
            this.SendEvent(new GamePausedEvent
            {
                Group = group,
                IsPaused = isPaused
            });
        };
    }
}

// 与命令系统配合
public class PauseCommand : AbstractCommand
{
    private readonly string _reason;
    private readonly PauseGroup _group;

    public PauseCommand(string reason, PauseGroup group = PauseGroup.Global)
    {
        _reason = reason;
        _group = group;
    }

    protected override void OnExecute()
    {
        var pauseManager = this.GetUtility<IPauseStackManager>();
        pauseManager.Push(_reason, _group);
    }
}
```

## 相关包

- [`architecture`](./architecture.md) - 架构核心，提供工具注册
- [`utility`](./utility.md) - 工具基类
- [`events`](./events.md) - 事件系统，用于状态通知
- [`lifecycle`](./lifecycle.md) - 生命周期管理
- [`logging`](./logging.md) - 日志系统，用于调试
- [Godot 集成](../godot/index.md) - Godot 引擎集成
