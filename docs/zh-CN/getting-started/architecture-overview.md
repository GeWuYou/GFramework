# 架构概览

GFramework 采用经典的五层架构模式，结合 CQRS 和事件驱动设计，为游戏开发提供清晰、可维护的架构基础。

## 核心架构模式

### 五层架构

```
┌─────────────────────────────────────────┐
│             View / UI                    │  ← 用户界面层
├─────────────────────────────────────────┤
│            Controller                    │  ← 控制层
├─────────────────────────────────────────┤
│             System                       │  ← 业务逻辑层
├─────────────────────────────────────────┤
│              Model                       │  ← 数据层
├─────────────────────────────────────────┤
│             Utility                      │  ← 工具层
└─────────────────────────────────────────┘
```

### 跨层操作机制

```
Command ──┐
Query   ──┼──→  跨层操作（修改/查询数据）
Event   ──┘
```

### 生命周期阶段

```
初始化：Init → BeforeUtilityInit → AfterUtilityInit → BeforeModelInit → AfterModelInit → BeforeSystemInit → AfterSystemInit → Ready
销毁：Destroy → Destroying → Destroyed
```

## 核心组件详解

### 1. Architecture（架构）

应用的中央调度器，负责管理所有组件的生命周期。

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册所有组件
        RegisterModel(new PlayerModel());
        RegisterSystem(new CombatSystem());
        RegisterUtility(new StorageUtility());
    }
}
```

**主要职责：**

- 组件注册和管理
- 生命周期协调
- 依赖注入
- 跨组件通信协调

### 2. Model（数据模型）

应用的状态存储层，只负责数据的存储和管理。

```csharp
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<string> Name { get; } = new("Player");
    
    protected override void OnInit()
    {
        // 监听自身数据变化
        Health.Register(OnHealthChanged);
    }
    
    private void OnHealthChanged(int newHealth)
    {
        if (newHealth <= 0)
            this.SendEvent(new PlayerDiedEvent());
    }
}
```

**设计原则：**

- 只存储数据，不包含业务逻辑
- 使用 BindableProperty 实现响应式数据
- 通过事件通知数据变化

### 3. System（业务系统）

应用的业务逻辑处理层。

```csharp
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
// 订阅相关事件
this.GetEvent<AttackEvent>().Register(OnAttack);
}

    private void OnAttack(AttackEvent e)
    {
        var attacker = e.Attacker;
        var target = e.Target;
        
        // 计算伤害
        var damage = CalculateDamage(attacker, target);
        
        // 更新目标生命值
        target.Health.Value -= damage;
        
        // 发送伤害事件
        this.SendEvent(new DamageEvent(target, damage));
    }
    
    private int CalculateDamage(Entity attacker, Entity target)
    {
        return Mathf.Max(1, attacker.Attack.Value - target.Defense.Value);
    }
}
```

**设计原则：**

- 处理业务逻辑，不直接存储数据
- 通过事件与其他组件通信
- 从 Model 获取数据，向 Model 发送更新

### 4. Controller（控制器）

连接 UI 和业务逻辑的桥梁。

```csharp
public class PlayerController : IController
{
    private IArchitecture _architecture;
    private PlayerModel _playerModel;
    
    public PlayerController(IArchitecture architecture)
    {
        _architecture = architecture;
        _playerModel = architecture.GetModel<PlayerModel>();
        
        // 监听模型变化并更新 UI
        _playerModel.Health.RegisterWithInitValue(UpdateHealthDisplay);
    }
    
    public void OnPlayerInput(Vector2 direction)
    {
        // 将用户输入转换为命令
        _architecture.SendCommand(new MovePlayerCommand { Direction = direction });
    }
    
    private void UpdateHealthDisplay(int health)
    {
        // 更新 UI 显示
        Console.WriteLine($"Player Health: {health}");
    }
}
```

**核心功能：**

- 接收用户输入
- 发送命令到系统
- 监听模型变化更新 UI
- 协调 UI 和业务逻辑

### 5. Utility（工具类）

提供无状态的辅助功能。

```csharp
public class StorageUtility : IUtility
{
    public void SaveData<T>(string key, T data)
    {
        // 实现数据保存逻辑
    }
    
