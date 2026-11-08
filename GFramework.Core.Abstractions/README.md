# GFramework.Core.Abstractions

`GFramework.Core.Abstractions` 承载 `Core` 运行时对应的接口、枚举和值对象，用来定义跨模块协作边界。

它只描述契约，不提供默认的架构、事件、状态、资源或 IoC 实现；这些实现都在 `GFramework.Core` 中。

## 什么时候单独依赖它

- 你在做插件、适配层或扩展包，只想依赖契约，不想把完整运行时拉进来
- 你需要为测试、编辑器工具或生成器提供替身实现
- 你在做多模块拆分，希望上层只面向接口编程

如果你只是直接使用框架功能，优先安装 `GFramework.Core`。

## 包关系

- 契约层：`GFramework.Core.Abstractions`
- 实现层：`GFramework.Core`
- 直接依赖：Microsoft DI 抽象；不依赖 CQRS 契约
- 上层契约：`GFramework.Game.Abstractions`

## 契约地图

| 目录族 | 作用 |
| --- | --- |
| `Architectures/` `Lifecycle/` `Registries/` | `IArchitecture`、上下文、模块、服务模块、阶段监听、注册表基类与生命周期契约 |
| `Bases/` `Controller/` `Model/` `Systems/` `Utility/` `Rule/` | 组件角色接口、优先级 / key 值对象、上下文感知约束与扩展边界 |
| `Events/` `Property/` `State/` `StateManagement/` | 事件总线、解绑对象、可绑定属性、状态机、Store / reducer / middleware 契约 |
| `Coroutine/` `Time/` `Pause/` `Concurrency/` | 协程阶段、失败策略、状态、时间源、暂停栈、键控异步锁和统计对象 |
| `Resource/` `Pool/` `Logging/` `Localization/` | 资源句柄、对象池、appender / filter / formatter 日志输出契约、本地化表与格式化契约 |
| `Configuration/` `Environment/` | 配置管理器、环境对象与运行时环境访问契约 |
| `Data/` `Serializer/` `Storage/` `Versioning/` | 数据装载、序列化、存储与版本化契约 |
| `Enums/` `Properties/` | 架构阶段枚举，以及架构 / logger 相关属性键 |

## XML 阅读入口

下面这份目录视图可以帮助你快速定位 `GFramework.Core.Abstractions` 的代表类型。更细的契约约束与交互语义，适合在阅读具体接口和成员时继续结合源码确认。

| 类型族 | 代表类型 | 阅读重点 |
| --- | --- | --- |
| `Architectures/` `Lifecycle/` `Registries/` | `IArchitecture`、`IArchitectureContext`、`IServiceModule`、`KeyValueRegistryBase<TKey, TValue>` | 看架构、上下文、模块装配与注册表基类边界 |
| `Events/` `Property/` `State/` `StateManagement/` | `IEventBus`、`IBindableProperty<T>`、`IStateMachine`、`IStore<TState>` | 看事件分发、可绑定状态与 store 契约 |
| `Coroutine/` `Time/` `Pause/` `Concurrency/` | `IYieldInstruction`、`ITimeProvider`、`IPauseStackManager`、`IAsyncKeyLockManager` | 看协程、时间源、暂停栈与并发协调能力 |
| `Resource/` `Pool/` `Logging/` `Localization/` | `IResourceManager`、`IObjectPoolSystem`、`ILogAppender`、`ILocalizationManager` | 看资源、对象池、日志输出引擎与本地化服务角色；日志记录契约使用 `Microsoft.Extensions.Logging` 的 `ILogger` |
| `Configuration/` `Environment/` `Data/` `Serializer/` `Storage/` `Versioning/` | `IConfigurationManager`、`IEnvironment`、`ILoadableFrom<T>`、`ISerializer`、`IStorage` | 看配置、环境、数据装载、序列化与存储边界 |
| `Bases/` `Controller/` `Model/` `Systems/` `Utility/` `Rule/` `Enums/` `Properties/` | `IPrioritized`、`IController`、`IModel`、`ISystem`、`IContextUtility`、`ArchitecturePhase` | 看组件角色、优先级和值对象约定 |

完整接入说明与阅读顺序见 [Core 抽象层说明](../docs/zh-CN/abstractions/core-abstractions.md)。

## 生命周期与上下文契约

IAsyncInitializable.InitializeAsync 接受 CancellationToken；IArchitecture 同时提供 IAsyncDisposable。取消或初始化失败由实现方清理已进入启动的资源；业务停止与 IDisposable/IAsyncDisposable 资源所有权分别处理。

IContextAware 要求显式绑定实例上下文；未绑定时不回退到全局状态。IIocContainer 的类型/工厂注册由正式 provider 拥有，实例注册保持外部所有权。generic RegisterPlurality 对可释放类型拒绝隐式 owning aliases，调用方应使用单一规范拥有绑定或外部实例。

`IArchitectureContext` 只承载组件获取、事件系统与环境访问。request / notification / stream 的分发契约位于 `GFramework.Cqrs.Abstractions`（`ICqrsRuntime` 等），按需安装，不在本包的契约面内。

协程宿主声明实际支持的 `Update`、`FixedUpdate`、`Deferred` 阶段；运行时通过 `SwitchTo` 切换阶段。`CoroutineFailurePolicy` 区分一个操作内的 FailFast 与独立任务容器的 Supervisor。`ICoroutineStatistics` 将调度执行耗时与包含等待的存活时间分开，具体调度器、作用域和结果类型由 Core 提供。

## 采用建议

- 框架消费者通常同时安装 `GFramework.Core` 与 `GFramework.Core.Abstractions`
- 若你只需要对接口编程，可以仅引用本包，再在应用层自行提供实现
- 若你在写上层模块，优先把公共契约放在 `*.Abstractions`，实现放在对应 runtime 包

## 推荐优先阅读的 XML 类型族

如果你在做模块拆分、测试替身或接口适配，优先看这些类型族的 XML 文档：

- 架构与模块入口：`IArchitecture`、`IArchitectureContext`、`IServiceModule`
- 运行时基础设施：`IIocContainer`、`ILogAppender`、`IResourceManager`、`IConfigurationManager`
- 状态与并发能力：`IStateMachine`、`IStore`、`IAsyncKeyLockManager`、`ITimeProvider`

## 对应文档

- 抽象接口栏目：[抽象接口总览](../docs/zh-CN/abstractions/index.md)
- Core 抽象页：[Core 抽象层说明](../docs/zh-CN/abstractions/core-abstractions.md)
- Core 运行时入口：[Core 运行时说明](../GFramework.Core/README.md)
- API 参考入口：[API 参考](../docs/zh-CN/api-reference/index.md)

`IPauseStackManager` 是独立的异步释放服务契约，供 UI、业务流程和宿主共享。实现与采用说明见 [暂停系统](../docs/zh-CN/core/pause.md)。
