# 最佳实践

本文档总结了使用 GFramework 的最佳实践和设计模式。

## 架构设计

### 1. 清晰的职责分离

**原则**：每一层都有明确的职责，不要混淆。

```csharp
// ✅ 正确的职责分离
public class PlayerModel : AbstractModel
{
    // Model：只存储数据
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> Score { get; } = new(0);
}

public class CombatSystem : AbstractSystem
{
    // System：处理业务逻辑
    protected override void OnInit()
    {
        this.RegisterEvent<AttackEvent>(OnAttack);
    }

    private void OnAttack(AttackEvent e)
    {
        var player = this.GetModel<PlayerModel>();
        player.Health.Value -= e.Damage;
    }
}

public class PlayerController : IController
{
    // Controller：连接 UI 和逻辑
    public void Initialize()
    {
        var player = _architecture.GetModel<PlayerModel>();
        player.Health.RegisterWithInitValue(OnHealthChanged);
    }

    private void OnHealthChanged(int health)
    {
        UpdateHealthDisplay(health);
    }
}
```

### 2. 事件驱动设计

**原则**：使用事件解耦组件，避免直接调用。

```csharp
// ❌ 紧耦合
public class SystemA : AbstractSystem
{
    private void OnEvent(EventA e)
    {
        var systemB = this.GetSystem<SystemB>();
        systemB.DoSomething(); // 直接调用
    }
}

// ✅ 松耦合
public class SystemA : AbstractSystem
{
    private void OnEvent(EventA e)
    {
        this.SendEvent(new EventB()); // 发送事件
    }
}

public class SystemB : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<EventB>(OnEventB);
    }
}
```

### 3. 命令查询分离

**原则**：明确区分修改状态（Command）和查询状态（Query）。

```csharp
// ✅ 正确的 CQRS
public class MovePlayerCommand : AbstractCommand
{
    public Vector2 Direction { get; set; }

    protected override void OnDo()
    {
        // 修改状态
        this.SendEvent(new PlayerMovedEvent { Direction = Direction });
    }
}

public class GetPlayerPositionQuery : AbstractQuery<Vector2>
{
    protected override Vector2 OnDo()
    {
        // 只查询，不修改
        return this.GetModel<PlayerModel>().Position.Value;
    }
}
```

## 代码组织

### 1. 项目结构

```
GameProject/
├── Models/
│   ├── PlayerModel.cs
│   ├── GameStateModel.cs
│   └── InventoryModel.cs
├── Systems/
│   ├── CombatSystem.cs
│   ├── InventorySystem.cs
│   └── GameLogicSystem.cs
├── Commands/
│   ├── AttackCommand.cs
│   ├── MoveCommand.cs
│   └── UseItemCommand.cs
├── Queries/
│   ├── GetPlayerHealthQuery.cs
│   └── GetInventoryItemsQuery.cs
├── Events/
│   ├── PlayerDiedEvent.cs
│   ├── ItemUsedEvent.cs
│   └── EnemyDamagedEvent.cs
├── Controllers/
│   ├── PlayerController.cs
│   └── UIController.cs
├── Utilities/
│   ├── StorageUtility.cs
│   └── MathUtility.cs
└── GameArchitecture.cs
```

### 2. 命名规范

```csharp
// Models：使用 Model 后缀
public class PlayerModel : AbstractModel { }
public class GameStateModel : AbstractModel { }

// Systems：使用 System 后缀
public class CombatSystem : AbstractSystem { }
public class InventorySystem : AbstractSystem { }

// Commands：使用 Command 后缀
public class AttackCommand : AbstractCommand { }
public class MoveCommand : AbstractCommand { }

// Queries：使用 Query 后缀
public class GetPlayerHealthQuery : AbstractQuery<int> { }
public class GetInventoryItemsQuery : AbstractQuery<List<Item>> { }

// Events：使用 Event 后缀
public class PlayerDiedEvent : IEvent { }
public class ItemUsedEvent : IEvent { }

// Controllers：使用 Controller 后缀
public class PlayerController : IController { }

// Utilities：使用 Utility 后缀
public class StorageUtility : IUtility { }
```

## 内存管理

### 1. 正确的注销管理

```csharp
public class MyController : IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();

    public void Initialize()
    {
        var model = _architecture.GetModel<PlayerModel>();

        // 注册事件并添加到注销列表
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
            .AddToUnregisterList(_unregisterList);

        // 注册属性监听并添加到注销列表
        model.Health.Register(OnHealthChanged)
            .AddToUnregisterList(_unregisterList);
    }

    public void Cleanup()
    {
        // 统一注销所有监听器
        _unregisterList.UnRegisterAll();
    }

    private void OnPlayerDied(PlayerDiedEvent e) { }
    private void OnHealthChanged(int health) { }
}
```

### 2. 生命周期管理

```csharp
public class GameManager
{
    private GameArchitecture _architecture;

    public void StartGame()
    {
        _architecture = new GameArchitecture();
        _architecture.Initialize();
    }

    public void EndGame()
    {
        // 销毁架构，自动清理所有组件
        _architecture.Destroy();
        _architecture = null;
    }
}
```

## 性能优化

### 1. 缓存组件引用

