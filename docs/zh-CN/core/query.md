# Query 包使用说明

## 概述

Query 包实现了 CQRS（命令查询职责分离）模式中的查询部分。Query 用于封装数据查询逻辑，与 Command 不同的是，Query
有返回值且不应该修改系统状态。

查询系统是 GFramework CQRS 架构的重要组成部分，专门负责数据读取操作，与命令系统和事件系统协同工作。

## 核心接口

### IQuery`<TResult>`

查询接口，定义了查询的基本契约。

**核心成员：**

```csharp
TResult Do();  // 执行查询并返回结果
```

## 核心类

### AbstractQuery`<TInput, TResult>`

抽象查询基类，提供了查询的基础实现。通过 `IQueryInput` 接口传递参数。

**核心方法：**

```csharp
TResult IQuery<TResult>.Do();           // 实现 IQuery 接口
protected abstract TResult OnDo(TInput input);  // 抽象查询方法，接收输入参数
```

**使用方式：**

```csharp
public abstract class AbstractQuery<TInput, TResult> : ContextAwareBase, IQuery<TResult>
    where TInput : IQueryInput
{
    public TResult Do() => OnDo(Input);          // 执行查询
    public TInput Input { get; set; }            // 输入参数
    protected abstract TResult OnDo(TInput input);  // 子类实现查询逻辑
}
```

### AbstractAsyncQuery`<TInput, TResult>`

支持异步执行的查询基类。

**核心方法：**

```csharp
Task<TResult> IAsyncQuery<TResult>.DoAsync();  // 实现异步查询接口
protected abstract Task<TResult> OnDoAsync(TInput input);  // 抽象异步查询方法
```

### EmptyQueryInput

空查询输入类，用于表示不需要任何输入参数的查询操作。

**使用方式：**

```csharp
public sealed class EmptyQueryInput : IQueryInput
{
    // 作为占位符使用，适用于那些不需要额外输入参数的查询场景
}
```

### QueryBus

查询总线实现，负责执行查询并返回结果。

**核心方法：**

```csharp
TResult Send<TResult>(IQuery<TResult> query);  // 发送并执行查询
```

**使用方式：**

```csharp
public sealed class QueryBus : IQueryBus
{
    public TResult Send<TResult>(IQuery<TResult> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return query.Do();
    }
}
```

## 基本使用

### 1. 定义查询

```csharp
// 定义查询输入
public class GetPlayerGoldInput : IQueryInput { }

// 查询玩家金币数量
public class GetPlayerGoldQuery : AbstractQuery<GetPlayerGoldInput, int>
{
    protected override int OnDo(GetPlayerGoldInput input)
    {
        return this.GetModel<PlayerModel>().Gold.Value;
    }
}

// 定义查询输入
public class GetItemCountInput : IQueryInput
{
    public string ItemId { get; set; }
}

// 查询背包中指定物品的数量
public class GetItemCountQuery : AbstractQuery<GetItemCountInput, int>
{
    protected override int OnDo(GetItemCountInput input)
    {
        var inventory = this.GetModel<InventoryModel>();
        return inventory.GetItemCount(input.ItemId);
    }
}

// 定义异步查询输入
public class LoadPlayerDataInput : IQueryInput
{
    public string PlayerId { get; set; }
}

// 异步查询玩家数据
public class LoadPlayerDataQuery : AbstractAsyncQuery<LoadPlayerDataInput, PlayerData>
{
    protected override async Task<PlayerData> OnDoAsync(LoadPlayerDataInput input)
    {
        var storage = this.GetUtility<IStorageUtility>();
        return await storage.LoadPlayerDataAsync(input.PlayerId);
    }
}
```

### 2. 发送查询

```csharp
public class ShopUI : IController
{
    [Export] private Button _buyButton;
    [Export] private int _itemPrice = 100;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void OnReady()
    {
        _buyButton.Pressed += OnBuyButtonPressed;
    }

    private void OnBuyButtonPressed()
    {
        // 查询玩家金币
        var query = new GetPlayerGoldQuery { Input = new GetPlayerGoldInput() };
        int playerGold = this.SendQuery(query);

        if (playerGold >= _itemPrice)
        {
            // 发送购买命令
            this.SendCommand(new BuyItemCommand { Input = new BuyItemInput { ItemId = "sword_01" } });
        }
        else
        {
            Console.WriteLine("金币不足！");
        }
    }
}
```

### 3. 在 System 中使用

