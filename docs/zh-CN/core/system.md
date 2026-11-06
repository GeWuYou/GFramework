---
title: System 包使用说明
description: 说明 GFramework.Core.System 的业务逻辑层职责、基类结构与协作方式。
---

# System 包使用说明

## 概述

System 包定义了业务逻辑层（Business Logic Layer）。System 负责处理游戏的核心业务逻辑，协调 Model 之间的交互，响应事件并执行复杂的业务流程。

System 是 GFramework 架构中业务逻辑的核心组件，通过事件系统与 Model 和 Controller 进行通信。

## 核心接口

### ISystem

ISystem 继承 IContextAware、IArchitecturePhaseListener 和 ILifecycle。业务停止由 IDestroyable.Destroy 定义，与标准 IDisposable 的资源所有权不同。

```csharp
void Initialize();
void Destroy();
void OnArchitecturePhase(ArchitecturePhase phase);
```

## 核心类

### AbstractSystem

AbstractSystem 继承 ContextAwareBase。同步 Initialize 调用派生类的 OnInit；业务 Destroy 调用 OnDestroy。需要异步工作时实现 IAsyncInitializable，并通过异步架构入口驱动；实例日志工厂从当前架构服务获取。

```csharp
protected abstract void OnInit();
protected virtual void OnDestroy();
public virtual void OnArchitecturePhase(ArchitecturePhase phase);
```

普通服务可直接使用标准 DI，不要求实现 System 角色。

## System 生命周期

System 的生命周期由架构管理：

1. **注册阶段**：在 ConfigureServices 中通过 RegisterSystem 登记描述
2. **初始化阶段**：正式 provider 激活后调用同步 Initialize/OnInit 或异步 InitializeAsync
3. **运行阶段**：处理事件和执行业务逻辑
4. **销毁阶段**：架构调用 `Destroy()` 方法，内部执行 `OnDestroy()`

## 基本使用

### 1. 定义 System

```csharp
// 战斗系统（基类提供经架构上下文解析的 _logger）
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        _logger.LogInformation("CombatSystem initialized");
        
        // 注册事件监听
        this.RegisterEvent<EnemyAttackEvent>(OnEnemyAttack);
        this.RegisterEvent<PlayerAttackEvent>(OnPlayerAttack);
    }
    
    private void OnEnemyAttack(EnemyAttackEvent e)
    {
        var playerModel = this.GetModel<PlayerModel>();
        
        // 计算伤害
        int damage = CalculateDamage(e.AttackPower, playerModel.Defense.Value);
        
        // 应用伤害
        playerModel.Health.Value -= damage;
        
        // 发送伤害事件
        this.SendEvent(new PlayerTookDamageEvent { Damage = damage });
        
        _logger.LogDebug($"Player took {damage} damage from enemy attack");
    }
    
    private void OnPlayerAttack(PlayerAttackEvent e)
    {
        var playerModel = this.GetModel<PlayerModel>();
        var enemyModel = this.GetModel<EnemyModel>();
        
        int damage = CalculateDamage(playerModel.AttackPower.Value, e.Enemy.Defense);
        e.Enemy.Health -= damage;
        
        this.SendEvent(new EnemyTookDamageEvent 
        { 
            EnemyId = e.Enemy.Id, 
            Damage = damage 
        });
        
        _logger.LogDebug($"Enemy {e.Enemy.Id} took {damage} damage from player attack");
    }
    
    private int CalculateDamage(int attackPower, int defense)
    {
        return Math.Max(1, attackPower - defense / 2);
    }

    protected override void OnDestroy()
    {
        _logger.LogInformation("CombatSystem destroyed");
        // 清理资源
        base.OnDestroy();
    }
    
    public override void OnArchitecturePhase(ArchitecturePhase phase)
    {
        switch (phase)
        {
            case ArchitecturePhase.AfterSystemInit:
                _logger.LogInformation("CombatSystem is ready");
                break;
            case ArchitecturePhase.Destroying:
                _logger.LogInformation("CombatSystem is shutting down");
                break;
        }
    }
}
```

