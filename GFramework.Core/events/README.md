# Events 包使用说明

## 概述

Events 包提供了一套完整的事件系统，实现了观察者模式（Observer Pattern）。通过事件系统，可以实现组件间的松耦合通信，支持无参和带参事件、事件注册/注销、以及灵活的事件组合。

## 核心接口

### 1. [`IEasyEvent`](IEasyEvent.cs)

基础事件接口，定义了事件注册的基本功能。

**核心方法：**
```csharp
IUnRegister Register(Action onEvent);  // 注册事件处理函数
```

### 2. [`IUnRegister`](IUnRegister.cs)

注销接口，用于取消事件注册。

**核心方法：**
```csharp
void UnRegister();  // 执行注销操作
```

### 3. [`IUnRegisterList`](IUnRegisterList.cs)

注销列表接口，用于批量管理注销对象。

**属性：**
```csharp
List<IUnRegister> UnregisterList { get; }  // 获取注销列表
```

### 4. 能力接口

- [`ICanRegisterEvent`](ICanRegisterEvent.cs) - 标记可以注册事件的组件
- [`ICanSendEvent`](ICanSendEvent.cs) - 标记可以发送事件的组件

## 核心类

### 1. [`EasyEvent`](EasyEvent.cs)

无参事件类，支持注册、注销和触发无参事件。

**使用示例：**

```csharp
// 创建事件
var onClicked = new EasyEvent();

// 注册监听
var unregister = onClicked.Register(() => 
{
    GD.Print("Button clicked!");
});

// 触发事件
onClicked.Trigger();

// 取消注册
unregister.UnRegister();
```

### 2. [`EasyEvent<T>`](EasyEventGeneric.cs)

单参数泛型事件类，支持一个参数的事件。

**使用示例：**

```csharp
// 创建带参数的事件
var onScoreChanged = new EasyEvent<int>();

// 注册监听
onScoreChanged.Register(newScore => 
{
    GD.Print($"Score changed to: {newScore}");
});

// 触发事件并传递参数
onScoreChanged.Trigger(100);
```

### 3. [`EasyEvent<T, TK>`](EasyEventGeneric.cs)

双参数泛型事件类。

**使用示例：**

```csharp
// 伤害事件：攻击者、伤害值
var onDamageDealt = new EasyEvent<string, int>();

onDamageDealt.Register((attacker, damage) =>
{
    GD.Print($"{attacker} dealt {damage} damage!");
});

onDamageDealt.Trigger("Player", 50);
```

### 4. [`EasyEvent<T, TK, TS>`](EasyEventGeneric.cs)

三参数泛型事件类。

**使用示例：**

```csharp
// 位置变化事件：对象、旧位置、新位置
var onPositionChanged = new EasyEvent<string, Vector3, Vector3>();

onPositionChanged.Register((obj, oldPos, newPos) =>
{
    GD.Print($"{obj} moved from {oldPos} to {newPos}");
});

onPositionChanged.Trigger("Player", Vector3.Zero, new Vector3(10, 0, 0));
```

### 5. [`EasyEvents`](EasyEvents.cs)

全局事件管理器，提供类型安全的事件注册和获取。

**使用示例：**

```csharp
// 注册全局事件类型
EasyEvents.Register<GameStartEvent>();

// 获取事件实例
var gameStartEvent = EasyEvents.Get<GameStartEvent>();

// 注册监听
gameStartEvent.Register(() => 
{
    GD.Print("Game started!");
});

// 触发事件
gameStartEvent.Trigger();
```

### 6. [`TypeEventSystem`](TypeEventSystem.cs)

类型化事件系统，支持基于类型的事件发送和注册。

**使用示例：**

```csharp
// 使用全局事件系统
var eventSystem = TypeEventSystem.Global;

// 注册类型化事件
eventSystem.Register<PlayerDiedEvent>(e => 
{
    GD.Print($"Player died at position: {e.Position}");
});

// 发送事件（自动创建实例）
eventSystem.Send<PlayerDiedEvent>();

// 发送事件（传递实例）
eventSystem.Send(new PlayerDiedEvent 
{ 
    Position = new Vector3(10, 0, 5) 
});
```

### 7. [`DefaultUnRegister`](DefaultUnRegister.cs)

默认注销器实现，封装注销回调。

**使用示例：**

```csharp
Action onUnregister = () => GD.Print("Unregistered");
var unregister = new DefaultUnRegister(onUnregister);

// 执行注销
unregister.UnRegister();
```

### 8. [`OrEvent`](OrEvent.cs)

事件或运算组合器，当任意一个事件触发时触发。

**使用示例：**

```csharp
var onAnyInput = new OrEvent()
    .Or(onKeyPressed)
    .Or(onMouseClicked)
    .Or(onTouchDetected);

// 当上述任意事件触发时，执行回调
onAnyInput.Register(() => 
{
    GD.Print("Input detected!");
});
```

## 在架构中使用事件

### 定义事件类

```csharp
// 简单事件
public struct GameStartedEvent { }

// 带数据的事件
public struct PlayerDiedEvent
{
    public Vector3 Position;
    public string Cause;
}

// 复杂事件
public struct LevelCompletedEvent
{
    public int LevelId;
    public float CompletionTime;
    public int Score;
    public List<string> Achievements;
}
```

### Model 中发送事件

```csharp
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
    
    protected override void OnInit()
    {
        // 监听生命值变化
        Health.Register(newHealth =>
        {
            if (newHealth <= 0)
            {
                // 发送玩家死亡事件
                this.SendEvent(new PlayerDiedEvent
                {
                    Position = Position,
                    Cause = "Health depleted"
                });
            }
        });
    }
}
```

### System 中发送事件