```csharp
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 注册事件监听
        this.RegisterEvent<EnemyAttackEvent>(OnEnemyAttack);
    }

    private void OnEnemyAttack(EnemyAttackEvent e)
    {
        // 查询玩家是否已经死亡
        var query = new IsPlayerDeadQuery { Input = new EmptyQueryInput() };
        bool isDead = this.SendQuery(query);

        if (!isDead)
        {
            // 执行伤害逻辑
            this.SendCommand(new TakeDamageCommand { Input = new TakeDamageInput { Damage = e.Damage } });
        }
    }
}

public class IsPlayerDeadQuery : AbstractQuery<EmptyQueryInput, bool>
{
    protected override bool OnDo(EmptyQueryInput input)
    {
        return this.GetModel<PlayerModel>().Health.Value <= 0;
    }
}
```
```

## 高级用法

### 1. 带参数的复杂查询

```csharp
// 定义查询输入
public class GetEnemiesInRangeInput : IQueryInput
{
    public Vector3 Center { get; set; }
    public float Radius { get; set; }
}

// 查询指定范围内的敌人列表
public class GetEnemiesInRangeQuery : AbstractQuery<GetEnemiesInRangeInput, List<Enemy>>
{
    protected override List<Enemy> OnDo(GetEnemiesInRangeInput input)
    {
        var enemySystem = this.GetSystem<EnemySpawnSystem>();
        return enemySystem.GetEnemiesInRange(input.Center, input.Radius);
    }
}

// 使用
var input = new GetEnemiesInRangeInput { Center = playerPosition, Radius = 10.0f };
var query = new GetEnemiesInRangeQuery { Input = input };
var enemies = this.SendQuery(query);
```

### 2. 组合查询

```csharp
// 定义查询输入
public class CanUseSkillInput : IQueryInput
{
    public string SkillId { get; set; }
}

// 查询玩家是否可以使用技能
public class CanUseSkillQuery : AbstractQuery<CanUseSkillInput, bool>
{
    protected override bool OnDo(CanUseSkillInput input)
    {
        var playerModel = this.GetModel<PlayerModel>();

        // 查询技能消耗
        var skillCostQuery = new GetSkillCostQuery { Input = new GetSkillCostInput { SkillId = input.SkillId } };
        var skillCost = this.SendQuery(skillCostQuery);

        // 检查是否满足条件
        return playerModel.Mana.Value >= skillCost.ManaCost
            && !this.SendQuery(new IsSkillOnCooldownQuery { Input = new IsSkillOnCooldownInput { SkillId = input.SkillId } });
    }
}

public class GetSkillCostInput : IQueryInput
{
    public string SkillId { get; set; }
}

public class GetSkillCostQuery : AbstractQuery<GetSkillCostInput, SkillCost>
{
    protected override SkillCost OnDo(GetSkillCostInput input)
    {
        return this.GetModel<SkillModel>().GetSkillCost(input.SkillId);
    }
}

public class IsSkillOnCooldownInput : IQueryInput
{
    public string SkillId { get; set; }
}

public class IsSkillOnCooldownQuery : AbstractQuery<IsSkillOnCooldownInput, bool>
{
    protected override bool OnDo(IsSkillOnCooldownInput input)
    {
        return this.GetModel<SkillModel>().IsOnCooldown(input.SkillId);
    }
}
```

### 3. 聚合数据查询

```csharp
// 查询玩家战斗力
public class GetPlayerPowerQuery : AbstractQuery<EmptyQueryInput, int>
{
    protected override int OnDo(EmptyQueryInput input)
    {
        var playerModel = this.GetModel<PlayerModel>();
        var equipmentModel = this.GetModel<EquipmentModel>();

        int basePower = playerModel.Level.Value * 10;
        int equipmentPower = equipmentModel.GetTotalPower();
        int buffPower = this.SendQuery(new GetBuffPowerQuery { Input = new EmptyQueryInput() });

        return basePower + equipmentPower + buffPower;
    }
}

// 查询玩家详细信息（用于UI显示）
public class GetPlayerInfoQuery : AbstractQuery<EmptyQueryInput, PlayerInfo>
{
    protected override PlayerInfo OnDo(EmptyQueryInput input)
    {
        var playerModel = this.GetModel<PlayerModel>();

        return new PlayerInfo
        {
            Name = playerModel.Name.Value,
            Level = playerModel.Level.Value,
            Health = playerModel.Health.Value,
            MaxHealth = playerModel.MaxHealth.Value,
            Gold = this.SendQuery(new GetPlayerGoldQuery { Input = new GetPlayerGoldInput() }),
            Power = this.SendQuery(new GetPlayerPowerQuery { Input = new EmptyQueryInput() })
        };
    }
}
```

### 4. 跨 System 查询

```csharp
// 在 AI System 中查询玩家状态
public class EnemyAISystem : AbstractSystem
{
    protected override void OnInit() { }