### 2. 注册 System

```csharp
public class GameArchitecture : Architecture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        // 注册 Model
        this.RegisterModel(new PlayerModel());
        this.RegisterModel(new EnemyModel());
        this.RegisterModel(new InventoryModel());

        // 登记 System；配置结束并构建 provider 后统一初始化
        this.RegisterSystem(new CombatSystem());
        this.RegisterSystem(new InventorySystem());
        this.RegisterSystem(new QuestSystem());
        this.RegisterSystem(new UISystem());
    }
}
```

### 3. 在其他组件中获取 System

```csharp
// 在 Controller 中
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class GameController : IController
{
    public void Start()
    {
        // 获取 System
        var combatSystem = this.GetSystem<CombatSystem>();
        var questSystem = this.GetSystem<QuestSystem>();

        // 使用 System
        combatSystem.StartBattle();
    }
}

// 在 System 中
public class BattleFlowSystem : AbstractSystem
{
    public void BeginBattle()
    {
        var combatSystem = this.GetSystem<CombatSystem>();
        combatSystem.StartBattle();
    }
}

// 在其他 System 中
public class AISystem : AbstractSystem
{
    protected override void OnInit()
    {
        var combatSystem = this.GetSystem<CombatSystem>();
        // 与其他 System 协作
    }
}
```

## 常见使用模式

### 异步 System

System 支持异步初始化，通过实现 `IAsyncInitializable` 接口可以在初始化时执行异步操作。

```csharp
// 异步系统示例
public class DataLoadSystem : AbstractSystem, IAsyncInitializable
{
    private GameData _gameData;

    protected override void OnInit()
    {
        // 同步初始化逻辑
        this.RegisterEvent<GameStartedEvent>(OnGameStarted);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // 异步加载游戏数据
        var storage = this.GetUtility<IStorageUtility>();
        _gameData = await storage.LoadGameDataAsync();

        // 数据加载完成后发送事件
        this.SendEvent(new GameDataLoadedEvent { Data = _gameData });
    }

    private void OnGameStarted(GameStartedEvent e)
    {
        // 使用已加载的数据
        Console.WriteLine($"Game data loaded: {_gameData.Version}");
    }

    protected override void OnDestroy()
    {
        // 清理资源
        _gameData = null;
    }
}

// 在架构中使用异步 System
public class GameArchitecture : Architecture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new DataLoadSystem());
        RegisterSystem(new CombatSystem());
    }
}

// 异步初始化架构
var architecture = new GameArchitecture();
await architecture.InitializeAsync();
```

### 1. 事件驱动的 System

```csharp
public class InventorySystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 监听物品相关事件
        this.RegisterEvent<ItemAddedEvent>(OnItemAdded);
        this.RegisterEvent<ItemRemovedEvent>(OnItemRemoved);
        this.RegisterEvent<ItemUsedEvent>(OnItemUsed);
    }
    
    private void OnItemAdded(ItemAddedEvent e)
    {
        var inventoryModel = this.GetModel<InventoryModel>();
        
        // 添加物品
        inventoryModel.AddItem(e.ItemId, e.Count);
        
        // 检查成就
        CheckAchievements(e.ItemId);
        
        // 发送通知
        this.SendEvent(new ShowNotificationEvent 
        { 
            Message = $"获得物品: {e.ItemId} x{e.Count}" 
        });
    }
    
    private void OnItemUsed(ItemUsedEvent e)
    {
        var inventoryModel = this.GetModel<InventoryModel>();
        var playerModel = this.GetModel<PlayerModel>();
        
        if (inventoryModel.HasItem(e.ItemId))
        {
            // 应用物品效果
            ApplyItemEffect(e.ItemId, playerModel);
            
            // 移除物品
            inventoryModel.RemoveItem(e.ItemId, 1);
            
            this.SendEvent(new ItemEffectAppliedEvent { ItemId = e.ItemId });
        }
    }
    
    private void ApplyItemEffect(string itemId, PlayerModel player)
    {
        // 物品效果逻辑...
        if (itemId == "health_potion")
        {
            player.Health.Value = Math.Min(
                player.Health.Value + 50, 
                player.MaxHealth.Value
            );
        }
    }
    
    private void CheckAchievements(string itemId)
    {
        // 成就检查逻辑...
    }
}
```

