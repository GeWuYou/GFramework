# GFramework.Core API 参考

> GFramework.Core 模块的完整 API 参考文档，包含所有核心类、接口和方法的详细说明。

## 📋 目录

- [Architecture 核心架构](#architecture-核心架构)
- [Models 数据模型](#models-数据模型)
- [Systems 业务系统](#systems-业务系统)
- [Commands 命令模式](#commands-命令模式)
- [Queries 查询模式](#queries-查询模式)
- [Events 事件系统](#events-事件系统)
- [Properties 属性绑定](#properties-属性绑定)
- [IoC 容器](#ioc-容器)
- [Utilities 工具类](#utilities-工具类)
- [Extensions 扩展方法](#extensions-扩展方法)
- [Rules 规则接口](#rules-规则接口)

## Architecture 核心架构

### IArchitecture

架构接口，定义了框架的核心功能契约。

#### 方法签名

```csharp
// 组件注册
void RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem;
void RegisterModel<TModel>(TModel model) where TModel : IModel;
void RegisterUtility<TUtility>(TUtility utility) where TUtility : IUtility;

// 组件获取
T GetModel<T>() where T : class, IModel;
T GetSystem<T>() where T : class, ISystem;
T GetUtility<T>() where T : class, IUtility;

// 命令处理
void SendCommand(ICommand command);
TResult SendCommand<TResult>(ICommand<TResult> command);

// 查询处理
TResult SendQuery<TResult>(IQuery<TResult> query);

// 事件管理
void SendEvent<T>(T e) where T : new();
void SendEvent<T>(T e);
IUnRegister RegisterEvent<T>(Action<T> onEvent) where T : new();
void UnRegisterEvent<T>(Action<T> onEvent) where T : new();
```

#### 使用示例

```csharp
// 注册组件
architecture.RegisterModel(new PlayerModel());
architecture.RegisterSystem(new CombatSystem());
architecture.RegisterUtility(new StorageUtility());

// 获取组件
var playerModel = architecture.GetModel<PlayerModel>();
var combatSystem = architecture.GetSystem<CombatSystem>();
var storageUtility = architecture.GetUtility<StorageUtility>();

// 发送命令
architecture.SendCommand(new AttackCommand { TargetId = "enemy1" });

// 发送查询
var canAttack = architecture.SendQuery(new CanAttackQuery { PlayerId = "player1" });

// 发送事件
architecture.SendEvent(new PlayerDiedEvent { PlayerId = "player1" });

// 注册事件监听
var unregister = architecture.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
```

### Architecture

抽象架构基类，实现了 IArchitecture 接口。

#### 构造函数

```csharp
public abstract class Architecture(
    IArchitectureConfiguration? configuration = null,
    IEnvironment? environment = null,
    IArchitectureServices? services = null,
    IArchitectureContext? context = null
)
```

#### 主要属性

```csharp
public IArchitectureContext Context { get; }
public ArchitecturePhase Phase { get; }
public bool IsInitialized { get; }
public bool IsDestroyed { get; }
```

#### 主要方法

```csharp
// 初始化
public void Initialize()
public async Task InitializeAsync()

// 销毁
public void Destroy()

// 模块安装
public void InstallModule(IArchitectureModule module)
public void InstallModule<T>() where T : IArchitectureModule, new();
```

#### 架构阶段

```csharp
public enum ArchitecturePhase
{
    None = 0,
    BeforeUtilityInit = 1,
    AfterUtilityInit = 2,
    BeforeModelInit = 3,
    AfterModelInit = 4,
    BeforeSystemInit = 5,
    AfterSystemInit = 6,
    Ready = 7,
    FailedInitialization = 8,
    Destroying = 9,
    Destroyed = 10
}
```

#### 使用示例

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new CombatSystem());
        RegisterUtility(new StorageUtility());
    }
    
    protected override void InstallModules()
    {
        InstallModule(new AudioModule());
        InstallModule(new SaveModule());
    }
    
    protected override void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        switch (phase)
        {
            case ArchitecturePhase.Ready:
                GD.Print("Architecture is ready");
                break;
            case ArchitecturePhase.Destroying:
                GD.Print("Architecture is destroying");
                break;
        }
    }
}
```

### IArchitectureModule

架构模块接口。

#### 方法签名

```csharp
void Install(IArchitecture architecture);
```

#### 使用示例

```csharp
public class AudioModule : IArchitectureModule
{
    public void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new AudioSystem());
        architecture.RegisterUtility(new AudioUtility());
    }
}
```

## Models 数据模型

### IModel

模型接口，定义了模型的基本行为。

#### 继承的接口

```csharp
IContextAware
ILogAware
IAsyncInitializable
IArchitecturePhaseAware
```

#### 方法签名

```csharp
void Init();
void OnArchitecturePhase(ArchitecturePhase phase);
```

### AbstractModel

抽象模型基类，实现了 IModel 接口。

#### 使用示例

```csharp
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> MaxHealth { get; } = new(100);
    public BindableProperty<Vector2> Position { get; } = new(Vector2.Zero);
    public BindableProperty<PlayerState> State { get; } = new(PlayerState.Idle);
    
    protected override void OnInit()
    {
        Health.Register(OnHealthChanged);
        State.Register(OnStateChanged);
    }
    
    protected override void OnArchitecturePhase(ArchitecturePhase phase)
    {
        if (phase == ArchitecturePhase.Ready)
        {
            // 架构准备就绪
            SendEvent(new PlayerModelReadyEvent());
        }
    }
    
    private void OnHealthChanged(int newHealth)
    {
        if (newHealth <= 0)
        {
            State.Value = PlayerState.Dead;
            SendEvent(new PlayerDiedEvent());
        }
    }
    
    private void OnStateChanged(PlayerState newState)
    {
        SendEvent(new PlayerStateChangedEvent { NewState = newState });
    }
}

public enum PlayerState
{
    Idle,
    Moving,
    Attacking,
    Dead
}
```

## Systems 业务系统

### ISystem

系统接口，定义了系统的基本行为。

#### 继承的接口

```csharp
IContextAware
ILogAware
IAsyncInitializable
IArchitecturePhaseAware
```

#### 方法签名

```csharp
void Init();
void OnArchitecturePhase(ArchitecturePhase phase);
```

### AbstractSystem

抽象系统基类，实现了 ISystem 接口。

#### 使用示例

```csharp
public class CombatSystem : AbstractSystem
{
    private PlayerModel _playerModel;
    private EnemyModel _enemyModel;
    
    protected override void OnInit()
    {
        _playerModel = GetModel<PlayerModel>();
        _enemyModel = GetModel<EnemyModel>();
        
        // 注册事件监听
        this.RegisterEvent<AttackInputEvent>(OnAttackInput);
        this.RegisterEvent<EnemyDeathEvent>(OnEnemyDeath);
    }
    
    protected override void OnArchitecturePhase(ArchitecturePhase phase)
    {
        if (phase == ArchitecturePhase.Ready)
        {
            // 系统准备就绪
            Logger.Info("CombatSystem is ready");
        }
    }
    
    private void OnAttackInput(AttackInputEvent e)
    {
        if (_playerModel.State.Value == PlayerState.Attacking)
            return;
            
        var enemy = FindEnemyInRange();
        if (enemy != null)
        {
            PerformAttack(_playerModel, enemy);
        }
    }
    
    private void OnEnemyDeath(EnemyDeathEvent e)
    {
        Logger.Info($"Enemy {e.EnemyId} died");
        
        // 更新玩家分数
        _playerModel.Score.Value += 100;
        
        // 发送事件通知其他系统
        SendEvent(new EnemyDefeatedEvent { EnemyId = e.EnemyId });
    }
    
    private Enemy FindEnemyInRange()
    {
        // 查找范围内的敌人逻辑
        return null;
    }
    
    private void PerformAttack(PlayerModel player, Enemy enemy)
    {
        var damage = CalculateDamage(player, enemy);
        enemy.Health.Value -= damage;
        
        SendEvent(new DamageDealtEvent
        {
            AttackerId = player.Id,
            TargetId = enemy.Id,
            Damage = damage
        });
    }
    
    private int CalculateDamage(PlayerModel player, Enemy enemy)
    {
        // 伤害计算逻辑
        return player.AttackPower.Value;
    }
}
```

## Commands 命令模式

### ICommand

命令接口，定义了命令的基本行为。

#### 方法签名

```csharp
void Execute();
```

### ICommand<TResult>

带返回值的命令接口。

#### 方法签名

```csharp
TResult Execute();
```

### AbstractCommand

抽象命令基类，实现了 ICommand 接口。

#### 可用的方法

```csharp
// 获取模型
T GetModel<T>() where T : class, IModel;

// 获取系统
T GetSystem<T>() where T : class, ISystem;

// 获取工具
T GetUtility<T>() where T : class, IUtility;

// 发送事件
void SendEvent<T>(T e) where T : new();
void SendEvent<T>(T e);

// 发送命令
void SendCommand(ICommand command);
TResult SendCommand<TResult>(ICommand<TResult> command);

// 发送查询
TResult SendQuery<TResult>(IQuery<TResult> query);
```

#### 使用示例

```csharp
public class AttackCommand : AbstractCommand
{
    public string AttackerId { get; set; }
    public string TargetId { get; set; }
    public int Damage { get; set; }
    
    protected override void OnExecute()
    {
        var attacker = GetModel<PlayerModel>(AttackerId);
        var target = GetModel<EnemyModel>(TargetId);
        
        if (!CanAttack(attacker, target))
            return;
            
        PerformAttack(attacker, target, Damage);
        
        SendEvent(new AttackCompletedEvent
        {
            AttackerId = AttackerId,
            TargetId = TargetId,
            Damage = Damage
        });
    }
    
    private bool CanAttack(PlayerModel attacker, EnemyModel target)
    {
        return attacker.State.Value == PlayerState.Idle &&
               target.Health.Value > 0 &&
               Vector2.Distance(attacker.Position.Value, target.Position.Value) <= attacker.AttackRange.Value;
    }
    
    private void PerformAttack(PlayerModel attacker, EnemyModel target, int damage)
    {
        target.Health.Value -= damage;
        
        SendEvent(new DamageDealtEvent
        {
            AttackerId = attacker.Id,
            TargetId = target.Id,
            Damage = damage
        });
    }
}

public class GetPlayerScoreQuery : AbstractQuery<int>
{
    public string PlayerId { get; set; }
    
    protected override int OnDo()
    {
        var playerModel = GetModel<PlayerModel>(PlayerId);
        return playerModel.Score.Value;
    }
}

// 使用示例
var attackCommand = new AttackCommand
{
    AttackerId = "player1",
    TargetId = "enemy1",
    Damage = 50
};

Context.SendCommand(attackCommand);

var scoreQuery = new GetPlayerScoreQuery { PlayerId = "player1" };
var score = Context.SendQuery(scoreQuery);
```

## Queries 查询模式

### IQuery<TResult>

查询接口，定义了查询的基本行为。

#### 方法签名

```csharp
TResult OnDo();
```

### AbstractQuery<TResult>

抽象查询基类，实现了 IQuery<TResult> 接口。

#### 可用的方法

与 AbstractCommand 相同，提供：

- GetModel\<T>()
- GetSystem\<T>()
- GetUtility\<T>()
- SendEvent\<T>()
- SendCommand()
- SendQuery()

#### 使用示例

```csharp
public class CanAttackQuery : AbstractQuery<bool>
{
    public string PlayerId { get; set; }
    public string TargetId { get; set; }
    
    protected override bool OnDo()
    {
        var player = GetModel<PlayerModel>(PlayerId);
        var target = GetModel<EnemyModel>(TargetId);
        
        if (player.State.Value != PlayerState.Idle)
            return false;
            
        if (player.Health.Value <= 0)
            return false;
            
        if (target.Health.Value <= 0)
            return false;
            
        var distance = Vector2.Distance(
            player.Position.Value, 
            target.Position.Value
        );
        
        return distance <= player.AttackRange.Value;
    }
}

public class GetInventoryItemsQuery : AbstractQuery<List<InventoryItem>>
{
    public string PlayerId { get; set; }
    public string ItemType { get; set; }
    
    protected override List<InventoryItem> OnDo()
    {
        var inventorySystem = GetSystem<InventorySystem>();
        return inventorySystem.GetPlayerItems(PlayerId, ItemType);
    }
}

public class GetGameStatisticsQuery : AbstractQuery<GameStatistics>
{
    protected override GameStatistics OnDo()
    {
        var playerModel = GetModel<PlayerModel>();
        var gameModel = GetModel<GameModel>();
        
        return new GameStatistics
        {
            PlayerLevel = playerModel.Level.Value,
            PlayerScore = playerModel.Score.Value,
            PlayerHealth = playerModel.Health.Value,
            PlayTime = gameModel.PlayTime.Value,
            EnemiesDefeated = gameModel.EnemiesDefeated.Value
        };
    }
}

// 使用示例
var canAttackQuery = new CanAttackQuery { PlayerId = "player1", TargetId = "enemy1" };
var canAttack = Context.SendQuery(canAttackQuery);

var inventoryQuery = new GetInventoryItemsQuery 
{ 
    PlayerId = "player1", 
    ItemType = "weapon" 
};
var weapons = Context.SendQuery(inventoryQuery);

var statsQuery = new GetGameStatisticsQuery();
var stats = Context.SendQuery(statsQuery);
```

## Events 事件系统

### IEvent

事件标记接口，用于标识事件类型。

```csharp
public interface IEvent
{
    // 事件类型标识
}
```

### IUnRegister

注销接口，用于取消事件注册。

#### 方法签名

```csharp
void UnRegister();
```

### IUnRegisterList

注销列表接口，用于批量管理注销对象。

#### 属性

```csharp
IList<IUnRegister> UnregisterList { get; }
```

#### 方法签名

```csharp
void Add(IUnRegister unRegister);
void UnRegisterAll();
```

### EasyEvent

无参事件类。

#### 构造函数

```csharp
public EasyEvent()
```

#### 方法

```csharp
IUnRegister Register(Action onEvent);
void Trigger();
```

#### 使用示例

```csharp
// 创建事件
var buttonClicked = new EasyEvent();

// 注册监听
var unregister = buttonClicked.Register(() => {
    GD.Print("Button clicked!");
});

// 触发事件
buttonClicked.Trigger();

// 注销监听
unregister.UnRegister();
```

### Event<T>

单参数泛型事件类。

#### 构造函数

```csharp
public Event<T>()
```

#### 方法

```csharp
IUnRegister Register(Action<T> onEvent);
void Trigger(T data);
```

#### 使用示例

```csharp
// 创建事件
var scoreChanged = new Event<int>();

// 注册监听
var unregister = scoreChanged.Register(score => {
    GD.Print($"Score changed to: {score}");
});

// 触发事件
scoreChanged.Trigger(100);

// 注销监听
unregister.UnRegister();
```

### EventBus

事件总线类，提供基于类型的事件发送和注册。

#### 方法

```csharp
IUnRegister Register<T>(Action<T> onEvent) where T : new();
void Send<T>(T e) where T : new();
void Send<T>() where T : new();
```

#### 使用示例

```csharp
// 创建事件总线
var eventBus = new EventBus();

// 注册事件
eventBus.Register<PlayerDiedEvent>(e => {
    GD.Print($"Player died at position: {e.Position}");
});

// 发送事件
eventBus.Send(new PlayerDiedEvent 
{ 
    Position = new Vector3(10, 0, 5) 
});
```

## Properties 属性绑定

### BindableProperty<T>

可绑定属性类，支持属性变化监听。

#### 构造函数

```csharp
public BindableProperty(T initialValue = default)
```

#### 属性

```csharp
public T Value { get; set; }
public T PreviousValue { get; }
```

#### 方法

```csharp
IUnRegister Register(Action<T> onValueChanged);
IUnRegister RegisterWithInitValue(Action<T> onValueChanged);
void SetValueWithoutNotify(T value);
```

#### 使用示例

```csharp
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<Vector2> Position { get; } = new(Vector2.Zero);
    
    protected override void OnInit()
    {
        // 注册健康值变化监听
        Health.Register(OnHealthChanged);
        
        // 注册位置变化监听（包含初始值）
        Position.RegisterWithInitValue(OnPositionChanged);
    }
    
    private void OnHealthChanged(int newHealth)
    {
        if (newHealth <= 0)
        {
            SendEvent(new PlayerDiedEvent());
        }
    }
    
    private void OnPositionChanged(Vector2 newPosition)
    {
        // 处理位置变化
        UpdatePlayerPosition(newPosition);
    }
}

// 外部使用
var playerModel = new PlayerModel();

// 监听属性变化
var healthUnregister = playerModel.Health.Register(health => {
    GD.Print($"Player health: {health}");
});

// 设置属性值
playerModel.Health.Value = 80;  // 会触发监听器
playerModel.Health.SetValueWithoutNotify(90);  // 不会触发监听器

// 注销监听
healthUnregister.UnRegister();
```

## IoC 容器

### IBelongToArchitecture

属于架构的接口标记。

### IContextAware

上下文感知接口。

#### 属性

```csharp
IContextAware.Context Context { get; }
void SetContext(IContextAware.Context context);
```

### ILogAware

日志感知接口。

#### 属性

```csharp
ILogger Logger { get; }
```

### ContextAwareBase

上下文感知基类，实现了 IContextAware 接口。

#### 属性

```csharp
public IContextAware.Context Context { get; }
```

#### 使用示例

```csharp
public class MyService : ContextAwareBase
{
    public void DoWork()
    {
        var playerModel = Context.GetModel<PlayerModel>();
        Logger.Info("Doing work with player model");
    }
}
```

## Utilities 工具类

### IUtility

工具接口，标记工具类。

#### 方法签名

```csharp
void Init();
```

### AbstractUtility

抽象工具基类。

#### 可用的方法

与 AbstractCommand 相同，提供：

- GetModel\<T>()
- GetSystem\<T>()
- GetUtility\<T>()
- SendEvent\<T>()
- SendCommand()
- SendQuery()

#### 使用示例

```csharp
public class MathUtility : AbstractUtility
{
    public float Distance(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b);
    }
    
    public float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
    
    public Vector2 Normalize(Vector2 vector)
    {
        if (vector == Vector2.Zero)
            return Vector2.Zero;
            
        return vector.Normalized();
    }
}

public class StorageUtility : AbstractUtility
{
    private readonly IStorage _storage;
    
    public StorageUtility(IStorage storage)
    {
        _storage = storage;
    }
    
    protected override void OnInit()
    {
        Logger.Info("Storage utility initialized");
    }
    
    public async Task SaveAsync<T>(string key, T data)
    {
        await _storage.WriteAsync(key, data);
        Logger.Info($"Saved data to key: {key}");
    }
    
    public async Task<T> LoadAsync<T>(string key, T defaultValue = default)
    {
        var data = await _storage.ReadAsync<T>(key, defaultValue);
        Logger.Info($"Loaded data from key: {key}");
        return data;
    }
}
```

## Extensions 扩展方法

### 组件获取扩展

```csharp
// 为 IArchitectureContext 提供扩展方法
public static class ArchitectureContextExtensions
{
    public static T GetModel<T>(this IContextAware.Context context) where T : class, IModel;
    public static T GetSystem<T>(this IContextAware.Context context) where T : class, ISystem;
    public static T GetUtility<T>(this IContextAware.Context context) where T : class, IUtility;
}
```

### 事件扩展

```csharp
// 为事件注册提供扩展方法
public static class EventExtensions
{
    public static IUnRegister UnRegisterWhenNodeExitTree<T>(
        this IUnRegister unRegister, 
        Node node) where T : new();
        
    public static IUnRegister AddToUnregisterList(
        this IUnRegister unRegister,
        IUnRegisterList unregisterList);
}
```

#### 使用示例

```csharp
// 使用扩展方法
var playerModel = Context.GetModel<PlayerModel>();
var combatSystem = Context.GetSystem<CombatSystem>();
var storageUtility = Context.GetUtility<StorageUtility>();

// 使用事件扩展
var unregister = this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
    .UnRegisterWhenNodeExitTree(this);

var unregisterList = new UnRegisterList();
this.RegisterEvent<GameEvent>(OnGameEvent)
    .AddToUnregisterList(unregisterList);
```

## Rules 规则接口

### IRule

规则接口标记。

### ICanGetModel

可获取模型的规则接口。

```csharp
T GetModel<T>() where T : class, IModel;
```

### ICanGetSystem

可获取系统的规则接口。

```csharp
T GetSystem<T>() where T : class, ISystem;
```

### ICanGetUtility

可获取工具的规则接口。

```csharp
T GetUtility<T>() where T : class, IUtility;
```

### ICanSendCommand

可发送命令的规则接口。

```csharp
void SendCommand(ICommand command);
TResult SendCommand<TResult>(ICommand<TResult> command);
```

### ICanSendQuery

可发送查询的规则接口。

```csharp
TResult SendQuery<TResult>(IQuery<TResult> query);
```

### ICanRegisterEvent

可注册事件的规则接口。

```csharp
IUnRegister RegisterEvent<T>(Action<T> onEvent) where T : new();
```

### IController

控制器接口，组合了多个规则接口。

```csharp
public interface IController : 
    ICanGetModel, 
    ICanGetSystem, 
    ICanGetUtility,
    ICanSendCommand,
    ICanSendQuery,
    ICanRegisterEvent
{
}
```

## 错误处理

### GFrameworkException

框架基础异常类。

```csharp
public class GFrameworkException : Exception
{
    public GFrameworkException(string message) : base(message) { }
    public GFrameworkException(string message, Exception innerException) : base(message, innerException) { }
}
```

### ArchitectureException

架构相关异常。

```csharp
public class ArchitectureException : GFrameworkException
{
    public ArchitectureException(string message) : base(message) { }
}
```

### ComponentException

组件相关异常。

```csharp
public class ComponentException : GFrameworkException
{
    public ComponentException(string message) : base(message) { }
}
```

---

**文档版本**: 1.0.0  
**更新日期**: 2026-01-12