    public void UpdateEnemyBehavior(Enemy enemy)
    {
        // 查询玩家位置
        var playerPosQuery = new GetPlayerPositionQuery { Input = new EmptyQueryInput() };
        var playerPos = this.SendQuery(playerPosQuery);

        // 查询玩家是否在攻击范围内
        var inRangeInput = new IsPlayerInRangeInput { Position = enemy.Position, Range = enemy.AttackRange };
        bool inRange = this.SendQuery(new IsPlayerInRangeQuery { Input = inRangeInput });

        if (inRange)
        {
            // 查询是否可以攻击
            var canAttackInput = new CanEnemyAttackInput { EnemyId = enemy.Id };
            bool canAttack = this.SendQuery(new CanEnemyAttackQuery { Input = canAttackInput });

            if (canAttack)
            {
                this.SendCommand(new EnemyAttackCommand { Input = new EnemyAttackInput { EnemyId = enemy.Id } });
            }
        }
    }
}
```

## Query 的执行机制

所有发送给查询总线的查询最终都会通过 `QueryExecutor` 来执行：

```csharp
public class QueryExecutor
{
    public static TResult Execute<TResult>(IQuery<TResult> query)
    {
        return query.Do();
    }
}
```

**特点：**

- 提供统一的查询执行机制
- 支持同步查询执行
- 与架构上下文集成

## Command vs Query

### Command（命令）

- **用途**：修改系统状态
- **返回值**：无返回值（void）或有返回值
- **示例**：购买物品、造成伤害、升级角色

### Query（查询）

- **用途**：读取数据，不修改状态
- **返回值**：必须有返回值
- **示例**：获取金币数量、检查技能冷却、查询玩家位置

```csharp
// ❌ 错误：在 Query 中修改状态
public class BadQuery : AbstractQuery<int>
{
    protected override int OnDo()
    {
        var model = this.GetModel<PlayerModel>();
        model.Gold.Value += 100;  // 不应该在 Query 中修改数据！
        return model.Gold.Value;
    }
}

// ✅ 正确：Query 只读取数据
public class GoodQuery : AbstractQuery<int>
{
    protected override int OnDo()
    {
        return this.GetModel<PlayerModel>().Gold.Value;
    }
}

// ✅ 修改数据应该使用 Command
public class AddGoldCommand : AbstractCommand
{
    private readonly int _amount;
    
    public AddGoldCommand(int amount)
    {
        _amount = amount;
    }
    
    protected override void OnExecute()
    {
        var model = this.GetModel<PlayerModel>();
        model.Gold.Value += _amount;
    }
}
```

## 最佳实践

1. **查询只读取，不修改** - 保持 Query 的纯粹性
2. **小而专注** - 每个 Query 只负责一个具体的查询任务
3. **可组合** - 复杂查询可以通过组合简单查询实现
4. **避免过度查询** - 如果需要频繁查询，考虑使用 BindableProperty
5. **命名清晰** - Query 名称应该清楚表达查询意图（Get、Is、Can、Has等前缀）
6. **参数通过构造函数传递** - 查询需要的参数应在创建时传入
7. **查询无状态** - 查询不应该保存长期状态，执行完即可丢弃
8. **合理使用缓存** - 对于复杂计算，可以在 Model 中缓存结果

## 性能优化

### 1. 缓存查询结果

```csharp
// 在 Model 中缓存复杂计算
public class PlayerModel : AbstractModel
{
    private int? _cachedPower;

    public int GetPower()
    {
        if (_cachedPower == null)
        {
            _cachedPower = CalculatePower();
        }
        return _cachedPower.Value;
    }
    
    private int CalculatePower()
    {
        // 复杂计算...
        return 100;
    }
    
    public void InvalidatePowerCache()
    {
        _cachedPower = null;
    }
}
```

### 2. 批量查询

```csharp
// 一次查询多个数据，而不是多次单独查询
public class GetMultipleItemCountsQuery : AbstractQuery<Dictionary<string, int>>
{
    private readonly List<string> _itemIds;
    
    public GetMultipleItemCountsQuery(List<string> itemIds)
    {
        _itemIds = itemIds;
    }
    
    protected override Dictionary<string, int> OnDo()
    {
        var inventory = this.GetModel<InventoryModel>();
        return _itemIds.ToDictionary(id => id, id => inventory.GetItemCount(id));
    }
}
```

## 查询模式优势

### 1. 职责分离

- 读写操作明确分离
- 便于优化读写性能
- 降低系统复杂度

### 2. 可扩展性

- 读写可以独立扩展
- 支持不同的数据存储策略
- 便于实现读写分离

### 3. 可维护性

- 查询逻辑集中管理
- 便于重构和优化
- 降低组件间耦合

## 相关包

- [`command`](./command.md) - CQRS 的命令部分
- [`model`](./model.md) - Query 主要从 Model 获取数据
- [`system`](./system.md) - System 中可以发送 Query
- **Controller** - Controller 中可以发送 Query（接口定义在 Core.Abstractions 中）
- [`extensions`](./extensions.md) - 提供 SendQuery 扩展方法
- [`architecture`](./architecture.md) - 架构核心，负责查询的分发和执行