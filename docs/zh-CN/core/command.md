# Command 包使用说明

## 概述

Command 包实现了命令模式（Command Pattern），用于封装用户操作和业务逻辑。通过命令模式，可以将请求封装为对象，实现操作的参数化、队列化、日志记录、撤销等功能。

命令系统是 GFramework CQRS 架构的重要组成部分，与事件系统和查询系统协同工作，实现完整的业务逻辑处理流程。

## 核心接口

### ICommand

无返回值命令接口，定义了命令的基本契约。

**核心方法：**

```csharp
void Execute();  // 执行命令
```

### ICommand`<TResult>`

带返回值的命令接口，用于需要返回执行结果的命令。

**核心方法：**

```csharp
TResult Execute();  // 执行命令并返回结果
```

## 核心类

### AbstractCommand

无返回值命令的抽象基类，提供了命令的基础实现。它继承自 ContextAwareBase，具有上下文感知能力。

**核心方法：**

```csharp
void ICommand.Execute();                    // 实现 ICommand 接口
protected abstract void OnExecute();        // 抽象执行方法，由子类实现
```

**使用示例：**

```csharp
// 定义一个开始游戏的命令
public class StartGameCommand : AbstractCommand
{
    private readonly int _levelId;
    private readonly string _playerName;

    public StartGameCommand(int levelId, string playerName)
    {
        _levelId = levelId;
        _playerName = playerName;
    }

    protected override void OnExecute()
    {
        // 获取需要的模型
        var playerModel = this.GetModel<PlayerModel>();
        var gameModel = this.GetModel<GameModel>();
        
        // 执行业务逻辑
        playerModel.PlayerName.Value = _playerName;
        gameModel.CurrentLevel.Value = _levelId;
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
        this.SendCommand(new StartGameCommand(1, "Player1"));
    }
}
```

### AbstractCommandWithResult`<TResult>`

带返回值命令的抽象基类，同样继承自 ContextAwareBase。

**核心方法：**

```csharp
TResult ICommand<TResult>.Execute();        // 实现 ICommand<TResult> 接口
protected abstract TResult OnExecute();     // 抽象执行方法，由子类实现
```

**使用示例：**

```csharp
// 定义一个计算伤害的命令
public class CalculateDamageCommand : AbstractCommandWithResult<int>
{
    private readonly int _attackerAttackPower;
    private readonly int _defenderDefense;

    public CalculateDamageCommand(int attackerAttackPower, int defenderDefense)
    {
        _attackerAttackPower = attackerAttackPower;
        _defenderDefense = defenderDefense;
    }

    protected override int OnExecute()
    {
        // 获取游戏配置
        var config = this.GetModel<GameConfigModel>();
        
        // 计算最终伤害
        var baseDamage = _attackerAttackPower - _defenderDefense;
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
        var damage = this.SendCommand(new CalculateDamageCommand(
            attacker.AttackPower,
            defender.Defense
        ));
        
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

**注意事项：**

- 命令应该是无状态的，执行完即可丢弃
- 避免在命令中保存长期引用
- 命令执行应该是原子操作

## CommandBus - 命令总线

### 功能说明

`CommandBus` 是命令执行的核心组件，负责发送和执行命令。

**主要方法：**

```csharp
void Send(ICommand command);                    // 发送无返回值命令
TResult Send<TResult>(ICommand<TResult> command);  // 发送带返回值命令
```

**特点：**

- 统一的命令执行入口
- 支持同步命令执行
- 与架构上下文集成

### 使用示例

```csharp
// 通过架构获取命令总线
var commandBus = architecture.Context.CommandBus;

// 发送无返回值命令
commandBus.Send(new StartGameCommand(1, "Player1"));

// 发送带返回值命令
var damage = commandBus.Send(new CalculateDamageCommand(100, 50));
```

## 命令基类变体

框架提供了多种命令基类以满足不同需求：

### AbstractCommand

最基础的无返回值命令类

### AbstractCommandWithResult`<TResult>`

带返回值的命令基类

### AbstractCommandWithInput`<TInput>`

带输入参数的无返回值命令类

### AbstractCommandWithInputAndResult`<TInput, TResult>`

既带输入参数又带返回值的命令类

### AbstractAsyncCommand

支持异步执行的命令基类

### AbstractAsyncCommandWithResult`<TResult>`

支持异步执行的带返回值命令基类

```csharp
// 异步命令示例
public class LoadSaveDataCommand : AbstractAsyncCommandWithResult<SaveData>
{
    private readonly string _saveSlot;