```csharp
// ❌ 低效：每次都查询
public void Update()
{
    var model = _architecture.GetModel<PlayerModel>();
    model.Health.Value -= 1;
}

// ✅ 高效：缓存引用
private PlayerModel _playerModel;

public void Initialize()
{
    _playerModel = _architecture.GetModel<PlayerModel>();
}

public void Update()
{
    _playerModel.Health.Value -= 1;
}
```

### 2. 避免频繁的事件创建

```csharp
// ❌ 低效：每帧创建新事件
public void Update()
{
    this.SendEvent(new UpdateEvent()); // 频繁分配内存
}

// ✅ 高效：复用事件或使用对象池
private UpdateEvent _updateEvent = new UpdateEvent();

public void Update()
{
    this.SendEvent(_updateEvent);
}
```

### 3. 异步处理重操作

```csharp
public class LoadDataCommand : AbstractCommand
{
    protected override async void OnDo()
    {
        // 异步加载数据，不阻塞主线程
        var data = await LoadDataAsync();
        this.SendEvent(new DataLoadedEvent { Data = data });
    }

    private async Task<Data> LoadDataAsync()
    {
        return await Task.Run(() =>
        {
            // 耗时操作
            return new Data();
        });
    }
}
```

## 测试

### 1. 单元测试

```csharp
[TestFixture]
public class CombatSystemTests
{
    private GameArchitecture _architecture;
    private PlayerModel _playerModel;

    [SetUp]
    public void Setup()
    {
        _architecture = new TestArchitecture();
        _architecture.Initialize();
        _playerModel = _architecture.GetModel<PlayerModel>();
    }

    [TearDown]
    public void Teardown()
    {
        _architecture.Destroy();
    }

    [Test]
    public void PlayerTakeDamage_ReducesHealth()
    {
        _playerModel.Health.Value = 100;
        _architecture.SendEvent(new DamageEvent { Amount = 10 });
        Assert.AreEqual(90, _playerModel.Health.Value);
    }

    [Test]
    public void PlayerDies_WhenHealthReachesZero()
    {
        _playerModel.Health.Value = 10;
        _architecture.SendEvent(new DamageEvent { Amount = 10 });
        Assert.AreEqual(0, _playerModel.Health.Value);
    }
}
```

### 2. 集成测试

```csharp
[TestFixture]
public class GameFlowTests
{
    private GameArchitecture _architecture;

    [SetUp]
    public void Setup()
    {
        _architecture = new GameArchitecture();
        _architecture.Initialize();
    }

    [Test]
    public void CompleteGameFlow()
    {
        // 初始化
        var player = _architecture.GetModel<PlayerModel>();
        Assert.AreEqual(100, player.Health.Value);

        // 执行操作
        _architecture.SendCommand(new AttackCommand { Damage = 20 });

        // 验证结果
        Assert.AreEqual(80, player.Health.Value);
    }
}
```

## 文档

### 1. 代码注释

```csharp
/// <summary>
/// 玩家模型，存储玩家的所有状态数据
/// </summary>
public class PlayerModel : AbstractModel
{
    /// <summary>
    /// 玩家的生命值，使用 BindableProperty 实现响应式更新
    /// </summary>
    public BindableProperty<int> Health { get; } = new(100);

    protected override void OnInit()
    {
        // 监听生命值变化，当生命值为 0 时发送死亡事件
        Health.Register(hp =>
        {
            if (hp <= 0)
                this.SendEvent(new PlayerDiedEvent());
        });
    }
}
```

### 2. 架构文档

为你的项目编写架构文档，说明：

- 主要的 Model、System、Command、Query
- 关键事件流
- 组件间的通信方式
- 扩展点和插件机制

## 常见陷阱

### 1. 在 Model 中包含业务逻辑

```csharp
// ❌ 错误
public class PlayerModel : AbstractModel
{
    public void TakeDamage(int damage)
    {
        Health.Value -= damage;
        if (Health.Value <= 0)
            Die();
    }
}

// ✅ 正确
public class CombatSystem : AbstractSystem
{
    private void OnDamage(DamageEvent e)
    {
        var player = this.GetModel<PlayerModel>();
        player.Health.Value -= e.Amount;
    }
}
```

### 2. 忘记注销监听器

```csharp
// ❌ 错误：可能导致内存泄漏
public void Initialize()
{
    this.RegisterEvent<Event1>(OnEvent1); // 未注销
}

// ✅ 正确
private IUnRegisterList _unregisterList = new UnRegisterList();

public void Initialize()
{
    this.RegisterEvent<Event1>(OnEvent1)
        .AddToUnregisterList(_unregisterList);
}

public void Cleanup()
{
    _unregisterList.UnRegisterAll();
}
```

### 3. 直接调用其他系统

```csharp
// ❌ 错误：紧耦合
public class SystemA : AbstractSystem
{
    private void OnEvent(EventA e)
    {
        var systemB = this.GetSystem<SystemB>();
        systemB.DoSomething();
    }
}

// ✅ 正确：使用事件解耦
public class SystemA : AbstractSystem
{
    private void OnEvent(EventA e)
    {
        this.SendEvent(new EventB());
    }
}
```

---

遵循这些最佳实践将帮助你构建可维护、高效、可扩展的应用程序。
