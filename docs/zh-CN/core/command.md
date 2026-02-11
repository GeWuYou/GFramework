# Command 包使用说明

## 概述

Command 包实现了命令模式（Command Pattern），用于封装用户操作和业务逻辑。通过命令模式，可以将请求封装为对象，实现操作的参数化、队列化、日志记录、撤销等功能。

## 核心接口

### 1. [`ICommand`](./command.md)

无返回值命令接口，定义了命令的基本契约。

**核心方法：**

```csharp
void Execute();  // 执行命令
```

### 2. [`ICommand<TResult>`](./command.md)

带返回值的命令接口，用于需要返回执行结果的命令。

**核心方法：**

```csharp
TResult Execute();  // 执行命令并返回结果
```

## 核心类

### 1. [`AbstractCommand<TInput>`](./command.md)

无返回值命令的抽象基类，提供了命令的基础实现。它继承自 [ContextAwareBase](./rule.md)
，具有上下文感知能力。

**使用示例：**

```csharp
// 定义一个命令输入参数
public struct StartGameCommandInput : ICommandInput
{
    public int LevelId { get; set; }
    public string PlayerName { get; set; }
}

// 定义一个开始游戏的命令
public class StartGameCommand : AbstractCommand<StartGameCommandInput>
{
    public StartGameCommand(StartGameCommandInput input) : base(input)
    {
    }

    protected override void OnExecute(StartGameCommandInput input)
    {
        // 获取需要的模型
        var playerModel = this.GetModel<PlayerModel>();
        var gameModel = this.GetModel<GameModel>();
        
        // 执行业务逻辑
        playerModel.PlayerName.Value = input.PlayerName;
        gameModel.CurrentLevel.Value = input.LevelId;
        gameModel.GameState.Value = GameState.Playing;
        
        // 发送事件通知其他模块
        this.SendEvent(new GameStartedEvent());
    }
}

// 使用命令
public class GameController : IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public void OnStartButtonClicked()
    {
        // 发送命令实例
        this.SendCommand(new StartGameCommand(new StartGameCommandInput 
        { 
            LevelId = 1, 
            PlayerName = "Player1" 
        }));
    }
}
```

### 2. [`AbstractCommand<TInput, TResult>`](./command.md)

带返回值命令的抽象基类，同样继承自 [ContextAwareBase](./rule.md)。

**使用示例：**

```csharp
// 定义一个计算伤害的命令输入
public struct CalculateDamageCommandInput : ICommandInput
{
    public int AttackerAttackPower { get; set; }
    public int DefenderDefense { get; set; }
}

// 定义一个计算伤害的命令
public class CalculateDamageCommand : AbstractCommand<CalculateDamageCommandInput, int>
{
    public CalculateDamageCommand(CalculateDamageCommandInput input) : base(input)
    {
    }

    protected override int OnExecute(CalculateDamageCommandInput input)
    {
        // 获取游戏配置
        var config = this.GetModel<GameConfigModel>();
        
        // 计算最终伤害
        var baseDamage = input.AttackerAttackPower - input.DefenderDefense;
        var finalDamage = Math.Max(1, baseDamage * config.DamageMultiplier);
        
        return (int)finalDamage;
    }
}

// 使用带返回值的命令
public class CombatSystem : AbstractSystem
{
    protected override void OnInit() { }
    
    public void Attack(Character attacker, Character defender)
    {
        // 发送命令并获取返回值
        var damage = this.SendCommand(new CalculateDamageCommand(new CalculateDamageCommandInput
        {
            AttackerAttackPower = attacker.AttackPower,
            DefenderDefense = defender.Defense
        }));
        
        // 应用伤害
        defender.Health -= damage;
        
        // 发送伤害事件
        this.SendEvent(new DamageDealtEvent(attacker, defender, damage));
    }
}
```

## 命令的生命周期

1. **创建命令**：实例化命令对象，传入必要的参数
2. **执行命令**：调用 `Execute()` 方法，内部委托给 `OnExecute()`
3. **返回结果**：对于带返回值的命令，返回执行结果
4. **命令销毁**：命令执行完毕后可以被垃圾回收

## CommandBus - 命令总线

### 功能说明

