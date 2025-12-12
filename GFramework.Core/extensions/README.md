# Extensions 包使用说明

## 概述

Extensions 包提供了一系列扩展方法，简化了框架各个接口的使用。通过扩展方法，可以用更简洁的语法访问框架功能，提高代码可读性和开发效率。

## 扩展方法类别

### 1. 获取组件扩展 ([`CanGetExtensions.cs`](CanGetExtensions.cs))

为 [`ICanGetModel`](../model/ICanGetModel.cs)、[`ICanGetSystem`](../system/ICanGetSystem.cs)、[
`ICanGetUtility`](../utility/ICanGetUtility.cs) 提供扩展方法。

#### CanGetModelExtension

```csharp
public static T GetModel<T>(this ICanGetModel self) where T : class, IModel
```

**使用示例：**

```csharp
// 在 Controller、Command、Query 中使用
public class PlayerController : IController
{
    public void UpdateUI()
    {
        // 直接通过 this 调用
        var playerModel = this.GetModel<PlayerModel>();
        var inventoryModel = this.GetModel<InventoryModel>();
    }
}
```

#### CanGetSystemExtension

```csharp
public static T GetSystem<T>(this ICanGetSystem self) where T : class, ISystem
```

**使用示例：**

```csharp
public class SaveCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        // 获取系统
        var saveSystem = this.GetSystem<SaveSystem>();
        var networkSystem = this.GetSystem<NetworkSystem>();
        
        saveSystem.SaveGame();
    }
}
```

#### CanGetUtilityExtension

```csharp
public static T GetUtility<T>(this ICanGetUtility self) where T : class, IUtility
```

**使用示例：**

```csharp
public class GameModel : AbstractModel
{
    protected override void OnInit()
    {
        // 获取工具
        var timeUtility = this.GetUtility<TimeUtility>();
        var storageUtility = this.GetUtility<StorageUtility>();
    }
}
```

### 2. 发送命令扩展 ([`CanSendExtensions.cs`](CanSendExtensions.cs))

为 [`ICanSendCommand`](../command/ICanSendCommand.cs) 提供扩展方法。

#### CanSendCommandExtension

```csharp
// 发送无参命令（通过类型）
public static void SendCommand<T>(this ICanSendCommand self) 
    where T : ICommand, new()

// 发送命令实例
public static void SendCommand<T>(this ICanSendCommand self, T command) 
    where T : ICommand

// 发送带返回值的命令
public static TResult SendCommand<TResult>(this ICanSendCommand self, 
    ICommand<TResult> command)
```

**使用示例：**

```csharp
public class GameController : IController
{
    public void OnStartButtonClicked()
    {
        // 方式1：通过类型发送（需要无参构造函数）
        this.SendCommand<StartGameCommand>();
        
        // 方式2：发送命令实例
        this.SendCommand(new LoadLevelCommand(levelId: 1));
        
        // 方式3：发送带返回值的命令
        var score = this.SendCommand(new CalculateScoreCommand());
    }
}
```

### 3. 发送事件扩展 ([`CanSendExtensions.cs`](CanSendExtensions.cs))

为 [`ICanSendEvent`](../events/ICanSendEvent.cs) 提供扩展方法。

#### CanSendEventExtension

```csharp
// 发送无参事件
public static void SendEvent<T>(this ICanSendEvent self) where T : new()

// 发送事件实例
public static void SendEvent<T>(this ICanSendEvent self, T e)
```

**使用示例：**

```csharp
public class PlayerModel : AbstractModel
{
    public void TakeDamage(int damage)
    {
        Health -= damage;
        
        if (Health <= 0)
        {
            // 方式1：发送无参事件
            this.SendEvent<PlayerDiedEvent>();
            
            // 方式2：发送带数据的事件
            this.SendEvent(new PlayerDiedEvent 
            { 
                Position = Position,
                Cause = "Damage" 
            });
        }
    }
}
```

### 4. 发送查询扩展 ([`CanSendExtensions.cs`](CanSendExtensions.cs))

为 [`ICanSendQuery`](../query/ICanSendQuery.cs) 提供扩展方法。

#### CanSendQueryExtension

```csharp
public static TResult SendQuery<TResult>(this ICanSendQuery self, 
    IQuery<TResult> query)
```

**使用示例：**

