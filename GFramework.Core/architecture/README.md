# Architecture 包使用说明

## 概述

Architecture 包是整个框架的核心，提供了基于 MVC 架构模式的应用程序架构基础。它实现了依赖注入（IoC）容器、组件生命周期管理，以及命令、查询、事件的统一调度机制。

**注意**：本框架的 Core 模块与 Godot 解耦，Godot 相关集成在 GFramework.Godot 包中实现。

## 核心接口

### IArchitecture

架构接口，定义了框架的核心功能契约。

**主要职责：**

- 组件注册：注册 System、Model、Utility
- 组件获取：从容器中获取已注册的组件
- 命令处理：发送并执行命令
- 查询处理：发送并执行查询
- 事件管理：发送、注册、注销事件

**核心方法：**

```csharp
// 组件注册
void RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem;
void RegisterModel<TModel>(TModel model) where TModel : IModel;
void RegisterUtility<TUtility>(TUtility utility) where TUtility : IUtility;

// 组件获取（通过容器）
T GetModel<T>() where T : class, IModel;
T GetSystem<T>() where T : class, ISystem;
T GetUtility<T>() where T : class, IUtility;

// 命令处理
void SendCommand(ICommand command);
TResult SendCommand<TResult>(ICommand<TResult> command);

// 查询处理
TResult SendQuery<TResult>(IQuery<TResult> query);

// 事件管理
void SendEvent<T>() where T : new();
void SendEvent<T>(T e);
IUnRegister RegisterEvent<T>(Action<T> onEvent);
void UnRegisterEvent<T>(Action<T> onEvent);
```

### IArchitecturePhaseAware

架构阶段感知接口，允许组件监听架构阶段变化。

**核心方法：**

```csharp
void OnArchitecturePhase(ArchitecturePhase phase);
```

### IArchitectureModule

架构模块接口，支持模块化架构扩展。

**核心方法：**

```csharp
void Install(IArchitecture architecture);
```

### IAsyncInitializable

异步初始化接口，支持组件异步初始化。

**核心方法：**

```csharp
Task InitializeAsync();
```

## 核心类

### [`Architecture`](Architecture.cs)

架构基类，实现了 `IArchitecture` 接口，提供完整的架构功能实现。

**构造函数参数：**

```csharp
public abstract class Architecture(
    IArchitectureConfiguration? configuration = null,
    IEnvironment? environment = null,
    IArchitectureServices? services = null,
    IArchitectureContext? context = null
)
```

**特性：**

- **阶段式生命周期管理**
  ：支持多个架构阶段（BeforeUtilityInit、AfterUtilityInit、BeforeModelInit、AfterModelInit、BeforeSystemInit、AfterSystemInit、Ready、Destroying、Destroyed）
- **模块安装系统**：支持通过 `InstallModule` 扩展架构功能
- **异步初始化**：支持同步和异步两种初始化方式
- **IoC 容器集成**：内置依赖注入容器
- **事件系统集成**：集成类型化事件系统
- **与平台无关**：Core 模块不依赖 Godot，可以在任何 .NET 环境中使用

**架构阶段：**

```csharp
public enum ArchitecturePhase
{
    None = 0,                    // 初始阶段
    BeforeUtilityInit = 1,       // 工具初始化前
    AfterUtilityInit = 2,        // 工具初始化后
    BeforeModelInit = 3,         // 模型初始化前
    AfterModelInit = 4,          // 模型初始化后
    BeforeSystemInit = 5,        // 系统初始化前
    AfterSystemInit = 6,         // 系统初始化后
    Ready = 7,                   // 就绪状态
    FailedInitialization = 8,    // 初始化失败
    Destroying = 9,              // 正在销毁
    Destroyed = 10               // 已销毁
}
```

**初始化流程：**

1. 创建架构实例（传入配置或使用默认配置）
2. 调用用户自定义的 `Init()` 方法
3. 初始化上下文工具（Context Utility）
4. 初始化所有注册的 Model
5. 初始化所有注册的 System
6. 冻结 IOC 容器
7. 进入 Ready 阶段

**销毁流程：**

