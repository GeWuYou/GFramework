# Architecture 架构详解

> 深入了解 GFramework 的核心架构设计和实现

## 目录

- [概述](#概述)
- [架构设计](#架构设计)
- [生命周期管理](#生命周期管理)
- [组件注册](#组件注册)
- [模块系统](#模块系统)
- [最佳实践](#最佳实践)
- [API 参考](#api-参考)

## 概述

Architecture 是 GFramework 的核心类,负责管理整个应用的生命周期、组件注册和模块管理。从 v1.1.0 开始,Architecture
采用模块化设计,将职责分离到专门的协作者中。

> 命名约定:
> - `ArchitectureServices` 是公开的基础服务入口,负责容器、事件总线、命令执行器、查询执行器和服务模块管理
> - `ArchitectureComponentRegistry` 是内部组件注册器,专门负责 System / Model / Utility 的注册与生命周期接入
> - 两者不是同一层职责,不要混用

### 设计目标

- **单一职责**: 每个管理器只负责一个明确的功能
- **类型安全**: 基于泛型的组件获取和注册
- **生命周期管理**: 自动的初始化和销毁机制
- **可扩展性**: 支持模块和钩子扩展
- **向后兼容**: 保持公共 API 稳定

### 核心组件

```
Architecture (核心协调器)
    ├── ArchitectureBootstrapper (初始化基础设施编排)
    ├── ArchitectureLifecycle (生命周期管理)
    ├── ArchitectureComponentRegistry (组件注册)
    └── ArchitectureModules (模块管理)
```

## 架构设计

### 设计模式

Architecture 采用以下设计模式:

1. **组合模式 (Composition)**: Architecture 组合多个内部协作者
2. **委托模式 (Delegation)**: 方法调用委托给专门的管理器
3. **协调器模式 (Coordinator)**: Architecture 作为协调器统一对外接口

### 类图

```
┌─────────────────────────────────────────────────────┐
│                   Architecture                       │
│  - _bootstrapper: ArchitectureBootstrapper          │
│  - _lifecycle: ArchitectureLifecycle                │
│  - _componentRegistry: ArchitectureComponentRegistry│
│  - _modules: ArchitectureModules                    │
│  - _logger: ILogger                                 │
│                                                     │
│  + RegisterSystem<T>()                              │
│  + RegisterModel<T>()                               │
│  + RegisterUtility<T>()                             │
│  + InstallModule()                                  │
│  + InitializeAsync()                                │
│  + DestroyAsync()                                   │
│  + event PhaseChanged                               │
└─────────────────────────────────────────────────────┘
         │                    │                    │                    │
         │                    │                    │                    │
         ▼                    ▼                    ▼                    ▼
┌──────────────┐   ┌──────────────────┐   ┌──────────────┐   ┌──────────────┐
│ Bootstrapper │   │ Lifecycle        │   │ComponentReg. │   │   Modules    │
│              │   │                  │   │              │   │              │
│ - 环境初始化  │   │ - 阶段管理        │   │ - System 注册│   │ - 模块安装    │
│ - 服务准备    │   │ - 钩子管理        │   │ - Model 注册 │   │ - 行为注册    │
│ - 上下文绑定  │   │ - 组件初始化      │   │ - Utility 注册│   │              │
│ - 容器冻结    │   │ - 就绪/销毁协调   │   │ - 生命周期接入│   │              │
└──────────────┘   └──────────────────┘   └──────────────┘   └──────────────┘
```

### 构造函数初始化

从 v1.1.0 开始,所有管理器在构造函数中初始化:

```csharp
protected Architecture(
    IArchitectureConfiguration? configuration = null,
    IEnvironment? environment = null,
    IArchitectureServices? services = null,
    IArchitectureContext? context = null)
{
    var resolvedConfiguration = configuration ?? new ArchitectureConfiguration();
    var resolvedEnvironment = environment ?? new DefaultEnvironment();
    var resolvedServices = services ?? new ArchitectureServices();
    _context = context;

    // 初始化 Logger
    LoggerFactoryResolver.Provider = resolvedConfiguration.LoggerProperties.LoggerFactoryProvider;
    _logger = LoggerFactoryResolver.Provider.CreateLogger(GetType().Name);

    // 初始化协作者
    _bootstrapper = new ArchitectureBootstrapper(GetType(), resolvedEnvironment, resolvedServices, _logger);
    _lifecycle = new ArchitectureLifecycle(this, resolvedConfiguration, resolvedServices, _logger);
    _componentRegistry = new ArchitectureComponentRegistry(this, resolvedConfiguration, resolvedServices, _lifecycle, _logger);
    _modules = new ArchitectureModules(this, resolvedServices, _logger);
}
```

**优势**:

- 消除 `null!` 断言,提高代码安全性
- 对象在构造后立即可用
- 符合"构造即完整"原则
- 可以在 InitializeAsync 之前访问事件

## 生命周期管理

### 架构阶段

Architecture 定义了 11 个生命周期阶段:

| 阶段                     | 说明           | 触发时机             |
|------------------------|--------------|------------------|
| `None`                 | 初始状态         | 构造函数完成后          |
| `BeforeUtilityInit`    | Utility 初始化前 | 开始初始化 Utility    |
| `AfterUtilityInit`     | Utility 初始化后 | 所有 Utility 初始化完成 |
| `BeforeModelInit`      | Model 初始化前   | 开始初始化 Model      |
| `AfterModelInit`       | Model 初始化后   | 所有 Model 初始化完成   |
| `BeforeSystemInit`     | System 初始化前  | 开始初始化 System     |
| `AfterSystemInit`      | System 初始化后  | 所有 System 初始化完成  |
| `Ready`                | 就绪状态         | 所有组件初始化完成        |
| `Destroying`           | 销毁中          | 开始销毁             |
| `Destroyed`            | 已销毁          | 销毁完成             |
| `FailedInitialization` | 初始化失败        | 初始化过程中发生异常       |

### 阶段转换

```
正常流程:
None → BeforeUtilityInit → AfterUtilityInit → BeforeModelInit → AfterModelInit
     → BeforeSystemInit → AfterSystemInit → Ready → Destroying → Destroyed

异常流程:
Any → FailedInitialization
```

### 阶段事件

可以通过 `PhaseChanged` 事件监听阶段变化:

```csharp
public class MyArchitecture : Architecture
{
    protected override void OnInitialize()
    {
        // 监听阶段变化
        PhaseChanged += phase =>
        {
            Console.WriteLine($"Phase changed to: {phase}");
        };
    }
}
```

### 生命周期钩子

实现 `IArchitectureLifecycleHook` 接口可以在阶段变化时执行自定义逻辑:

```csharp
public class MyLifecycleHook : IArchitectureLifecycleHook
{
    public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        switch (phase)
        {
            case ArchitecturePhase.Ready:
                Console.WriteLine("Architecture is ready!");
                break;
            case ArchitecturePhase.Destroying:
                Console.WriteLine("Architecture is being destroyed!");
                break;
        }
    }
}

// 注册钩子
architecture.RegisterLifecycleHook(new MyLifecycleHook());
```

### 初始化流程

```
1. 创建 Architecture 实例
   └─> 构造函数初始化管理器

2. 调用 InitializeAsync() 或 Initialize()
   ├─> ArchitectureBootstrapper 准备基础设施
   │   ├─> 初始化环境 (Environment.Initialize())
   │   ├─> 注册内置服务模块
   │   ├─> 初始化架构上下文并绑定 GameContext
   │   ├─> 执行服务钩子
   │   └─> 初始化服务模块
   ├─> 调用 OnInitialize() (用户注册组件)
   ├─> 初始化所有组件
   │   ├─> BeforeUtilityInit → 初始化 Utility → AfterUtilityInit
   │   ├─> BeforeModelInit → 初始化 Model → AfterModelInit
   │   └─> BeforeSystemInit → 初始化 System → AfterSystemInit
   ├─> CompleteInitialization() 冻结 IoC 容器
   └─> 进入 Ready 阶段

3. 等待就绪 (可选)
   └─> await architecture.WaitUntilReadyAsync()
```

### 销毁流程

```
1. 调用 DestroyAsync() 或 Destroy()
   ├─> 检查当前阶段 (如果是 None 或已销毁则直接返回)
   ├─> 进入 Destroying 阶段
   ├─> 逆序销毁所有组件
   │   ├─> 优先调用 IAsyncDestroyable.DestroyAsync()
   │   └─> 否则调用 IDestroyable.Destroy()
   ├─> 销毁服务模块
   ├─> 进入 Destroyed 阶段
   └─> 清空 IoC 容器
```

---

**版本**: 1.1.0
**更新日期**: 2026-03-17
**相关文档**:

- [核心框架概述](./index.md)
