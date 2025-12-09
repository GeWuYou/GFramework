# Command 包使用说明

## 概述

Command 包实现了命令模式（Command Pattern），用于封装用户操作和业务逻辑。通过命令模式，可以将请求封装为对象，实现操作的参数化、队列化、日志记录、撤销等功能。

## 核心接口

### 1. [`ICommand`](ICommand.cs)

无返回值命令接口，定义了命令的基本契约。

**继承的能力接口：**
- [`ICanSetArchitecture`](../rule/ICanSetArchitecture.cs) - 可设置架构
- [`ICanGetSystem`](../system/ICanGetSystem.cs) - 可获取系统
- [`ICanGetModel`](../model/ICanGetModel.cs) - 可获取模型
- [`ICanGetUtility`](../utility/ICanGetUtility.cs) - 可获取工具
- [`ICanSendEvent`](../events/ICanSendEvent.cs) - 可发送事件
- [`ICanSendCommand`](ICanSendCommand.cs) - 可发送命令
- [`ICanSendQuery`](../query/ICanSendQuery.cs) - 可发送查询

**核心方法：**
```csharp
void Execute();  // 执行命令
```

### 2. [`ICommand<TResult>`](ICommand.cs)

带返回值的命令接口，用于需要返回执行结果的命令。

**核心方法：**
```csharp
TResult Execute();  // 执行命令并返回结果
```

### 3. [`ICanSendCommand`](ICanSendCommand.cs)

标记接口，表示实现者可以发送命令。继承自 [`IBelongToArchitecture`](../rule/IBelongToArchitecture.cs)。

## 核心类

### 1. [`AbstractCommand`](AbstractCommand.cs)

无返回值命令的抽象基类，提供了命令的基础实现。

**使用示例：**

```csharp
// 定义一个开始游戏的命令
public class StartGameCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        // 获取需要的模型
        var playerModel = this.GetModel<PlayerModel>();
        var gameModel = this.GetModel<GameModel>();
        
        // 执行业务逻辑
        playerModel.Health.Value = 100;
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
        // 方式1：发送命令实例
        this.SendCommand(new StartGameCommand());
        
        // 方式2：通过泛型发送（需要无参构造函数）
        this.SendCommand<StartGameCommand>();
    }
}
```

### 2. [`AbstractCommand<TResult>`](AbstractCommand.cs)

带返回值命令的抽象基类。

**使用示例：**

```csharp
// 定义一个计算伤害的命令
public class CalculateDamageCommand : AbstractCommand<int>
{
    private readonly int _attackPower;
    private readonly int _defense;
    
    public CalculateDamageCommand(int attackPower, int defense)
    {
        _attackPower = attackPower;
        _defense = defense;
    }
    
    protected override int OnExecute()
    {
        // 获取游戏配置
        var config = this.GetModel<GameConfigModel>();
        
        // 计算最终伤害
        var baseDamage = _attackPower - _defense;
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
        var damage = this.SendCommand(
            new CalculateDamageCommand(attacker.AttackPower, defender.Defense)
        );
        
        // 应用伤害
        defender.Health -= damage;
        
        // 发送伤害事件
        this.SendEvent(new DamageDealtEvent(attacker, defender, damage));
    }
}
```

## 命令的生命周期

1. **创建命令**：实例化命令对象，传入必要的参数
2. **设置架构**：框架自动调用 `SetArchitecture()` 设置架构引用
3. **执行命令**：调用 `Execute()` 方法，内部委托给 `OnExecute()`
4. **返回结果**：对于带返回值的命令，返回执行结果
5. **命令销毁**：命令执行完毕后可以被垃圾回收

## 使用场景

### 1. 用户交互操作

```csharp
// UI 按钮点击
public class SaveGameCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var saveSystem = this.GetSystem<SaveSystem>();
        var playerModel = this.GetModel<PlayerModel>();
        
        saveSystem.SavePlayerData(playerModel);
        this.SendEvent(new GameSavedEvent());
    }
}
```

### 2. 业务流程控制

```csharp
// 关卡切换
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

### 3. 网络请求封装

```csharp
// 登录命令
public class LoginCommand : AbstractCommand<bool>
{
    private readonly string _username;
    private readonly string _password;
    
    public LoginCommand(string username, string password)
    {
        _username = username;
        _password = password;
    }
    
    protected override bool OnExecute()
    {
        var networkSystem = this.GetSystem<NetworkSystem>();
        var playerModel = this.GetModel<PlayerModel>();
        
        // 发送登录请求
        var result = networkSystem.Login(_username, _password);
        
        if (result.Success)
        {
            playerModel.UserId = result.UserId;
            playerModel.Username = _username;
            this.SendEvent(new LoginSuccessEvent());
            return true;
        }
        else
        {
            this.SendEvent(new LoginFailedEvent(result.ErrorMessage));
            return false;
        }
    }
}
```

## 命令 vs 系统方法

**何时使用命令：**
- 需要参数化操作
- 需要记录操作历史（用于撤销/重做）
- 操作需要跨多个系统协调
- 用户触发的离散操作

**何时使用系统方法：**
- 持续运行的逻辑（如每帧更新）
- 系统内部的私有逻辑
- 不需要外部调用的功能

## 最佳实践

1. **保持命令原子性**：一个命令应该完成一个完整的业务操作
2. **命令无状态**：命令不应该保存长期状态，执行完即可丢弃
3. **参数通过构造函数传递**：命令需要的参数应在创建时传入
4. **避免命令嵌套**：命令内部尽量不要发送其他命令，使用事件通信
5. **合理使用返回值**：只在确实需要返回结果时使用 `ICommand<TResult>`
6. **命令命名规范**：使用动词+名词形式，如 `StartGameCommand`、`SavePlayerCommand`

## 扩展功能

### 命令撤销/重做（可扩展）

```csharp
// 可撤销命令接口
public interface IUndoableCommand : ICommand
{
    void Undo();
}

// 实现可撤销命令
public class MoveCommand : AbstractCommand, IUndoableCommand
{
    private Vector3 _oldPosition;
    private Vector3 _newPosition;
    
    protected override void OnExecute()
    {
        var player = this.GetModel<PlayerModel>();
        _oldPosition = player.Position;
        player.Position = _newPosition;
    }
    
    public void Undo()
    {
        var player = this.GetModel<PlayerModel>();
        player.Position = _oldPosition;
    }
}
```

## 相关包

- [`architecture`](../architecture/README.md) - 架构核心，负责命令的分发和执行
- [`extensions`](../extensions/README.md) - 提供 `SendCommand()` 扩展方法
- [`query`](../query/README.md) - 查询模式，用于数据查询
- [`events`](../events/README.md) - 事件系统，命令执行后的通知机制
- [`system`](../system/README.md) - 业务系统，命令的主要执行者
- [`model`](../model/README.md) - 数据模型，命令操作的数据