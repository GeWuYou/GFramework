# System 包使用说明

## 概述

System 包定义了业务逻辑层（Business Logic Layer）。System 负责处理游戏的核心业务逻辑，协调 Model 之间的交互，响应事件并执行复杂的业务流程。

## 核心接口

### [`ICanGetSystem`](ICanGetSystem.cs)

标记接口，表示该类型可以获取其他 System。

**继承关系：**

```csharp
public interface ICanGetSystem : IBelongToArchitecture
```

### [`ISystem`](ISystem.cs)

System 接口，定义了系统的基本行为。

**核心成员：**

```csharp
void Init();  // 系统初始化方法
```

**继承的能力：**

- `ICanSetArchitecture` - 可设置架构
- `ICanGetModel` - 可获取 Model
- `ICanGetUtility` - 可获取 Utility
- `ICanGetSystem` - 可获取其他 System
- `ICanRegisterEvent` - 可注册事件
- `ICanSendEvent` - 可发送事件

## 核心类

### [`AbstractSystem`](AbstractSystem.cs)

抽象 System 基类，提供了 System 的基础实现。

**使用方式：**

```csharp
public abstract class AbstractSystem : ISystem
{
    void ISystem.Init() => OnInit();
    protected abstract void OnInit();  // 子类实现初始化逻辑
}
```

## 基本使用

### 1. 定义 System

```csharp
// 战斗系统
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
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
    }
    
    private int CalculateDamage(int attackPower, int defense)
    {
        return Math.Max(1, attackPower - defense / 2);
    }
}
```

### 2. 注册 System

```csharp
public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 注册 Model
        this.RegisterModel<PlayerModel>(new PlayerModel());
        this.RegisterModel<EnemyModel>(new EnemyModel());
        
        // 注册 System（系统注册后会自动调用 Init）
        this.RegisterSystem<CombatSystem>(new CombatSystem());
        this.RegisterSystem<InventorySystem>(new InventorySystem());
        this.RegisterSystem<QuestSystem>(new QuestSystem());
    }
}
```

### 3. 在其他组件中获取 System

```csharp
// 在 Controller 中
public partial class GameController : Node, IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        // 获取 System
        var combatSystem = this.GetSystem<CombatSystem>();
        var questSystem = this.GetSystem<QuestSystem>();
    }
}

// 在 Command 中
public class StartBattleCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var combatSystem = this.GetSystem<CombatSystem>();
        // 使用 System...
    }
}
```

## 常见使用模式

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
                this.SendCommand(new StartGameCommand());
                break;
            case GameState.Paused:
                // 暂停游戏
                this.SendEvent(new GamePausedEvent());
                break;
            case GameState.GameOver:
                // 游戏结束
                this.SendCommand(new GameOverCommand());
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

- [`model`](../model/README.md) - System 操作 Model 的数据
- [`events`](../events/README.md) - System 通过事件通信
- [`command`](../command/README.md) - System 中可以发送 Command
- [`query`](../query/README.md) - System 中可以发送 Query
- [`utility`](../utility/README.md) - System 可以使用 Utility
- [`architecture`](../architecture/README.md) - 在架构中注册 System