    public T LoadData<T>(string key, T defaultValue = default)
    {
        // 实现数据加载逻辑
        return defaultValue;
    }
}
```

**使用场景：**

- 数据存储和读取
- 数学计算工具
- 字符串处理
- 网络通信辅助

## 通信机制

### 1. Command（命令）

用于修改应用状态的操作：

```csharp
public class MovePlayerCommand : AbstractCommand
{
    public Vector2 Direction { get; set; }
    
    protected override void OnDo()
    {
        // 执行移动逻辑
        this.SendEvent(new PlayerMovedEvent { Position = CalculateNewPosition() });
    }
}
```

### 2. Query（查询）

用于查询应用状态：`

```csharp
public class GetPlayerHealthQuery : AbstractQuery<int>
{
    protected override int OnDo()
    {
        var playerModel = this.GetModel<PlayerModel>();
        return playerModel.Health.Value;
    }
}
```

### 3. Event（事件）

组件间通信的主要机制：`

```
// 发送事件
this.SendEvent(new PlayerDiedEvent());

// 监听事件
this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
```

## 响应式编程

### BindableProperty

```csharp
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<string> Name { get; } = new("Player");
}

// 使用方式
playerModel.Health.Value = 50; // 自动触发所有监听器
playerModel.Health.Register(newValue => {
    Console.WriteLine($"Health changed to: {newValue}");
});
```

### 数据绑定优势

- **自动更新**：数据变化自动通知监听者
- **内存安全**：自动管理监听器生命周期
- **类型安全**：编译时类型检查
- **性能优化**：只在值真正改变时触发

## 最佳实践

### 1. 分层职责明确

```csharp
// ✅ 正确：Model 只存储数据
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
}

// ❌ 错误：Model 包含业务逻辑
public class PlayerModel : AbstractModel
{
    public void TakeDamage(int damage) // 业务逻辑应该在 System 中
    {
        Health.Value -= damage;
    }
}
```

### 2. 事件驱动设计

```csharp
// ✅ 正确：使用事件解耦
public class CombatSystem : AbstractSystem
{
    private void OnPlayerAttack(PlayerAttackEvent e)
    {
        // 处理攻击逻辑
        this.SendEvent(new EnemyDamagedEvent { Damage = CalculateDamage() });
    }
}

// ❌ 错误：直接调用其他组件
public class CombatSystem : AbstractSystem
{
    private void OnPlayerAttack(PlayerAttackEvent e)
    {
        var enemySystem = this.GetSystem<EnemySystem>(); // 紧耦合
        enemySystem.TakeDamage(CalculateDamage());
    }
}
```

### 3. 命令查询分离

```csharp
// ✅ 正确：明确区分命令和查询
public class MovePlayerCommand : AbstractCommand { }  // 修改状态
public class GetPlayerPositionQuery : AbstractQuery<Vector2> { }  // 查询状态

// ❌ 错误：混合读写操作
public class PlayerManager 
{
    public void MoveAndGetPosition(Vector2 direction, out Vector2 position) // 职责不清
    {
        // ...
    }
}
```

## 架构优势

### 1. 可维护性

- 清晰的职责分离
- 松耦合的组件设计
- 易于定位和修复问题

### 2. 可测试性

- 组件可独立测试
- 依赖可轻松模拟
- 支持单元测试和集成测试

### 3. 可扩展性

- 新功能通过添加组件实现
- 现有组件无需修改
- 支持插件化架构

### 4. 团队协作

- 统一的架构规范
- 易于新人上手
- 减少代码冲突

## 下一步学习

- [深入了解 Architecture 组件](./core/architecture)
- [掌握事件系统](./core/events)
- [学习命令查询模式](./core/command)
- [探索属性系统](./core/property)