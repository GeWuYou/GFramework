# Controller 包使用说明

## 概述

Controller 包定义了控制器（Controller）的接口规范。控制器是 MVC 架构中的 C 层，负责处理用户交互、协调视图和模型，是连接表现层和业务层的桥梁。在本框架中，Controller 通常对应 Godot 的节点脚本。

## 核心接口

### [`IController`](IController.cs)

控制器接口，定义了控制器需要实现的所有功能契约。

**继承的能力接口：**
- [`ICanSendCommand`](../command/ICanSendCommand.cs) - 可发送命令
- [`ICanGetSystem`](../system/ICanGetSystem.cs) - 可获取系统
- [`ICanGetModel`](../model/ICanGetModel.cs) - 可获取模型
- [`ICanRegisterEvent`](../events/ICanRegisterEvent.cs) - 可注册事件
- [`ICanSendQuery`](../query/ICanSendQuery.cs) - 可发送查询
- [`ICanGetUtility`](../utility/ICanGetUtility.cs) - 可获取工具

**能力说明：**

控制器拥有框架中最全面的能力集合，可以：
1. 发送命令执行业务逻辑
2. 获取系统调用服务
3. 获取模型读写数据
4. 注册事件监听变化
5. 发送查询获取信息
6. 获取工具使用辅助功能

## 使用示例

### 基础控制器实现

```csharp
using Godot;
using GFramework.framework.controller;
using GFramework.framework.architecture;

// Godot 节点控制器
public partial class PlayerController : Node, IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    // 实现架构获取
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        // 获取模型
        var playerModel = this.GetModel<PlayerModel>();
        
        // 监听模型变化
        playerModel.Health.RegisterWithInitValue(OnHealthChanged)
            .AddToUnregisterList(_unregisterList);
        
        // 注册事件
        this.RegisterEvent<PlayerLevelUpEvent>(OnPlayerLevelUp)
            .AddToUnregisterList(_unregisterList);
    }
    
    // 处理用户输入
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("attack"))
        {
            // 发送命令
            this.SendCommand(new AttackCommand());
        }
        
        if (Input.IsActionJustPressed("use_item"))
        {
            // 发送查询
            var inventory = this.SendQuery(new GetInventoryQuery());
            if (inventory.HasItem("potion"))
            {
                this.SendCommand(new UseItemCommand("potion"));
            }
        }
    }
    
    private void OnHealthChanged(int newHealth)
    {
        // 更新 UI 显示
        UpdateHealthBar(newHealth);
    }
    
    private void OnPlayerLevelUp(PlayerLevelUpEvent e)
    {
        // 显示升级特效
        ShowLevelUpEffect();
    }
    
    public override void _ExitTree()
    {
        // 清理事件注册
        _unregisterList.UnRegisterAll();
    }
    
    private void UpdateHealthBar(int health) { /* UI 更新逻辑 */ }
    private void ShowLevelUpEffect() { /* 特效逻辑 */ }
}
```

### UI 控制器示例

```csharp
// UI 面板控制器
public partial class MainMenuController : Control, IController
{
    [Export] private Button _startButton;
    [Export] private Button _settingsButton;
    [Export] private Button _quitButton;
    
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        // 绑定按钮事件
        _startButton.Pressed += OnStartButtonPressed;
        _settingsButton.Pressed += OnSettingsButtonPressed;
        _quitButton.Pressed += OnQuitButtonPressed;
        
        // 获取模型更新 UI
        var gameModel = this.GetModel<GameModel>();
        UpdateUI(gameModel);
    }
    
    private void OnStartButtonPressed()
    {
        // 通过命令启动游戏
        this.SendCommand<StartGameCommand>();
    }
    
    private void OnSettingsButtonPressed()
    {
        // 查询当前设置
        var settings = this.SendQuery(new GetSettingsQuery());
        
        // 打开设置面板
        var uiSystem = this.GetSystem<UISystem>();
        uiSystem.OpenSettingsPanel(settings);
    }
    
    private void OnQuitButtonPressed()
    {
        // 发送退出命令
        this.SendCommand<QuitGameCommand>();
    }
    
    private void UpdateUI(GameModel model) { /* UI 更新逻辑 */ }
}
```

### 复杂交互控制器

```csharp
// 战斗控制器
public partial class CombatController : Node, IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();
    private PlayerModel _playerModel;
    private CombatSystem _combatSystem;
    
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        // 缓存常用引用
        _playerModel = this.GetModel<PlayerModel>();
        _combatSystem = this.GetSystem<CombatSystem>();
        
        // 注册多个事件
        this.RegisterEvent<EnemySpawnedEvent>(OnEnemySpawned)
            .AddToUnregisterList(_unregisterList);
            
        this.RegisterEvent<CombatEndedEvent>(OnCombatEnded)
            .AddToUnregisterList(_unregisterList);
        
        // 监听模型状态
        _playerModel.CombatState.Register(OnCombatStateChanged)
            .AddToUnregisterList(_unregisterList);
    }
    
    private void OnEnemySpawned(EnemySpawnedEvent e)
    {
        // 进入战斗状态
        this.SendCommand(new EnterCombatCommand(e.Enemy));
    }
    
    private void OnCombatEnded(CombatEndedEvent e)
    {
        if (e.Victory)
        {
            // 查询奖励
            var rewards = this.SendQuery(new CalculateRewardsQuery(e.Enemy));
            
            // 发放奖励
            this.SendCommand(new GiveRewardsCommand(rewards));
        }
        else
        {
            // 处理失败
            this.SendCommand<GameOverCommand>();
        }
    }
    
    private void OnCombatStateChanged(CombatState state)
    {
        // 根据战斗状态更新 UI
        var uiSystem = this.GetSystem<UISystem>();
        uiSystem.UpdateCombatUI(state);
    }
    
    public override void _ExitTree()
    {
        _unregisterList.UnRegisterAll();
    }
}
```

