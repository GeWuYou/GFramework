# Controller 包使用说明

## 概述

Controller 包定义了控制器（Controller）的接口规范。控制器是 MVC 架构中的 C 层，负责处理用户交互、协调视图和模型，是连接表现层和业务层的桥梁。

**注意**：本框架使用依赖注入模式，Controller 通过构造函数或属性注入获取架构实例，而非使用全局单例。

## 核心接口

### [`IController`](./controller.md)

控制器接口，定义了控制器需要实现的所有功能契约。

**继承的能力接口：**

- [`ICanSendCommand`](./command.md) - 可发送命令
- [`ICanGetSystem`](./system.md) - 可获取系统
- [`ICanGetModel`](./model.md) - 可获取模型
- [`ICanRegisterEvent`](./events.md) - 可注册事件
- [`ICanSendQuery`](./query.md) - 可发送查询
- [`ICanGetUtility`](./utility.md) - 可获取工具

**能力说明：**

控制器拥有框架中最全面的能力集合，可以：

1. 发送命令执行业务逻辑
2. 获取系统调用服务
3. 获取模型读写数据
4. 注册事件监听变化
5. 发送查询获取信息
6. 获取工具使用辅助功能

## 使用示例

### 基础控制器实现（依赖注入模式）

```csharp
using GFramework.Core.architecture;

// 通过依赖注入获取架构
public class PlayerController : IController
{
    private readonly IArchitecture _architecture;
    private readonly IUnRegisterList _unregisterList = new UnRegisterList();
    
    // 通过构造函数注入架构
    public PlayerController(IArchitecture architecture)
    {
        _architecture = architecture;
    }
    
    public void Initialize()
    {
        // 获取模型
        var playerModel = _architecture.GetModel<PlayerModel>();
        
        // 监听模型变化
        playerModel.Health.RegisterWithInitValue(OnHealthChanged)
            .AddToUnregisterList(_unregisterList);
        
        // 注册事件
        _architecture.RegisterEvent<PlayerLevelUpEvent>(OnPlayerLevelUp)
            .AddToUnregisterList(_unregisterList);
    }
    
    // 处理用户输入
    public void ProcessInput(double delta)
    {
        if (Input.IsActionJustPressed("attack"))
        {
            // 发送命令
            _architecture.SendCommand(new AttackCommand());
        }
        
        if (Input.IsActionJustPressed("use_item"))
        {
            // 发送查询
            var inventory = _architecture.SendQuery(new GetInventoryQuery());
            if (inventory.HasItem("potion"))
            {
                _architecture.SendCommand(new UseItemCommand("potion"));
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
    
    public void Cleanup()
    {
        // 清理事件注册
        _unregisterList.UnRegisterAll();
    }
    
    private void UpdateHealthBar(int health) { /* UI 更新逻辑 */ }
    private void ShowLevelUpEffect() { /* 特效逻辑 */ }
}
```

### UI 控制器示例

``csharp
// UI 面板控制器
public class MainMenuController : IController
{
    [Inject] private IArchitecture _architecture;
    [Inject] private IUISystem _uiSystem;
    
    [Export] private Button _startButton;
    [Export] private Button _settingsButton;
    [Export] private Button _quitButton;
    
    public void Initialize()
    {
        // 绑定按钮事件
        _startButton.Pressed += OnStartButtonPressed;
        _settingsButton.Pressed += OnSettingsButtonPressed;
        _quitButton.Pressed += OnQuitButtonPressed;
        
        // 获取模型更新 UI
        var gameModel = _architecture.GetModel<GameModel>();
        UpdateUI(gameModel);
    }
    
    private void OnStartButtonPressed()
    {
        // 通过命令启动游戏
        _architecture.SendCommand<StartGameCommand>();
    }
    
    private void OnSettingsButtonPressed()
    {
        // 查询当前设置
        var settings = _architecture.SendQuery(new GetSettingsQuery());
        
        // 打开设置面板
        _uiSystem.OpenSettingsPanel(settings);
    }
    
    private void OnQuitButtonPressed()
    {
        // 发送退出命令
        _architecture.SendCommand<QuitGameCommand>();
    }
    
