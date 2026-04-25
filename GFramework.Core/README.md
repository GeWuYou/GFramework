# GFramework.Core

`GFramework.Core` 是框架的基础运行时，负责架构生命周期、组件注册、上下文访问，以及不依赖具体引擎的通用能力。

如果你只想先把框架跑起来，应先从这个模块开始。

## 模块定位

这一层提供：

- `Architecture` 与 `ArchitectureContext`
- `Model` / `System` / `Utility` 运行时
- 旧版 `Command` / `Query` 执行器，以及与新版 `CQRS` runtime 的接线入口
- 事件、属性、状态机、状态管理、规则与上下文扩展
- 资源、对象池、日志、协程、并发、环境、配置与本地化
- 服务模块管理、时间提供器与默认的 IoC 容器适配

它不负责：

- 游戏内容配置、Scene / UI / Storage 等游戏层能力
- Godot 节点与场景集成
- 新版 CQRS 请求模型的消息契约定义

## 包关系

- 直接依赖：
  - `GFramework.Cqrs`
  - `GFramework.Cqrs.Abstractions`
  - `GFramework.Core.Abstractions`
- 常见上层模块：
  - `GFramework.Game`
  - `GFramework.Godot`

如果你只需要契约，不需要实现层，改为依赖 [Core 抽象层说明](../GFramework.Core.Abstractions/README.md)。

## 子系统地图

| 目录 | 作用 |
| --- | --- |
| `Architectures/` | 架构入口、上下文、生命周期、模块安装与组件注册 |
| `Services/` | 服务模块注册、生命周期协调与模块管理 |
| `Command/` | 旧版命令执行器与同步 / 异步命令基类 |
| `Query/` | 旧版查询执行器与同步 / 异步查询基类 |
| `Events/` | 事件总线、事件作用域、统计与过滤 |
| `Property/` | `BindableProperty<T>` 与相关解绑对象 |
| `State/` | 状态机与状态切换事件 |
| `StateManagement/` | Store、selector、middleware 与状态诊断 |
| `Coroutine/` | 协程调度、快照、统计与优先级 |
| `Time/` | 默认时间提供器与协程时间源 |
| `Resource/` | 资源缓存、句柄和释放策略 |
| `Pool/` | 对象池系统与常用池化辅助实现 |
| `Logging/` | logger、factory、配置与组合日志器 |
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
| `Command/` `Query/` | `CommandExecutor`、`AsyncQueryExecutor`、`AbstractCommand<TInput>`、`AbstractQuery<TResult>` | 看命令 / 查询执行入口、同步异步边界与抽象基类约束 |
| `Events/` `Property/` `State/` `StateManagement/` | `EventBus`、`BindableProperty<T>`、`StateMachine`、`Store<TState>` | 看事件分发、状态绑定、状态机与 Store 的职责划分 |
| `Coroutine/` `Time/` `Pause/` `Concurrency/` | `CoroutineScheduler`、`CoroutineHandle`、`PauseStackManager`、`AsyncKeyLockManager` | 看调度、暂停、时间推进与并发控制的生命周期语义 |
| `Resource/` `Pool/` | `ResourceManager`、`AutoReleaseStrategy`、`AbstractObjectPoolSystem<TKey, TObject>` | 看资源释放策略、池化抽象与可复用对象边界 |
| `Logging/` `Localization/` `Configuration/` `Environment/` `Ioc/` | `ConsoleLogger`、`LocalizationManager`、`ConfigurationManager`、`DefaultEnvironment`、`MicrosoftDiContainer` | 看基础设施入口与默认实现的组合方式 |
| `Model/` `Systems/` `Utility/` `Rule/` `Extensions/` `Functional/` | `AbstractModel`、`AbstractSystem`、`NumericDisplayFormatter`、`ContextAwareBase`、`Result<T>` | 看业务模型、系统基类、上下文辅助类型与函数式工具如何配合 |

完整的模块化接入说明和阅读顺序见 [Core 栏目](../docs/zh-CN/core/index.md)。

## 最小接入路径

```bash
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions
```

最小入口：

1. 继承 `Architecture`
2. 在 `OnInitialize()` 中注册模型、系统、工具或模块
3. 通过 `architecture.Context` 或 `ContextAwareBase` 的扩展方法访问上下文

最小示例见：

- [快速开始示例](../docs/zh-CN/getting-started/quick-start.md)

## 什么时候继续接别的包

- 需要推荐的新请求模型：加 `GFramework.Cqrs`
- 需要游戏层路由、设置、配置和存储：加 `GFramework.Game`
- 需要 Godot 节点与场景适配：加 `GFramework.Godot`
- 需要编译期生成日志、上下文注入或模块注册：加 `GFramework.Core.SourceGenerators`

## 对应文档

- Core 栏目：[Core 文档首页](../docs/zh-CN/core/index.md)
- Core 抽象层：[Core Abstractions 文档](../docs/zh-CN/abstractions/core-abstractions.md)
- API 参考入口：[API 参考首页](../docs/zh-CN/api-reference/index.md)
- CQRS：[CQRS 文档](../docs/zh-CN/core/cqrs.md)
- 入门指南：[入门指南首页](../docs/zh-CN/getting-started/index.md)