## 控制器职责

### ✅ 应该做的事

1. **处理用户输入**
   - 键盘、鼠标、触摸输入
   - UI 按钮点击
   - 手势识别

2. **协调视图和模型**
   - 监听模型变化更新视图
   - 将用户操作转换为命令

3. **管理界面逻辑**
   - UI 元素的显示/隐藏
   - 动画播放控制
   - 视觉反馈

4. **事件监听**
   - 注册关心的事件
   - 响应事件更新界面

### ❌ 不应该做的事

1. **不包含业务逻辑**
   - 业务逻辑应该在 System 中
   - 控制器只负责调用和协调

2. **不直接修改模型**
   - 应该通过 Command 修改模型
   - 避免直接访问 Model 的 setter

3. **不处理复杂计算**
   - 复杂计算应该在 Query 或 System 中
   - 控制器保持简洁

4. **不保存核心状态**
   - 核心状态应该在 Model 中
   - 控制器可以保存临时 UI 状态

## 生命周期管理

### 事件注销

```csharp
public partial class MyController : Node, IController
{
    // 使用 UnRegisterList 统一管理
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public override void _Ready()
    {
        // 所有事件注册都添加到列表
        this.RegisterEvent<GameEvent>(OnGameEvent)
            .AddToUnregisterList(_unregisterList);
            
        this.GetModel<PlayerModel>().Health.Register(OnHealthChanged)
            .AddToUnregisterList(_unregisterList);
    }
    
    public override void _ExitTree()
    {
        // 节点销毁时统一注销所有事件
        _unregisterList.UnRegisterAll();
    }
}
```

### Godot 特定的生命周期

```csharp
public partial class GameController : Node, IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    // 节点进入场景树
    public override void _Ready()
    {
        // 初始化控制器
        InitializeController();
    }
    
    // 每帧更新
    public override void _Process(double delta)
    {
        // 处理实时输入
        HandleInput();
    }
    
    // 物理帧更新
    public override void _PhysicsProcess(double delta)
    {
        // 处理物理相关输入
    }
    
    // 节点即将退出场景树
    public override void _ExitTree()
    {
        // 清理资源
        CleanupController();
    }
}
```

## 最佳实践

1. **一个控制器对应一个视图**
   - 每个 Godot 场景/节点有对应的控制器
   - 避免一个控制器管理多个不相关的视图

2. **使用依赖注入获取依赖**
   - 通过 `GetModel()`、`GetSystem()` 获取依赖
   - 不要在构造函数中获取，应在 `_Ready()` 中

3. **保持控制器轻量**
   - 复杂逻辑放在 Command、Query、System 中
   - 控制器只做协调和转发

4. **合理使用缓存**
   - 频繁使用的 Model、System 可以缓存引用
   - 平衡性能和内存占用

5. **统一管理事件注销**
   - 使用 `IUnRegisterList` 统一管理
   - 在 `_ExitTree()` 中统一注销

6. **命名规范**
   - 控制器类名：`XxxController`
   - 继承 Godot 节点：`Node`、`Control`、`Node2D` 等

## 常见模式

### 数据绑定模式

```csharp
public partial class ScoreController : Label, IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        // 绑定模型数据到 UI
        this.GetModel<GameModel>()
            .Score
            .RegisterWithInitValue(score => Text = $"Score: {score}")
            .UnRegisterWhenNodeExitTree(this);
    }
}
```

### 状态机模式

```csharp
public partial class PlayerStateController : Node, IController
{
    private Dictionary<PlayerState, Action> _stateHandlers;
    
    public override void _Ready()
    {
        _stateHandlers = new Dictionary<PlayerState, Action>
        {
            { PlayerState.Idle, HandleIdleState },
            { PlayerState.Moving, HandleMovingState },
            { PlayerState.Attacking, HandleAttackingState }
        };
        
        this.GetModel<PlayerModel>()
            .State
            .Register(OnStateChanged)
            .UnRegisterWhenNodeExitTree(this);
    }
    
    private void OnStateChanged(PlayerState state)
    {
        _stateHandlers[state]?.Invoke();
    }
}
```

## 相关包

- [`architecture`](../architecture/README.md) - 提供架构访问能力
- [`command`](../command/README.md) - 控制器发送命令执行业务逻辑
- [`query`](../query/README.md) - 控制器发送查询获取数据
- [`events`](../events/README.md) - 控制器注册事件监听变化
- [`model`](../model/README.md) - 控制器读取模型数据
- [`system`](../system/README.md) - 控制器调用系统服务
- [`extensions`](../extensions/README.md) - 提供便捷的扩展方法