### 2. 定时更新的 System

```csharp
public class BuffSystem : AbstractSystem
{
    private List<BuffData> _activeBuffs = new();
    
    protected override void OnInit()
    {
        this.RegisterEvent<BuffAppliedEvent>(OnBuffApplied);
        this.RegisterEvent<GameUpdateEvent>(OnUpdate);
    }
    
    private void OnBuffApplied(BuffAppliedEvent e)
    {
        _activeBuffs.Add(new BuffData
        {
            BuffId = e.BuffId,
            Duration = e.Duration,
            RemainingTime = e.Duration
        });
        
        ApplyBuffEffect(e.BuffId, true);
    }
    
    private void OnUpdate(GameUpdateEvent e)
    {
        // 更新所有 Buff
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = _activeBuffs[i];
            buff.RemainingTime -= e.DeltaTime;
            
            if (buff.RemainingTime <= 0)
            {
                // Buff 过期
                ApplyBuffEffect(buff.BuffId, false);
                _activeBuffs.RemoveAt(i);
                
                this.SendEvent(new BuffExpiredEvent { BuffId = buff.BuffId });
            }
        }
    }
    
    private void ApplyBuffEffect(string buffId, bool apply)
    {
        var playerModel = this.GetModel<PlayerModel>();
        // 应用或移除 Buff 效果...
    }
}
```

### 3. 跨 System 协作

```csharp
public class QuestSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<EnemyKilledEvent>(OnEnemyKilled);
        this.RegisterEvent<ItemCollectedEvent>(OnItemCollected);
    }
    
    private void OnEnemyKilled(EnemyKilledEvent e)
    {
        var questModel = this.GetModel<QuestModel>();
        var activeQuests = questModel.GetActiveQuests();
        
        foreach (var quest in activeQuests)
        {
            if (quest.Type == QuestType.KillEnemy && quest.TargetId == e.EnemyType)
            {
                quest.Progress++;
                
                if (quest.Progress >= quest.RequiredAmount)
                {
                    // 任务完成
                    CompleteQuest(quest.Id);
                }
            }
        }
    }
    
    private void CompleteQuest(string questId)
    {
        var questModel = this.GetModel<QuestModel>();
        var quest = questModel.GetQuest(questId);
        
        // 标记任务完成
        questModel.CompleteQuest(questId);
        
        // 发放奖励（通过其他 System）
        this.SendEvent(new QuestCompletedEvent
        {
            QuestId = questId,
            Rewards = quest.Rewards
        });
    }
}

public class RewardSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<QuestCompletedEvent>(OnQuestCompleted);
    }
    
    private void OnQuestCompleted(QuestCompletedEvent e)
    {
        var playerModel = this.GetModel<PlayerModel>();
        
        // 发放奖励
        foreach (var reward in e.Rewards)
        {
            switch (reward.Type)
            {
                case RewardType.Gold:
                    playerModel.Gold.Value += reward.Amount;
                    break;
                case RewardType.Experience:
                    playerModel.Experience.Value += reward.Amount;
                    break;
                case RewardType.Item:
                    this.SendEvent(new ItemAddedEvent 
                    { 
                        ItemId = reward.ItemId, 
                        Count = reward.Amount 
                    });
                    break;
            }
        }
        
        this.SendEvent(new RewardsGrantedEvent { Rewards = e.Rewards });
    }
}
```

### 4. 管理复杂状态机

