---
title: Core 抽象层
description: GFramework.Core.Abstractions 的契约边界、包关系与源码阅读重点。
---

# Core 抽象层

`GFramework.Core.Abstractions` 是 `Core` 运行时的契约包。

它负责定义架构、生命周期、事件、状态、资源、日志、配置、并发和持久化相关的接口、枚举和值对象，用来建立跨模块协作边界；
默认实现、基类、容器适配和运行时装配则在 `GFramework.Core` 中。

如果你要开箱即用地使用框架能力，应依赖 `GFramework.Core`；如果你在做扩展包、测试替身、工具层或多模块拆分，才单独消费本包。

## 什么时候单独依赖它

- 你在写插件、模块扩展或测试替身，只想依赖接口而不拉入默认运行时
- 你需要让多个程序集共享架构、状态、资源或日志契约
- 你希望把公共边界放进 `*.Abstractions`，而把具体实现留在应用层或宿主层

## 包关系

- 契约层：`GFramework.Core.Abstractions`
- 运行时实现：`GFramework.Core`
- 常见相邻契约：`GFramework.Cqrs.Abstractions`、`GFramework.Game.Abstractions`

## 契约地图

| 契约族 | 作用 |
| --- | --- |
| `Architectures/` `Lifecycle/` `Registries/` | `IArchitecture`、上下文、模块、服务模块、阶段监听、注册表与初始化 / 销毁生命周期契约 |
| `Bases/` `Controller/` `Model/` `Systems/` `Utility/` `Rule/` | 组件角色接口、优先级 / key 值对象、上下文感知边界 |
| `Command/` `Query/` `Cqrs/` | 旧版命令 / 查询执行器接口，以及与新版请求模型衔接的运行时契约 |
| `Events/` `Property/` `State/` `StateManagement/` | 事件总线、解绑对象、可绑定属性、状态机、Store / reducer / middleware 契约 |
| `Coroutine/` `Time/` `Pause/` `Concurrency/` | 协程状态、时间源、暂停栈、键控异步锁与统计对象 |
| `Resource/` `Pool/` `Logging/` `Localization/` | 资源句柄、对象池、日志、logger factory、本地化表与格式化契约 |
| `Configuration/` `Environment/` | 配置管理器、环境对象与运行时环境访问契约 |
| `Data/` `Serializer/` `Storage/` `Versioning/` | 数据装载、序列化、存储与版本化契约 |
| `Enums/` `Properties/` | 架构阶段枚举，以及架构 / logger 相关属性键 |

## 最小接入路径

### 1. 只面向契约编程

```csharp
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Logging;

public sealed class DiagnosticsFeature
{
    private readonly IArchitecture _architecture;
    private readonly ILogger _logger;

    public DiagnosticsFeature(IArchitecture architecture, ILogger logger)
    {
        _architecture = architecture;
        _logger = logger;
    }
}
```

### 2. 什么时候切到运行时包

下面这些需求都属于 `GFramework.Core` 的职责，而不是本包：

- 继承 `Architecture` 并完成默认初始化流程
- 使用 `ContextAwareBase`、`AbstractModel`、`AbstractSystem` 等默认基类
- 使用默认的 `CommandExecutor`、`QueryExecutor`、`BindableProperty<T>`、`StateMachine`
- 直接启用默认的 `Microsoft.Extensions.DependencyInjection` 容器适配或资源 / 协程 / 日志实现

## XML 阅读重点

如果你在做契约确认、采用设计或扩展适配，优先核对这些类型族的 XML 文档：

- 架构与模块入口：`IArchitecture`、`IArchitectureContext`、`IServiceModule`
- 运行时基础设施：`IIocContainer`、`ILogger`、`IResourceManager`、`IConfigurationManager`
- 状态与并发能力：`IStateMachine`、`IStore`、`IAsyncKeyLockManager`、`ITimeProvider`
- 迁移与组合边界：`ICommandExecutor`、`IQueryExecutor`，以及旧命名空间下作为 compatibility alias 暴露的 `ICqrsRuntime`

`GFramework.Core.Abstractions.Cqrs.ICqrsRuntime` 当前主要承担旧命名空间兼容入口的角色。编写新模块或新增请求处理逻辑时，
应直接引用 `GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime`，让 runtime seam 与 CQRS 请求契约保持一致。

## 契约族阅读入口

如果你要回到源码 XML 文档确认契约，请优先看下面这些族群：

| 契约族 | 代表类型 | 建议先确认什么 |
| --- | --- | --- |
| `Architectures/` | `IArchitecture`、`IArchitectureContext`、`IArchitectureServices`、`IServiceModule` | 架构上下文、服务访问面与模块安装 / 生命周期约束 |
| `Lifecycle/` `Registries/` | `ILifecycle`、`IAsyncInitializable`、`IRegistry<T, TR>`、`KeyValueRegistryBase<TKey, TValue>` | 初始化 / 销毁阶段和注册表抽象边界 |
| `Command/` `Query/` `Cqrs/` | `ICommandExecutor`、`IAsyncCommand<TResult>`、`IQueryExecutor`、`ICqrsRuntime` | 旧命令 / 查询接口，以及 CQRS runtime compatibility alias 的迁移边界 |
| `Events/` `Property/` | `IEventBus`、`IEventFilter<T>`、`IBindableProperty<T>`、`IReadonlyBindableProperty<T>` | 事件传播、过滤、解绑对象和属性订阅语义 |
| `State/` `StateManagement/` | `IStateMachine`、`IAsyncState`、`IStore<TState>`、`IStoreMiddleware<TState>` | 状态机契约与 Store 的 reducer / middleware / diagnostics 边界 |
| `Coroutine/` `Time/` `Pause/` `Concurrency/` | `IYieldInstruction`、`ICoroutineStatistics`、`ITimeProvider`、`IPauseStackManager`、`IAsyncKeyLockManager` | 调度模型、时间源、暂停栈和异步锁契约 |
| `Resource/` `Pool/` `Logging/` `Localization/` | `IResourceManager`、`IObjectPoolSystem`、`ILogger`、`IStructuredLogger`、`ILocalizationManager` | 资源 / 池化 / 日志 / 本地化这些基础设施的宿主责任 |
| `Configuration/` `Environment/` `Data/` `Serializer/` `Storage/` `Versioning/` | `IConfigurationManager`、`IEnvironment`、`ILoadableFrom<T>`、`ISerializer`、`IStorage`、`IVersioned` | 配置、环境、序列化和持久化边界，以及谁负责具体实现 |
| `Bases/` `Controller/` `Model/` `Systems/` `Utility/` `Rule/` `Enums/` `Properties/` | `IPrioritized`、`IController`、`IModel`、`ISystem`、`IContextUtility`、`ArchitecturePhase` | 基础角色接口、辅助值对象和架构属性键的复用方式 |

## 阅读顺序

1. 先读本页，确认你是否真的只需要契约层
2. 再看 [Core 模块总览](../core/index.md) 了解默认运行时怎么组织这些契约
3. 再按需要回到：
   - [入门指南](../getting-started/index.md)
   - [Core 模块总览](../core/index.md)
4. 需要统一导航时，再看 [API 参考](../api-reference/index.md)
