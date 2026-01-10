# GFramework.Core.Abstractions 抽象层

> GFramework 框架的接口定义模块，提供所有核心组件的抽象契约

## 概述

GFramework.Core.Abstractions 是 GFramework 框架的抽象层定义模块，包含了框架中所有核心组件的接口（Interface）、枚举（Enum）和配置类（Class）。该模块采用
`netstandard2.0` 作为目标框架，确保了广泛的兼容性和可移植性。

本模块遵循以下设计原则：

- **接口隔离**：每个接口职责单一，便于实现和测试
- **依赖倒置**：上层模块依赖抽象接口，而非具体实现
- **组合优于继承**：通过接口组合获得能力，而非通过继承获得
- **类型安全**：充分利用泛型系统确保类型安全

本模块的包 ID 为 `GeWuYou.GFramework.Core.Abstractions`，遵循命名空间 `GFramework.Core.Abstractions` 下的所有定义。

## 目录结构

```
GFramework.Core.Abstractions/
├── architecture/         # 架构核心接口
│   ├── IArchitecture.cs
│   ├── IArchitectureConfiguration.cs
│   ├── IArchitectureContext.cs
│   ├── IArchitectureLifecycle.cs
│   ├── IArchitectureModule.cs
│   ├── IArchitecturePhaseAware.cs
│   ├── IArchitectureServices.cs
│   └── IAsyncInitializable.cs
├── command/              # 命令模式接口
│   ├── ICommand.cs
│   ├── ICommandBus.cs
│   └── ICommandInput.cs
├── controller/           # 控制器接口
│   └── IController.cs
├── enums/                # 枚举定义
│   └── ArchitecturePhase.cs
├── environment/          # 环境接口
│   └── IEnvironment.cs
├── events/               # 事件系统接口
│   ├── IEasyEvent.cs
│   ├── ITypeEventSystem.cs
│   ├── IUnRegister.cs
│   └── IUnRegisterList.cs
├── ioc/                  # 依赖注入容器接口
│   └── IIocContainer.cs
├── logging/              # 日志系统接口
│   ├── ILogger.cs
│   ├── ILoggerFactory.cs
│   ├── ILoggerFactoryProvider.cs
│   └── LogLevel.cs
├── model/                # 模型接口
│   └── IModel.cs
├── properties/           # 配置类
│   ├── ArchitectureProperties.cs
│   └── LoggerProperties.cs
├── property/             # 可绑定属性接口
│   ├── IBindableProperty.cs
│   └── IReadonlyBindableProperty.cs
├── query/                # 查询模式接口
│   ├── IQuery.cs
│   ├── IQueryBus.cs
│   └── IQueryInput.cs
├── rule/                 # 规则接口
│   ├── IContextAware.cs
│   └── ILogAware.cs
├── system/               # 系统接口
│   └── ISystem.cs
└── utility/              # 工具接口
    ├── IContextUtility.cs
    └── IUtility.cs
```

## 模块说明

### 1. architecture（架构核心）

架构模块是整个框架的核心，定义了应用架构的生命周期管理和组件注册机制。该模块包含以下接口：

#### [`IArchitecture`](architecture/IArchitecture.cs)