[CommandBus](file:///d:/Project/Rider/GFramework/GFramework.Core/command/CommandBus.cs#L8-L34) 是命令执行的核心组件，负责发送和执行命令。

**主要方法：**

```csharp
void Send(ICommand command);              // 发送无返回值命令
TResult Send<TResult>(ICommand<TResult> command);  // 发送带返回值命令
```

### 使用示例

```csharp
var commandBus = new CommandBus();

// 发送无返回值命令
commandBus.Send(new StartGameCommand(new StartGameCommandInput()));

// 发送带返回值命令
var result = commandBus.Send(new CalculateDamageCommand(new CalculateDamageCommandInput()));
```

## EmptyCommandInput - 空命令输入

当命令不需要输入参数时，可以使用 `EmptyCommandInput` 类：

```csharp
public class SimpleActionCommand : AbstractCommand<EmptyCommandInput>
{
    public SimpleActionCommand(EmptyCommandInput input) : base(input)
    {
    }

    protected override void OnExecute(EmptyCommandInput input)
    {
        // 执行简单操作，无需额外参数
        this.SendEvent(new SimpleActionEvent());
    }
}
```

## 使用场景

### 1. 用户交互操作

```csharp
public struct SaveGameCommandInput : ICommandInput
{
    public string SaveSlot { get; set; }
}

public class SaveGameCommand : AbstractCommand<SaveGameCommandInput>
{
    public SaveGameCommand(SaveGameCommandInput input) : base(input)
    {
    }

    protected override void OnExecute(SaveGameCommandInput input)
    {
        var saveSystem = this.GetSystem<SaveSystem>();
        var playerModel = this.GetModel<PlayerModel>();
        
        saveSystem.SavePlayerData(playerModel, input.SaveSlot);
        this.SendEvent(new GameSavedEvent(input.SaveSlot));
    }
}
```

### 2. 业务流程控制

```csharp
public struct LoadLevelCommandInput : ICommandInput
{
    public int LevelId { get; set; }
}

public class LoadLevelCommand : AbstractCommand<LoadLevelCommandInput>
{
    public LoadLevelCommand(LoadLevelCommandInput input) : base(input)
    {
    }

    protected override void OnExecute(LoadLevelCommandInput input)
    {
        var levelSystem = this.GetSystem<LevelSystem>();
        var uiSystem = this.GetSystem<UISystem>();
        
        // 显示加载界面
        uiSystem.ShowLoadingScreen();
        
        // 加载关卡
        levelSystem.LoadLevel(input.LevelId);
        
        // 发送事件
        this.SendEvent(new LevelLoadedEvent(input.LevelId));
    }
}
```

## 最佳实践

1. **保持命令原子性**：一个命令应该完成一个完整的业务操作
2. **命令无状态**：命令不应该保存长期状态，执行完即可丢弃
3. **参数通过构造函数传递**：命令需要的参数应在创建时传入
4. **避免命令嵌套**：命令内部尽量不要发送其他命令，使用事件通信
5. **合理使用返回值**：只在确实需要返回结果时使用 `AbstractCommand<TInput, TResult>`
6. **命令命名规范**：使用动词+名词形式，如 `StartGameCommand`、`SavePlayerCommand`
7. **输入参数结构化**：使用 `ICommandInput` 接口的实现类来组织命令参数

## 扩展功能

### 命令撤销/重做（可扩展）

```csharp
public struct MoveCommandInput : ICommandInput
{
    public Vector3 NewPosition { get; set; }
}

// 实现可撤销命令
public class MoveCommand : AbstractCommand<MoveCommandInput>, IUndoableCommand
{
    private Vector3 _oldPosition;
    private Vector3 _newPosition;
    
    public MoveCommand(MoveCommandInput input) : base(input)
    {
        _newPosition = input.NewPosition;
    }

    protected override void OnExecute(MoveCommandInput input)
    {
        var player = this.GetModel<PlayerModel>();
        _oldPosition = player.Position;
        player.Position = input.NewPosition;
    }
    
    public void Undo()
    {
        var player = this.GetModel<PlayerModel>();
        player.Position = _oldPosition;
    }
}
```

## 相关包

- [`architecture`](./architecture.md) - 架构核心，负责命令的分发和执行
- [`extensions`](./extensions.md) - 提供 `SendCommand()` 扩展方法
- [`query`](./query.md) - 查询模式，用于数据查询
- [`events`](./events.md) - 事件系统，命令执行后的通知机制
- [`system`](./system.md) - 业务系统，命令的主要执行者
- [`model`](./model.md) - 数据模型，命令操作的数据

---

**许可证**: Apache 2.0