    public LoadSaveDataCommand(string saveSlot)
    {
        _saveSlot = saveSlot;
    }

    protected override async Task<SaveData> OnExecuteAsync()
    {
        var storage = this.GetUtility<IStorageUtility>();
        return await storage.LoadSaveDataAsync(_saveSlot);
    }
}
```

## 命令处理器执行

所有发送给命令总线的命令最终都会通过 `CommandExecutor` 来执行：

```csharp
public class CommandExecutor
{
    public static void Execute(ICommand command)
    {
        command.Execute();
    }
    
    public static TResult Execute<TResult>(ICommand<TResult> command)
    {
        return command.Execute();
    }
}
```

**特点：**

- 提供统一的命令执行机制
- 支持同步和异步命令执行
- 可以扩展添加中间件逻辑

## 使用场景

### 1. 用户交互操作

```csharp
public class SaveGameCommand : AbstractCommand
{
    private readonly string _saveSlot;

    public SaveGameCommand(string saveSlot)
    {
        _saveSlot = saveSlot;
    }

    protected override void OnExecute()
    {
        var saveSystem = this.GetSystem<SaveSystem>();
        var playerModel = this.GetModel<PlayerModel>();
        
        saveSystem.SavePlayerData(playerModel, _saveSlot);
        this.SendEvent(new GameSavedEvent(_saveSlot));
    }
}
```

### 2. 业务流程控制

```csharp
public class LoadLevelCommand : AbstractCommand
{
    private readonly int _levelId;

    public LoadLevelCommand(int levelId)
    {
        _levelId = levelId;
    }

    protected override void OnExecute()
    {
        var levelSystem = this.GetSystem<LevelSystem>();
        var uiSystem = this.GetSystem<UISystem>();
        
        // 显示加载界面
        uiSystem.ShowLoadingScreen();
        
        // 加载关卡
        levelSystem.LoadLevel(_levelId);
        
        // 发送事件
        this.SendEvent(new LevelLoadedEvent(_levelId));
    }
}
```

## 最佳实践

1. **保持命令原子性**：一个命令应该完成一个完整的业务操作
2. **命令无状态**：命令不应该保存长期状态，执行完即可丢弃
3. **参数通过构造函数传递**：命令需要的参数应在创建时传入
4. **避免命令嵌套**：命令内部尽量不要发送其他命令，使用事件通信
5. **合理使用返回值**：只在确实需要返回结果时使用带返回值的命令
6. **命令命名规范**：使用动词+名词形式，如 `StartGameCommand`、`SavePlayerCommand`
7. **单一职责原则**：每个命令只负责一个特定的业务操作
8. **使用异步命令**：对于需要长时间执行的操作，使用异步命令避免阻塞
9. **命令验证**：在命令执行前验证输入参数的有效性
10. **错误处理**：在命令中适当处理异常情况

## 命令模式优势

### 1. 可扩展性

- 命令可以被序列化和存储
- 支持命令队列和批处理
- 便于实现撤销/重做功能

### 2. 可测试性

- 命令逻辑独立，易于单元测试
- 可以模拟命令执行结果
- 支持行为驱动开发

### 3. 可维护性

- 业务逻辑集中管理
- 降低组件间耦合度
- 便于重构和扩展

## 相关包

- [`architecture`](./architecture.md) - 架构核心，负责命令的分发和执行
- [`extensions`](./extensions.md) - 提供 `SendCommand()` 扩展方法
- [`query`](./query.md) - 查询模式，用于数据查询
- [`events`](./events.md) - 事件系统，命令执行后的通知机制
- [`system`](./system.md) - 业务系统，命令的主要执行者
- [`model`](./model.md) - 数据模型，命令操作的数据

---

**许可证**：Apache 2.0