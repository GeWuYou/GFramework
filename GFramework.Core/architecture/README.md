# Architecture 包使用说明

## 概述

Architecture 包是整个框架的核心，提供了基于 MVC 架构模式的应用程序架构基础。它实现了依赖注入（IoC）容器、组件生命周期管理，以及命令、查询、事件的统一调度机制。

## 核心类

### 1. [`IArchitecture`](IArchitecture.cs)

架构接口，定义了框架的核心功能契约。

**主要职责：**

- 组件注册：注册 System、Model、Utility
- 组件获取：从容器中获取已注册的组件
- 命令处理：发送并执行命令
- 查询处理：发送并执行查询
- 事件管理：发送、注册、注销事件

**核心方法：**

```csharp
// 注册组件
void RegisterSystem<T>(T system) where T : ISystem;
void RegisterModel<T>(T model) where T : IModel;
void RegisterUtility<T>(T utility) where T : IUtility;

// 获取组件
T GetSystem<T>() where T : class, ISystem;
T GetModel<T>() where T : class, IModel;
T GetUtility<T>() where T : class, IUtility;

// 命令处理
void SendCommand<T>(T command) where T : ICommand;
TResult SendCommand<TResult>(ICommand<TResult> command);

// 查询处理
TResult SendQuery<TResult>(IQuery<TResult> query);

// 事件管理
void SendEvent<T>() where T : new();
void SendEvent<T>(T e);
IUnRegister RegisterEvent<T>(Action<T> onEvent);
void UnRegisterEvent<T>(Action<T> onEvent);
```

### 2. [`Architecture<T>`](Architecture.cs)

架构基类，实现了 [`IArchitecture`](IArchitecture.cs) 接口，提供完整的架构功能实现。

**特性：**

- **单例模式**：使用泛型和 `Lazy<T>` 确保全局唯一实例
- **线程安全**：采用 `LazyThreadSafetyMode.ExecutionAndPublication` 保证多线程安全
- **生命周期管理**：自动管理 System 和 Model 的初始化顺序
- **IoC 容器**：内置依赖注入容器，管理所有组件实例
- **事件系统**：集成类型化事件系统，支持类型安全的事件通信

**初始化流程：**

1. 创建架构实例
2. 调用用户自定义的 `Init()` 方法
3. 执行注册的补丁逻辑（`OnRegisterPatch`）
4. 初始化所有已注册的 Model
5. 初始化所有已注册的 System
6. 标记初始化完成

**使用示例：**

```csharp
// 1. 定义你的架构
public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 注册 Model
        RegisterModel(new PlayerModel());
        RegisterModel(new InventoryModel());
        
        // 注册 System
        RegisterSystem(new GameplaySystem());
        RegisterSystem(new SaveSystem());
        
        // 注册 Utility
        RegisterUtility(new StorageUtility());
        RegisterUtility(new TimeUtility());
    }
}

// 2. 在其他地方使用架构
public class GameController : IController
{
    private IArchitecture _architecture;
    
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public void Start()
    {
        // 获取 Model
        var playerModel = this.GetModel<PlayerModel>();
        
        // 发送命令
        this.SendCommand<StartGameCommand>();
        
        // 发送查询
        var score = this.SendQuery(new GetScoreQuery());
        
        // 注册事件
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        // 处理玩家死亡事件
    }
}
```

**高级特性：**

```csharp
// 动态扩展架构（补丁系统）
Architecture<GameArchitecture>.OnRegisterPatch += arch =>
{
    // 在架构初始化完成前注入额外逻辑
    arch.RegisterSystem(new DebugSystem());
};

// 在运行时动态注册组件（初始化后）
var newSystem = new DynamicSystem();
GameArchitecture.Interface.RegisterSystem(newSystem);
// newSystem.Init() 会被立即调用
```

## 设计模式

### 单例模式

通过泛型约束和 `Lazy<T>` 实现类型安全的单例。

### 依赖注入（IoC）

使用内置 IoC 容器管理组件生命周期和依赖关系。

### 命令模式

通过 [`ICommand`](../command/ICommand.cs) 封装所有用户操作。

### 查询模式（CQRS）

通过 [`IQuery<T>`](../query/IQuery.cs) 分离查询和命令操作。

### 观察者模式

通过事件系统实现组件间的松耦合通信。

## 最佳实践

1. **保持架构类简洁**：只在 `Init()` 中注册组件，不要包含业务逻辑
2. **合理划分职责**：
    - Model：数据和状态
    - System：业务逻辑和规则
    - Utility：无状态的工具方法
3. **使用接口访问**：通过 `Interface` 属性访问架构，便于测试
4. **事件命名规范**：使用过去式命名事件类，如 `PlayerDiedEvent`
5. **避免循环依赖**：System 不应直接引用 System，应通过事件通信

## 相关包

- [`command`](../command/README.md) - 命令模式实现
- [`query`](../query/README.md) - 查询模式实现
- [`events`](../events/README.md) - 事件系统
- [`ioc`](../ioc/README.md) - IoC 容器
- [`model`](../model/README.md) - 数据模型
- [`system`](../system/README.md) - 业务系统
- [`utility`](../utility/README.md) - 工具类

---

**许可证**: Apache 2.0