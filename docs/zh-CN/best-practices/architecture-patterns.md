---
title: 架构设计模式指南
description: 围绕 GFramework 常见架构模式的职责划分、适用场景与组合建议。
---

# 架构设计模式指南

> 全面介绍 GFramework 中的架构设计模式，帮助你构建清晰、可维护、可扩展的游戏架构。

## 📋 目录

- [概述](#概述)
- [MVC 模式](#mvc-模式)
- [MVVM 模式](#mvvm-模式)
- [命令模式](#命令模式)
- [查询模式](#查询模式)
- [事件驱动模式](#事件驱动模式)
- [依赖注入模式](#依赖注入模式)
- [服务定位器模式](#服务定位器模式)
- [对象池模式](#对象池模式)
- [状态模式](#状态模式)
- [设计原则](#设计原则)
- [架构分层](#架构分层)
- [依赖管理](#依赖管理)
- [事件系统设计](#事件系统设计)
- [模块化架构](#模块化架构)
- [错误处理策略](#错误处理策略)
- [测试策略](#测试策略)
- [重构指南](#重构指南)
- [模式选择与组合](#模式选择与组合)
- [常见问题](#常见问题)

## 概述

架构设计模式是经过验证的解决方案，用于解决软件开发中的常见问题。GFramework 内置了多种设计模式，帮助你构建高质量的游戏应用。

### 为什么需要架构设计模式？

1. **提高代码质量**：遵循最佳实践，减少 bug
2. **增强可维护性**：清晰的结构，易于理解和修改
3. **促进团队协作**：统一的代码风格和架构
4. **提升可扩展性**：轻松添加新功能
5. **简化测试**：解耦的组件更容易测试

### GFramework 支持的核心模式

| 模式        | 用途           | 核心组件                    |
|-----------|--------------|-------------------------|
| **MVC**   | 分离数据、视图和控制逻辑 | Model, Controller       |
| **MVVM**  | 数据绑定和响应式 UI  | Model, BindableProperty |
| **命令模式**  | 封装操作请求       | ICommand, CommandBus    |
| **查询模式**  | 分离读操作        | IQuery, QueryBus        |
| **事件驱动**  | 松耦合通信        | IEventBus, Event        |
| **依赖注入**  | 控制反转         | IIocContainer           |
| **服务定位器** | 服务查找         | Architecture.GetSystem  |
| **对象池**   | 对象复用         | IObjectPoolSystem       |
| **状态模式**  | 状态管理         | IStateMachine           |

## MVC 模式

### 概念

MVC（Model-View-Controller）是一种将应用程序分为三个核心组件的架构模式：

- **Model（模型）**：管理数据和业务逻辑
- **View（视图）**：显示数据给用户
- **Controller（控制器）**：处理用户输入，协调 Model 和 View

### 在 GFramework 中的实现

```csharp
// Model - 数据层
public class PlayerModel : AbstractModel
{
    public BindableProperty&lt;int&gt; Health { get; } = new(100);
    public BindableProperty&lt;int&gt; Score { get; } = new(0);
    public BindableProperty&lt;string&gt; Name { get; } = new("Player");

    protected override void OnInit()
    {
        // 监听数据变化
        Health.Register(newHealth =>
        {
            if (newHealth &lt;= 0)
            {
                this.SendEvent(new PlayerDiedEvent());
            }
        });
    }
}

// Controller - 控制层
[ContextAware]
public partial class PlayerController : Node, IController
{
    private PlayerModel _playerModel;

    public override void _Ready()
    {
        _playerModel = this.GetModel&lt;PlayerModel&gt;();

        // 监听数据变化，更新视图
        _playerModel.Health.Register(UpdateHealthUI);
        _playerModel.Score.Register(UpdateScoreUI);
    }

    // 处理用户输入
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Space)
            {
                // 发送命令修改 Model
                this.SendCommand(new AttackCommand());
            }
        }
    }

    // 更新视图
    private void UpdateHealthUI(int health)
    {
        var healthBar = GetNode&lt;ProgressBar&gt;("HealthBar");
        healthBar.Value = health;
    }

    private void UpdateScoreUI(int score)
    {
        var scoreLabel = GetNode&lt;Label&gt;("ScoreLabel");
        scoreLabel.Text = $"Score: {score}";
    }
}

// System - 业务逻辑层（可选）
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent&lt;AttackCommand&gt;(OnAttack);
    }

    private void OnAttack(AttackCommand cmd)
    {
        var playerModel = this.GetModel&lt;PlayerModel&gt;();
        var enemyModel = this.GetModel&lt;EnemyModel&gt;();

        // 计算伤害
        int damage = CalculateDamage(playerModel, enemyModel);
        enemyModel.Health.Value -= damage;

        // 增加分数
        if (enemyModel.Health.Value &lt;= 0)
        {
            playerModel.Score.Value += 100;
        }
    }

    private int CalculateDamage(PlayerModel player, EnemyModel enemy)
    {
        return 10; // 简化示例
    }
}
```

### MVC 优势

- ✅ **职责分离**：Model、View、Controller 各司其职
- ✅ **易于测试**：可以独立测试每个组件
- ✅ **可维护性高**：修改一个组件不影响其他组件
- ✅ **支持多视图**：同一个 Model 可以有多个 View

### 最佳实践

1. **Model 只负责数据**：不包含 UI 逻辑
2. **Controller 协调交互**：不直接操作 UI 细节
3. **View 只负责显示**：不包含业务逻辑
4. **使用事件通信**：Model 变化通过事件通知 Controller

## MVVM 模式

### 概念

MVVM（Model-View-ViewModel）是 MVC 的变体，强调数据绑定和响应式编程：

- **Model**：数据和业务逻辑
- **View**：用户界面
- **ViewModel**：View 的抽象，提供数据绑定

### 在 GFramework 中的实现

```csharp
// Model - 数据层
public class GameModel : AbstractModel
{
    public BindableProperty&lt;int&gt; CurrentLevel { get; } = new(1);
    public BindableProperty&lt;float&gt; Progress { get; } = new(0f);
    public BindableProperty&lt;bool&gt; IsLoading { get; } = new(false);

    protected override void OnInit()
    {
        Progress.Register(progress =>
        {
            if (progress &gt;= 1.0f)
            {
                this.SendEvent(new LevelCompletedEvent());
            }
        });
    }
}

// ViewModel - 视图模型（在 GFramework 中，Model 本身就是 ViewModel）
public class PlayerViewModel : AbstractModel
{
    private PlayerModel _playerModel;

    // 计算属性
    public BindableProperty&lt;string&gt; HealthText { get; } = new("");
    public BindableProperty&lt;float&gt; HealthPercentage { get; } = new(1.0f);
    public BindableProperty&lt;bool&gt; IsAlive { get; } = new(true);

    protected override void OnInit()
    {
        _playerModel = this.GetModel&lt;PlayerModel&gt;();

        // 绑定数据转换
        _playerModel.Health.Register(health =>
        {
            HealthText.Value = $"{health} / {_playerModel.MaxHealth.Value}";
            HealthPercentage.Value = (float)health / _playerModel.MaxHealth.Value;
            IsAlive.Value = health &gt; 0;
        });
    }
}

// View - 视图层
[ContextAware]
public partial class PlayerView : Control, IController
{
    private PlayerViewModel _viewModel;
    private Label _healthLabel;
    private ProgressBar _healthBar;
    private Panel _deathPanel;

    public override void _Ready()
    {
        _viewModel = this.GetModel&lt;PlayerViewModel&gt;();

        _healthLabel = GetNode&lt;Label&gt;("HealthLabel");
        _healthBar = GetNode&lt;ProgressBar&gt;("HealthBar");
        _deathPanel = GetNode&lt;Panel&gt;("DeathPanel");

        // 数据绑定
        _viewModel.HealthText.Register(text =&gt; _healthLabel.Text = text);
        _viewModel.HealthPercentage.Register(pct =&gt; _healthBar.Value = pct * 100);
        _viewModel.IsAlive.Register(alive =&gt; _deathPanel.Visible = !alive);
    }
}
```

### MVVM 优势

- ✅ **自动更新 UI**：数据变化自动反映到界面
- ✅ **减少样板代码**：不需要手动更新 UI
- ✅ **易于测试**：ViewModel 可以独立测试
- ✅ **支持复杂 UI**：适合数据驱动的界面

### 最佳实践

1. **使用 BindableProperty**：实现响应式数据
2. **ViewModel 不依赖 View**：保持单向依赖
3. **计算属性放在 ViewModel**：如百分比、格式化文本
4. **避免在 View 中写业务逻辑**：只负责数据绑定

## 命令模式

### 概念

命令模式将请求封装为对象，从而支持参数化、队列化、日志记录和撤销操作。

### 在 GFramework 中的实现

```csharp
// 定义命令输入
public class BuyItemInput : ICommandInput
{
    public string ItemId { get; set; }
    public int Quantity { get; set; }
}

// 实现命令
public class BuyItemCommand : AbstractCommand&lt;BuyItemInput&gt;
{
    protected override void OnExecute(BuyItemInput input)
    {
        var playerModel = this.GetModel&lt;PlayerModel&gt;();
        var inventoryModel = this.GetModel&lt;InventoryModel&gt;();
        var shopModel = this.GetModel&lt;ShopModel&gt;();

        // 获取物品信息
        var item = shopModel.GetItem(input.ItemId);
        var totalCost = item.Price * input.Quantity;

        // 检查金币
        if (playerModel.Gold.Value &lt; totalCost)
        {
            this.SendEvent(new InsufficientGoldEvent());
            return;
        }

        // 扣除金币
        playerModel.Gold.Value -= totalCost;

        // 添加物品
        inventoryModel.AddItem(input.ItemId, input.Quantity);

        // 发送事件
        this.SendEvent(new ItemPurchasedEvent
        {
            ItemId = input.ItemId,
            Quantity = input.Quantity,
            Cost = totalCost
        });
    }
}

// 使用命令
[ContextAware]
public partial class ShopController : IController
{
    public void OnBuyButtonClicked(string itemId, int quantity)
    {
        // 创建并发送命令
        var input = new BuyItemInput
        {
            ItemId = itemId,
            Quantity = quantity
        };

        this.SendCommand(new BuyItemCommand { Input = input });
    }
}
```

### 支持撤销的命令

```csharp
public interface IUndoableCommand : ICommand
{
    void Undo();
}

public class MoveCommand : AbstractCommand, IUndoableCommand
{
    private Vector2 _previousPosition;
    private Vector2 _newPosition;

    public MoveCommand(Vector2 newPosition)
    {
        _newPosition = newPosition;
    }

    protected override void OnExecute()
    {
        var playerModel = this.GetModel&lt;PlayerModel&gt;();
        _previousPosition = playerModel.Position.Value;
        playerModel.Position.Value = _newPosition;
    }

    public void Undo()
    {
        var playerModel = this.GetModel&lt;PlayerModel&gt;();
        playerModel.Position.Value = _previousPosition;
    }
}

// 命令历史管理器
public class CommandHistory
{
    private readonly Stack&lt;IUndoableCommand&gt; _history = new();

    public void Execute(IUndoableCommand command)
    {
        command.Execute();
        _history.Push(command);
    }

    public void Undo()
    {
        if (_history.Count &gt; 0)
        {
            var command = _history.Pop();
            command.Undo();
        }
    }
}
```

### 命令模式优势

- ✅ **解耦发送者和接收者**：调用者不需要知道实现细节
- ✅ **支持撤销/重做**：保存命令历史
- ✅ **支持队列和日志**：可以记录所有操作
- ✅ **易于扩展**：添加新命令不影响现有代码

### 最佳实践

1. **命令保持原子性**：一个命令完成一个完整操作
2. **使用输入对象传参**：避免构造函数参数过多
3. **命令无状态**：执行完即可丢弃
4. **发送事件通知结果**：而不是返回值

## 查询模式

### 概念

查询模式（CQRS 的一部分）将读操作与写操作分离，查询只读取数据，不修改状态。

### 在 GFramework 中的实现

```csharp
// 定义查询输入
public class GetPlayerStatsInput : IQueryInput
{
    public string PlayerId { get; set; }
}

// 定义查询结果
public class PlayerStats
{
    public int Level { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int TotalPower { get; set; }
}

// 实现查询
public class GetPlayerStatsQuery : AbstractQuery&lt;GetPlayerStatsInput, PlayerStats&gt;
{
    protected override PlayerStats OnDo(GetPlayerStatsInput input)
    {
        var playerModel = this.GetModel&lt;PlayerModel&gt;();
        var equipmentModel = this.GetModel&lt;EquipmentModel&gt;();

        // 计算总战力
        int basePower = playerModel.Level.Value * 10;
        int equipmentPower = equipmentModel.GetTotalPower();

        return new PlayerStats
        {
            Level = playerModel.Level.Value,
            Health = playerModel.Health.Value,
            MaxHealth = playerModel.MaxHealth.Value,
            Attack = playerModel.Attack.Value + equipmentModel.GetAttackBonus(),
            Defense = playerModel.Defense.Value + equipmentModel.GetDefenseBonus(),
            TotalPower = basePower + equipmentPower
        };
    }
}

// 使用查询
[ContextAware]
public partial class CharacterPanelController : IController
{
    public void ShowCharacterStats()
    {
        var input = new GetPlayerStatsInput { PlayerId = "player1" };
        var query = new GetPlayerStatsQuery { Input = input };
        var stats = this.SendQuery(query);

        // 显示统计信息
        DisplayStats(stats);
    }

    private void DisplayStats(PlayerStats stats)
    {
        Console.WriteLine($"Level: {stats.Level}");
        Console.WriteLine($"Health: {stats.Health}/{stats.MaxHealth}");
        Console.WriteLine($"Attack: {stats.Attack}");
        Console.WriteLine($"Defense: {stats.Defense}");
        Console.WriteLine($"Total Power: {stats.TotalPower}");
    }
}
```

### 复杂查询示例

```csharp
// 查询背包中可装备的物品
public class GetEquippableItemsQuery : AbstractQuery&lt;EmptyQueryInput, List&lt;Item&gt;&gt;
{
    protected override List&lt;Item&gt; OnDo(EmptyQueryInput input)
    {
        var inventoryModel = this.GetModel&lt;InventoryModel&gt;();
        var playerModel = this.GetModel&lt;PlayerModel&gt;();

        return inventoryModel.GetAllItems()
            .Where(item =&gt; item.Type == ItemType.Equipment)
            .Where(item =&gt; item.RequiredLevel &lt;= playerModel.Level.Value)
            .OrderByDescending(item =&gt; item.Power)
            .ToList();
    }
}

// 组合查询
public class CanUseSkillQuery : AbstractQuery&lt;CanUseSkillInput, bool&gt;
{
    protected override bool OnDo(CanUseSkillInput input)
    {
        var playerModel = this.GetModel&lt;PlayerModel&gt;();

        // 查询技能消耗
        var costQuery = new GetSkillCostQuery { Input = new GetSkillCostInput { SkillId = input.SkillId } };
        var cost = this.SendQuery(costQuery);

        // 查询冷却状态
        var cooldownQuery = new IsSkillOnCooldownQuery { Input = new IsSkillOnCooldownInput { SkillId = input.SkillId } };
        var onCooldown = this.SendQuery(cooldownQuery);

        // 综合判断
        return playerModel.Mana.Value &gt;= cost.ManaCost
            &amp;&amp; !onCooldown
            &amp;&amp; playerModel.Health.Value &gt; 0;
    }
}
```

### 查询模式优势

- ✅ **职责分离**：读写操作明确分离
- ✅ **易于优化**：可以针对查询进行缓存优化
- ✅ **提高可读性**：查询意图清晰
- ✅ **支持复杂查询**：可以组合多个简单查询

### 最佳实践

1. **查询只读取，不修改**：保持查询的纯粹性
2. **使用清晰的命名**：Get、Is、Can、Has 等前缀
3. **避免过度查询**：频繁查询考虑使用 BindableProperty
4. **合理使用缓存**：复杂计算结果可以缓存

## 事件驱动模式

### 概念

事件驱动模式通过事件实现组件间的松耦合通信。发送者不需要知道接收者，接收者通过订阅事件来响应变化。

### 在 GFramework 中的实现

```csharp
// 定义事件
public struct PlayerDiedEvent
{
    public Vector3 Position { get; set; }
    public string Cause { get; set; }
    public int FinalScore { get; set; }
}

public struct EnemyKilledEvent
{
    public string EnemyId { get; set; }
    public int Reward { get; set; }
}

// Model 发送事件
public class PlayerModel : AbstractModel
{
    public BindableProperty&lt;int&gt; Health { get; } = new(100);
    public BindableProperty&lt;Vector3&gt; Position { get; } = new(Vector3.Zero);

    protected override void OnInit()
    {
        Health.Register(newHealth =>
        {
            if (newHealth &lt;= 0)
            {
                // 发送玩家死亡事件
                this.SendEvent(new PlayerDiedEvent
                {
                    Position = Position.Value,
                    Cause = "Health depleted",
                    FinalScore = this.GetModel&lt;GameModel&gt;().Score.Value
                });
            }
        });
    }
}

// System 监听和发送事件
public class AchievementSystem : AbstractSystem
{
    private int _enemyKillCount = 0;

    protected override void OnInit()
    {
        // 监听敌人被杀事件
        this.RegisterEvent&lt;EnemyKilledEvent&gt;(OnEnemyKilled);

        // 监听玩家死亡事件
        this.RegisterEvent&lt;PlayerDiedEvent&gt;(OnPlayerDied);
    }

    private void OnEnemyKilled(EnemyKilledEvent e)
    {
        _enemyKillCount++;

        // 检查成就条件
        if (_enemyKillCount == 10)
        {
            this.SendEvent(new AchievementUnlockedEvent
            {
                AchievementId = "first_blood_10",
                Title = "新手猎人",
                Description = "击败10个敌人"
            });
        }
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        // 记录统计数据
        var statsModel = this.GetModel&lt;StatisticsModel&gt;();
        statsModel.RecordDeath(e.Position, e.Cause);
    }
}

// Controller 监听事件
[ContextAware]
public partial class UIController : IController
{
    private IUnRegisterList _unregisterList = new UnRegisterList();

    public void Initialize()
    {
        // 监听成就解锁事件
        this.RegisterEvent<AchievementUnlockedEvent>(OnAchievementUnlocked)
            .AddToUnregisterList(_unregisterList);

        // 监听玩家死亡事件
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
            .AddToUnregisterList(_unregisterList);
    }

    private void OnAchievementUnlocked(AchievementUnlockedEvent e)
    {
        ShowAchievementNotification(e.Title, e.Description);
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        ShowGameOverScreen(e.FinalScore);
    }

    public void Cleanup()
    {
        _unregisterList.UnRegisterAll();
    }
}
```

### 事件组合

```csharp
using GFramework.Core.SourceGenerators.Abstractions.Rule;

// 使用 OrEvent 组合多个事件
[ContextAware]
public partial class InputController : IController
{
    public void Initialize()
    {
        var onAnyInput = new OrEvent()
            .Or(keyboardEvent)
            .Or(mouseEvent)
            .Or(gamepadEvent);

        onAnyInput.Register(() =>
        {
            ResetIdleTimer();
        });
    }
}
```

### 事件驱动模式优势

- ✅ **松耦合**：发送者和接收者互不依赖
- ✅ **一对多通信**：一个事件可以有多个监听者
- ✅ **易于扩展**：添加新监听者不影响现有代码
- ✅ **支持异步**：事件可以异步处理

### 最佳实践

1. **事件命名使用过去式**：PlayerDiedEvent、LevelCompletedEvent
2. **事件使用结构体**：减少内存分配
3. **及时注销事件**：使用 IUnRegisterList 管理
4. **避免事件循环**：事件处理器中谨慎发送新事件

## 依赖注入模式

### 概念

依赖注入（DI）是一种实现控制反转（IoC）的技术，通过外部注入依赖而不是在类内部创建。

### 在 GFramework 中的实现

```csharp
// 定义接口
public interface IStorageService
{
    Task SaveAsync&lt;T&gt;(string key, T data);
    Task&lt;T&gt; LoadAsync&lt;T&gt;(string key);
}

public interface IAudioService
{
    void PlaySound(string soundId);
    void PlayMusic(string musicId);
}

// 实现服务
public class LocalStorageService : IStorageService
{
    public async Task SaveAsync&lt;T&gt;(string key, T data)
    {
        var json = JsonSerializer.Serialize(data);
        await File.WriteAllTextAsync($"saves/{key}.json", json);
    }

    public async Task&lt;T&gt; LoadAsync&lt;T&gt;(string key)
    {
        var json = await File.ReadAllTextAsync($"saves/{key}.json");
        return JsonSerializer.Deserialize&lt;T&gt;(json);
    }
}

public class GodotAudioService : IAudioService
{
    private AudioStreamPlayer _soundPlayer;
    private AudioStreamPlayer _musicPlayer;

    public void PlaySound(string soundId)
    {
        var sound = GD.Load&lt;AudioStream&gt;($"res://sounds/{soundId}.ogg");
        _soundPlayer.Stream = sound;
        _soundPlayer.Play();
    }

    public void PlayMusic(string musicId)
    {
        var music = GD.Load&lt;AudioStream&gt;($"res://music/{musicId}.ogg");
        _musicPlayer.Stream = music;
        _musicPlayer.Play();
    }
}

// 在架构中注册服务
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册服务实现
        RegisterUtility&lt;IStorageService&gt;(new LocalStorageService());
        RegisterUtility&lt;IAudioService&gt;(new GodotAudioService());

        // 注册 System（System 会自动获取依赖）
        RegisterSystem(new SaveSystem());
        RegisterSystem(new AudioSystem());
    }
}

// System 使用依赖注入
public class SaveSystem : AbstractSystem
{
    private IStorageService _storageService;

    protected override void OnInit()
    {
        // 从容器获取依赖
        _storageService = this.GetUtility&lt;IStorageService&gt;();

        this.RegisterEvent&lt;SaveGameEvent&gt;(OnSaveGame);
    }

    private async void OnSaveGame(SaveGameEvent e)
    {
        var playerModel = this.GetModel&lt;PlayerModel&gt;();
        var saveData = new SaveData
        {
            PlayerName = playerModel.Name.Value,
            Level = playerModel.Level.Value,
            Health = playerModel.Health.Value
        };

        await _storageService.SaveAsync("current_save", saveData);
    }
}
```

### 构造函数注入（推荐）

```csharp
public class SaveSystem : AbstractSystem
{
    private readonly IStorageService _storageService;
    private readonly IAudioService _audioService;

    // 通过构造函数注入依赖
    public SaveSystem(IStorageService storageService, IAudioService audioService)
    {
        _storageService = storageService;
        _audioService = audioService;
    }

    protected override void OnInit()
    {
        this.RegisterEvent&lt;SaveGameEvent&gt;(OnSaveGame);
    }

    private async void OnSaveGame(SaveGameEvent e)
    {
        await _storageService.SaveAsync("save", e.Data);
        _audioService.PlaySound("save_success");
    }
}

// 注册时传入依赖
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        var storageService = new LocalStorageService();
        var audioService = new GodotAudioService();

        RegisterUtility&lt;IStorageService&gt;(storageService);
        RegisterUtility&lt;IAudioService&gt;(audioService);

        // 构造函数注入
        RegisterSystem(new SaveSystem(storageService, audioService));
    }
}
```

### 依赖注入优势

- ✅ **易于测试**：可以注入模拟对象
- ✅ **松耦合**：依赖接口而非实现
- ✅ **灵活配置**：运行时选择实现
- ✅ **提高可维护性**：依赖关系清晰

### 最佳实践

1. **依赖接口而非实现**：使用 IStorageService 而非 LocalStorageService
2. **优先使用构造函数注入**：依赖关系更明确
3. **避免循环依赖**：System 不应相互依赖
4. **使用 IoC 容器管理生命周期**：让框架管理对象创建

## 服务定位器模式

### 概念

服务定位器模式提供一个全局访问点来获取服务，是依赖注入的替代方案。

### 在 GFramework 中的实现

```csharp
// GFramework 的 Architecture 本身就是服务定位器
public class GameplaySystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 通过服务定位器获取服务
        var playerModel = this.GetModel&lt;PlayerModel&gt;();
        var audioSystem = this.GetSystem&lt;AudioSystem&gt;();
        var storageUtility = this.GetUtility&lt;IStorageUtility&gt;();

        // 使用服务
        playerModel.Health.Value = 100;
        audioSystem.PlayBGM("gameplay");
    }
}

// 在 Controller 中使用
[ContextAware]
public partial class MenuController : IController
{
    public void OnStartButtonClicked()
    {
        // 通过架构获取服务
        var gameModel = this.GetModel<GameModel>();
        gameModel.GameState.Value = GameState.Playing;

        // 发送命令
        this.SendCommand(new StartGameCommand());
    }
}
```

### 自定义服务定位器

```csharp
// 创建专门的服务定位器
public static class ServiceLocator
{
    private static readonly Dictionary&lt;Type, object&gt; _services = new();

    public static void Register&lt;T&gt;(T service)
    {
        _services[typeof(T)] = service;
    }

    public static T Get&lt;T&gt;()
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        throw new InvalidOperationException($"Service {typeof(T)} not registered");
    }

    public static void Clear()
    {
        _services.Clear();
    }
}

// 使用自定义服务定位器
public class GameInitializer
{
    public void Initialize()
    {
        // 注册服务
        ServiceLocator.Register&lt;IAnalyticsService&gt;(new AnalyticsService());
        ServiceLocator.Register&lt;ILeaderboardService&gt;(new LeaderboardService());
    }
}

public class GameOverScreen
{
    public void SubmitScore(int score)
    {
        // 获取服务
        var leaderboard = ServiceLocator.Get&lt;ILeaderboardService&gt;();
        leaderboard.SubmitScore(score);

        var analytics = ServiceLocator.Get&lt;IAnalyticsService&gt;();
        analytics.TrackEvent("game_over", new { score });
    }
}
```

### 服务定位器 vs 依赖注入

| 特性        | 服务定位器        | 依赖注入       |
|-----------|--------------|------------|
| **依赖可见性** | 隐式（运行时获取）    | 显式（构造函数参数） |
| **易用性**   | 简单直接         | 需要配置       |
| **测试性**   | 较难（需要模拟全局状态） | 容易（注入模拟对象） |
| **编译时检查** | 无            | 有          |
| **适用场景**  | 快速原型、小项目     | 大型项目、团队协作  |

### 最佳实践

1. **小项目使用服务定位器**：简单直接
2. **大项目使用依赖注入**：更易维护
3. **避免过度使用**：不要把所有东西都放入定位器
4. **提供清晰的 API**：GetModel、GetSystem、GetUtility

## 对象池模式

### 概念

对象池模式通过复用对象来减少内存分配和垃圾回收，提高性能。

### 在 GFramework 中的实现

```csharp
// 定义可池化对象
public class Bullet : Node2D, IPoolableNode
{
    public bool IsInPool { get; set; }

    public void OnSpawn()
    {
        // 从池中取出时调用
        Visible = true;
        IsInPool = false;
    }

    public void OnRecycle()
    {
        // 回收到池中时调用
        Visible = false;
        IsInPool = true;
        Position = Vector2.Zero;
        Rotation = 0;
    }
}

// 创建对象池系统
public class BulletPoolSystem : AbstractNodePoolSystem&lt;Bullet&gt;
{
    protected override Bullet CreateInstance()
    {
        var bullet = new Bullet();
        // 初始化子弹
        return bullet;
    }

    protected override void OnInit()
    {
        // 预创建对象
        PrewarmPool(50);
    }
}

// 使用对象池
public class WeaponSystem : AbstractSystem
{
    private BulletPoolSystem _bulletPool;

    protected override void OnInit()
    {
        _bulletPool = this.GetSystem&lt;BulletPoolSystem&gt;();
        this.RegisterEvent&lt;FireWeaponEvent&gt;(OnFireWeapon);
    }

    private void OnFireWeapon(FireWeaponEvent e)
    {
        // 从池中获取子弹
        var bullet = _bulletPool.Spawn();
        bullet.Position = e.Position;
        bullet.Rotation = e.Direction;

        // 3秒后回收
        ScheduleRecycle(bullet, 3.0f);
    }

    private async void ScheduleRecycle(Bullet bullet, float delay)
    {
        await Task.Delay((int)(delay * 1000));
        _bulletPool.Recycle(bullet);
    }
}
```

### 通用对象池

```csharp
// 通用对象池实现
public class ObjectPool&lt;T&gt; where T : class, new()
{
    private readonly Stack&lt;T&gt; _pool = new();
    private readonly Action&lt;T&gt; _onSpawn;
    private readonly Action&lt;T&gt; _onRecycle;
    private readonly int _maxSize;

    public ObjectPool(int initialSize = 10, int maxSize = 100,
        Action&lt;T&gt; onSpawn = null, Action&lt;T&gt; onRecycle = null)
    {
        _maxSize = maxSize;
        _onSpawn = onSpawn;
        _onRecycle = onRecycle;

        // 预创建对象
        for (int i = 0; i &lt; initialSize; i++)
        {
            _pool.Push(new T());
        }
    }

    public T Spawn()
    {
        T obj = _pool.Count &gt; 0 ? _pool.Pop() : new T();
        _onSpawn?.Invoke(obj);
        return obj;
    }

    public void Recycle(T obj)
    {
        if (_pool.Count &lt; _maxSize)
        {
            _onRecycle?.Invoke(obj);
            _pool.Push(obj);
        }
    }

    public void Clear()
    {
        _pool.Clear();
    }
}

// 使用通用对象池
public class ParticleSystem : AbstractSystem
{
    private ObjectPool&lt;Particle&gt; _particlePool;

    protected override void OnInit()
    {
        _particlePool = new ObjectPool&lt;Particle&gt;(
            initialSize: 100,
            maxSize: 500,
            onSpawn: p => p.Reset(),
            onRecycle: p => p.Clear()
        );
    }

    public void EmitParticles(Vector3 position, int count)
    {
        for (int i = 0; i &lt; count; i++)
        {
            var particle = _particlePool.Spawn();
            particle.Position = position;
            particle.Velocity = GetRandomVelocity();
        }
    }
}
```

### 对象池优势

- ✅ **减少 GC 压力**：复用对象，减少内存分配
- ✅ **提高性能**：避免频繁创建销毁对象
- ✅ **稳定帧率**：减少 GC 导致的卡顿
- ✅ **适合高频对象**：子弹、粒子、特效等

### 最佳实践

1. **预热池**：提前创建对象避免运行时分配
2. **设置最大容量**：防止池无限增长
3. **重置对象状态**：OnRecycle 中清理状态
4. **监控池使用情况**：记录 Spawn/Recycle 次数

## 状态模式

### 概念

状态模式允许对象在内部状态改变时改变其行为，将状态相关的行为封装到独立的状态类中。

### 在 GFramework 中的实现

```csharp
// 定义游戏状态
public class MenuState : ContextAwareStateBase
{
    public override void OnEnter(IState from)
    {
        Console.WriteLine("进入菜单状态");

        // 显示菜单 UI
        var uiSystem = this.GetSystem&lt;UISystem&gt;();
        uiSystem.ShowMenu();

        // 播放菜单音乐
        var audioSystem = this.GetSystem&lt;AudioSystem&gt;();
        audioSystem.PlayBGM("menu_theme");
    }

    public override void OnExit(IState to)
    {
        Console.WriteLine("退出菜单状态");

        // 隐藏菜单 UI
        var uiSystem = this.GetSystem&lt;UISystem&gt;();
        uiSystem.HideMenu();
    }

    public override bool CanTransitionTo(IState target)
    {
        // 菜单可以转换到游戏或设置状态
        return target is GameplayState or SettingsState;
    }
}

public class GameplayState : ContextAwareStateBase
{
    public override void OnEnter(IState from)
    {
        Console.WriteLine("进入游戏状态");

        // 初始化游戏
        var gameModel = this.GetModel&lt;GameModel&gt;();
        gameModel.Reset();

        // 加载关卡
        this.SendCommand(new LoadLevelCommand { LevelId = 1 });

        // 播放游戏音乐
        var audioSystem = this.GetSystem&lt;AudioSystem&gt;();
        audioSystem.PlayBGM("gameplay_theme");
    }

    public override void OnExit(IState to)
    {
        Console.WriteLine("退出游戏状态");

        // 保存游戏进度
        this.SendCommand(new SaveGameCommand());
    }

    public override bool CanTransitionTo(IState target)
    {
        // 游戏中可以暂停或结束
        return target is PauseState or GameOverState;
    }
}

public class PauseState : ContextAwareStateBase
{
    public override void OnEnter(IState from)
    {
        Console.WriteLine("进入暂停状态");

        // 暂停游戏
        var timeSystem = this.GetSystem&lt;TimeSystem&gt;();
        timeSystem.Pause();

        // 显示暂停菜单
        var uiSystem = this.GetSystem&lt;UISystem&gt;();
        uiSystem.ShowPauseMenu();
    }

    public override void OnExit(IState to)
    {
        Console.WriteLine("退出暂停状态");

        // 恢复游戏
        var timeSystem = this.GetSystem&lt;TimeSystem&gt;();
        timeSystem.Resume();

        // 隐藏暂停菜单
        var uiSystem = this.GetSystem&lt;UISystem&gt;();
        uiSystem.HidePauseMenu();
    }

    public override bool CanTransitionTo(IState target)
    {
        // 暂停只能返回游戏或退出到菜单
        return target is GameplayState or MenuState;
    }
}

// 注册状态机
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 创建状态机系统
        var stateMachine = new StateMachineSystem();

        // 注册所有状态
        stateMachine
            .Register(new MenuState())
            .Register(new GameplayState())
            .Register(new PauseState())
            .Register(new GameOverState())
            .Register(new SettingsState());

        RegisterSystem&lt;IStateMachineSystem&gt;(stateMachine);
    }
}

// 使用状态机
[ContextAware]
public partial class GameController : IController
{
    public async Task StartGame()
    {
        var stateMachine = this.GetSystem<IStateMachineSystem>();
        await stateMachine.ChangeToAsync<GameplayState>();
    }

    public async Task PauseGame()
    {
        var stateMachine = this.GetSystem<IStateMachineSystem>();
        await stateMachine.ChangeToAsync<PauseState>();
    }

    public async Task ResumeGame()
    {
        var stateMachine = this.GetSystem<IStateMachineSystem>();
        await stateMachine.ChangeToAsync<GameplayState>();
    }
}
```

### 异步状态

```csharp
public class LoadingState : AsyncContextAwareStateBase
{
    public override async Task OnEnterAsync(IState from)
    {
        Console.WriteLine("开始加载...");

        // 显示加载界面
        var uiSystem = this.GetSystem&lt;UISystem&gt;();
        uiSystem.ShowLoadingScreen();

        // 异步加载资源
        await LoadResourcesAsync();

        Console.WriteLine("加载完成");

        // 自动切换到游戏状态
        var stateMachine = this.GetSystem&lt;IStateMachineSystem&gt;();
        await stateMachine.ChangeToAsync&lt;GameplayState&gt;();
    }

    private async Task LoadResourcesAsync()
    {
        // 模拟异步加载
        await Task.Delay(2000);
    }

    public override async Task OnExitAsync(IState to)
    {
        // 隐藏加载界面
        var uiSystem = this.GetSystem&lt;UISystem&gt;();
        uiSystem.HideLoadingScreen();

        await Task.CompletedTask;
    }
}
```

### 状态模式优势

- ✅ **清晰的状态管理**：每个状态独立封装
- ✅ **易于扩展**：添加新状态不影响现有代码
- ✅ **状态转换验证**：CanTransitionTo 控制合法转换
- ✅ **支持异步操作**：异步状态处理加载等操作

### 最佳实践

1. **状态保持单一职责**：每个状态只负责一个场景
2. **使用转换验证**：防止非法状态转换
3. **在 OnEnter 初始化，OnExit 清理**：保持状态独立
4. **异步操作使用异步状态**：避免阻塞主线程

## 设计原则

### 1. 单一职责原则 (SRP)

确保每个类只负责一个功能领域：

```csharp
// ✅ 好的做法：职责单一
public class PlayerMovementController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerInputEvent>(OnPlayerInput);
    }
    
    private void OnPlayerInput(PlayerInputEvent e)
    {
        // 只负责移动逻辑
        ProcessMovement(e.Direction);
    }
    
    private void ProcessMovement(Vector2 direction)
    {
        // 移动相关的业务逻辑
    }
}

public class PlayerCombatController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<AttackInputEvent>(OnAttackInput);
    }
    
    private void OnAttackInput(AttackInputEvent e)
    {
        // 只负责战斗逻辑
        ProcessAttack(e.Target);
    }
    
    private void ProcessAttack(Entity target)
    {
        // 战斗相关的业务逻辑
    }
}

// ❌ 避免：职责混乱
public class PlayerController : AbstractSystem
{
    private void OnPlayerInput(PlayerInputEvent e)
    {
        // 移动逻辑
        ProcessMovement(e.Direction);
        
        // 战斗逻辑
        if (e.IsAttacking)
        {
            ProcessAttack(e.Target);
        }
        
        // UI逻辑
        UpdateHealthBar();
        
        // 音效逻辑
        PlaySoundEffect();
        
        // 存档逻辑
        SaveGame();
        
        // 职责太多，难以维护
    }
}
```

### 2. 开闭原则 (OCP)

设计应该对扩展开放，对修改封闭：

```csharp
// ✅ 好的做法：使用接口和策略模式
public interface IWeaponStrategy
{
    void Attack(Entity attacker, Entity target);
    int CalculateDamage(Entity attacker, Entity target);
}

public class SwordWeaponStrategy : IWeaponStrategy
{
    public void Attack(Entity attacker, Entity target)
    {
        var damage = CalculateDamage(attacker, target);
        target.TakeDamage(damage);
        PlaySwingAnimation();
    }
    
    public int CalculateDamage(Entity attacker, Entity target)
    {
        return attacker.Strength + GetSwordBonus() - target.Armor;
    }
}

public class MagicWeaponStrategy : IWeaponStrategy
{
    public void Attack(Entity attacker, Entity target)
    {
        var damage = CalculateDamage(attacker, target);
        target.TakeDamage(damage);
        CastMagicEffect();
    }
    
    public int CalculateDamage(Entity attacker, Entity target)
    {
        return attacker.Intelligence * 2 + GetMagicBonus() - target.MagicResistance;
    }
}

public class CombatSystem : AbstractSystem
{
    private readonly Dictionary<WeaponType, IWeaponStrategy> _weaponStrategies;
    
    public CombatSystem()
    {
        _weaponStrategies = new()
        {
            { WeaponType.Sword, new SwordWeaponStrategy() },
            { WeaponType.Magic, new MagicWeaponStrategy() }
        };
    }
    
    public void Attack(Entity attacker, Entity target)
    {
        var weaponType = attacker.EquippedWeapon.Type;
        
        if (_weaponStrategies.TryGetValue(weaponType, out var strategy))
        {
            strategy.Attack(attacker, target);
        }
    }
    
    // 添加新武器类型时，只需要添加新的策略，不需要修改现有代码
    public void RegisterWeaponStrategy(WeaponType type, IWeaponStrategy strategy)
    {
        _weaponStrategies[type] = strategy;
    }
}

// ❌ 避免：需要修改现有代码来扩展
public class CombatSystem : AbstractSystem
{
    public void Attack(Entity attacker, Entity target)
    {
        var weaponType = attacker.EquippedWeapon.Type;
        
        switch (weaponType)
        {
            case WeaponType.Sword:
                // 剑的攻击逻辑
                break;
            case WeaponType.Bow:
                // 弓的攻击逻辑
                break;
            default:
                throw new NotSupportedException($"Weapon type {weaponType} not supported");
        }
        
        // 添加新武器类型时需要修改这里的 switch 语句
    }
}
```

### 3. 依赖倒置原则 (DIP)

高层模块不应该依赖低层模块，两者都应该依赖抽象：

```csharp
// ✅ 好的做法：依赖抽象
public interface IDataStorage
{
    Task SaveAsync&lt;T&gt;(string key, T data);
    Task&lt;T&gt; LoadAsync&lt;T&gt;(string key, T defaultValue = default);
    Task<bool> ExistsAsync(string key);
}

public class FileStorage : IDataStorage
{
    public async Task SaveAsync&lt;T&gt;(string key, T data)
    {
        var json = JsonConvert.SerializeObject(data);
        await File.WriteAllTextAsync(GetFilePath(key), json);
    }
    
    public async Task&lt;T&gt; LoadAsync&lt;T&gt;(string key, T defaultValue = default)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
            return defaultValue;
            
        var json = await File.ReadAllTextAsync(filePath);
        return JsonConvert.DeserializeObject&lt;T&gt;(json);
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        return File.Exists(GetFilePath(key));
    }
    
    private string GetFilePath(string key)
    {
        return $"saves/{key}.json";
    }
}

public class CloudStorage : IDataStorage
{
    public async Task SaveAsync&lt;T&gt;(string key, T data)
    {
        // 云存储实现
        await UploadToCloud(key, data);
    }
    
    public async Task&lt;T&gt; LoadAsync&lt;T&gt;(string key, T defaultValue = default)
    {
        // 云存储实现
        return await DownloadFromCloud&lt;T&gt;(key, defaultValue);
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        // 云存储实现
        return await CheckCloudExists(key);
    }
}

// 高层模块依赖抽象
public class SaveSystem : AbstractSystem
{
    private readonly IDataStorage _storage;
    
    public SaveSystem(IDataStorage storage)
    {
        _storage = storage;
    }
    
    public async Task SaveGameAsync(SaveData data)
    {
        await _storage.SaveAsync("current_save", data);
    }
    
    public async Task<SaveData> LoadGameAsync()
    {
        return await _storage.LoadAsync<SaveData>("current_save");
    }
}

// ❌ 避免：依赖具体实现
public class SaveSystem : AbstractSystem
{
    private readonly FileStorage _storage; // 直接依赖具体实现
    
    public SaveSystem()
    {
        _storage = new FileStorage(); // 硬编码依赖
    }
    
    // 无法轻松切换到其他存储方式
}
```

## 架构分层

### 1. 清晰的层次结构

```csharp
// ✅ 好的做法：清晰的分层架构
namespace Game.Models
{
    // 数据层：只负责存储状态
    public class PlayerModel : AbstractModel
    {
        public BindableProperty<int> Health { get; } = new(100);
        public BindableProperty<int> MaxHealth { get; } = new(100);
        public BindableProperty<Vector2> Position { get; } = new(Vector2.Zero);
        public BindableProperty<PlayerState> State { get; } = new(PlayerState.Idle);
        
        protected override void OnInit()
        {
            // 只处理数据相关的逻辑
            Health.Register(OnHealthChanged);
        }
        
        private void OnHealthChanged(int newHealth)
        {
            if (newHealth <= 0)
            {
                State.Value = PlayerState.Dead;
                SendEvent(new PlayerDeathEvent());
            }
        }
    }
    
    public enum PlayerState
    {
        Idle,
        Moving,
        Attacking,
        Dead
    }
}

namespace Game.Systems
{
    // 业务逻辑层：处理游戏逻辑
    public class PlayerMovementSystem : AbstractSystem
    {
        private PlayerModel _playerModel;
        private GameModel _gameModel;
        
        protected override void OnInit()
        {
            _playerModel = GetModel<PlayerModel>();
            _gameModel = GetModel<GameModel>();
            
            this.RegisterEvent<PlayerInputEvent>(OnPlayerInput);
        }
        
        private void OnPlayerInput(PlayerInputEvent e)
        {
            if (_gameModel.State.Value != GameState.Playing)
                return;
                
            if (_playerModel.State.Value == PlayerState.Dead)
                return;
                
            // 处理移动逻辑
            ProcessMovement(e.Direction);
        }
        
        private void ProcessMovement(Vector2 direction)
        {
            if (direction != Vector2.Zero)
            {
                _playerModel.Position.Value += direction.Normalized() * GetMovementSpeed();
                _playerModel.State.Value = PlayerState.Moving;
                
                SendEvent(new PlayerMovedEvent { 
                    NewPosition = _playerModel.Position.Value,
                    Direction = direction
                });
            }
            else
            {
                _playerModel.State.Value = PlayerState.Idle;
            }
        }
        
        private float GetMovementSpeed()
        {
            // 从玩家属性或其他地方获取速度
            return 5.0f;
        }
    }
}

namespace Game.Controllers
{
    // 控制层：连接用户输入和业务逻辑
    [ContextAware]
    public partial class PlayerController : Node, IController
    {
        private PlayerModel _playerModel;
        
        public override void _Ready()
        {
            _playerModel = this.GetModel<PlayerModel>();
            
            // 监听用户输入
            SetProcessInput(true);
            
            // 监听数据变化，更新UI
            _playerModel.Health.Register(UpdateHealthUI);
            _playerModel.Position.Register(UpdatePosition);
        }
        
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                var direction = GetInputDirection(keyEvent);
                this.SendEvent(new PlayerInputEvent { Direction = direction });
            }
        }
        
        private void UpdateHealthUI(int health)
        {
            // 更新UI显示
            var healthBar = GetNode<ProgressBar>("UI/HealthBar");
            healthBar.Value = (float)health / _playerModel.MaxHealth.Value * 100;
        }
        
        private void UpdatePosition(Vector2 position)
        {
            // 更新玩家位置
            Position = position;
        }
        
        private Vector2 GetInputDirection(InputEventKey keyEvent)
        {
            return keyEvent.Keycode switch
            {
                Key.W => Vector2.Up,
                Key.S => Vector2.Down,
                Key.A => Vector2.Left,
                Key.D => Vector2.Right,
                _ => Vector2.Zero
            };
        }
    }
}
```

### 2. 避免层次混乱

```csharp
// ❌ 避免：层次混乱
public class PlayerController : Node, IController
{
    // 混合了数据层、业务逻辑层和控制层的职责
    public BindableProperty<int> Health { get; } = new(100); // 数据层职责
    
    public override void _Input(InputEvent @event) // 控制层职责
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.W)
            {
                Position += Vector2.Up * MovementSpeed; // 业务逻辑层职责
            }
            
            if (keyEvent.Keycode == Key.Space)
            {
                Health -= 10; // 业务逻辑层职责
                PlaySoundEffect(); // 业务逻辑层职责
            }
        }
    }
    
    // 这样会导致代码难以测试和维护
}
```

## 依赖管理

### 1. 构造函数注入

```csharp
// ✅ 好的做法：构造函数注入
public class PlayerCombatSystem : AbstractSystem
{
    private readonly PlayerModel _playerModel;
    private readonly IWeaponService _weaponService;
    private readonly ISoundService _soundService;
    private readonly IEffectService _effectService;
    
    // 通过构造函数注入依赖
    public PlayerCombatSystem(
        PlayerModel playerModel,
        IWeaponService weaponService,
        ISoundService soundService,
        IEffectService effectService)
    {
        _playerModel = playerModel;
        _weaponService = weaponService;
        _soundService = soundService;
        _effectService = effectService;
    }
    
    protected override void OnInit()
    {
        this.RegisterEvent<AttackEvent>(OnAttack);
    }
    
    private void OnAttack(AttackEvent e)
    {
        var weapon = _weaponService.GetEquippedWeapon(_playerModel);
        var damage = _weaponService.CalculateDamage(weapon, e.Target);
        
        e.Target.TakeDamage(damage);
        _soundService.PlayAttackSound(weapon.Type);
        _effectService.PlayAttackEffect(_playerModel.Position, weapon.Type);
    }
}

// ❌ 避免：依赖注入容器
public class PlayerCombatSystem : AbstractSystem
{
    private PlayerModel _playerModel;
    private IWeaponService _weaponService;
    private ISoundService _soundService;
    private IEffectService _effectService;
    
    protected override void OnInit()
    {
        // 在运行时获取依赖，难以测试
        _playerModel = GetModel<PlayerModel>();
        _weaponService = GetService<IWeaponService>();
        _soundService = GetService<ISoundService>();
        _effectService = GetService<IEffectService>();
    }
    
    // 测试时难以模拟依赖
}
```

### 2. 接口隔离

```csharp
// ✅ 好的做法：小而专注的接口
public interface IMovementController
{
    void Move(Vector2 direction);
    void Stop();
    bool CanMove();
}

public interface ICombatController
{
    void Attack(Entity target);
    void Defend();
    bool CanAttack();
}

public interface IUIController
{
    void ShowHealthBar();
    void HideHealthBar();
    void UpdateHealthDisplay(int currentHealth, int maxHealth);
}

public class PlayerController : Node, IMovementController, ICombatController, IUIController
{
    // 实现各个接口，职责清晰
}

// ❌ 避免：大而全的接口
public interface IPlayerController
{
    void Move(Vector2 direction);
    void Stop();
    void Attack(Entity target);
    void Defend();
    void ShowHealthBar();
    void HideHealthBar();
    void UpdateHealthDisplay(int currentHealth, int maxHealth);
    void SaveGame();
    void LoadGame();
    void Respawn();
    void PlayAnimation(string animationName);
    void StopAnimation();
    // ... 更多方法，接口过于庞大
}
```

## 事件系统设计

### 1. 事件命名和结构

```csharp
// ✅ 好的做法：清晰的事件命名和结构
public struct PlayerHealthChangedEvent
{
    public int PreviousHealth { get; }
    public int NewHealth { get; }
    public int MaxHealth { get; }
    public Vector3 DamagePosition { get; }
    public DamageType DamageType { get; }
}

public struct PlayerDiedEvent
{
    public Vector3 DeathPosition { get; }
    public string CauseOfDeath { get; }
    public TimeSpan SurvivalTime { get; }
}

public struct WeaponEquippedEvent
{
    public string PlayerId { get; }
    public WeaponType WeaponType { get; }
    public string WeaponId { get; }
}

// ❌ 避免：模糊的事件命名和结构
public struct PlayerEvent
{
    public EventType Type { get; }
    public object Data { get; } // 类型不安全
    public Dictionary<string, object> Properties { get; } // 难以理解
}
```

### 2. 事件处理职责

```csharp
// ✅ 好的做法：单一职责的事件处理
public class UIHealthBarController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        UpdateHealthBar(e.NewHealth, e.MaxHealth);
        
        if (e.NewHealth < e.PreviousHealth)
        {
            ShowDamageEffect(e.DamagePosition, e.PreviousHealth - e.NewHealth);
        }
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        HideHealthBar();
        ShowDeathScreen(e.CauseOfDeath);
    }
}

public class AudioController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        if (e.NewHealth < e.PreviousHealth)
        {
            PlayHurtSound(e.DamageType);
        }
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        PlayDeathSound();
    }
}

// ❌ 避免：一个处理器处理多种不相关的事件
public class PlayerEventHandler : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
        this.RegisterEvent<WeaponEquippedEvent>(OnWeaponEquipped);
        this.RegisterEvent<LevelUpEvent>(OnLevelUp);
        // 注册太多事件，职责混乱
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        UpdateUI();          // UI职责
        PlayAudio();         // 音频职责
        SaveStatistics();     // 存档职责
        UpdateAchievements(); // 成就系统职责
        // 一个事件处理器承担太多职责
    }
}
```

## 模块化架构

### 1. 模块边界清晰

```csharp
// ✅ 好的做法：清晰的模块边界
public class AudioModule : AbstractModule
{
    // 模块只负责音频相关的功能
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new AudioSystem());
        architecture.RegisterSystem(new MusicSystem());
        architecture.RegisterUtility(new AudioUtility());
    }
}

public class InputModule : AbstractModule
{
    // 模块只负责输入相关的功能
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new InputSystem());
        architecture.RegisterSystem(new InputMappingSystem());
        architecture.RegisterUtility(new InputUtility());
    }
}

public class UIModule : AbstractModule
{
    // 模块只负责UI相关的功能
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new UISystem());
        architecture.RegisterSystem(new HUDSystem());
        architecture.RegisterSystem(new MenuSystem());
        architecture.RegisterUtility(new UIUtility());
    }
}

// ❌ 避免：模块职责混乱
public class GameModule : AbstractModule
{
    public override void Install(IArchitecture architecture)
    {
        // 一个模块包含所有功能
        architecture.RegisterSystem(new AudioSystem());        // 音频
        architecture.RegisterSystem(new InputSystem());         // 输入
        architecture.RegisterSystem(new UISystem());            // UI
        architecture.RegisterSystem(new CombatSystem());        // 战斗
        architecture.RegisterSystem(new InventorySystem());     // 背包
        architecture.RegisterSystem(new QuestSystem());          // 任务
        // 模块过于庞大，难以维护
    }
}
```

### 2. 模块间通信

```csharp
// ✅ 好的做法：通过事件进行模块间通信
public class AudioModule : AbstractModule
{
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new AudioSystem());
    }
}

public class AudioSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 监听其他模块发送的事件
        this.RegisterEvent<PlayerAttackEvent>(OnPlayerAttack);
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
        this.RegisterEvent<WeaponEquippedEvent>(OnWeaponEquipped);
    }
    
    private void OnPlayerAttack(PlayerAttackEvent e)
    {
        PlayAttackSound(e.WeaponType);
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        PlayDeathSound();
    }
    
    private void OnWeaponEquipped(WeaponEquippedEvent e)
    {
        PlayEquipSound(e.WeaponType);
    }
}

public class CombatModule : AbstractModule
{
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new CombatSystem());
    }
}

public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<AttackInputEvent>(OnAttackInput);
    }
    
    private void OnAttackInput(AttackInputEvent e)
    {
        ProcessAttack(e);
        
        // 发送事件通知其他模块
        SendEvent(new PlayerAttackEvent { 
            PlayerId = e.PlayerId, 
            WeaponType = GetPlayerWeaponType(e.PlayerId) 
        });
    }
}

// ❌ 避免：模块间直接依赖
public class CombatSystem : AbstractSystem
{
    private AudioSystem _audioSystem; // 直接依赖其他模块
    
    protected override void OnInit()
    {
        // 直接获取其他模块的系统
        _audioSystem = GetSystem<AudioSystem>();
    }
    
    private void OnAttackInput(AttackInputEvent e)
    {
        ProcessAttack(e);
        
        // 直接调用其他模块的方法
        _audioSystem.PlayAttackSound(weaponType);
    }
}
```

## 错误处理策略

### 1. 异常处理层次

```csharp
// ✅ 好的做法：分层异常处理
public class GameApplicationException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Context { get; }
    
    public GameApplicationException(string message, string errorCode, 
        Dictionary<string, object> context = null, Exception innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Context = context ?? new Dictionary<string, object>();
    }
}

public class PlayerException : GameApplicationException
{
    public PlayerException(string message, string errorCode, 
        Dictionary<string, object> context = null, Exception innerException = null)
        : base(message, errorCode, context, innerException)
    {
    }
}

public class InventoryException : GameApplicationException
{
    public InventoryException(string message, string errorCode, 
        Dictionary<string, object> context = null, Exception innerException = null)
        : base(message, errorCode, context, innerException)
    {
    }
}

// 在系统中的使用
public class PlayerInventorySystem : AbstractSystem
{
    public void AddItem(string playerId, Item item)
    {
        try
        {
            ValidateItem(item);
            CheckInventorySpace(playerId, item);
            
            AddItemToInventory(playerId, item);
            
            SendEvent(new ItemAddedEvent { PlayerId = playerId, Item = item });
        }
        catch (ItemValidationException ex)
        {
            throw new InventoryException(
                $"Failed to add item {item.Id} to player {playerId}",
                "ITEM_VALIDATION_FAILED",
                new Dictionary<string, object>
                {
                    ["playerId"] = playerId,
                    ["itemId"] = item.Id,
                    ["validationError"] = ex.Message
                },
                ex
            );
        }
        catch (InventoryFullException ex)
        {
            throw new InventoryException(
                $"Player {playerId} inventory is full",
                "INVENTORY_FULL",
                new Dictionary<string, object>
                {
                    ["playerId"] = playerId,
                    ["itemId"] = item.Id,
                    ["maxSlots"] = ex.MaxSlots,
                    ["currentSlots"] = ex.CurrentSlots
                },
                ex
            );
        }
        catch (Exception ex)
        {
            // 捕获未知异常并包装
            throw new InventoryException(
                $"Unexpected error adding item {item.Id} to player {playerId}",
                "UNKNOWN_ERROR",
                new Dictionary<string, object>
                {
                    ["playerId"] = playerId,
                    ["itemId"] = item.Id,
                    ["originalError"] = ex.Message
                },
                ex
            );
        }
    }
    
    private void ValidateItem(Item item)
    {
        if (item == null)
            throw new ItemValidationException("Item cannot be null");
            
        if (string.IsNullOrEmpty(item.Id))
            throw new ItemValidationException("Item ID cannot be empty");
            
        if (item.StackSize <= 0)
            throw new ItemValidationException("Item stack size must be positive");
    }
    
    private void CheckInventorySpace(string playerId, Item item)
    {
        var inventory = GetPlayerInventory(playerId);
        var requiredSpace = CalculateRequiredSpace(item);
        
        if (inventory.FreeSpace < requiredSpace)
        {
            throw new InventoryFullException(
                inventory.FreeSpace,
                inventory.MaxSlots
            );
        }
    }
}
```

### 2. 错误恢复策略

```csharp
// ✅ 好的做法：优雅的错误恢复
public class SaveSystem : AbstractSystem
{
    private readonly IStorage _primaryStorage;
    private readonly IStorage _backupStorage;
    
    public SaveSystem(IStorage primaryStorage, IStorage backupStorage = null)
    {
        _primaryStorage = primaryStorage;
        _backupStorage = backupStorage ?? new LocalStorage("backup");
    }
    
    public async Task<SaveData> LoadSaveDataAsync(string saveId)
    {
        try
        {
            // 尝试从主存储加载
            return await _primaryStorage.ReadAsync<SaveData>(saveId);
        }
        catch (StorageException ex)
        {
            Logger.Warning($"Failed to load from primary storage: {ex.Message}");
            
            try
            {
                // 尝试从备份存储加载
                var backupData = await _backupStorage.ReadAsync<SaveData>(saveId);
                Logger.Info($"Successfully loaded from backup storage: {saveId}");
                
                // 恢复到主存储
                await _primaryStorage.WriteAsync(saveId, backupData);
                
                return backupData;
            }
            catch (Exception backupEx)
            {
                Logger.Error($"Failed to load from backup storage: {backupEx.Message}");
                
                // 返回默认存档数据
                return GetDefaultSaveData();
            }
        }
    }
    
    private SaveData GetDefaultSaveData()
    {
        Logger.Warning("Returning default save data due to loading failures");
        return new SaveData
        {
            PlayerId = "default",
            Level = 1,
            Health = 100,
            Position = Vector3.Zero,
            CreatedAt = DateTime.UtcNow
        };
    }
}

// ❌ 避免：粗暴的错误处理
public class SaveSystem : AbstractSystem
{
    public async Task<SaveData> LoadSaveDataAsync(string saveId)
    {
        try
        {
            return await _storage.ReadAsync<SaveData>(saveId);
        }
        catch (Exception ex)
        {
            // 直接抛出异常，不提供恢复机制
            throw new Exception($"Failed to load save: {ex.Message}", ex);
        }
    }
}
```

## 测试策略

### 1. 可测试的架构设计

```csharp
// ✅ 好的做法：可测试的架构
public interface IPlayerMovementService
{
    void MovePlayer(string playerId, Vector2 direction);
    bool CanPlayerMove(string playerId);
}

public class PlayerMovementService : IPlayerMovementService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly ICollisionService _collisionService;
    private readonly IMapService _mapService;
    
    public PlayerMovementService(
        IPlayerRepository playerRepository,
        ICollisionService collisionService,
        IMapService mapService)
    {
        _playerRepository = playerRepository;
        _collisionService = collisionService;
        _mapService = mapService;
    }
    
    public void MovePlayer(string playerId, Vector2 direction)
    {
        if (!CanPlayerMove(playerId))
            return;
            
        var player = _playerRepository.GetById(playerId);
        var newPosition = player.Position + direction * player.Speed;
        
        if (_collisionService.CanMoveTo(newPosition))
        {
            player.Position = newPosition;
            _playerRepository.Update(player);
        }
    }
    
    public bool CanPlayerMove(string playerId)
    {
        var player = _playerRepository.GetById(playerId);
        return player != null && player.IsAlive && !player.IsStunned;
    }
}

// 测试代码
[TestFixture]
public class PlayerMovementServiceTests
{
    private Mock<IPlayerRepository> _mockPlayerRepository;
    private Mock<ICollisionService> _mockCollisionService;
    private Mock<IMapService> _mockMapService;
    private PlayerMovementService _movementService;
    
    [SetUp]
    public void Setup()
    {
        _mockPlayerRepository = new Mock<IPlayerRepository>();
        _mockCollisionService = new Mock<ICollisionService>();
        _mockMapService = new Mock<IMapService>();
        
        _movementService = new PlayerMovementService(
            _mockPlayerRepository.Object,
            _mockCollisionService.Object,
            _mockMapService.Object
        );
    }
    
    [Test]
    public void MovePlayer_ValidMovement_ShouldUpdatePlayerPosition()
    {
        // Arrange
        var playerId = "player1";
        var player = new Player { Id = playerId, Position = Vector2.Zero, Speed = 5.0f };
        var direction = Vector2.Right;
        
        _mockPlayerRepository.Setup(r => r.GetById(playerId)).Returns(player);
        _mockCollisionService.Setup(c => c.CanMoveTo(It.IsAny<Vector2>())).Returns(true);
        
        // Act
        _movementService.MovePlayer(playerId, direction);
        
        // Assert
        _mockPlayerRepository.Verify(r => r.Update(It.Is<Player>(p => p.Position == Vector2.Right * 5.0f)), Times.Once);
    }
    
    [Test]
    public void MovePlayer_CollisionBlocked_ShouldNotUpdatePlayerPosition()
    {
        // Arrange
        var playerId = "player1";
        var player = new Player { Id = playerId, Position = Vector2.Zero, Speed = 5.0f };
        var direction = Vector2.Right;
        
        _mockPlayerRepository.Setup(r => r.GetById(playerId)).Returns(player);
        _mockCollisionService.Setup(c => c.CanMoveTo(It.IsAny<Vector2>())).Returns(false);
        
        // Act
        _movementService.MovePlayer(playerId, direction);
        
        // Assert
        _mockPlayerRepository.Verify(r => r.Update(It.IsAny<Player>()), Times.Never);
    }
}

// ❌ 避免：难以测试的设计
public class PlayerMovementSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<MovementInputEvent>(OnMovementInput);
    }
    
    private void OnMovementInput(MovementInputEvent e)
    {
        var player = GetModel<PlayerModel>(); // 依赖架构，难以测试
        var newPosition = player.Position + e.Direction * player.Speed;
        
        if (CanMoveTo(newPosition)) // 私有方法，难以直接测试
        {
            player.Position = newPosition;
        }
    }
    
    private bool CanMoveTo(Vector2 position)
    {
        // 复杂的碰撞检测逻辑，难以测试
        return true;
    }
}
```

## 重构指南

### 1. 识别代码异味

```csharp
// ❌ 代码异味：长方法、重复代码、上帝类
public class GameManager : Node
{
    public void ProcessPlayerInput(InputEvent @event)
    {
        // 长方法 - 做太多事情
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.W:
                    MovePlayer(Vector2.Up);
                    PlayFootstepSound();
                    UpdatePlayerAnimation("walk_up");
                    CheckPlayerCollisions();
                    UpdateCameraPosition();
                    SavePlayerPosition();
                    break;
                case Key.S:
                    MovePlayer(Vector2.Down);
                    PlayFootstepSound();
                    UpdatePlayerAnimation("walk_down");
                    CheckPlayerCollisions();
                    UpdateCameraPosition();
                    SavePlayerPosition();
                    break;
                // 重复代码
            }
        }
    }
    
    private void MovePlayer(Vector2 direction)
    {
        Player.Position += direction * Player.Speed;
    }
    
    private void PlayFootstepSound()
    {
        AudioPlayer.Play("footstep.wav");
    }
    
    // ... 更多方法，类过于庞大
}

// ✅ 重构后：职责分离
public class PlayerInputController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<InputEvent>(OnInput);
    }
    
    private void OnInput(InputEvent e)
    {
        if (e is InputEventKey keyEvent && keyEvent.Pressed)
        {
            var direction = GetDirectionFromKey(keyEvent.Keycode);
            if (direction != Vector2.Zero)
            {
                SendEvent(new PlayerMoveEvent { Direction = direction });
            }
        }
    }
    
    private Vector2 GetDirectionFromKey(Key keycode)
    {
        return keycode switch
        {
            Key.W => Vector2.Up,
            Key.S => Vector2.Down,
            Key.A => Vector2.Left,
            Key.D => Vector2.Right,
            _ => Vector2.Zero
        };
    }
}

public class PlayerMovementSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerMoveEvent>(OnPlayerMove);
    }
    
    private void OnPlayerMove(PlayerMoveEvent e)
    {
        var playerModel = GetModel<PlayerModel>();
        var newPosition = playerModel.Position + e.Direction * playerModel.Speed;
        
        if (CanMoveTo(newPosition))
        {
            playerModel.Position = newPosition;
            SendEvent(new PlayerMovedEvent { NewPosition = newPosition });
        }
    }
    
    private bool CanMoveTo(Vector2 position)
    {
        var collisionService = GetUtility<ICollisionService>();
        return collisionService.CanMoveTo(position);
    }
}

public class AudioSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerMovedEvent>(OnPlayerMoved);
    }
    
    private void OnPlayerMoved(PlayerMovedEvent e)
    {
        PlayFootstepSound();
    }
    
    private void PlayFootstepSound()
    {
        var audioUtility = GetUtility<AudioUtility>();
        audioUtility.PlaySound("footstep.wav");
    }
}

public class AnimationSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerMovedEvent>(OnPlayerMoved);
    }
    
    private void OnPlayerMoved(PlayerMovedEvent e)
    {
        var animationName = GetAnimationNameFromDirection(e.Direction);
        SendEvent(new PlayAnimationEvent { AnimationName = animationName });
    }
    
    private string GetAnimationNameFromDirection(Vector2 direction)
    {
        if (direction == Vector2.Up) return "walk_up";
        if (direction == Vector2.Down) return "walk_down";
        if (direction == Vector2.Left) return "walk_left";
        if (direction == Vector2.Right) return "walk_right";
        return "idle";
    }
}
```

### 2. 渐进式重构

```csharp
// 第一步：提取重复代码
public class PlayerController : Node
{
    public void ProcessInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            Vector2 direction;
            switch (keyEvent.Keycode)
            {
                case Key.W:
                    direction = Vector2.Up;
                    break;
                case Key.S:
                    direction = Vector2.Down;
                    break;
                case Key.A:
                    direction = Vector2.Left;
                    break;
                case Key.D:
                    direction = Vector2.Right;
                    break;
                default:
                    return;
            }
            
            MovePlayer(direction);
        }
    }
}

// 第二步：提取方法
public class PlayerController : Node
{
    public void ProcessInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            var direction = GetDirectionFromKey(keyEvent.Keycode);
            if (direction != Vector2.Zero)
            {
                MovePlayer(direction);
            }
        }
    }
    
    private Vector2 GetDirectionFromKey(Key keycode)
    {
        return keycode switch
        {
            Key.W => Vector2.Up,
            Key.S => Vector2.Down,
            Key.A => Vector2.Left,
            Key.D => Vector2.Right,
            _ => Vector2.Zero
        };
    }
}

// 第三步：引入系统和事件
public class PlayerController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<InputEvent>(OnInput);
    }
    
    private void OnInput(InputEvent e)
    {
        if (e is InputEventKey keyEvent && keyEvent.Pressed)
        {
            var direction = GetDirectionFromKey(keyEvent.Keycode);
            if (direction != Vector2.Zero)
            {
                SendEvent(new PlayerMoveEvent { Direction = direction });
            }
        }
    }
    
    private Vector2 GetDirectionFromKey(Key keycode)
    {
        return keycode switch
        {
            Key.W => Vector2.Up,
            Key.S => Vector2.Down,
            Key.A => Vector2.Left,
            Key.D => Vector2.Right,
            _ => Vector2.Zero
        };
    }
}
```

---

## 模式选择与组合

### 何时使用哪种模式？

#### 小型项目（原型、Demo）

```csharp
// 推荐组合：MVC + 事件驱动 + 服务定位器
public class SimpleGameArchitecture : Architecture
{
    protected override void Init()
    {
        // Model
        RegisterModel(new PlayerModel());
        RegisterModel(new GameModel());

        // System
        RegisterSystem(new GameplaySystem());
        RegisterSystem(new AudioSystem());

        // 使用服务定位器模式
        // Controller 通过 this.GetModel/GetSystem 获取服务
    }
}
```

**优势**：

- 快速开发
- 代码简洁
- 易于理解

#### 中型项目（独立游戏）

```csharp
// 推荐组合：MVC + MVVM + 命令/查询 + 事件驱动 + 依赖注入
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册 Utility（依赖注入）
        RegisterUtility&lt;IStorageService&gt;(new LocalStorageService());
        RegisterUtility&lt;IAudioService&gt;(new GodotAudioService());

        // 注册 Model（MVVM）
        RegisterModel(new PlayerModel());
        RegisterModel(new PlayerViewModel());

        // 注册 System（命令/查询处理）
        RegisterSystem(new CombatSystem());
        RegisterSystem(new InventorySystem());

        // 状态机
        var stateMachine = new StateMachineSystem();
        stateMachine
            .Register(new MenuState())
            .Register(new GameplayState());
        RegisterSystem&lt;IStateMachineSystem&gt;(stateMachine);
    }
}
```

**优势**：

- 职责清晰
- 易于测试
- 支持团队协作

#### 大型项目（商业游戏）

```csharp
// 推荐组合：所有模式 + 模块化架构
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 模块化安装
        InstallModule(new CoreModule());
        InstallModule(new AudioModule());
        InstallModule(new NetworkModule());
        InstallModule(new UIModule());
        InstallModule(new GameplayModule());
    }
}

// 核心模块
public class CoreModule : IArchitectureModule
{
    public void Install(IArchitecture architecture)
    {
        // 依赖注入
        architecture.RegisterUtility&lt;IStorageService&gt;(new CloudStorageService());
        architecture.RegisterUtility&lt;IAnalyticsService&gt;(new AnalyticsService());

        // 对象池
        architecture.RegisterSystem(new BulletPoolSystem());
        architecture.RegisterSystem(new ParticlePoolSystem());
    }
}

// 游戏玩法模块
public class GameplayModule : IArchitectureModule
{
    public void Install(IArchitecture architecture)
    {
        // Model
        architecture.RegisterModel(new PlayerModel());
        architecture.RegisterModel(new EnemyModel());

        // System（使用命令/查询模式）
        architecture.RegisterSystem(new CombatSystem());
        architecture.RegisterSystem(new MovementSystem());

        // 状态机
        var stateMachine = new StateMachineSystem();
        stateMachine
            .Register(new MenuState())
            .Register(new LoadingState())
            .Register(new GameplayState())
            .Register(new PauseState())
            .Register(new GameOverState());
        architecture.RegisterSystem&lt;IStateMachineSystem&gt;(stateMachine);
    }
}
```

**优势**：

- 高度模块化
- 易于维护和扩展
- 支持大型团队
- 完善的测试覆盖

### 模式组合示例

#### 组合 1：MVVM + 命令模式

```csharp
// ViewModel
public class ShopViewModel : AbstractModel
{
    public BindableProperty&lt;int&gt; PlayerGold { get; } = new(1000);
    public BindableProperty&lt;bool&gt; CanBuy { get; } = new(true);
    public BindableProperty&lt;string&gt; StatusMessage { get; } = new("");

    protected override void OnInit()
    {
        // 监听购买事件
        this.RegisterEvent&lt;ItemPurchasedEvent&gt;(OnItemPurchased);
        this.RegisterEvent&lt;InsufficientGoldEvent&gt;(OnInsufficientGold);

        // 监听金币变化
        PlayerGold.Register(gold =>
        {
            CanBuy.Value = gold &gt;= 100;
        });
    }

    private void OnItemPurchased(ItemPurchasedEvent e)
    {
        PlayerGold.Value -= e.Cost;
        StatusMessage.Value = $"购买成功：{e.ItemName}";
    }

    private void OnInsufficientGold(InsufficientGoldEvent e)
    {
        StatusMessage.Value = "金币不足！";
    }
}

// View
public class ShopView : Control
{
    private ShopViewModel _viewModel;

    public override void _Ready()
    {
        _viewModel = GetModel&lt;ShopViewModel&gt;();

        // 数据绑定
        _viewModel.PlayerGold.Register(gold =>
        {
            GetNode&lt;Label&gt;("GoldLabel").Text = $"金币：{gold}";
        });

        _viewModel.CanBuy.Register(canBuy =>
        {
            GetNode&lt;Button&gt;("BuyButton").Disabled = !canBuy;
        });

        _viewModel.StatusMessage.Register(msg =>
        {
            GetNode&lt;Label&gt;("StatusLabel").Text = msg;
        });
    }

    private void OnBuyButtonPressed()
    {
        // 发送命令
        this.SendCommand(new BuyItemCommand
        {
            Input = new BuyItemInput { ItemId = "sword_01" }
        });
    }
}
```

#### 组合 2：状态模式 + 对象池

```csharp
// 游戏状态使用对象池
public class GameplayState : ContextAwareStateBase
{
    private BulletPoolSystem _bulletPool;
    private ParticlePoolSystem _particlePool;

    public override void OnEnter(IState from)
    {
        // 获取对象池
        _bulletPool = this.GetSystem&lt;BulletPoolSystem&gt;();
        _particlePool = this.GetSystem&lt;ParticlePoolSystem&gt;();

        // 预热对象池
        _bulletPool.PrewarmPool(100);
        _particlePool.PrewarmPool(200);

        // 注册事件
        this.RegisterEvent&lt;FireWeaponEvent&gt;(OnFireWeapon);
    }

    private void OnFireWeapon(FireWeaponEvent e)
    {
        // 从池中获取子弹
        var bullet = _bulletPool.Spawn();
        bullet.Position = e.Position;
        bullet.Direction = e.Direction;

        // 生成粒子效果
        var particle = _particlePool.Spawn();
        particle.Position = e.Position;
    }

    public override void OnExit(IState to)
    {
        // 回收所有对象
        _bulletPool.RecycleAll();
        _particlePool.RecycleAll();
    }
}
```

#### 组合 3：事件驱动 + 查询模式

```csharp
// 成就系统：监听事件，使用查询验证条件
public class AchievementSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent&lt;EnemyKilledEvent&gt;(OnEnemyKilled);
        this.RegisterEvent&lt;LevelCompletedEvent&gt;(OnLevelCompleted);
    }

    private void OnEnemyKilled(EnemyKilledEvent e)
    {
        // 查询击杀总数
        var query = new GetTotalKillsQuery { Input = new EmptyQueryInput() };
        var totalKills = this.SendQuery(query);

        // 检查成就
        if (totalKills == 100)
        {
            UnlockAchievement("kill_100_enemies");
        }
    }

    private void OnLevelCompleted(LevelCompletedEvent e)
    {
        // 查询是否满足完美通关条件
        var query = new IsPerfectClearQuery
        {
            Input = new IsPerfectClearInput { LevelId = e.LevelId }
        };
        var isPerfect = this.SendQuery(query);

        if (isPerfect)
        {
            UnlockAchievement($"perfect_clear_level_{e.LevelId}");
        }
    }
}
```

### 模式选择决策树

```
需要管理游戏状态？
├─ 是 → 使用状态模式
└─ 否 → 继续

需要频繁创建/销毁对象？
├─ 是 → 使用对象池模式
└─ 否 → 继续

需要解耦组件通信？
├─ 是 → 使用事件驱动模式
└─ 否 → 继续

需要封装操作？
├─ 是 → 使用命令模式
└─ 否 → 继续

需要分离读写操作？
├─ 是 → 使用查询模式（CQRS）
└─ 否 → 继续

需要数据绑定和响应式 UI？
├─ 是 → 使用 MVVM 模式
└─ 否 → 使用 MVC 模式

需要管理依赖？
├─ 大型项目 → 使用依赖注入
└─ 小型项目 → 使用服务定位器
```

## 常见问题

### Q1: 应该使用 MVC 还是 MVVM？

**A**: 取决于项目需求：

- **使用 MVC**：
    - 简单的 UI 更新
    - 不需要复杂的数据绑定
    - 快速原型开发

- **使用 MVVM**：
    - 复杂的数据驱动 UI
    - 需要自动更新界面
    - 大量计算属性

**推荐**：可以混合使用，简单界面用 MVC，复杂界面用 MVVM。

### Q2: 命令模式和查询模式有什么区别？

**A**:

| 特性      | 命令模式           | 查询模式                |
|---------|----------------|---------------------|
| **目的**  | 修改状态           | 读取数据                |
| **返回值** | 可选             | 必须有                 |
| **副作用** | 有              | 无                   |
| **示例**  | BuyItemCommand | GetPlayerStatsQuery |

**原则**：命令改变状态，查询读取状态，两者不混用。

### Q3: 何时使用事件，何时使用命令？

**A**:

- **使用事件**：
    - 通知状态变化
    - 一对多通信
    - 跨模块通信
    - 不关心处理结果

- **使用命令**：
    - 执行具体操作
    - 需要封装逻辑
    - 需要撤销/重做
    - 需要返回结果

**示例**：

```csharp
// 使用命令执行操作
this.SendCommand(new BuyItemCommand());

// 使用事件通知结果
this.SendEvent(new ItemPurchasedEvent());
```

### Q4: 依赖注入和服务定位器哪个更好？

**A**:

- **依赖注入**：
    - ✅ 依赖关系明确
    - ✅ 易于测试
    - ✅ 编译时检查
    - ❌ 配置复杂

- **服务定位器**：
    - ✅ 简单直接
    - ✅ 易于使用
    - ❌ 依赖隐式
    - ❌ 难以测试

**推荐**：

- 小项目：服务定位器
- 大项目：依赖注入
- 混合使用：核心服务用依赖注入，辅助服务用服务定位器

### Q5: 对象池适合哪些场景？

**A**:

**适合**：

- 频繁创建/销毁的对象（子弹、粒子）
- 创建成本高的对象（网络连接）
- 需要稳定帧率的场景

**不适合**：

- 创建频率低的对象
- 对象状态复杂难以重置
- 内存受限的场景

**示例**：

```csharp
// ✅ 适合使用对象池
- 子弹、导弹
- 粒子效果
- UI 元素（列表项）
- 音效播放器

// ❌ 不适合使用对象池
- 玩家角色
- 关卡数据
- 配置对象
```

### Q6: 状态机和简单的 if-else 有什么区别？

**A**:

**简单 if-else**：

```csharp
// ❌ 难以维护
public void Update()
{
    if (gameState == GameState.Menu)
    {
        UpdateMenu();
    }
    else if (gameState == GameState.Playing)
    {
        UpdateGameplay();
    }
    else if (gameState == GameState.Paused)
    {
        UpdatePause();
    }
    // 状态逻辑分散，难以管理
}
```

**状态机**：

```csharp
// ✅ 清晰易维护
public class MenuState : ContextAwareStateBase
{
    public override void OnEnter(IState from)
    {
        // 进入菜单的所有逻辑集中在这里
    }

    public override void OnExit(IState to)
    {
        // 退出菜单的所有逻辑集中在这里
    }
}
```

**优势**：

- 状态逻辑封装
- 易于添加新状态
- 支持状态转换验证
- 支持状态历史

### Q7: 如何避免过度设计？

**A**:

**原则**：

1. **从简单开始**：先用最简单的方案
2. **按需重构**：遇到问题再优化
3. **YAGNI 原则**：You Aren't Gonna Need It

**示例**：

```csharp
// 第一版：简单直接
public class Player
{
    public int Health = 100;
}

// 第二版：需要通知时添加事件
public class Player
{
    private int _health = 100;
    public int Health
    {
        get => _health;
        set
        {
            _health = value;
            OnHealthChanged?.Invoke(value);
        }
    }
    public event Action&lt;int&gt; OnHealthChanged;
}

// 第三版：需要更多功能时使用 BindableProperty
public class PlayerModel : AbstractModel
{
    public BindableProperty&lt;int&gt; Health { get; } = new(100);
}
```

### Q8: 如何在现有项目中引入这些模式？

**A**:

**渐进式重构**：

1. **第一步：引入事件系统**
   ```csharp
   // 替换直接调用为事件
   // 之前：uiManager.UpdateHealth(health);
   // 之后：SendEvent(new HealthChangedEvent { Health = health });
   ```

2. **第二步：提取 Model**
   ```csharp
   // 将数据从各处集中到 Model
   public class PlayerModel : AbstractModel
   {
       public BindableProperty&lt;int&gt; Health { get; } = new(100);
   }
   ```

3. **第三步：引入命令模式**
   ```csharp
   // 封装操作为命令
   public class HealPlayerCommand : AbstractCommand
   {
       protected override void OnExecute()
       {
           var player = this.GetModel&lt;PlayerModel&gt;();
           player.Health.Value = player.MaxHealth.Value;
       }
   }
   ```

4. **第四步：添加查询模式**
   ```csharp
   // 分离读操作
   public class GetPlayerStatsQuery : AbstractQuery&lt;PlayerStats&gt;
   {
       protected override PlayerStats OnDo()
       {
           // 查询逻辑
       }
   }
   ```

### Q9: 性能会受到影响吗？

**A**:

**影响很小**：

- 事件系统：微秒级开销
- 命令/查询：几乎无开销
- IoC 容器：字典查找，O(1)

**优化建议**：

1. **避免频繁事件**：不要每帧发送事件
2. **缓存查询结果**：复杂查询结果可以缓存
3. **使用对象池**：减少 GC 压力
4. **批量操作**：合并多个小操作

**性能对比**：

```csharp
// 直接调用：~1ns
player.Health = 100;

// 通过命令：~100ns
SendCommand(new SetHealthCommand { Health = 100 });

// 差异可以忽略不计，但带来了更好的架构
```

### Q10: 如何测试使用这些模式的代码？

**A**:

**单元测试示例**：

```csharp
[Test]
public void BuyItemCommand_InsufficientGold_ShouldNotBuyItem()
{
    // Arrange
    var architecture = new TestArchitecture();
    var playerModel = new PlayerModel();
    playerModel.Gold.Value = 50; // 金币不足
    architecture.RegisterModel(playerModel);

    var command = new BuyItemCommand
    {
        Input = new BuyItemInput { ItemId = "sword", Price = 100 }
    };
    command.SetArchitecture(architecture);

    // Act
    command.Execute();

    // Assert
    Assert.AreEqual(50, playerModel.Gold.Value); // 金币未变化
}

[Test]
public void GetPlayerStatsQuery_ShouldReturnCorrectStats()
{
    // Arrange
    var architecture = new TestArchitecture();
    var playerModel = new PlayerModel();
    playerModel.Level.Value = 10;
    playerModel.Health.Value = 80;
    architecture.RegisterModel(playerModel);

    var query = new GetPlayerStatsQuery();
    query.SetArchitecture(architecture);

    // Act
    var stats = query.Do();

    // Assert
    Assert.AreEqual(10, stats.Level);
    Assert.AreEqual(80, stats.Health);
}
```

---

## 总结

遵循这些架构模式最佳实践，你将能够构建：

- ✅ **清晰的代码结构** - 易于理解和维护
- ✅ **松耦合的组件** - 便于测试和扩展
- ✅ **可重用的模块** - 提高开发效率
- ✅ **健壮的错误处理** - 提高系统稳定性
- ✅ **完善的测试覆盖** - 保证代码质量

### 关键要点

1. **从简单开始**：不要过度设计，按需添加模式
2. **理解每个模式的适用场景**：选择合适的模式解决问题
3. **模式可以组合使用**：发挥各自优势
4. **持续重构**：随着项目发展优化架构
5. **注重可测试性**：好的架构应该易于测试

### 推荐学习路径

1. **入门**：MVC + 事件驱动
2. **进阶**：命令模式 + 查询模式 + MVVM
3. **高级**：状态模式 + 对象池 + 依赖注入
4. **专家**：模块化架构 + 所有模式组合

### 相关资源

- [架构核心文档](/zh-CN/core/architecture.md)
- [命令模式文档](/zh-CN/core/command.md)
- [查询模式文档](/zh-CN/core/query.md)
- [事件系统文档](/zh-CN/core/events.md)
- [状态机文档](/zh-CN/core/state-machine.md)
- [IoC 容器文档](/zh-CN/core/ioc.md)

记住，好的架构不是一蹴而就的，需要持续的学习、实践和改进。

---

**文档版本**: 2.0.0
**最后更新**: 2026-03-07
**作者**: GFramework Team
