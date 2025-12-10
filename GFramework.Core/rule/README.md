# Rule 包使用说明

## 概述

Rule 包定义了框架的核心规则接口，这些接口规定了框架各个组件之间的关系和约束。所有框架组件都需要遵循这些规则接口。

## 核心接口

### [`IBelongToArchitecture`](IBelongToArchitecture.cs)

标记接口，表示某个对象属于特定的架构体系。

**接口定义：**
```csharp
public interface IBelongToArchitecture
{
    IArchitecture GetArchitecture();
}
```

**实现此接口的类型：**
- Controller
- System
- Model
- Command
- Query
- Event 处理器

**作用：**
所有实现此接口的类型都能够获取其所属的架构实例，从而访问架构提供的各种能力。

### [`ICanSetArchitecture`](ICanSetArchitecture.cs)

定义可以设置架构实例的能力。

**接口定义：**
```csharp
public interface ICanSetArchitecture
{
    void SetArchitecture(IArchitecture architecture);
}
```

**实现此接口的类型：**
- Command
- Query

**作用：**
在 Command 和 Query 执行前，框架会自动调用 `SetArchitecture` 方法注入架构实例，使其能够访问 Model、System 等组件。

## 接口关系图

```
IBelongToArchitecture (属于架构)
    ↓ 被继承
    ├── ICanGetModel (可获取 Model)
    ├── ICanGetSystem (可获取 System)
    ├── ICanGetUtility (可获取 Utility)
    ├── ICanSendCommand (可发送 Command)
    ├── ICanSendEvent (可发送 Event)
    ├── ICanSendQuery (可发送 Query)
    └── ICanRegisterEvent (可注册 Event)

ICanSetArchitecture (可设置架构)
    ↓ 被继承
    ├── ICommand (命令接口)
    └── IQuery<T> (查询接口)
```

## 使用场景

### 1. Controller 实现 IBelongToArchitecture

```csharp
// Controller 通过实现 IBelongToArchitecture 获得架构访问能力
public partial class PlayerController : Node, IController
{
    // 实现 IBelongToArchitecture 接口
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        // 因为实现了 IBelongToArchitecture，所以可以使用扩展方法
        var playerModel = this.GetModel<PlayerModel>();
        this.SendCommand(new InitPlayerCommand());
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        GD.Print("Player died!");
    }
}
```

### 2. Command 实现 ICanSetArchitecture

```csharp
// Command 实现 ICanSetArchitecture，框架会自动注入架构
public class BuyItemCommand : AbstractCommand
{
    public string ItemId { get; set; }
    
    protected override void OnExecute()
    {
        // 框架已经通过 SetArchitecture 注入了架构实例
        // 所以这里可以直接使用 this.GetModel
        var playerModel = this.GetModel<PlayerModel>();
        var shopModel = this.GetModel<ShopModel>();
        
        int price = shopModel.GetItemPrice(ItemId);
        if (playerModel.Gold.Value >= price)
        {
            playerModel.Gold.Value -= price;
            this.SendCommand(new AddItemCommand { ItemId = ItemId });
        }
    }
}
```

### 3. 自定义组件遵循规则

```csharp
// 自定义管理器遵循框架规则
public class SaveManager : IBelongToArchitecture
{
    private IArchitecture _architecture;
    
    public SaveManager(IArchitecture architecture)
    {
        _architecture = architecture;
    }
    
    public IArchitecture GetArchitecture() => _architecture;
    
    public void SaveGame()
    {
        // 因为实现了 IBelongToArchitecture，可以使用扩展方法
        var playerModel = this.GetModel<PlayerModel>();
        var saveData = new SaveData
        {
            PlayerName = playerModel.Name.Value,
            Level = playerModel.Level.Value,
            Gold = playerModel.Gold.Value
        };
        
        // 保存逻辑...
    }
}
```

## 规则约束

### 1. 组件注册规则

```csharp
public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // ✅ 正确：Model/System/Utility 自动获得架构引用
        this.RegisterModel<PlayerModel>(new PlayerModel());
        this.RegisterSystem<CombatSystem>(new CombatSystem());
        this.RegisterUtility<StorageUtility>(new StorageUtility());
    }
}

// Model 继承 AbstractModel，AbstractModel 实现了 IBelongToArchitecture
public class PlayerModel : AbstractModel
{
    // 无需手动实现 GetArchitecture，基类已实现
    protected override void OnInit()
    {
        // 可以直接使用架构能力
        this.SendEvent(new PlayerModelInitializedEvent());
    }
}
```

