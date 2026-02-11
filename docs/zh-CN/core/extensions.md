# Extensions 包使用说明

## 概述

Extensions 包提供了一系列扩展方法，简化了框架各个接口的使用。通过扩展方法，可以用更简洁的语法访问框架功能，提高代码可读性和开发效率。

## 扩展方法类别

### 1. ContextAware 扩展 ([`ContextAwareExtensions.cs`](ContextAwareExtensions.cs))

为 [`IContextAware`](../../GFramework.Core.Abstractions/rule/IContextAware.cs)
提供扩展方法，允许直接从实现了 [IContextAware](file:///d:/Project/Rider/GFramework/GFramework.Core.Abstractions/rule/IContextAware.cs)
的对象获取架构组件。

#### GetSystem 扩展方法

```csharp
public static TSystem? GetSystem<TSystem>(this IContextAware contextAware) 
    where TSystem : class, ISystem
```

**使用示例：**

```csharp
// 在实现了 IContextAware 的类中使用
public class PlayerController : IController
{
    public void UpdateUI()
    {
        // 直接通过 this 调用
        var playerSystem = this.GetSystem<PlayerSystem>();
        var inventorySystem = this.GetSystem<InventorySystem>();
    }
}
```

#### GetModel 扩展方法

```csharp
public static TModel? GetModel<TModel>(this IContextAware contextAware) 
    where TModel : class, IModel
```

**使用示例：**

```csharp
public class PlayerController : IController
{
    public void UpdateStats()
    {
        // 获取模型
        var playerModel = this.GetModel<PlayerModel>();
        var inventoryModel = this.GetModel<InventoryModel>();
        
        // 使用模型数据
        playerModel.Health += 10;
    }
}
```

#### GetUtility 扩展方法

```csharp
public static TUtility? GetUtility<TUtility>(this IContextAware contextAware) 
    where TUtility : class, IUtility
```

**使用示例：**

```csharp
public class GameModel : AbstractModel, IContextAware
{
    protected override void OnInit()
    {
        // 获取工具
        var timeUtility = this.GetUtility<TimeUtility>();
        var storageUtility = this.GetUtility<StorageUtility>();
    }
}
```

#### SendCommand 扩展方法

```csharp
// 发送无返回值的命令
public static void SendCommand(this IContextAware contextAware, ICommand command)

// 发送带返回值的命令
public static TResult SendCommand<TResult>(this IContextAware contextAware, ICommand<TResult> command)
```

**使用示例：**

```csharp
public class GameController : IController
{
    public void OnStartButtonClicked()
    {
        // 发送命令实例
        this.SendCommand(new StartGameCommand());
        
        // 发送带返回值的命令
        var result = this.SendCommand(new CalculateScoreCommand());
    }
}
```

#### SendQuery 扩展方法

```csharp
public static TResult SendQuery<TResult>(this IContextAware contextAware, IQuery<TResult> query)
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

#### SendEvent 扩展方法

```csharp
// 发送无参事件
public static void SendEvent<T>(this IContextAware contextAware) where T : new()

// 发送事件实例
public static void SendEvent<T>(this IContextAware contextAware, T e) where T : class
```

**使用示例：**

```csharp
public class PlayerModel : AbstractModel, IContextAware
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

#### RegisterEvent 扩展方法

```csharp
public static IUnRegister RegisterEvent<TEvent>(this IContextAware contextAware, Action<TEvent> handler)
```

**使用示例：**

```csharp
public class GameController : IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public void Initialize()
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

#### UnRegisterEvent 扩展方法

```csharp
public static void UnRegisterEvent<TEvent>(this IContextAware contextAware, Action<TEvent> onEvent)
```

### GetEnvironment 扩展方法

```csharp
public static T? GetEnvironment<T>(this IContextAware contextAware) where T : class
public static IEnvironment GetEnvironment(this IContextAware contextAware)
```

### 2. Object 扩展 ([`ObjectExtensions.cs`](ObjectExtensions.cs))

提供基于运行时类型判断的对象扩展方法，用于简化类型分支、链式调用和架构分派逻辑。

#### IfType 扩展方法

```csharp
// 最简单的类型判断
public static bool IfType<T>(this object obj, Action<T> action)

// 带条件的类型判断
public static bool IfType<T>(
    this object obj, 
    Func<T, bool> predicate, 
    Action<T> action
)

// 条件判断，带不匹配时的处理
public static void IfType<T>(
    this object obj, 
    Action<T> whenMatch, 
    Action<object>? whenNotMatch = null
)
```

**使用示例：**

```csharp
object obj = new MyRule();

// 简单类型判断
bool executed = obj.IfType<MyRule>(rule =>
{
    rule.Initialize();
});

// 带条件的类型判断
obj.IfType<MyRule>(
    r => r.Enabled,      // 条件
    r => r.Execute()     // 执行动作
);

// 带不匹配处理的类型判断
obj.IfType<IRule>(
    rule => rule.Execute(),
    other => Logger.Warn($"Unsupported type: {other.GetType()}")
);
```

#### IfType`<T, TResult>` 扩展方法

```csharp
public static TResult? IfType<T, TResult>(
    this object obj,
    Func<T, TResult> func
)
```

**使用示例：**

```csharp
object obj = new MyRule { Name = "TestRule" };

string? name = obj.IfType<MyRule, string>(r => r.Name);
```

#### As 和 Do 扩展方法

```csharp
// 安全类型转换
public static T? As<T>(this object obj) where T : class

// 流式调用
public static T Do<T>(this T obj, Action<T> action)
```

**使用示例：**

```csharp
// 安全类型转换
obj.As<MyRule>()
   ?.Execute();

// 流式调用
obj.As<MyRule>()
   ?.Do(r => r.Initialize())
   ?.Do(r => r.Execute());

// 组合使用
obj.As<MyRule>()
   ?.Do(rule => 
   {
       if (rule.Enabled)
           rule.Execute();
   });
```

#### SwitchType 扩展方法

```csharp
public static void SwitchType(
    this object obj,
    params (Type type, Action<object> action)[] handlers
)
```

**使用示例：**

```csharp
obj.SwitchType(
    (typeof(IRule), o => HandleRule((IRule)o)),
    (typeof(ISystem), o => HandleSystem((ISystem)o)),
    (typeof(IModel), o => HandleModel((IModel)o))
);
```

### 3. OrEvent 扩展 ([`OrEventExtensions.cs`](OrEventExtensions.cs))

为 [`IEvent`](../../GFramework.Core.Abstractions/events/IEvent.cs) 提供事件组合功能。

#### OrEventExtensions

```csharp
public static OrEvent Or(this IEvent self, IEvent e)
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

### 4. UnRegisterList 扩展 ([`UnRegisterListExtension.cs`](UnRegisterListExtension.cs))

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
public class ComplexController : IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public void Initialize()
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
    
    public void Cleanup()
    {
        // 一次性注销所有
        _unregisterList.UnRegisterAll();
    }
}
```

## 完整使用示例

### Controller 示例

```csharp
public partial class GameplayController : IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public void Initialize()
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
    }
    
    public void Process(double delta)
    {
        // 发送命令
        this.SendCommand(new AttackCommand(targetId: 1));
        
        // 发送查询
        var hasPotion = this.SendQuery(new HasItemQuery("health_potion"));
        if (hasPotion)
        {
            this.SendCommand<UseHealthPotionCommand>();
        }
    }
    
    private void OnGameStarted(GameStartedEvent e)
    {
        Console.WriteLine("Game started!");
    }
    
    private void OnHealthChanged(int health)
    {
        UpdateHealthBar(health);
    }
    
    public void Cleanup()
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

1. **简洁的语法**
   ：不需要显式调用 [GetContext()](file:///d:/Project/Rider/GFramework/GFramework.Core.Abstractions/rule/IContextAware.cs#L13-L15)
2. **类型安全**：编译时检查类型
3. **可读性高**：代码意图更清晰
4. **智能提示**：IDE 可以提供完整的自动补全
5. **链式调用**：支持流式编程风格

## 注意事项

1. **确保引用命名空间：**
   ```csharp
   using GFramework.Core.extensions;
   ```

2. **理解扩展方法本质：**
    - 扩展方法是静态方法的语法糖
    - 不会改变原始类型的结构
    - 仅在编译时解析

3. **性能考虑：**
    - 扩展方法本身无性能开销
    - 实际调用的是底层方法

## 相关包

- [`architecture`](./architecture.md) - 扩展方法最终调用架构方法
- [`command`](./command.md) - 命令发送扩展
- [`query`](./query.md) - 查询发送扩展
- [`events`](./events.md) - 事件注册和 Or 组合扩展
- [`model`](./model.md) - 模型获取扩展
- [`system`](./system.md) - 系统获取扩展
- [`utility`](./utility.md) - 工具获取扩展