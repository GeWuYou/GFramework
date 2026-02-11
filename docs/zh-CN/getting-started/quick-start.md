# 快速开始

本指南将帮助您快速构建第一个基于 GFramework 的应用程序。

## 1. 创建项目架构

首先定义您的应用架构：

```csharp
using GFramework.Core.architecture;

public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册模型 - 存储应用状态
        RegisterModel(new PlayerModel());
        RegisterModel(new GameStateModel());
        
        // 注册系统 - 处理业务逻辑
        RegisterSystem(new PlayerSystem());
        RegisterSystem(new GameLogicSystem());
        
        // 注册工具类 - 提供辅助功能
        RegisterUtility(new StorageUtility());
    }
}
```

## 2. 定义数据模型

创建您的数据模型：

```csharp
public class PlayerModel : AbstractModel
{
    // 使用可绑定属性实现响应式数据
    public BindableProperty<string> Name { get; } = new("Player");
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> Score { get; } = new(0);
    
    protected override void OnInit()
    {
        // 监听健康值变化
        Health.Register(OnHealthChanged);
    }
    
    private void OnHealthChanged(int newHealth)
    {
        if (newHealth <= 0)
        {
            this.SendEvent(new PlayerDiedEvent());
        }
    }
}

public class GameStateModel : AbstractModel
{
    public BindableProperty<bool> IsGameRunning { get; } = new(false);
    public BindableProperty<int> CurrentLevel { get; } = new(1);
}
```

## 3. 实现业务逻辑

创建处理业务逻辑的系统：

```csharp
public class PlayerSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 监听玩家输入事件
        this.RegisterEvent<PlayerMoveEvent>(OnPlayerMove);
        this.RegisterEvent<PlayerAttackEvent>(OnPlayerAttack);
    }
    
    private void OnPlayerMove(PlayerMoveEvent e)
    {
        var playerModel = this.GetModel<PlayerModel>();
        // 处理移动逻辑
        Console.WriteLine($"Player moved to {e.Direction}");
    }
    
    private void OnPlayerAttack(PlayerAttackEvent e)
    {
        var playerModel = this.GetModel<PlayerModel>();
        // 处理攻击逻辑
        playerModel.Score.Value += 10;
        this.SendEvent(new EnemyDamagedEvent { Damage = 25 });
    }
}

public class GameLogicSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<EnemyDamagedEvent>(OnEnemyDamaged);
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }
    
    private void OnEnemyDamaged(EnemyDamagedEvent e)
    {
        Console.WriteLine($"Enemy took {e.Damage} damage");
        // 检查是否需要升级关卡
        CheckLevelProgress();
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        var gameState = this.GetModel<GameStateModel>();
        gameState.IsGameRunning.Value = false;
        Console.WriteLine("Game Over!");
    }
    
    private void CheckLevelProgress()
    {
        // 实现关卡进度检查逻辑
    }
}
```

## 4. 定义事件

创建应用中使用的事件：

```csharp
public class PlayerMoveEvent : IEvent
{
    public Vector2 Direction { get; set; }
}

public class PlayerAttackEvent : IEvent
{
    public Vector2 TargetPosition { get; set; }
}

public class PlayerDiedEvent : IEvent
{
    // 玩家死亡事件
}

public class EnemyDamagedEvent : IEvent
{
    public int Damage { get; set; }
}
```

## 5. 创建控制器

实现控制器来连接 UI 和业务逻辑：

```csharp
public class GameController : IController
{
    private IArchitecture _architecture;
    private PlayerModel _playerModel;
    private GameStateModel _gameStateModel;
    
    public GameController(IArchitecture architecture)
    {
        _architecture = architecture;
        _playerModel = architecture.GetModel<PlayerModel>();
        _gameStateModel = architecture.GetModel<GameStateModel>();
        
        // 初始化事件监听
        InitializeEventListeners();
    }
    
    private void InitializeEventListeners()
    {
        // 监听模型变化并更新 UI
        _playerModel.Health.RegisterWithInitValue(OnHealthChanged);
        _playerModel.Score.RegisterWithInitValue(OnScoreChanged);
        _gameStateModel.IsGameRunning.Register(OnGameStateChanged);
    }
    
    public void StartGame()
    {
        _gameStateModel.IsGameRunning.Value = true;
        _architecture.SendEvent(new GameStartEvent());
        Console.WriteLine("Game started!");
    }
    
    public void MovePlayer(Vector2 direction)
    {
        _architecture.SendCommand(new MovePlayerCommand { Direction = direction });
    }
    
    public void PlayerAttack(Vector2 target)
    {
        _architecture.SendCommand(new AttackCommand { TargetPosition = target });
    }
    
    // UI 更新回调
    private void OnHealthChanged(int health)
    {
        UpdateHealthDisplay(health);
    }
    
    private void OnScoreChanged(int score)
    {
        UpdateScoreDisplay(score);
    }
    
    private void OnGameStateChanged(bool isRunning)
    {
        UpdateGameStatusDisplay(isRunning);
    }
    
    private void UpdateHealthDisplay(int health)
    {
        // 更新血条 UI
        Console.WriteLine($"Health: {health}");
    }
    
    private void UpdateScoreDisplay(int score)
    {
        // 更新分数显示
        Console.WriteLine($"Score: {score}");
    }
    
    private void UpdateGameStatusDisplay(bool isRunning)
    {
        // 更新游戏状态显示
        Console.WriteLine($"Game running: {isRunning}");
    }
}
```

## 6. 定义命令

创建命令来封装用户操作：

```csharp
public class MovePlayerCommand : AbstractCommand
{
    public Vector2 Direction { get; set; }
    
    protected override void OnDo()
    {
        // 发送移动事件
        this.SendEvent(new PlayerMoveEvent { Direction = Direction });
    }
}

public class AttackCommand : AbstractCommand
{
    public Vector2 TargetPosition { get; set; }
    
    protected override void OnDo()
    {
        // 发送攻击事件
        this.SendEvent(new PlayerAttackEvent { TargetPosition = TargetPosition });
    }
}
```

## 7. 运行应用

现在让我们运行这个简单的应用：

```csharp
class Program
{
    static void Main(string[] args)
    {
        // 创建并初始化架构
        var architecture = new GameArchitecture();
        architecture.Initialize();
        
        // 创建控制器
        var gameController = new GameController(architecture);
        
        // 开始游戏
        gameController.StartGame();
        
        // 模拟玩家操作
        gameController.MovePlayer(new Vector2(1, 0));
        gameController.PlayerAttack(new Vector2(5, 5));
        
        // 模拟玩家受伤
        var playerModel = architecture.GetModel<PlayerModel>();
        playerModel.Health.Value = 50;
        
        // 模拟玩家死亡
        playerModel.Health.Value = 0;
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
```

## 8. 运行结果

执行程序后，您应该看到类似以下输出：

```
Game started!
Game running: True
Player moved to (1, 0)
Player took 25 damage
Score: 10
Health: 50
Health: 0
Player died
Game Over!
Game running: False
Press any key to exit...
```

## 下一步

这个简单的示例展示了 GFramework 的核心概念：

1. **架构模式** - 清晰的分层结构
2. **响应式数据** - BindableProperty 自动更新
3. **事件驱动** - 松耦合的组件通信
4. **命令模式** - 封装用户操作

接下来您可以：

- [深入了解架构组件](./core/architecture)
- [学习事件系统](./core/events)
- [探索 Godot 集成](./godot/overview)
- [查看完整教程](./tutorials/basic-tutorial)