    private void UpdateUI(GameModel model) { /* UI 更新逻辑 */ }
}
```

### 复杂交互控制器

``csharp
// 战斗控制器
public class CombatController : IController
{
    [Inject] protected IArchitecture _architecture;
    
    private IUnRegisterList _unregisterList = new UnRegisterList();
    private PlayerModel _playerModel;
    private CombatSystem _combatSystem;
    
    [PostConstruct]
    public void Init()
    {
        // 缓存常用引用
        _playerModel = _architecture.GetModel<PlayerModel>();
        _combatSystem = _architecture.GetSystem<CombatSystem>();
        
        // 注册多个事件
        _architecture.RegisterEvent<EnemySpawnedEvent>(OnEnemySpawned)
            .AddToUnregisterList(_unregisterList);
            
        _architecture.RegisterEvent<CombatEndedEvent>(OnCombatEnded)
            .AddToUnregisterList(_unregisterList);
        
        // 监听模型状态
        _playerModel.CombatState.Register(OnCombatStateChanged)
            .AddToUnregisterList(_unregisterList);
    }
    
    private void OnEnemySpawned(EnemySpawnedEvent e)
    {
        // 进入战斗状态
        _architecture.SendCommand(new EnterCombatCommand(e.Enemy));
    }
    
    private void OnCombatEnded(CombatEndedEvent e)
    {
        if (e.Victory)
        {
            // 查询奖励
            var rewards = _architecture.SendQuery(new CalculateRewardsQuery(e.Enemy));
            
            // 发放奖励
            _architecture.SendCommand(new GiveRewardsCommand(rewards));
        }
        else
        {
            // 处理失败
            _architecture.SendCommand<GameOverCommand>();
        }
    }
    
    private void OnCombatStateChanged(CombatState state)
    {
        // 根据战斗状态更新 UI
        _architecture.GetSystem<UISystem>().UpdateCombatUI(state);
    }
    
    public void Cleanup()
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

``csharp
public class MyController : IController
{
    [Inject] private IArchitecture _architecture;
    
    // 使用 UnRegisterList 统一管理
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public void Initialize()
    {
        // 所有事件注册都添加到列表
        _architecture.RegisterEvent<GameEvent>(OnGameEvent)
            .AddToUnregisterList(_unregisterList);
            
        _architecture.GetModel<PlayerModel>().Health.Register(OnHealthChanged)
            .AddToUnregisterList(_unregisterList);
    }
    
    public void Cleanup()
    {
        // 统一注销所有事件
        _unregisterList.UnRegisterAll();
    }
}
```

## 最佳实践

1. **使用依赖注入获取依赖**
    - 通过构造函数注入 `IArchitecture`
    - 使用 `[Inject]` 属性标记注入字段

2. **保持控制器轻量**
    - 复杂逻辑放在 Command、Query、System 中
    - 控制器只做协调和转发

3. **合理使用缓存**
    - 频繁使用的 Model、System 可以缓存引用
    - 平衡性能和内存占用

4. **统一管理事件注销**
    - 使用 `IUnRegisterList` 统一管理
    - 在 `Cleanup()` 中统一注销

5. **命名规范**
    - 控制器类名：`XxxController`
    - 使用 `[Inject]` 或构造函数注入获取架构

## 常见模式

### 数据绑定模式

``csharp
public class ScoreController : IController
{
    [Inject] private IArchitecture _architecture;
    
    public void Initialize()
    {
        // 绑定模型数据到 UI
        _architecture.GetModel<GameModel>()
            .Score
            .RegisterWithInitValue(score => UpdateDisplay(score))
            .AddToUnregisterList(_unregisterList);
    }
    
    private void UpdateDisplay(int score)
    {
        // 更新分数显示
    }
}
```

### 状态机模式

``csharp
public class PlayerStateController : IController
{
    [Inject] private IArchitecture _architecture;
    private Dictionary<PlayerState, Action> _stateHandlers;
    
    public void Initialize()
    {
        _stateHandlers = new Dictionary<PlayerState, Action>
        {
            { PlayerState.Idle, HandleIdleState },
            { PlayerState.Moving, HandleMovingState },
            { PlayerState.Attacking, HandleAttackingState }
        };
        
        _architecture.GetModel<PlayerModel>()
            .State
            .Register(OnStateChanged)
            .AddToUnregisterList(_unregisterList);
    }
    
    private void OnStateChanged(PlayerState state)
    {
        _stateHandlers[state]?.Invoke();
    }
}
```

## 与 Godot 集成

在 Godot 项目中，可以使用 GFramework.Godot 提供的扩展：

``csharp
using GFramework.Godot;

public partial class GodotPlayerController : Node, IController
{
    [Inject] private IArchitecture _architecture;
    
    public override void _Ready()
    {
        // 使用 Godot 特定的自动注销扩展
        _architecture.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
            .UnRegisterWhenNodeExitTree(this);
            
        _architecture.GetModel<PlayerModel>()
            .Health
            .RegisterWithInitValue(OnHealthChanged)
            .UnRegisterWhenNodeExitTree(this);
    }
}
```

## 相关包

- [`architecture`](./architecture.md) - 提供架构访问能力
- [`command`](./command.md) - 控制器发送命令执行业务逻辑
- [`query`](./query.md) - 控制器发送查询获取数据
- [`events`](./events.md) - 控制器注册事件监听变化
- [`model`](./model.md) - 控制器读取模型数据
- [`system`](./system.md) - 控制器调用系统服务
- [`extensions`](./extensions.md) - 提供便捷的扩展方法
- **GFramework.Godot** - Godot 特定的控制器扩展

---

**许可证**: Apache 2.0