1. 进入 Destroying 阶段
2. 发送 `ArchitectureDestroyingEvent` 事件
3. 销毁所有 System
4. 进入 Destroyed 阶段
5. 发送 `ArchitectureDestroyedEvent` 事件

**使用示例：**

``csharp
// 1. 定义你的架构（继承 Architecture 基类）
public class GameArchitecture : Architecture
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

// 2. 创建并初始化架构
var architecture = new GameArchitecture();
architecture.Initialize();

// 或者异步初始化
// var architecture = new GameArchitecture();
// await architecture.InitializeAsync();

// 3. 通过依赖注入使用架构
// 在 Controller 或其他组件中注入架构实例
public class GameController : IController
{
    private readonly IArchitecture _architecture;
    
    // 通过构造函数注入架构
    public GameController(IArchitecture architecture)
    {
        _architecture = architecture;
    }
    
    public void Start()
    {
        // 获取 Model
        var playerModel = _architecture.GetModel<PlayerModel>();
        
        // 发送命令
        _architecture.SendCommand(new StartGameCommand());
        
        // 发送查询
        var score = _architecture.SendQuery(new GetScoreQuery());
        
        // 注册事件
        _architecture.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        // 处理玩家死亡事件
    }
}
```

**核心方法与属性：**

### 初始化方法

#### `Initialize()`

同步初始化方法，阻塞当前线程直到初始化完成。

```csharp
public void Initialize()
```

**使用示例：**

```csharp
var architecture = new GameArchitecture();
architecture.Initialize(); // 阻塞等待初始化完成
```

**异常处理：**

如果初始化过程中发生异常，架构会进入 `FailedInitialization` 阶段并发送 `ArchitectureFailedInitializationEvent` 事件。

#### `InitializeAsync()`

异步初始化方法，返回 Task 以便调用者可以等待初始化完成。

```csharp
public async Task InitializeAsync()
```

**使用示例：**

```csharp
var architecture = new GameArchitecture();
await architecture.InitializeAsync(); // 异步等待初始化完成
```

**优势：**
- 支持异步初始化 Model 和 System
- 可以利用异步 I/O 操作（如异步加载数据）
- 提高初始化性能

### 模块管理

#### `InstallModule(IArchitectureModule module)`

安装架构模块，用于扩展架构功能。

```csharp
public void InstallModule(IArchitectureModule module)
```

**参数：**
- `module`：要安装的模块实例

**使用示例：**

```csharp
// 定义模块
public class NetworkModule : IArchitectureModule
{
    public void Install(IArchitecture architecture)
    {
        // 注册网络相关的 System 和 Utility
        architecture.RegisterSystem(new NetworkSystem());
        architecture.RegisterUtility(new HttpClientUtility());
    }
}

// 安装模块
var architecture = new GameArchitecture();
architecture.InstallModule(new NetworkModule());
```

### 生命周期钩子

#### `RegisterLifecycleHook(IArchitectureLifecycle hook)`

注册生命周期钩子，用于在架构阶段变化时执行自定义逻辑。

```csharp
public void RegisterLifecycleHook(IArchitectureLifecycle hook)
```

**参数：**
- `hook`：生命周期钩子实例

**使用示例：**

```csharp
// 定义生命周期钩子
public class PerformanceMonitorHook : IArchitectureLifecycle
{
    public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        switch (phase)
        {
            case ArchitecturePhase.BeforeModelInit:
                Console.WriteLine("开始监控 Model 初始化性能");
                break;
            case ArchitecturePhase.AfterModelInit:
                Console.WriteLine("Model 初始化完成，停止监控");
                break;
            case ArchitecturePhase.Ready:
                Console.WriteLine("架构就绪，开始性能统计");
                break;
        }
    }
}

// 注册生命周期钩子
var architecture = new GameArchitecture();
architecture.RegisterLifecycleHook(new PerformanceMonitorHook());
architecture.Initialize();
```

**注意：** 生命周期钩子只能在架构进入 Ready 阶段之前注册。

### 属性

#### `CurrentPhase`

获取当前架构的阶段。

```csharp
public ArchitecturePhase CurrentPhase { get; }
```

**使用示例：**

```csharp
var architecture = new GameArchitecture();

// 检查架构是否已就绪
if (architecture.CurrentPhase == ArchitecturePhase.Ready)
{
    Console.WriteLine("架构已就绪，可以开始游戏");
}

