# Framework 架构框架

> 一个基于 CQRS、MVC 和事件驱动的轻量级游戏开发架构框架

## 📖 目录

- [框架概述](#框架概述)
- [核心概念](#核心概念)
- [架构图](#架构图)
- [快速开始](#快速开始)
- [包说明](#包说明)
- [组件联动](#组件联动)
- [最佳实践](#最佳实践)
- [示例项目](#示例项目)

## 框架概述

本框架是一个专为 Godot C# 游戏开发设计的轻量级架构，它结合了多种经典设计模式：

- **MVC 架构模式** - 清晰的层次划分
- **CQRS 模式** - 命令查询职责分离
- **IoC/DI** - 依赖注入和控制反转
- **事件驱动** - 松耦合的组件通信
- **响应式编程** - 可绑定属性和数据流

### 核心特性

✅ **清晰的分层架构** - Model、View、Controller、System、Utility 各司其职  
✅ **类型安全** - 基于泛型的组件获取和事件系统  
✅ **松耦合** - 通过事件和接口实现组件解耦  
✅ **易于测试** - 依赖注入和纯函数设计  
✅ **可扩展** - 基于接口的规则体系  
✅ **生命周期管理** - 自动的注册和注销机制

## 核心概念

### 五层架构

```
┌─────────────────────────────────────────┐
│           View (Godot Nodes)            │  UI 层：Godot 节点
├─────────────────────────────────────────┤
│            Controller                    │  控制层：处理用户输入
├─────────────────────────────────────────┤
│             System                       │  逻辑层：业务逻辑
├─────────────────────────────────────────┤
│              Model                       │  数据层：游戏状态
├─────────────────────────────────────────┤
│             Utility                      │  工具层：无状态工具
└─────────────────────────────────────────┘
```

### 横切关注点

```
Command ──┐
Query   ──┼──→  跨层操作（修改/查询数据）
Event   ──┘
```

## 架构图

### 整体架构

```
                    ┌──────────────────┐
                    │   Architecture   │ ← 单例，管理所有组件
                    └────────┬─────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
    ┌───▼────┐          ┌───▼────┐          ┌───▼─────┐
    │ Model  │          │ System │          │ Utility │
    │  层    │          │  层    │          │  层     │
    └───┬────┘          └───┬────┘          └────────┘
        │                   │
        │    ┌─────────────┤
        │    │             │
    ┌───▼────▼───┐    ┌───▼──────┐
    │ Controller │    │ Command/ │
    │    层      │    │  Query   │
    └─────┬──────┘    └──────────┘
          │
    ┌─────▼─────┐
    │   View    │
    │ (Godot节点)│
    └───────────┘
```

### 数据流向

```
用户输入 → Controller → Command → System → Model → Event → Controller → View 更新

查询流程：Controller → Query → Model → 返回数据
```

## 快速开始

本框架采用"约定优于配置"的设计理念，只需 4 步即可搭建完整的游戏架构。

### 为什么需要这个框架？

在传统的 Godot 开发中，我们经常遇到这些问题：

- 💔 **代码耦合严重**：UI 直接访问游戏逻辑，逻辑直接操作 UI
- 🔄 **难以维护**：修改一个功能需要改动多个文件
- 🐛 **难以测试**：业务逻辑和 UI 混在一起无法独立测试
- 📦 **难以复用**：代码紧密耦合，无法在其他项目中复用

本框架通过清晰的分层解决这些问题。

### 1. 定义架构（Architecture）

**作用**：Architecture 是整个游戏的"中央调度器"，负责管理所有组件的生命周期。

```csharp
using GFramework.framework.architecture;

public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 注册 Model - 游戏数据
        this.RegisterModel<PlayerModel>(new PlayerModel());
        
        // 注册 System - 业务逻辑
        this.RegisterSystem<CombatSystem>(new CombatSystem());
        
        // 注册 Utility - 工具类
        this.RegisterUtility<StorageUtility>(new StorageUtility());
    }
}
```

**优势**：

- ✅ **单例模式**：通过 `GameArchitecture.Interface` 全局访问
- ✅ **自动初始化**：注册时自动调用组件的 Init 方法
- ✅ **依赖注入**：组件自动获得架构引用，无需手动传递
- ✅ **集中管理**：所有组件注册在一处，一目了然

### 2. 定义 Model（数据层）

**作用**：Model 是游戏的"数据库"，只负责存储和管理游戏状态。

```csharp
public class PlayerModel : AbstractModel
{
    // 使用 BindableProperty 实现响应式数据
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> Gold { get; } = new(0);
    
    protected override void OnInit()
    {
        // Model 中可以监听自己的数据变化
        Health.Register(hp =>
        {
            if (hp <= 0) this.SendEvent(new PlayerDiedEvent());
        });
    }
}
//当然也可以不这样
public class PlayerModel : AbstractModel
{
    public int Health { get;private set; } 
    public int Gold { get;private set; }
    
    protected override void OnInit()
    {
        Health = 100;
        Gold = 0;
    }
}
```

**优势**：

- ✅ **数据响应式**：BindableProperty 让数据变化自动通知 UI
- ✅ **职责单一**：只存储数据，不包含复杂业务逻辑
- ✅ **易于测试**：可以独立测试数据逻辑
- ✅ **数据持久化**：可以轻松序列化整个 Model 进行存档

**为什么不在 Model 中写业务逻辑？**

- 保持 Model 简单纯粹，业务逻辑应该在 System 中处理
- Model 应该是"被动"的，等待其他组件修改它

### 3. 定义 System（业务逻辑层）

**作用**：System 是游戏的"大脑"，处理所有业务逻辑。

```csharp
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // System 通过事件驱动，响应游戏中的各种事件
        this.RegisterEvent<EnemyAttackEvent>(OnEnemyAttack);
    }
    
    private void OnEnemyAttack(EnemyAttackEvent e)
    {
        var playerModel = this.GetModel<PlayerModel>();
        
        // 处理业务逻辑：计算伤害、更新数据
        playerModel.Health.Value -= e.Damage;
        
        // 发送事件通知其他组件
        this.SendEvent(new PlayerTookDamageEvent { Damage = e.Damage });
    }
}
```

**优势**：

- ✅ **事件驱动**：通过事件解耦，不同 System 之间松耦合
- ✅ **可组合**：多个 System 协同工作，每个专注自己的领域
- ✅ **易于扩展**：新增功能只需添加新的 System 和事件监听
- ✅ **独立测试**：可以模拟事件来测试 System 的逻辑

**System 与 Model 的关系**：

- System 读取和修改 Model 的数据
- System 不应该存储重要的游戏状态（状态应在 Model 中）
- System 可以存储临时的计算结果或缓存

### 4. 定义 Controller（控制层）

**作用**：Controller 是"桥梁"，连接 UI（View）和业务逻辑。

```csharp
public partial class PlayerController : Node, IController
{
    [Export] private Label _healthLabel;
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    // 实现接口，连接到架构
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        var playerModel = this.GetModel<PlayerModel>();
        
        // 数据绑定：Model 数据变化自动更新 UI
        playerModel.Health
            .RegisterWithInitValue(hp => _healthLabel.Text = $"HP: {hp}")
            .AddToUnregisterList(_unregisterList);
    }
    
    public override void _ExitTree()
    {
        // 重要：节点销毁时注销所有监听，避免内存泄漏
        _unregisterList.UnRegisterAll();
    }
}
```

**优势**：

- ✅ **自动更新 UI**：通过 BindableProperty，数据变化自动反映到界面
- ✅ **生命周期管理**：自动注销监听，避免内存泄漏
- ✅ **分离关注点**：UI 逻辑和业务逻辑完全分离
- ✅ **易于修改 UI**：改变 UI 不影响业务逻辑

**为什么使用 RegisterWithInitValue？**

- 注册监听时立即获得当前值，避免 UI 显示空白
- 后续数据变化会自动触发更新

### 完成！现在你有了一个完整的架构

这 4 步完成后，你就拥有了：

- 📦 **清晰的数据层**（Model）
- 🧠 **独立的业务逻辑**（System）
- 🎮 **灵活的控制层**（Controller）
- 🔧 **统一的生命周期管理**（Architecture）

### 下一步该做什么？

1. **添加 Command**：封装用户操作（如购买物品、使用技能）
2. **添加 Query**：封装数据查询（如查询背包物品数量）
3. **添加更多 System**：如任务系统、背包系统、商店系统
4. **使用 Utility**：添加工具类（如存档工具、数学工具）

## 包说明

| 包名               | 职责              | 文档                              |
|------------------|-----------------|---------------------------------|
| **architecture** | 架构核心，管理所有组件生命周期 | [📖 查看](architecture/README.md) |
| **model**        | 数据模型层，存储游戏状态    | [📖 查看](model/README.md)        |
| **system**       | 业务逻辑层，处理游戏系统    | [📖 查看](system/README.md)       |
| **controller**   | 控制器层，连接视图和逻辑    | [📖 查看](controller/README.md)   |
| **utility**      | 工具类层，提供无状态工具    | [📖 查看](utility/README.md)      |
| **command**      | 命令模式，封装写操作      | [📖 查看](command/README.md)      |
| **query**        | 查询模式，封装读操作      | [📖 查看](query/README.md)        |
| **events**       | 事件系统，组件间通信      | [📖 查看](events/README.md)       |
| **property**     | 可绑定属性，响应式编程     | [📖 查看](property/README.md)     |
| **ioc**          | IoC 容器，依赖注入     | [📖 查看](ioc/README.md)          |
| **rule**         | 规则接口，定义组件约束     | [📖 查看](rule/README.md)         |
| **extensions**   | 扩展方法，简化 API 调用  | [📖 查看](extensions/README.md)   |

## 组件联动

### 1. 初始化流程

```
Architecture.Interface
    └─> Init()
        ├─> RegisterModel → Model.SetArchitecture() → Model.Init()
        ├─> RegisterSystem → System.SetArchitecture() → System.Init()
        └─> RegisterUtility (无需初始化)
```

### 2. Command 执行流程

```
Controller.SendCommand(command)
    └─> command.SetArchitecture(architecture)  // 自动注入
        └─> command.Execute()
            └─> command.OnExecute()  // 子类实现
                ├─> GetModel<T>()    // 获取数据
                ├─> 修改 Model 数据
                └─> SendEvent()      // 发送事件
```

### 3. Event 传播流程

```
组件.SendEvent(event)
    └─> TypeEventSystem.Send(event)
        └─> 通知所有订阅者
            ├─> Controller 响应 → 更新 UI
            ├─> System 响应 → 执行逻辑
            └─> Model 响应 → 更新状态
```

### 4. BindableProperty 数据绑定

```
Model: BindableProperty<int> Health = new(100);
Controller: Health.RegisterWithInitValue(hp => UpdateUI(hp))
修改值: Health.Value = 50 → 触发所有回调 → UI 自动更新
```

## 最佳实践

掌握这些最佳实践，能让你充分发挥框架的优势，避免常见陷阱。

### 1. 分层职责原则 📋

每一层都有明确的职责边界，遵循这些原则能让代码更清晰、更易维护。

#### ✅ 好的实践 vs ❌ 坏的实践

**Model 层**：

```csharp
// ✅ 好：只存储数据
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> MaxHealth { get; } = new(100);
    protected override void OnInit() { }
}

// ❌ 坏：包含业务逻辑
public class PlayerModel : AbstractModel
{
    public void TakeDamage(int damage)  // ❌ 业务逻辑应在 System
    {
        Health.Value -= damage;
        if (Health.Value <= 0) Die();
    }
}
```

**System 层**：

```csharp
// ✅ 好：处理业务逻辑
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<AttackEvent>(OnAttack);
    }
    
    private void OnAttack(AttackEvent e)
    {
        // 业务逻辑在这里
        var target = this.GetModel<PlayerModel>();
        int finalDamage = CalculateDamage(e.BaseDamage, target);
        target.Health.Value -= finalDamage;
    }
}

// ❌ 坏：直接操作 UI
public class CombatSystem : AbstractSystem
{
    private Label _damageLabel;  // ❌ 不应持有 UI 引用
    
    private void OnAttack(AttackEvent e)
    {
        _damageLabel.Text = $"-{e.Damage}";  // ❌ 应通过事件通知
    }
}
```

**Controller 层**：

```csharp
// ✅ 好：只处理 UI 和用户输入
public partial class AttackButton : Button, IController
{
    public override void _Ready()
    {
        Pressed += () => this.SendCommand(new AttackCommand());
    }
}

// ❌ 坏：包含业务逻辑
public partial class AttackButton : Button, IController
{
    public override void _Ready()
    {
        Pressed += () =>
        {
            var player = this.GetModel<PlayerModel>();
            var enemy = this.GetModel<EnemyModel>();
            
            // ❌ 这些业务逻辑应该在 System/Command 中
            int damage = player.AttackPower.Value - enemy.Defense.Value;
            enemy.Health.Value -= damage;
        };
    }
}
```

### 2. 通信方式选择指南 💬

不同的通信方式适用于不同场景，选对方式能让代码更优雅。

| 通信方式                 | 使用场景       | 示例             | 优势       |
|----------------------|------------|----------------|----------|
| **Command**          | 用户操作、修改状态  | 购买物品、攻击敌人      | 可撤销、可记录  |
| **Query**            | 查询数据、检查条件  | 查询金币数量、检查是否可购买 | 明确只读意图   |
| **Event**            | 通知其他组件     | 玩家死亡、物品拾取      | 松耦合、可扩展  |
| **BindableProperty** | 数据→UI 自动同步 | 生命值变化更新血条      | 自动化、不会遗漏 |

**实战示例：购物系统**

```csharp
// 1. Controller：用户点击购买按钮
public void OnBuyButtonPressed()
{
    // 先用 Query 检查条件
    bool canBuy = this.SendQuery(new CanBuyItemQuery
    {
        ItemId = "sword",
        Price = 100
    });
    
    if (canBuy)
    {
        // 使用 Command 执行操作
        this.SendCommand(new BuyItemCommand { ItemId = "sword" });
    }
    else
    {
        ShowMessage("金币不足！");
    }
}

// 2. Query：检查是否可以购买
public class CanBuyItemQuery : AbstractQuery<bool>
{
    public string ItemId { get; set; }
    public int Price { get; set; }
    
    protected override bool OnDo()
    {
        var player = this.GetModel<PlayerModel>();
        return player.Gold.Value >= Price;
    }
}

// 3. Command：执行购买
public class BuyItemCommand : AbstractCommand
{
    public string ItemId { get; set; }
    
    protected override void OnExecute()
    {
        var player = this.GetModel<PlayerModel>();
        var shop = this.GetModel<ShopModel>();
        
        int price = shop.GetPrice(ItemId);
        player.Gold.Value -= price;
        
        // 发送事件通知
        this.SendEvent(new ItemPurchasedEvent { ItemId = ItemId });
    }
}

// 4. System：响应购买事件
public class InventorySystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<ItemPurchasedEvent>(OnItemPurchased);
    }
    
    private void OnItemPurchased(ItemPurchasedEvent e)
    {
        var inventory = this.GetModel<InventoryModel>();
        inventory.AddItem(e.ItemId);
    }
}

// 5. Controller：通过 BindableProperty 自动更新 UI
public partial class GoldDisplay : Label, IController
{
    public override void _Ready()
    {
        var player = this.GetModel<PlayerModel>();
        
        // 金币变化自动更新 UI
        player.Gold
            .RegisterWithInitValue(gold => Text = $"金币: {gold}")
            .UnRegisterWhenNodeExitTree(this);
    }
}
```

### 3. 生命周期管理 ♻️

**为什么需要注销？**

忘记注销监听器会导致：

- 💥 **内存泄漏**：对象无法被 GC 回收
- 🐛 **逻辑错误**：已销毁的对象仍在响应事件
- 📉 **性能下降**：无用的监听器消耗资源

#### 方式一：自动注销（推荐用于 Godot 节点）

```csharp
public override void _Ready()
{
    // 节点退出场景树时自动注销
    this.RegisterEvent<GameEvent>(OnEvent)
        .UnRegisterWhenNodeExitTree(this);
    
    playerModel.Health
        .RegisterWithInitValue(UpdateHealthBar)
        .UnRegisterWhenNodeExitTree(this);
}
```

**优势**：一行代码搞定，不会忘记

#### 方式二：统一管理（推荐用于多个监听器）

```csharp
private IUnRegisterList _unregisterList = new UnRegisterList();

public override void _Ready()
{
    // 所有监听器统一管理
    this.RegisterEvent<Event1>(OnEvent1)
        .AddToUnregisterList(_unregisterList);
    
    this.RegisterEvent<Event2>(OnEvent2)
        .AddToUnregisterList(_unregisterList);
    
    playerModel.Health
        .RegisterWithInitValue(UpdateUI)
        .AddToUnregisterList(_unregisterList);
}

public override void _ExitTree()
{
    // 一次性注销所有
    _unregisterList.UnRegisterAll();
}
```

**优势**：集中管理，清晰明了

#### 方式三：手动注销（需要精确控制时机）

```csharp
private IUnRegister _healthUnregister;

public override void _Ready()
{
    _healthUnregister = playerModel.Health.Register(UpdateUI);
}

public void StopListening()
{
    // 在特定时机手动注销
    _healthUnregister?.UnRegister();
    _healthUnregister = null;
}
```

**优势**：可以在任何时候注销

### 4. 性能优化技巧 ⚡

#### 技巧 1：缓存组件引用

```csharp
// ❌ 低效：每帧都查询
public override void _Process(double delta)
{
    var model = this.GetModel<PlayerModel>();  // 每帧查询 IoC 容器
    UpdateUI(model.Health.Value);
}

// ✅ 高效：只查询一次
private PlayerModel _playerModel;

public override void _Ready()
{
    _playerModel = this.GetModel<PlayerModel>();  // 只查询一次
}

public override void _Process(double delta)
{
    UpdateUI(_playerModel.Health.Value);
}
```

**原因**：GetModel 需要查询 IoC 容器，虽然很快但不必要重复查询

#### 技巧 2：批量操作避免频繁触发

```csharp
// ❌ 低效：每次修改都触发事件
foreach (var item in 100items)
{
    inventory.AddItem(item);  // 触发100次 UI 更新！
}

// ✅ 高效：批量操作后统一通知
foreach (var item in items)
{
    inventory.Items.SetValueWithoutEvent(
        inventory.Items.Value + item
    );
}
// 最后统一触发一次
this.SendEvent(new InventoryChangedEvent());
```

**原因**：每次数据变化都会触发所有监听器，批量操作会导致性能问题

#### 技巧 3：使用对象池

```csharp
// 在 Utility 中实现对象池
public class PoolUtility : IUtility
{
    private Dictionary<Type, Queue<object>> _pools = new();
    
    public T Get<T>() where T : new()
    {
        var type = typeof(T);
        if (_pools.ContainsKey(type) && _pools[type].Count > 0)
            return (T)_pools[type].Dequeue();
        return new T();
    }
    
    public void Return<T>(T obj)
    {
        var type = typeof(T);
        if (!_pools.ContainsKey(type))
            _pools[type] = new Queue<object>();
        _pools[type].Enqueue(obj);
    }
}

// 使用对象池
var pool = this.GetUtility<PoolUtility>();
var bullet = pool.Get<Bullet>();
// 使用完毕后归还
pool.Return(bullet);
```

### 5. 可测试性设计 🧪

框架天然支持单元测试，因为所有组件都是通过接口交互的。

```csharp
[TestFixture]
public class CombatSystemTests
{
    private TestArchitecture _architecture;
    private PlayerModel _player;
    private CombatSystem _combat;
    
    [SetUp]
    public void Setup()
    {
        // 1. 创建测试架构
        _architecture = new TestArchitecture();
        
        // 2. 注册需要的组件
        _player = new PlayerModel();
        _architecture.RegisterModel(_player);
        
        _combat = new CombatSystem();
        _architecture.RegisterSystem(_combat);
    }
    
    [Test]
    public void TestPlayerTakesDamage()
    {
        // 3. 设置初始状态
        _player.Health.Value = 100;
        
        // 4. 触发事件
        _architecture.SendEvent(new EnemyAttackEvent { Damage = 30 });
        
        // 5. 验证结果
        Assert.AreEqual(70, _player.Health.Value);
    }
    
    [TearDown]
    public void TearDown()
    {
        _architecture = null;
    }
}
```

**优势**：

- 不需要启动游戏就能测试逻辑
- 可以快速验证各种边界情况
- 易于进行回归测试

## 示例项目

通过一个完整的 RPG 战斗系统示例，展示框架各组件如何协同工作。

### 完整示例：RPG 战斗系统 ⚔️

实现功能：回合制战斗、伤害计算、战斗日志、胜负判定、UI 实时更新。

```csharp
// 架构定义
public class RPGArchitecture : Architecture<RPGArchitecture>
{
    protected override void Init()
    {
        this.RegisterModel<PlayerModel>(new PlayerModel());
        this.RegisterModel<EnemyModel>(new EnemyModel());
        this.RegisterSystem<CombatSystem>(new CombatSystem());
        this.RegisterUtility<MathUtility>(new MathUtility());
    }
}

// 数据层
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> AttackPower { get; } = new(20);
    public BindableProperty<bool> IsAlive { get; } = new(true);
    
    protected override void OnInit()
    {
        Health.Register(hp =>
        {
            IsAlive.Value = hp > 0;
            if (hp <= 0) this.SendEvent(new PlayerDiedEvent());
        });
    }
}

// 业务逻辑层
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerAttackEvent>(OnPlayerAttack);
    }
    
    private void OnPlayerAttack(PlayerAttackEvent e)
    {
        var player = this.GetModel<PlayerModel>();
        var enemy = this.GetModel<EnemyModel>();
        var mathUtil = this.GetUtility<MathUtility>();
        
        int damage = mathUtil.CalculateDamage(player.AttackPower.Value, enemy.Defense.Value);
        enemy.Health.Value -= damage;
        
        this.SendEvent(new DamageDealtEvent { Damage = damage });
    }
}

// 控制层
public partial class BattleUI : Control, IController
{
    [Export] private Label _healthLabel;
    [Export] private Button _attackButton;
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public IArchitecture GetArchitecture() => RPGArchitecture.Interface;
    
    public override void _Ready()
    {
        var player = this.GetModel<PlayerModel>();
        
        // 数据绑定：自动更新 UI
        player.Health
            .RegisterWithInitValue(hp => _healthLabel.Text = $"HP: {hp}")
            .AddToUnregisterList(_unregisterList);
        
        _attackButton.Pressed += () => this.SendCommand(new AttackCommand());
    }
    
    public override void _ExitTree() => _unregisterList.UnRegisterAll();
}
```

**扩展示例**：添加技能系统只需新增 Model 和 System，无需修改现有代码。

```csharp
// 新增技能系统
public class SkillSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<UseSkillEvent>(OnUseSkill);
    }
    
    private void OnUseSkill(UseSkillEvent e)
    {
        // 技能逻辑
    }
}

// 在架构中注册
protected override void Init()
{
    this.RegisterSystem<SkillSystem>(new SkillSystem());
    // 现有代码无需修改
}
```

**优势总结**：

- 数据驱动，易于存档
- 事件解耦，易于扩展
- UI 自动化，不会遗漏更新
- 可独立测试业务逻辑

## 设计理念

框架的设计遵循 SOLID 原则和经典设计模式，让代码更易维护和扩展。

### 1. 单一职责原则（SRP）💼

**理念**：每个类只负责一件事，只有一个改变的理由。

**在框架中的体现**：

- **Model**：只负责存储数据
- **System**：只负责处理业务逻辑
- **Controller**：只负责 UI 交互
- **Utility**：只负责提供工具方法

**好处**：

- 代码更容易理解和维护
- 修改一个功能不会影响其他功能
- 单元测试更简单

### 2. 开闭原则（OCP）🔓

**理念**：对扩展开放，对修改封闭。

**在框架中的实现**：

- 通过**事件系统**添加新功能，无需修改现有代码
- 新的 System 可以监听现有事件，插入自己的逻辑
- 符合"插件式"的架构设计

**示例**：

```csharp
// 现有：战斗系统
public class CombatSystem : AbstractSystem
{
    private void OnAttack(AttackEvent e)
    {
        // 计算伤害
        this.SendEvent(new DamageDealtEvent { Damage = damage });
    }
}

// 扩展：添加暴击系统，不修改 CombatSystem
public class CriticalSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<DamageDealtEvent>(OnDamage);
    }
    
    private void OnDamage(DamageDealtEvent e)
    {
        if (RollCritical())
        {
            this.SendEvent(new CriticalHitEvent { Damage = e.Damage * 2 });
        }
    }
}
```

### 3. 依赖倒置原则（DIP）🔄

**理念**：依赖抽象（接口）而非具体实现。

**在框架中的应用**：

- 所有组件通过接口交互
- 通过 IoC 容器注入依赖
- 易于替换实现和编写测试

**好处**：

- 降低耦合度
- 易于进行单元测试（可以 mock）
- 可以灵活替换实现

### 4. 接口隔离原则（ISP）✂️

**理念**：使用多个专门的接口，而不是一个庞大的接口。

**在框架中的体现**：

```csharp
// 小而专注的接口
public interface ICanGetModel : IBelongToArchitecture { }
public interface ICanSendCommand : IBelongToArchitecture { }
public interface ICanRegisterEvent : IBelongToArchitecture { }

// Controller 组合需要的能力
public interface IController :
    ICanGetModel,
    ICanSendCommand,
    ICanRegisterEvent { }
```

**优势**：

- 类只需要实现它真正需要的方法
- 接口更容易理解和使用
- 减少不必要的依赖

### 5. 组合优于继承 🧩

**理念**：通过接口组合获得能力，而不是通过继承。

**传统继承的问题**：

- 继承层次深，难以理解
- 子类与父类紧密耦合
- 难以灵活组合能力

**框架的解决方案**：

```csharp
// ✅ 通过接口组合能力
public interface IController :
    ICanGetModel,      // 组合：获取 Model 的能力
    ICanSendCommand,   // 组合：发送 Command 的能力
    ICanRegisterEvent  // 组合：注册事件的能力
{ }

// ❌ 避免深层继承
public class BaseController { }
public class GameController : BaseController { }
public class BattleController : GameController { }
```

### 框架核心设计模式 🎨

| 设计模式      | 应用位置         | 解决的问题     | 带来的好处   |
|-----------|--------------|-----------|---------|
| **单例模式**  | Architecture | 全局唯一的架构实例 | 统一的组件管理 |
| **工厂模式**  | IoC 容器       | 组件的创建和管理  | 解耦创建逻辑  |
| **观察者模式** | Event 系统     | 组件间的通信    | 松耦合通信   |
| **命令模式**  | Command      | 封装操作请求    | 支持撤销重做  |
| **策略模式**  | System       | 不同的业务逻辑   | 易于切换策略  |
| **依赖注入**  | 整体架构         | 组件间的依赖    | 自动管理依赖  |
| **模板方法**  | Abstract 类   | 定义算法骨架    | 统一流程规范  |

### 为什么这样设计？🤔

#### 1. 降低学习成本

- 遵循业界标准模式和原则
- 有经验的开发者能快速上手
- 清晰的分层易于理解

#### 2. 提高代码质量

- 强制分层，避免意大利面代码
- 职责明确，减少 bug
- 易于 Code Review

#### 3. 便于团队协作

- 清晰的职责划分，减少冲突
- 统一的代码风格
- 新人容易融入项目

#### 4. 易于维护扩展

- 符合 SOLID 原则
- 通过事件添加功能无需修改现有代码
- 模块化设计，易于替换

#### 5. 支持单元测试

- 依赖注入让 mock 变得简单
- 接口设计便于测试
- 业务逻辑与 UI 分离，可独立测试

### 架构演进建议 🚀

#### 小型项目（< 5000 行代码）

- 使用基础的 MVC 分层
- 适度使用 Command 和 Query
- 事件系统用于关键通知

#### 中型项目（5000-20000 行）

- 完整使用框架所有特性
- 细分 System 的职责
- 引入更多 Utility 复用代码

#### 大型项目（> 20000 行）

- 考虑按功能模块拆分多个 Architecture
- 使用事件总线连接不同模块
- 引入领域驱动设计（DDD）概念

### 常见反模式 ⚠️

#### 反模式 1：上帝类（God Class）

```csharp
// ❌ 一个类做所有事情
public class GameManager
{
    public void Attack() { }
    public void Move() { }
    public void SaveGame() { }
    public void LoadGame() { }
    public void UpdateUI() { }
    // ... 几千行代码
}

// ✅ 正确：分层设计
public class CombatSystem { /* 只负责战斗 */ }
public class MovementSystem { /* 只负责移动 */ }
public class SaveSystem { /* 只负责存档 */ }
```

#### 反模式 2：循环依赖

```csharp
// ❌ A 依赖 B，B 又依赖 A
public class SystemA
{
    private SystemB _systemB;
}
public class SystemB
{
    private SystemA _systemA;
}

// ✅ 正确：通过事件解耦
public class SystemA
{
    protected override void OnInit()
    {
        this.SendEvent(new EventA());
    }
}
public class SystemB
{
    protected override void OnInit()
    {
        this.RegisterEvent<EventA>(OnEventA);
    }
}
```

#### 反模式 3：过度设计

```csharp
// ❌ 简单功能过度抽象
public interface IClickable { }
public interface IHoverable { }
public interface IFocusable { }
public class Button : IClickable, IHoverable, IFocusable { }

// ✅ 正确：根据实际需求设计
public class Button : Control, IController
{
    // 简单直接
}
```

### 设计哲学总结 📝

1. **简单优于复杂**：能用简单方案就不用复杂方案
2. **显式优于隐式**：让代码意图明确
3. **解耦优于耦合**：通过接口和事件降低耦合
4. **组合优于继承**：灵活组合能力而非深层继承
5. **约定优于配置**：遵循框架约定，减少配置

**记住**：好的架构不是一开始就完美的，而是在迭代中不断演进的。从简单开始，根据项目需求逐步采用框架特性。

## 相关资源

- Godot 官方文档: https://docs.godotengine.org/
- CQRS 模式介绍: https://martinfowler.com/bliki/CQRS.html
- 事件驱动架构: https://martinfowler.com/articles/201701-event-driven.html

---

**版本**: 1.0.0
**适用引擎**: Godot 4.x (C#)
**许可证**: Apache 2.0