# Rule 包使用说明

## 概述

Rule 包定义了框架的核心规则接口，这些接口规定了框架各个组件之间的关系和约束。所有框架组件都需要遵循这些规则接口。

## 核心接口

### IContextAware

标记接口，表示该类型可以感知架构上下文。

**接口定义：**

```csharp
public interface IContextAware
{
    void SetContext(IArchitectureContext context);
    IArchitectureContext GetContext();
}
```

**实现此接口的类型：**
- System
- Query
- Model
- Command
- 以及其他需要感知架构上下文的组件

**作用：**
所有实现此接口的类型都能够获取其所属的架构上下文实例，从而访问架构提供的各种能力。

## 核心类

### [`ContextAwareBase`](ContextAwareBase.cs)

上下文感知基类，实现了 IContextAware 接口，为需要感知架构上下文的类提供基础实现。

**使用方式：**

```csharp
public abstract class ContextAwareBase : IContextAware
{
    protected IArchitectureContext? Context { get; set; }
    
    void IContextAware.SetContext(IArchitectureContext context)
    {
        Context = context;
        OnContextReady();  // 上下文准备好后调用此方法
    }
    
    IArchitectureContext IContextAware.GetContext()
    {
        Context ??= GameContext.GetFirstArchitectureContext();
        return Context;
    }
    
    protected virtual void OnContextReady()  // 子类可以重写此方法进行初始化
    {
    }
}
```

## 接口关系图

```
IContextAware (上下文感知接口)
    ↓ 被继承于
    ├── AbstractSystem (抽象系统基类)
    ├── AbstractQuery<TInput, TResult> (抽象查询基类)
    ├── AbstractModel (抽象模型基类)
    └── AbstractCommand (抽象命令基类)
```

## 使用场景

### 1. Component 继承 ContextAwareBase

```csharp
// 组件通过继承 ContextAwareBase 获得架构上下文访问能力
public partial class PlayerController : Node, IController
{
    // 不再需要手动实现 IContextAware，基类已处理
    // 可以直接使用扩展方法
    public override void _Ready()
    {
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

### 2. Command 继承 AbstractCommand (IContextAware)

```csharp
// Command 继承 AbstractCommand，自动成为 IContextAware
public class BuyItemCommand : AbstractCommand
{
    public string ItemId { get; set; }
    
    protected override void OnExecute()
    {
        // 框架或上下文系统会自动注入 IArchitectureContext
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
// 自定义管理器遵循框架规则，继承 ContextAwareBase
public class SaveManager : ContextAwareBase
{
    // 不再需要手动构造函数传参，上下文由框架注入
    // protected override void OnContextReady() 可用于初始化
    
    public void SaveGame()
    {
        // 因为继承了 ContextAwareBase，可以使用扩展方法
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
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // Model/System/Utility 自动获得架构引用
        this.RegisterModel<PlayerModel>(new PlayerModel());
        this.RegisterSystem<CombatSystem>(new CombatSystem());
        this.RegisterUtility<StorageUtility>(new StorageUtility());
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
public interface IContextAware
{
    void SetContext(IArchitectureContext context);
    IArchitectureContext GetContext();
}

// 框架负责"提供什么"
public static class CanSendExtensions
{
    public static void SendCommand<T>(this ICanSendCommand self, T command) 
        where T : ICommand
    {
        // 自动注入架构上下文依赖
        command.SetContext(self.GetContext());
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
    void SetContext(IArchitectureContext context);
    IArchitectureContext GetContext();
    T GetModel<T>() where T : class, IModel;
    T GetSystem<T>() where T : class, ISystem;
    void SendCommand<T>(T command) where T : ICommand;
    // ... 更多方法
}

// ✅ 好的设计：小接口组合
public interface IContextAware { ... }          // 只负责上下文的设置与获取
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

// Command 需要上下文感知和获取 Model/System
public interface ICommand : IContextAware, ICanGetModel, ICanGetSystem, 
    ICanSendEvent, ICanSendQuery
{
}

// System 只需要获取其他组件
public interface ISystem : IContextAware, ICanGetModel, ICanGetUtility, ICanGetSystem, 
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

- [`architecture`](../architecture/README.md) - 定义 IArchitectureContext 接口
- [`command`](../command/README.md) - Command 继承 AbstractCommand (IContextAware)
- [`query`](../query/README.md) - Query 继承 AbstractQuery (IContextAware)
- [`controller`](../controller/README.md) - Controller 实现 ICanSendCommand 等接口
- [`system`](../system/README.md) - System 继承 AbstractSystem (IContextAware)
- [`model`](../model/README.md) - Model 继承 AbstractModel (IContextAware)
- [`extensions`](../extensions/README.md) - 基于规则接口提供扩展方法