```csharp
public class CombatSystem : AbstractSystem
{
    protected override void OnInit() { }
    
    public void DealDamage(Character attacker, Character target, int damage)
    {
        target.Health -= damage;
        
        // 发送伤害事件
        this.SendEvent(new DamageDealtEvent
        {
            Attacker = attacker.Name,
            Target = target.Name,
            Damage = damage
        });
    }
}
```

### Controller 中注册事件

```csharp
public partial class GameController : Node, IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        // 注册多个事件
        this.RegisterEvent<GameStartedEvent>(OnGameStarted)
            .AddToUnregisterList(_unregisterList);
            
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
            .AddToUnregisterList(_unregisterList);
            
        this.RegisterEvent<LevelCompletedEvent>(OnLevelCompleted)
            .AddToUnregisterList(_unregisterList);
    }
    
    private void OnGameStarted(GameStartedEvent e)
    {
        GD.Print("Game started!");
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        GD.Print($"Player died at {e.Position}: {e.Cause}");
        ShowGameOverScreen();
    }
    
    private void OnLevelCompleted(LevelCompletedEvent e)
    {
        GD.Print($"Level {e.LevelId} completed! Score: {e.Score}");
        ShowVictoryScreen(e);
    }
    
    public override void _ExitTree()
    {
        _unregisterList.UnRegisterAll();
    }
}
```

## 高级用法

### 1. 事件链式组合

```csharp
// 使用 Or 组合多个事件
var onAnyDamage = new OrEvent()
    .Or(onPhysicalDamage)
    .Or(onMagicDamage)
    .Or(onPoisonDamage);

onAnyDamage.Register(() => 
{
    PlayDamageSound();
});
```

### 2. 事件过滤

```csharp
// 只处理高伤害事件
this.RegisterEvent<DamageDealtEvent>(e =>
{
    if (e.Damage >= 50)
    {
        ShowCriticalHitEffect();
    }
});
```

### 3. 事件转发

```csharp
public class EventBridge : AbstractSystem
{
    protected override void OnInit()
    {
        // 将内部事件转发为公共事件
        this.RegisterEvent<InternalPlayerDiedEvent>(e =>
        {
            this.SendEvent(new PublicPlayerDiedEvent
            {
                PlayerId = e.Id,
                Timestamp = DateTime.Now
            });
        });
    }
}
```

### 4. 临时事件监听

```csharp
public class TutorialController : Node, IController
{
    public override void _Ready()
    {
        // 只监听一次
        IUnRegister unregister = null;
        unregister = this.RegisterEvent<FirstEnemyKilledEvent>(e =>
        {
            ShowTutorialComplete();
            unregister?.UnRegister();  // 立即注销
        });
    }
}
```

### 5. 条件事件

```csharp
public class AchievementSystem : AbstractSystem
{
    private int _killCount = 0;
    
    protected override void OnInit()
    {
        this.RegisterEvent<EnemyKilledEvent>(e =>
        {
            _killCount++;
            
            // 条件满足时发送成就事件
            if (_killCount >= 100)
            {
                this.SendEvent(new AchievementUnlockedEvent
                {
                    AchievementId = "kill_100_enemies"
                });
            }
        });
    }
}
```

## 生命周期管理

### 使用 UnRegisterList

```csharp
public class MyController : Node, IController
{
    // 统一管理所有注销对象
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public override void _Ready()
    {
        // 所有注册都添加到列表
        this.RegisterEvent<Event1>(OnEvent1)
            .AddToUnregisterList(_unregisterList);
            
        this.RegisterEvent<Event2>(OnEvent2)
            .AddToUnregisterList(_unregisterList);
    }
    
    public override void _ExitTree()
    {
        // 一次性注销所有
        _unregisterList.UnRegisterAll();
    }
}
```

### 使用 Godot 节点生命周期

```csharp
public override void _Ready()
{
    // 当节点退出场景树时自动注销
    this.RegisterEvent<GameEvent>(OnGameEvent)
        .UnRegisterWhenNodeExitTree(this);
}
```

## 最佳实践

1. **事件命名规范**
   - 使用过去式：`PlayerDiedEvent`、`LevelCompletedEvent`
   - 使用 `Event` 后缀：便于识别
   - 使用结构体：减少内存分配

2. **事件数据设计**
   - 只包含必要信息
   - 使用值类型（struct）提高性能
   - 避免传递可变引用

3. **避免事件循环**
   - 事件处理器中谨慎发送新事件
   - 使用命令打破循环依赖

4. **合理使用事件**
   - 用于通知状态变化
   - 用于跨模块通信
   - 不用于返回数据（使用 Query）

5. **注销管理**
   - 始终注销事件监听
   - 使用 `IUnRegisterList` 批量管理
   - 利用 Godot 节点生命周期

6. **性能考虑**
   - 避免频繁触发的事件（如每帧）
   - 事件处理器保持轻量
   - 使用结构体事件减少 GC

## 事件 vs 其他通信方式

| 方式 | 适用场景 | 优点 | 缺点 |
|------|---------|------|------|
| **Event** | 状态变化通知、跨模块通信 | 松耦合、一对多 | 难以追踪调用链 |
| **Command** | 执行操作、修改状态 | 封装逻辑、可撤销 | 单向通信 |
| **Query** | 查询数据 | 职责清晰、有返回值 | 同步调用 |
| **BindableProperty** | UI 数据绑定 | 自动更新、响应式 | 仅限单一属性 |

## 相关包

- [`architecture`](../architecture/README.md) - 提供全局事件系统
- [`extensions`](../extensions/README.md) - 提供事件扩展方法
- [`property`](../property/README.md) - 可绑定属性基于事件实现
- [`controller`](../controller/README.md) - 控制器监听事件
- [`model`](../model/README.md) - 模型发送事件
- [`system`](../system/README.md) - 系统发送和监听事件