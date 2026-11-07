# GFramework.Core

`GFramework.Core` 是框架的基础运行时，负责架构生命周期、组件注册、上下文访问，以及不依赖具体引擎的通用能力。

如果你需要使用 Architecture，可从这个模块开始。Core 不传递依赖 CQRS；需要 request / notification / stream 消息模型的项目显式安装 `GFramework.Cqrs` 并安装 runtime 模块。

## 模块定位

这一层提供：

- `Architecture` 与 `ArchitectureContext`
- `Model` / `System` / `Utility` 运行时
- 事件、属性、状态机、状态管理、规则与上下文扩展
- 资源、对象池、日志、协程、并发、环境、配置与本地化
- 服务模块管理、时间提供器与默认的 IoC 容器适配

`IArchitectureContext` 只承载组件获取、事件系统与环境访问；消息分发不在上下文接口上，由显式安装的 CQRS runtime 及其上下文扩展提供。

它不负责：

- 游戏内容配置、Scene / UI / Storage 等游戏层能力
- Godot 节点与场景集成
- CQRS 消息契约与 runtime

## 包关系

- 直接依赖：
  - `GFramework.Core.Abstractions`
- 常见上层模块（按需安装）：
  - `GFramework.Cqrs`
  - `GFramework.Game`
  - `GFramework.Godot`

如果你只需要契约，不需要实现层，改为依赖 [Core 抽象层说明](../GFramework.Core.Abstractions/README.md)。

## 子系统地图

| 目录 | 作用 |
| --- | --- |
| `Architectures/` | 架构入口、上下文、生命周期、模块安装与组件注册 |
| `Services/` | 服务模块注册、生命周期协调与模块管理 |
| `Events/` | 事件总线、事件作用域、统计与过滤 |
| `Property/` | `BindableProperty<T>` 与相关解绑对象 |
| `State/` | 状态机与状态切换事件 |
| `StateManagement/` | Store、selector、middleware 与状态诊断 |
| `Coroutine/` | 协程调度、快照、统计与优先级 |
| `Time/` | 默认时间提供器与协程时间源 |
| `Resource/` | 资源缓存、句柄和释放策略 |
| `Pool/` | 对象池系统与常用池化辅助实现 |
| `Logging/` | 标准 ILoggerFactory 接线、appender 输出引擎与配置加载 |
| `Ioc/` | 基于 `Microsoft.Extensions.DependencyInjection` 的容器适配 |
| `Concurrency/` | 键控异步锁与统计 |
| `Configuration/` | 配置管理器与配置监听解绑对象 |
| `Environment/` | 运行环境对象与上下文环境扩展 |
| `Pause/` | 暂停栈和暂停范围 |
| `Localization/` | 本地化表与格式化入口 |
| `Rule/` | `ContextAwareBase` 等上下文感知基类 |
| `Functional/` | `Option`、`Result` 等轻量函数式工具 |
| `Extensions/` | 上下文与集合等扩展方法 |

## XML 阅读入口

下面这份目录视图可以帮助你快速定位 `GFramework.Core` 的类型级 XML 文档入口。更细的参数、返回值和生命周期语义，适合在阅读对应类型与成员时继续结合源码确认。