### 2. Command/Query 自动注入规则

```csharp
// Controller 中发送 Command
public class ShopUI : Control, IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    private void OnBuyButtonPressed()
    {
        var command = new BuyItemCommand { ItemId = "sword_01" };
        
        // SendCommand 内部会自动调用 command.SetArchitecture(this.GetArchitecture())
        this.SendCommand(command);
    }
}

// Command 执行时已经有了架构引用
public class BuyItemCommand : AbstractCommand
{
    public string ItemId { get; set; }
    
    protected override void OnExecute()
    {
        // 此时 GetArchitecture() 已经可用
        var model = this.GetModel<ShopModel>();
    }
}
```

## 设计模式

### 1. 依赖注入模式

Rule 接口体现了依赖注入（DI）的思想：

```csharp
// 接口定义了"需要什么"
public interface IBelongToArchitecture
{
    IArchitecture GetArchitecture();
}

// 框架负责"提供什么"
public static class CanSendExtensions
{
    public static void SendCommand<T>(this ICanSendCommand self, T command) 
        where T : ICommand
    {
        // 自动注入架构依赖
        command.SetArchitecture(self.GetArchitecture());
        command.Execute();
    }
}
```

### 2. 接口隔离原则（ISP）

Rule 接口遵循接口隔离原则，每个接口职责单一：

```csharp
// ❌ 不好的设计：一个大接口包含所有能力
public interface IBigInterface
{
    IArchitecture GetArchitecture();
    void SetArchitecture(IArchitecture architecture);
    T GetModel<T>() where T : class, IModel;
    T GetSystem<T>() where T : class, ISystem;
    void SendCommand<T>(T command) where T : ICommand;
    // ... 更多方法
}

// ✅ 好的设计：小接口组合
public interface IBelongToArchitecture { ... }  // 只负责获取架构
public interface ICanSetArchitecture { ... }    // 只负责设置架构
public interface ICanGetModel { ... }           // 只负责获取 Model
public interface ICanSendCommand { ... }        // 只负责发送 Command
```

### 3. 组合优于继承

通过接口组合实现不同能力：

```csharp
// Controller 需要获取 Model 和发送 Command
public interface IController : ICanGetModel, ICanGetSystem, ICanSendCommand, 
    ICanSendQuery, ICanRegisterEvent
{
}

// Command 需要设置架构和获取 Model/System
public interface ICommand : ICanSetArchitecture, ICanGetModel, ICanGetSystem, 
    ICanSendEvent, ICanSendQuery
{
}

// System 只需要获取其他组件
public interface ISystem : ICanGetModel, ICanGetUtility, ICanGetSystem, 
    ICanRegisterEvent, ICanSendEvent, ICanSendQuery
{
}
```

## 扩展规则

### 自定义规则接口

```csharp
// 定义新的规则接口
public interface ICanAccessDatabase : IBelongToArchitecture
{
}

// 提供扩展方法
public static class CanAccessDatabaseExtensions
{
    public static DatabaseUtility GetDatabase(this ICanAccessDatabase self)
    {
        return self.GetArchitecture().GetUtility<DatabaseUtility>();
    }
}

// 让特定组件实现这个接口
public class DatabaseCommand : AbstractCommand, ICanAccessDatabase
{
    protected override void OnExecute()
    {
        var db = this.GetDatabase();
        // 使用数据库...
    }
}
```

## 最佳实践

1. **遵循既有规则** - 优先使用框架提供的规则接口
2. **不要绕过规则** - 不要直接存储架构引用，使用接口获取
3. **接口组合** - 通过实现多个小接口获得所需能力
4. **扩展规则** - 需要新能力时，定义新的规则接口
5. **保持一致** - 自定义组件也应遵循相同的规则体系

## 相关包

- [`architecture`](../architecture/README.md) - 定义 IArchitecture 接口
- [`command`](../command/README.md) - Command 实现 ICanSetArchitecture
- [`query`](../query/README.md) - Query 实现 ICanSetArchitecture
- [`controller`](../controller/README.md) - Controller 实现 IBelongToArchitecture
- [`system`](../system/README.md) - System 实现 IBelongToArchitecture
- [`model`](../model/README.md) - Model 实现 IBelongToArchitecture
- [`extensions`](../extensions/README.md) - 基于规则接口提供扩展方法