```csharp
public class InventoryController : IController
{
    public void ShowInventory()
    {
        // 发送查询获取数据
        var items = this.SendQuery(new GetInventoryItemsQuery());
        var gold = this.SendQuery(new GetPlayerGoldQuery());
        
        UpdateInventoryUI(items, gold);
    }
}
```

### 5. 注册事件扩展 ([`CanRegisterEventExtensions.cs`](CanRegisterEventExtensions.cs))

为 [`ICanRegisterEvent`](../events/ICanRegisterEvent.cs) 提供扩展方法。

#### CanRegisterEventExtensions

```csharp
// 注册事件
public static IUnRegister RegisterEvent<T>(this ICanRegisterEvent self, 
    Action<T> onEvent)

// 注销事件
public static void UnRegisterEvent<T>(this ICanRegisterEvent self, 
    Action<T> onEvent)
```

**使用示例：**

```csharp
public class GameController : Node, IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public override void _Ready()
    {
        // 注册事件监听
        this.RegisterEvent<GameStartedEvent>(OnGameStarted)
            .AddToUnregisterList(_unregisterList);
            
        this.RegisterEvent<PlayerLevelUpEvent>(OnPlayerLevelUp)
            .AddToUnregisterList(_unregisterList);
    }
    
    private void OnGameStarted(GameStartedEvent e) { }
    private void OnPlayerLevelUp(PlayerLevelUpEvent e) { }
}
```

### 6. OrEvent 扩展 ([`OrEventExtensions.cs`](OrEventExtensions.cs))

为 [`IEasyEvent`](../events/IEasyEvent.cs) 提供事件组合功能。

#### OrEventExtensions

```csharp
public static OrEvent Or(this IEasyEvent self, IEasyEvent e)
```

**使用示例：**

```csharp
// 组合多个事件：当任意一个触发时执行
var onAnyInput = onKeyPressed.Or(onMouseClicked).Or(onTouchDetected);

onAnyInput.Register(() => 
{
    GD.Print("Any input detected!");
});

// 链式组合
var onAnyDamage = onPhysicalDamage
    .Or(onMagicDamage)
    .Or(onPoisonDamage);
```

### 7. UnRegister 扩展 ([`UnRegisterExtension.cs`](UnRegisterExtension.cs))

为 [`IUnRegister`](../events/IUnRegister.cs) 提供 Godot 生命周期绑定。

#### UnRegisterExtension

```csharp
public static IUnRegister UnRegisterWhenNodeExitTree(this IUnRegister unRegister, 
    Node node)
```

**使用示例：**

```csharp
#if GODOT
public class PlayerController : Node, IController
{
    public override void _Ready()
    {
        // 当节点退出场景树时自动注销
        this.RegisterEvent<GameEvent>(OnGameEvent)
            .UnRegisterWhenNodeExitTree(this);
            
        this.GetModel<PlayerModel>()
            .Health
            .Register(OnHealthChanged)
            .UnRegisterWhenNodeExitTree(this);
    }
    
    // 不需要手动在 _ExitTree 中注销
}
#endif
```

### 8. UnRegisterList 扩展 ([`UnRegisterListExtension.cs`](UnRegisterListExtension.cs))

为 [`IUnRegister`](../events/IUnRegister.cs) 和 [`IUnRegisterList`](../events/IUnRegisterList.cs) 提供批量管理功能。

#### UnRegisterListExtension

```csharp
// 添加到注销列表
public static void AddToUnregisterList(this IUnRegister self, 
    IUnRegisterList unRegisterList)

// 批量注销
public static void UnRegisterAll(this IUnRegisterList self)
```

**使用示例：**

```csharp
public class ComplexController : Node, IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public override void _Ready()
    {
        // 所有注册都添加到列表中
        this.RegisterEvent<Event1>(OnEvent1)
            .AddToUnregisterList(_unregisterList);
            
        this.RegisterEvent<Event2>(OnEvent2)
            .AddToUnregisterList(_unregisterList);
            
        this.GetModel<Model1>().Property1.Register(OnProperty1Changed)
            .AddToUnregisterList(_unregisterList);
            
        this.GetModel<Model2>().Property2.Register(OnProperty2Changed)
            .AddToUnregisterList(_unregisterList);
    }
    
    public override void _ExitTree()
    {
        // 一次性注销所有
        _unregisterList.UnRegisterAll();
    }
}
```

## 完整使用示例

### Controller 示例