架构接口是整个应用的核心管理器，负责管理系统、模型和工具类的注册与获取。它继承自 [
`IAsyncInitializable`](architecture/IAsyncInitializable.cs) 接口，支持异步初始化。该接口提供了注册系统（[
`RegisterSystem<T>`](architecture/IArchitecture.cs#L39)）、注册模型（[
`RegisterModel<T>`](architecture/IArchitecture.cs#L46)）、注册工具（[
`RegisterUtility<T>`](architecture/IArchitecture.cs#L53)）的方法，以及安装模块（[
`InstallModule`](architecture/IArchitecture.cs#L59)）和注册生命周期钩子（[
`RegisterLifecycleHook`](architecture/IArchitecture.cs#L65)）的功能。

#### [`IArchitectureContext`](architecture/IArchitectureContext.cs)

架构上下文接口提供了对已注册组件的访问能力，是组件之间通信的桥梁。通过该接口，可以获取系统（[
`GetSystem<TSystem>`](architecture/IArchitectureContext.cs#L22)）、模型（[
`GetModel<TModel>`](architecture/IArchitectureContext.cs#L29)）和工具（[
`GetUtility<TUtility>`](architecture/IArchitectureContext.cs#L36)）实例。同时，该接口还支持发送命令（[
`SendCommand`](architecture/IArchitectureContext.cs#L42)）、发送查询（[
`SendQuery`](architecture/IArchitectureContext.cs#L58)）和发送事件（[
`SendEvent`](architecture/IArchitectureContext.cs#L64)）等横切操作。

#### [`IArchitectureConfiguration`](architecture/IArchitectureConfiguration.cs)

架构配置接口定义了框架的配置选项，包括日志配置（[`LoggerProperties`](architecture/IArchitectureConfiguration.cs#L13)
）和架构配置（[`ArchitectureProperties`](architecture/IArchitectureConfiguration.cs#L18)
）。通过该接口，可以在运行时调整框架的行为，如设置日志级别、启用延迟注册等。

#### [`IArchitectureLifecycle`](architecture/IArchitectureLifecycle.cs)

架构生命周期接口定义了架构在不同阶段的回调方法。当架构进入特定阶段时，会通知所有注册的生命周期监听器。该接口主要用于模块化架构的阶段感知。

#### [`IArchitectureModule`](architecture/IArchitectureModule.cs)

架构模块接口继承自 [`IArchitectureLifecycle`](architecture/IArchitectureLifecycle.cs) 和 [
`IArchitecturePhaseAware`](architecture/IArchitecturePhaseAware.cs) 接口，定义了模块安装到架构的标准方法（[
`Install`](architecture/IArchitectureModule.cs#L13)）。通过模块化机制，可以将复杂功能封装为可插拔的模块。

#### [`IArchitecturePhaseAware`](architecture/IArchitecturePhaseAware.cs)

架构阶段感知接口允许组件在架构的不同阶段执行相应的逻辑。该接口提供了 [
`OnArchitecturePhase`](architecture/IArchitecturePhaseAware.cs#L14) 方法，当架构进入指定阶段时会被调用。

#### [`IArchitectureServices`](architecture/IArchitectureServices.cs)

架构服务接口定义了框架核心服务组件，继承自 [`IContextAware`](rule/IContextAware.cs) 接口。该接口提供了依赖注入容器（[
`Container`](architecture/IArchitectureServices.cs#L18)）、类型事件系统（[
`TypeEventSystem`](architecture/IArchitectureServices.cs#L24)）、命令总线（[
`CommandBus`](architecture/IArchitectureServices.cs#L29)）和查询总线（[
`QueryBus`](architecture/IArchitectureServices.cs#L34)）的访问能力。

#### [`IAsyncInitializable`](architecture/IAsyncInitializable.cs)

异步初始化接口定义了组件的异步初始化方法（[`InitializeAsync`](architecture/IAsyncInitializable.cs#L14)
）。该接口用于需要执行异步初始化操作的组件，如加载资源、建立网络连接等。

### 2. command（命令模式）

命令模块实现了命令查询职责分离模式（CQRS）中的命令部分，用于封装写操作。该模块包含以下接口：

#### [`ICommand`](command/ICommand.cs)

命令接口定义了无返回值命令的基本契约，继承自 [`IContextAware`](rule/IContextAware.cs) 接口。该接口提供了命令执行方法（[
`Execute`](command/ICommand.cs#L15)），用于执行具体的业务逻辑。带返回值的命令由泛型接口 [
`ICommand<TResult>`](command/ICommand.cs#L23) 定义，同样继承自 [`IContextAware`](rule/IContextAware.cs) 接口。

#### [`ICommandBus`](command/ICommandBus.cs)

命令总线接口负责命令的发送和执行调度。该接口提供了发送无返回值命令（[`Send`](command/ICommandBus.cs#L12)
）和发送带返回值命令（[`Send<TResult>`](command/ICommandBus.cs#L20)）的方法。

#### [`ICommandInput`](command/ICommandInput.cs)

命令输入接口是命令模式中输入数据的标记接口，不包含任何成员定义。该接口用于规范化命令的输入参数类型。

### 3. controller（控制器）

控制器模块定义了表现层与业务逻辑层之间的桥梁接口。

#### [`IController`](controller/IController.cs)

控制器接口是 MVC 架构中控制层的抽象定义。该接口作为标记接口使用，不包含任何方法定义，但实现该接口的类通常会获得访问架构上下文的能力，从而可以发送命令、查询数据、注册事件等。

### 4. enums（枚举定义）

枚举模块定义了框架中使用的枚举类型。

#### [`ArchitecturePhase`](enums/ArchitecturePhase.cs)

架构阶段枚举定义了系统架构初始化和运行过程中的各个关键阶段。按照初始化流程，依次包括：`None`（无效阶段）、`BeforeUtilityInit`
（工具类初始化之前）、`AfterUtilityInit`（工具类初始化之后）、`BeforeModelInit`（模型初始化之前）、`AfterModelInit`（模型初始化之后）、
`BeforeSystemInit`（系统初始化之前）、`AfterSystemInit`（系统初始化之后）、`Ready`（就绪阶段），以及销毁相关阶段：`Destroying`
（正在销毁中）、`Destroyed`（已销毁）、`FailedInitialization`（初始化失败）。

### 5. environment（环境接口）

环境模块定义了应用程序运行环境的抽象接口。

#### [`IEnvironment`](environment/IEnvironment.cs)

环境接口提供了获取应用程序运行环境相关信息的能力。该接口支持根据键值获取配置值（[
`Get<T>`](environment/IEnvironment.cs#L20)）、尝试获取环境值（[`TryGet`](environment/IEnvironment.cs#L29)
）、获取必需的环境值（[`GetRequired<T>`](environment/IEnvironment.cs#L37)），以及注册键值对（[
`Register`](environment/IEnvironment.cs#L44)）和初始化环境（[`Initialize`](environment/IEnvironment.cs#L49)）。

### 6. events（事件系统）

事件模块实现了框架的事件驱动通信机制，支持类型安全和松耦合的组件通信。

#### [`ITypeEventSystem`](events/ITypeEventSystem.cs)

类型事件系统接口是基于类型的事件发布-订阅机制的抽象定义。该接口支持发送无参事件（[
`Send<T>`](events/ITypeEventSystem.cs#L14)）、发送带参事件（[`Send<T>`](events/ITypeEventSystem.cs#L21)）、注册事件监听器（[
`Register<T>`](events/ITypeEventSystem.cs#L29)）和注销事件监听器（[`UnRegister<T>`](events/ITypeEventSystem.cs#L36)）。

#### [`IEasyEvent`](events/IEasyEvent.cs)

简单事件接口定义了基础的事件注册功能（[`Register`](events/IEasyEvent.cs#L15)）。该接口用于简单的无参事件场景。

#### [`IUnRegister`](events/IUnRegister.cs)

注销接口提供了事件监听器的注销功能（[`UnRegister`](events/IUnRegister.cs#L11)）。所有事件注册方法的返回值都实现了该接口，用于在适当时机取消事件监听。

#### [`IUnRegisterList`](events/IUnRegisterList.cs)

统一注销接口提供了管理多个注销句柄的能力。该接口维护了一个注销对象列表（[`UnregisterList`](events/IUnRegisterList.cs#L13)
），可以在批量注销时统一处理。

### 7. ioc（依赖注入）

IoC 模块实现了控制反转和依赖注入机制，用于管理组件的生命周期和依赖关系。

#### [`IIocContainer`](ioc/IIocContainer.cs)

依赖注入容器接口是框架的核心服务接口之一，继承自 [`IContextAware`](rule/IContextAware.cs)
接口。该接口提供了丰富的服务注册和解析方法，包括注册单例（[`RegisterSingleton<T>`](ioc/IIocContainer.cs#L22)
）、注册多个实例（[`RegisterPlurality`](ioc/IIocContainer.cs#L30)）、注册系统（[`RegisterSystem`](ioc/IIocContainer.cs#L36)
）、注册类型（[`Register`](ioc/IIocContainer.cs#L43)）等。在解析方面，支持获取单个实例（[`Get<T>`](ioc/IIocContainer.cs#L62)
）、获取必需实例（[`GetRequired<T>`](ioc/IIocContainer.cs#L70)）、获取所有实例（[`GetAll<T>`](ioc/IIocContainer.cs#L77)
）和获取排序后的实例（[`GetAllSorted<T>`](ioc/IIocContainer.cs#L85)）。此外，还提供了检查（[
`Contains`](ioc/IIocContainer.cs#L96)）、清空（[`Clear`](ioc/IIocContainer.cs#L108)）和冻结（[
`Freeze`](ioc/IIocContainer.cs#L113)）等实用方法。

### 8. logging（日志系统）

日志模块提供了完整的日志记录抽象，支持多级别日志输出。

#### [`LogLevel`](logging/LogLevel.cs)

日志级别枚举定义了日志消息的严重程度等级，包括：`Trace`（跟踪级别，用于详细的程序执行流程信息）、`Debug`（调试级别，用于调试过程中的详细信息）、
`Info`（信息级别，用于一般性的程序运行信息）、`Warning`（警告级别，用于表示可能的问题或异常情况）、`Error`
（错误级别，用于表示错误但程序仍可继续运行的情况）、`Fatal`（致命级别，用于表示严重的错误导致程序无法继续运行）。

#### [`ILogger`](logging/ILogger.cs)

日志记录器接口是框架日志系统的核心接口，提供了完整的日志记录能力。该接口支持以下功能：

- **级别启用检查**：提供了各个日志级别的是否启用检查方法，如 [`IsTraceEnabled`](logging/ILogger.cs#L22)、[
  `IsDebugEnabled`](logging/ILogger.cs#L28)、[`IsInfoEnabled`](logging/ILogger.cs#L34)、[
  `IsWarnEnabled`](logging/ILogger.cs#L40)、[`IsErrorEnabled`](logging/ILogger.cs#L46)、[
  `IsFatalEnabled`](logging/ILogger.cs#L52)，以及通用的 [`IsEnabledForLevel`](logging/ILogger.cs#L59) 方法。
- **日志记录方法**：每个日志级别都有多种重载形式，支持简单消息、格式化消息和异常记录。格式化为 `Trace`、`Debug`、`Info`、
  `Warn`、`Error`、`Fatal` 六个级别，每个级别都提供了 `msg`、`format+arg`、`format+arg1+arg2`、`format+params`、`msg+exception`
  等多种调用方式。
- **获取日志记录器名称**：通过 [`Name`](logging/ILogger.cs#L14) 方法获取日志记录器的名称。

#### [`ILoggerFactory`](logging/ILoggerFactory.cs)

日志工厂接口用于创建日志记录器实例（[`GetLogger`](logging/ILoggerFactory.cs#L14)）。该接口支持指定日志记录器名称和最小日志级别。

#### [`ILoggerFactoryProvider`](logging/ILoggerFactoryProvider.cs)

日志工厂提供者接口扩展了日志工厂的功能，支持动态设置最小日志级别（[`MinLevel`](logging/ILoggerFactoryProvider.cs#L11)
）和创建日志记录器（[`CreateLogger`](logging/ILoggerFactoryProvider.cs#L18)）。

### 9. model（模型接口）

模型模块定义了数据层的抽象接口。

#### [`IModel`](model/IModel.cs)

模型接口继承自 [`IContextAware`](rule/IContextAware.cs) 和 [
`IArchitecturePhaseAware`](architecture/IArchitecturePhaseAware.cs)
接口，定义了模型组件的基本行为。该接口提供了模型初始化方法（[`Init`](model/IModel.cs#L14)），用于执行模型相关的初始化逻辑。

### 10. properties（配置类）

配置模块定义了框架使用的配置选项类。

#### [`LoggerProperties`](properties/LoggerProperties.cs)

日志配置选项类用于配置日志系统的相关参数，包含日志工厂提供程序属性（[
`LoggerFactoryProvider`](properties/LoggerProperties.cs#L14)）。

#### [`ArchitectureProperties`](properties/ArchitectureProperties.cs)

架构选项配置类用于定义架构行为的相关配置选项，包含两个属性：`AllowLateRegistration`（允许延迟注册开关，控制是否允许在初始化完成后进行组件注册）和
`StrictPhaseValidation`（严格阶段验证开关，控制是否启用严格的阶段验证机制）。

### 11. property（可绑定属性）

可绑定属性模块实现了响应式数据绑定机制，支持数据变化的自动通知。

#### [`IReadonlyBindableProperty<T>`](property/IReadonlyBindableProperty.cs)

只读可绑定属性接口继承自 [`IEasyEvent`](events/IEasyEvent.cs)
接口，提供了属性值的读取和变更监听功能。该接口支持获取当前值（[`Value`](property/IReadonlyBindableProperty.cs#L15)
）、注册带初始值的回调（[`RegisterWithInitValue`](property/IReadonlyBindableProperty.cs#L22)）、取消注册回调（[
`UnRegister`](property/IReadonlyBindableProperty.cs#L28)）和注册回调（[
`Register`](property/IReadonlyBindableProperty.cs#L35)）。

#### [`IBindableProperty<T>`](property/IBindableProperty.cs)

可绑定属性接口继承自只读可绑定属性接口，提供了可读写的属性绑定功能。该接口在只读接口的基础上增加了属性值的设置能力（[
`Value`](property/IBindableProperty.cs#L12) 的 setter）以及不触发事件的设置方法（[
`SetValueWithoutEvent`](property/IBindableProperty.cs#L18)）。

### 12. query（查询模式）

查询模块实现了命令查询职责分离模式（CQRS）中的查询部分，用于封装读操作。

#### [`IQuery<TResult>`](query/IQuery.cs)

查询接口继承自 [`IContextAware`](rule/IContextAware.cs) 接口，定义了执行查询操作的契约。该接口提供了查询执行方法（[
`Do`](query/IQuery.cs#L15)），返回指定类型的结果。

#### [`IQueryBus`](query/IQueryBus.cs)

查询总线接口负责查询的发送和执行调度。该接口提供了发送查询并返回结果的方法（[`Send<TResult>`](query/IQueryBus.cs#L14)）。

#### [`IQueryInput`](query/IQueryInput.cs)

查询输入接口是查询模式中输入数据的标记接口，不包含任何成员定义。

### 13. rule（规则接口）

规则模块定义了框架组件需要遵循的约束和规则接口。

#### [`IContextAware`](rule/IContextAware.cs)

上下文感知接口允许实现类设置和获取架构上下文。该接口提供了设置上下文（[`SetContext`](rule/IContextAware.cs#L14)
）和获取上下文（[`GetContext`](rule/IContextAware.cs#L20)）的方法。框架中大多数核心组件（如命令、查询、系统、模型、工具、IoC
容器等）都实现了该接口，以获得访问架构服务的能力。

#### [`ILogAware`](rule/ILogAware.cs)

日志感知接口允许实现类设置和使用日志记录器。该接口提供了设置日志记录器的方法（[`SetLogger`](rule/ILogAware.cs#L14)）。

### 14. system（系统接口）

系统模块定义了业务逻辑层的抽象接口。

#### [`ISystem`](system/ISystem.cs)

系统接口继承自 [`IContextAware`](rule/IContextAware.cs) 和 [
`IArchitecturePhaseAware`](architecture/IArchitecturePhaseAware.cs)
接口，定义了系统组件的基本行为。该接口提供了系统初始化方法（[`Init`](system/ISystem.cs#L16)）和系统销毁方法（[
`Destroy`](system/ISystem.cs#L22)），用于管理系统的生命周期。

### 15. utility（工具接口）

工具模块定义了无状态工具类的抽象接口。

#### [`IUtility`](utility/IUtility.cs)

工具接口是所有工具类实现的基础接口，作为标记接口使用，不包含任何成员定义。该接口定义了通用工具类的基本契约。

#### [`IContextUtility`](utility/IContextUtility.cs)

上下文工具接口继承自 [`IUtility`](utility/IUtility.cs) 和 [`IContextAware`](rule/IContextAware.cs)
接口，提供了具有上下文感知能力的工具功能。该接口在工具接口的基础上增加了初始化方法（[
`Init`](utility/IContextUtility.cs#L14)）。

## 接口继承关系图

```
IArchitecture
    └── IAsyncInitializable

IArchitectureModule
    ├── IArchitectureLifecycle
    └── IArchitecturePhaseAware

IArchitectureServices
    └── IContextAware

ICommand
    └── IContextAware

ICommand<TResult>
    └── IContextAware

IIocContainer
    └── IContextAware

IQuery<TResult>
    └── IContextAware

IModel
    ├── IContextAware
    └── IArchitecturePhaseAware

ISystem
    ├── IContextAware
    └── IArchitecturePhaseAware

IContextUtility
    ├── IUtility
    └── IContextAware

IReadonlyBindableProperty<T>
    └── IEasyEvent

IBindableProperty<T>
    └── IReadonlyBindableProperty<T>
```

## 核心能力接口

框架中的组件通过实现特定的接口来获得相应的能力。这些能力接口主要分为以下几类：

### 上下文感知能力

通过实现 [`IContextAware`](rule/IContextAware.cs) 接口，组件可以获得设置和获取架构上下文的能力，从而访问框架提供的各种服务。命令、查询、系统、模型、工具、IoC
容器等核心组件都实现了该接口。

### 日志能力

通过实现 [`ILogAware`](rule/ILogAware.cs) 接口，组件可以获得使用日志记录器的能力。该接口提供了设置日志记录器的方法，使组件可以输出日志信息。

### 阶段感知能力

通过实现 [`IArchitecturePhaseAware`](architecture/IArchitecturePhaseAware.cs)
接口，组件可以在架构的不同阶段执行相应的逻辑。该接口提供了 [
`OnArchitecturePhase`](architecture/IArchitecturePhaseAware.cs#L14) 方法，当架构进入指定阶段时会被调用。

## 使用指南

### 实现框架组件

当需要实现框架的组件时，通常需要实现相应的接口并遵循框架的生命周期约定：

```csharp
// 实现系统组件
public class CombatSystem : ISystem
{
    public void Init()
    {
        // 系统初始化逻辑
    }
    
    public void Destroy()
    {
        // 系统销毁逻辑
    }
}

// 实现模型组件
public class PlayerModel : IModel
{
    public void Init()
    {
        // 模型初始化逻辑
    }
}

// 实现工具组件
public class StorageUtility : IUtility
{
    // 工具类方法
}

// 实现命令
public class AttackCommand : ICommand
{
    public void Execute()
    {
        // 命令执行逻辑
    }
}

// 实现查询
public class GetPlayerInfoQuery : IQuery<PlayerInfo>
{
    public PlayerInfo Do()
    {
        // 查询执行逻辑
    }
}
```

### 访问框架服务

通过实现 [`IContextAware`](rule/IContextAware.cs) 接口，组件可以获得访问框架服务的能力：

```csharp
public class MySystem : ISystem
{
    private IArchitectureContext _context;
    
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }
    
    public IArchitectureContext GetContext()
    {
        return _context;
    }
    
    private void SomeMethod()
    {
        // 获取其他组件
        var playerModel = _context.GetModel<PlayerModel>();
        
        // 发送命令
        _context.SendCommand(new AttackCommand());
        
        // 发送查询
        var health = _context.SendQuery(new GetHealthQuery());
        
        // 发送事件
        _context.SendEvent(new GameEvent());
        
        // 注册事件监听
        _context.RegisterEvent<EnemySpawnedEvent>(OnEnemySpawned);
    }
}
```

## 相关文档

- [GFramework.Core](../GFramework.Core/README.md) - 核心框架实现模块
- [GFramework.Godot](../GFramework.Godot/README.md) - Godot 平台集成模块
- [架构模块文档](architecture/README.md) - 架构接口详细说明
- [控制器模块文档](controller/README.md) - 控制器使用说明

---

**版本**: 1.0.0

**许可证**: Apache 2.0