| 类型族 | 代表类型 | 阅读重点 |
| --- | --- | --- |
| `Architectures/` `Services/` | `Architecture`、`ArchitectureContext`、`ArchitectureLifecycle`、`ServiceModuleManager` | 先看架构入口、上下文对象与模块生命周期如何协同 |
| `Events/` `Property/` `State/` `StateManagement/` | `EventBus`、`BindableProperty<T>`、`StateMachine`、`Store<TState>` | 看事件分发、状态绑定、状态机与 Store 的职责划分 |
| `Coroutine/` `Time/` `Pause/` `Concurrency/` | `CoroutineScheduler`、`CoroutineHandle`、`PauseStackManager`、`AsyncKeyLockManager` | 看调度、暂停、时间推进与并发控制的生命周期语义 |
| `Resource/` `Pool/` | `ResourceManager`、`AutoReleaseStrategy`、`AbstractObjectPoolSystem<TKey, TObject>` | 看资源释放策略、池化抽象与可复用对象边界 |
| `Logging/` `Localization/` `Configuration/` `Environment/` `Ioc/` | `AppenderLoggerProvider`、`LoggingConfigurationLoader`、`LocalizationManager`、`ConfigurationManager`、`DefaultEnvironment`、`MicrosoftDiContainer` | 看基础设施入口与默认实现的组合方式 |
| `Model/` `Systems/` `Utility/` `Rule/` `Extensions/` `Functional/` | `AbstractModel`、`AbstractSystem`、`NumericDisplayFormatter`、`ContextAwareBase`、`Result<T>` | 看业务模型、系统基类、上下文辅助类型与函数式工具如何配合 |

完整的模块化接入说明和阅读顺序见 [Core 栏目](../docs/zh-CN/core/index.md)。

## 当前使用边界

Architecture 的配置阶段只登记描述，随后构建一次正式 provider，再激活和初始化组件。服务、上下文和日志按实例隔离；框架不再绑定全局 GameContext，也不替换全局日志工厂。

DestroyAsync/DisposeAsync 会停止业务组件并异步释放 provider 拥有的资源。传入的外部实例由调用者负责最终释放。角色组件的类型注册使用同一 provider；可释放实现采用单一拥有服务键，避免工厂别名重复捕获资源。

Core-only 的消费方没有 CQRS 传递依赖。需要消息模型时，安装 `GeWuYou.GFramework.Cqrs` 并通过 `architecture.UseCqrsRuntime(...)` 或 `RegisterServiceModule(new CqrsRuntimeModule(...))` 显式安装 runtime，见[架构生命周期](../docs/zh-CN/core/architecture.md)与 [CQRS 运行时](../docs/zh-CN/core/cqrs.md)。

## 最小接入路径

```bash
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions
```

最小入口：

1. 继承 `Architecture`
2. 在 `ConfigureServices(IServiceCollection services)` 中登记服务、组件与模块，在 OnStarted/OnStartedAsync 中执行已构建后的宿主操作
3. 等待 InitializeAsync 后，通过 architecture.Context 或已绑定的 ContextAwareBase 访问服务；使用完成后等待 DisposeAsync

最小示例见：

- [快速开始示例](../docs/zh-CN/getting-started/quick-start.md)

## 可选采用包

- 需要 request / notification / stream 消息模型：显式引用 `GFramework.Cqrs` 并安装 runtime 模块
- 需要游戏层路由、设置、配置和存储：加 `GFramework.Game`
- 需要 Godot 节点与场景适配：加 `GFramework.Godot`
- 需要编译期生成日志、上下文注入或模块注册：加 `GFramework.Core.SourceGenerators`

## 对应文档

- Core 栏目：[Core 文档首页](../docs/zh-CN/core/index.md)
- Core 抽象层：[Core Abstractions 文档](../docs/zh-CN/abstractions/core-abstractions.md)
- API 参考入口：[API 参考首页](../docs/zh-CN/api-reference/index.md)
- CQRS：[CQRS 运行时](../docs/zh-CN/core/cqrs.md)
- 入门指南：[入门指南首页](../docs/zh-CN/getting-started/index.md)

## 资源释放责任

资源缓存保留创建条目时的 loader 释放操作；loader 的 Unload 负责完整释放，管理器不再额外 Dispose。同一路径仍被句柄持有时，Unload 与 UnloadAll 都保留该资源。采用方式见[资源管理](../docs/zh-CN/core/resource.md)。


## 宿主停止顺序

OnStoppingAsync 先关闭宿主接入，随后停止组件和服务模块；OnStoppedAsync 在它们停止后分离宿主根节点，最后释放 provider。需要异步退出回调的宿主应把节点分离放在 OnStoppedAsync，确保回调执行期间节点和依赖仍然存活。
