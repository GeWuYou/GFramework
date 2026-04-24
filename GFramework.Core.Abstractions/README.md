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
- 相关扩展：
  - `GFramework.Cqrs.Abstractions`
  - `GFramework.Game.Abstractions`

## 契约地图

| 目录族 | 作用 |
| --- | --- |
| `Architectures/` `Lifecycle/` `Registries/` | `IArchitecture`、上下文、模块、服务模块、阶段监听、注册表基类与生命周期契约 |
| `Bases/` `Controller/` `Model/` `Systems/` `Utility/` `Rule/` | 组件角色接口、优先级 / key 值对象、上下文感知约束与扩展边界 |
| `Command/` `Query/` `Cqrs/` | 旧版命令 / 查询执行器接口，以及 `ICqrsRuntime` 这类新请求模型接线契约 |
| `Events/` `Property/` `State/` `StateManagement/` | 事件总线、解绑对象、可绑定属性、状态机、Store / reducer / middleware 契约 |
| `Coroutine/` `Time/` `Pause/` `Concurrency/` | 协程状态、时间源、暂停栈、键控异步锁和统计对象 |
| `Resource/` `Pool/` `Logging/` `Localization/` | 资源句柄、对象池、日志、日志工厂、本地化表与格式化契约 |
| `Configuration/` `Environment/` | 配置管理器、环境对象与运行时环境访问契约 |
| `Data/` `Serializer/` `Storage/` `Versioning/` | 数据装载、序列化、存储与版本化契约 |
| `Enums/` `Properties/` | 架构阶段枚举，以及架构 / logger 相关属性键 |

## XML 覆盖基线

截至 `2026-04-22`，已按顶层目录对 `GFramework.Core.Abstractions` 的公开 / 内部类型声明做过一轮轻量盘点；当前契约目录族的类型声明都已带
XML 注释。这里记录的是类型族级基线，成员级契约细节仍需要在后续波次继续审计。

| 类型族 | 基线状态 | 代表类型 |
| --- | --- | --- |
| `Architectures/` `Lifecycle/` `Registries/` | `20/20` 个类型声明已带 XML 注释 | `IArchitecture`、`IArchitectureContext`、`IServiceModule`、`KeyValueRegistryBase<TKey, TValue>` |
| `Command/` `Query/` `Cqrs/` | `10/10` 个类型声明已带 XML 注释 | `ICommandExecutor`、`IAsyncQueryExecutor`、`ICqrsRuntime` |
| `Events/` `Property/` `State/` `StateManagement/` | `25/25` 个类型声明已带 XML 注释 | `IEventBus`、`IBindableProperty<T>`、`IStateMachine`、`IStore<TState>` |
| `Coroutine/` `Time/` `Pause/` `Concurrency/` | `17/17` 个类型声明已带 XML 注释 | `IYieldInstruction`、`ITimeProvider`、`IPauseStackManager`、`IAsyncKeyLockManager` |
| `Resource/` `Pool/` `Logging/` `Localization/` | `27/27` 个类型声明已带 XML 注释 | `IResourceManager`、`IObjectPoolSystem`、`ILogger`、`ILocalizationManager` |
| `Configuration/` `Environment/` `Data/` `Serializer/` `Storage/` `Versioning/` | `7/7` 个类型声明已带 XML 注释 | `IConfigurationManager`、`IEnvironment`、`ILoadableFrom<T>`、`ISerializer`、`IStorage` |
| `Bases/` `Controller/` `Model/` `Systems/` `Utility/` `Rule/` `Enums/` `Properties/` | `19/19` 个类型声明已带 XML 注释 | `IPrioritized`、`IController`、`IModel`、`ISystem`、`IContextUtility`、`ArchitecturePhase` |

完整接入说明与阅读顺序见 [Core 抽象层说明](../docs/zh-CN/abstractions/core-abstractions.md)。

## 采用建议

- 框架消费者通常同时安装 `GFramework.Core` 与 `GFramework.Core.Abstractions`
- 若你只需要对接口编程，可以仅引用本包，再在应用层自行提供实现
- 若你在写上层模块，优先把公共契约放在 `*.Abstractions`，实现放在对应 runtime 包

## 重点 XML 关注点

如果你在做契约审计、模块拆分或测试替身，优先看这些类型族的 XML 文档：

- 架构与模块入口：`IArchitecture`、`IArchitectureContext`、`IServiceModule`
- 运行时基础设施：`IIocContainer`、`ILogger`、`IResourceManager`、`IConfigurationManager`
- 状态与并发能力：`IStateMachine`、`IStore`、`IAsyncKeyLockManager`、`ITimeProvider`
- 迁移与组合边界：`ICommandExecutor`、`IQueryExecutor`、`ICqrsRuntime`

## 对应文档

- 抽象接口栏目：[`../docs/zh-CN/abstractions/index.md`](../docs/zh-CN/abstractions/index.md)
- Core 抽象页：[`../docs/zh-CN/abstractions/core-abstractions.md`](../docs/zh-CN/abstractions/core-abstractions.md)
- Core 运行时入口：[`../GFramework.Core/README.md`](../GFramework.Core/README.md)
- API 参考入口：[`../docs/zh-CN/api-reference/index.md`](../docs/zh-CN/api-reference/index.md)