```csharp
public partial class GameplayController : Node, IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        // 使用扩展方法获取 Model
        var playerModel = this.GetModel<PlayerModel>();
        var gameModel = this.GetModel<GameModel>();
        
        // 使用扩展方法注册事件
        this.RegisterEvent<GameStartedEvent>(OnGameStarted)
            .AddToUnregisterList(_unregisterList);
        
        // 监听可绑定属性
        playerModel.Health.Register(OnHealthChanged)
            .AddToUnregisterList(_unregisterList);
        
        // 或者使用 Godot 特定的自动注销
        gameModel.Score.Register(OnScoreChanged)
            .UnRegisterWhenNodeExitTree(this);
    }
    
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("attack"))
        {
            // 使用扩展方法发送命令
            this.SendCommand(new AttackCommand(targetId: 1));
        }
        
        if (Input.IsActionJustPressed("use_item"))
        {
            // 使用扩展方法发送查询
            var hasPotion = this.SendQuery(new HasItemQuery("health_potion"));
            if (hasPotion)
            {
                this.SendCommand<UseHealthPotionCommand>();
            }
        }
    }
    
    private void OnGameStarted(GameStartedEvent e)
    {
        GD.Print("Game started!");
    }
    
    private void OnHealthChanged(int health)
    {
        UpdateHealthBar(health);
    }
    
    private void OnScoreChanged(int score)
    {
        UpdateScoreDisplay(score);
    }
    
    public override void _ExitTree()
    {
        _unregisterList.UnRegisterAll();
    }
}
```

### Command 示例

```csharp
public class ComplexGameCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        // 获取多个组件
        var playerModel = this.GetModel<PlayerModel>();
        var gameSystem = this.GetSystem<GameSystem>();
        var timeUtility = this.GetUtility<TimeUtility>();
        
        // 执行业务逻辑
        var currentTime = timeUtility.GetCurrentTime();
        gameSystem.ProcessGameLogic(playerModel, currentTime);
        
        // 发送事件通知
        this.SendEvent(new GameStateChangedEvent());
        
        // 可以发送其他命令（谨慎使用）
        this.SendCommand<SaveGameCommand>();
    }
}
```

### System 示例

```csharp
public class AchievementSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 注册事件监听
        this.RegisterEvent<EnemyKilledEvent>(OnEnemyKilled);
        this.RegisterEvent<LevelCompletedEvent>(OnLevelCompleted);
    }
    
    private void OnEnemyKilled(EnemyKilledEvent e)
    {
        // 获取模型
        var playerModel = this.GetModel<PlayerModel>();
        playerModel.EnemyKillCount++;
        
        // 检查成就
        if (playerModel.EnemyKillCount >= 100)
        {
            // 发送成就解锁事件
            this.SendEvent(new AchievementUnlockedEvent 
            { 
                AchievementId = "kill_100_enemies" 
            });
        }
    }
    
    private void OnLevelCompleted(LevelCompletedEvent e)
    {
        // 发送查询
        var completionTime = this.SendQuery(new GetLevelTimeQuery(e.LevelId));
        
        if (completionTime < 60)
        {
            this.SendEvent(new AchievementUnlockedEvent 
            { 
                AchievementId = "speed_runner" 
            });
        }
    }
}
```

## 扩展方法的优势

1. **简洁的语法**：不需要显式调用 `GetArchitecture()`
2. **类型安全**：编译时检查类型
3. **可读性高**：代码意图更清晰
4. **智能提示**：IDE 可以提供完整的自动补全
5. **链式调用**：支持流式编程风格

## 注意事项

1. **确保引用命名空间**：
   ```csharp
   using GFramework.framework.extensions;
   ```

2. **理解扩展方法本质**：
    - 扩展方法是静态方法的语法糖
    - 不会改变原始类型的结构
    - 仅在编译时解析

3. **Godot 特定功能**：
    - `UnRegisterWhenNodeExitTree` 仅在 Godot 环境下可用
    - 使用 `#if GODOT` 编译指令控制

4. **性能考虑**：
    - 扩展方法本身无性能开销
    - 实际调用的是底层方法

## 相关包

- [`architecture`](../architecture/README.md) - 扩展方法最终调用架构方法
- [`command`](../command/README.md) - 命令发送扩展
- [`query`](../query/README.md) - 查询发送扩展
- [`events`](../events/README.md) - 事件注册和 Or 组合扩展
- [`model`](../model/README.md) - 模型获取扩展
- [`system`](../system/README.md) - 系统获取扩展
- [`utility`](../utility/README.md) - 工具获取扩展