```csharp
public class GameStateSystem : AbstractSystem
{
    private GameState _currentState = GameState.MainMenu;
    
    protected override void OnInit()
    {
        this.RegisterEvent<GameStateChangeRequestEvent>(OnStateChangeRequest);
    }
    
    private void OnStateChangeRequest(GameStateChangeRequestEvent e)
    {
        if (CanTransition(_currentState, e.TargetState))
        {
            ExitState(_currentState);
            _currentState = e.TargetState;
            EnterState(_currentState);
            
            this.SendEvent(new GameStateChangedEvent 
            { 
                PreviousState = _currentState,
                NewState = e.TargetState 
            });
        }
    }
    
    private bool CanTransition(GameState from, GameState to)
    {
        // 状态转换规则
        return (from, to) switch
        {
            (GameState.MainMenu, GameState.Playing) => true,
            (GameState.Playing, GameState.Paused) => true,
            (GameState.Paused, GameState.Playing) => true,
            (GameState.Playing, GameState.GameOver) => true,
            _ => false
        };
    }
    
    private void EnterState(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                // 开始游戏
                this.GetSystem<GameFlowSystem>().Start();
                break;
            case GameState.Paused:
                // 暂停游戏
                this.SendEvent(new GamePausedEvent());
                break;
            case GameState.GameOver:
                // 游戏结束
                this.GetSystem<GameFlowSystem>().Stop();
                break;
        }
    }
    
    private void ExitState(GameState state)
    {
        // 清理当前状态
    }
}
```

## System vs Model

### Model（数据层）

- **职责**：存储数据和状态
- **特点**：被动，等待修改
- **示例**：PlayerModel、InventoryModel

### System（逻辑层）

- **职责**：处理业务逻辑，协调 Model
- **特点**：主动，响应事件
- **示例**：CombatSystem、QuestSystem

```csharp
// ✅ 正确的职责划分

// Model: 存储数据
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> Mana { get; } = new(50);
    
    protected override void OnInit() { }
}

// System: 处理逻辑
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<AttackEvent>(OnAttack);
    }
    
    private void OnAttack(AttackEvent e)
    {
        var playerModel = this.GetModel<PlayerModel>();
        
        // System 负责计算和决策
        int damage = CalculateDamage(e);
        playerModel.Health.Value -= damage;
        
        if (playerModel.Health.Value <= 0)
        {
            this.SendEvent(new PlayerDiedEvent());
        }
    }
}
```

## 最佳实践

1. **单一职责** - 每个 System 专注于一个业务领域
2. **事件驱动** - 通过事件与其他组件通信
3. **无状态或少状态** - 优先将状态存储在 Model 中
4. **可组合** - System 之间通过事件松耦合协作
5. **初始化注册** - 在 `OnInit` 中注册所有事件监听

## 性能优化

### 1. 避免频繁的 GetModel/GetSystem

```csharp
// ❌ 不好：每次都获取
private void OnUpdate(GameUpdateEvent e)
{
    var model = this.GetModel<PlayerModel>();  // 频繁调用
    // ...
}

// ✅ 好：缓存引用
private PlayerModel _playerModel;

protected override void OnInit()
{
    _playerModel = this.GetModel<PlayerModel>();  // 只获取一次
}

private void OnUpdate(GameUpdateEvent e)
{
    // 直接使用缓存的引用
    _playerModel.Health.Value += 1;
}
```

### 2. 批量处理

```csharp
public class ParticleSystem : AbstractSystem
{
    private List<Particle> _particles = new();
    
    private void OnUpdate(GameUpdateEvent e)
    {
        // 批量更新，而不是每个粒子发一个事件
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            UpdateParticle(_particles[i], e.DeltaTime);
        }
    }
}
```

## 相关包

- [`model`](./model.md) - System 操作 Model 的数据
- [`events`](./events.md) - System 通过事件通信
- [`command`](./command.md) - System 中可以发送 Command
- [`query`](./query.md) - System 中可以发送 Query
- [`utility`](./utility.md) - System 可以使用 Utility
- [`architecture`](./architecture.md) - 在架构中注册 System