// 在异步操作中检查阶段变化
await Task.Run(async () =>
{
    while (architecture.CurrentPhase != ArchitecturePhase.Ready)
    {
        Console.WriteLine($"当前阶段: {architecture.CurrentPhase}");
        await Task.Delay(100);
    }
});
```

#### `Context`

获取架构上下文，提供对架构服务的访问。

```csharp
public IArchitectureContext Context { get; }
```

**使用示例：**

```csharp
// 通过 Context 访问服务
var context = architecture.Context;
var eventBus = context.EventBus;
var commandBus = context.CommandBus;
var queryBus = context.QueryBus;
var environment = context.Environment;
```

**高级特性：**

``csharp
// 1. 使用自定义配置
var config = new ArchitectureConfiguration();
var architecture = new GameArchitecture(configuration: config);

// 2. 模块安装
var module = new GameModule();
architecture.InstallModule(module);

// 3. 监听架构阶段变化
public class GamePhaseListener : IArchitecturePhaseAware
{
    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
        switch (phase)
        {
            case ArchitecturePhase.Ready:
                GD.Print("架构已就绪，可以开始游戏了");
                break;
            case ArchitecturePhase.Destroying:
                GD.Print("架构正在销毁");
                break;
        }
    }
}

// 4. 生命周期钩子
public class LifecycleHook : IArchitectureLifecycle
{
    public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        GD.Print($"架构阶段变化: {phase}");
    }
}
```

### [`ArchitectureConfiguration`](ArchitectureConfiguration.cs)

架构配置类，用于配置架构的行为。

**使用示例：**

``csharp
var config = new ArchitectureConfiguration
{
    // 严格阶段验证
    StrictPhaseValidation = true,
    // 允许延迟注册
    AllowLateRegistration = false
};

var architecture = new GameArchitecture(configuration: config);
```

### [`ArchitectureServices`](ArchitectureServices.cs)

架构服务类，管理命令总线、查询总线、IOC容器和类型事件系统。

### [`ArchitectureContext`](ArchitectureContext.cs)

架构上下文类，提供对架构服务的访问。

### [`GameContext`](GameContext.cs)

游戏上下文类，管理架构上下文与类型的绑定关系。

## 设计模式

### 1. 依赖注入

通过构造函数注入或容器解析获取架构实例。

### 2. 依赖注入（IoC）

使用内置 IoC 容器管理组件生命周期和依赖关系。

### 3. 命令模式

通过 `ICommand` 封装所有用户操作。

### 4. 查询模式（CQRS）

通过 `IQuery<T>` 分离查询和命令操作。

### 5. 观察者模式

通过事件系统实现组件间的松耦合通信。

### 6. 阶段式生命周期管理

通过 `ArchitecturePhase` 枚举和生命周期钩子管理架构状态。

### 7. 组合优于继承

通过接口组合获得不同能力，而不是深层继承链。

## 最佳实践

1. **保持架构类简洁**：只在 `Init()` 中注册组件，不要包含业务逻辑
2. **合理划分职责**：
   - Model：数据和状态
   - System：业务逻辑和规则
   - Utility：无状态的工具方法
3. **使用依赖注入**：通过构造函数注入架构实例，便于测试
4. **事件命名规范**：使用过去式命名事件类，如 `PlayerDiedEvent`
5. **避免循环依赖**：System 不应直接引用 System，应通过事件通信
6. **使用模块扩展**：通过 `IArchitectureModule` 实现架构的可扩展性
7. **Core 模块与平台解耦**：GFramework.Core 不包含 Godot 相关代码，Godot 集成在单独模块中

## 相关包

- [`command`](../command/README.md) - 命令模式实现
- [`query`](../query/README.md) - 查询模式实现
- [`events`](../events/README.md) - 事件系统
- [`ioc`](../ioc/README.md) - IoC 容器
- [`model`](../model/README.md) - 数据模型
- [`system`](../system/README.md) - 业务系统
- [`utility`](../utility/README.md) - 工具类
- **GFramework.Godot** - Godot 特定集成（GodotNode 扩展、GodotLogger 等）

---

**许可证**: Apache 2.0
