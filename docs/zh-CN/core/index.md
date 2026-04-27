---
title: Core 模块
description: GFramework.Core 与 GFramework.Core.Abstractions 的运行时入口、采用顺序和源码阅读导航。
---

# Core 模块

`Core` 栏目对应 `GFramework` 的基础运行时层，主要覆盖 `GFramework.Core` 与 `GFramework.Core.Abstractions`，以及与之直接相邻的旧版
`Command` / `Query` 执行器和新版 `CQRS` 迁移入口。

如果你第一次接入框架，可以先把这里当作“运行时底座说明”：先确认 `Core` 解决什么问题、最小安装组合是什么，再决定什么时候转向 `CQRS`、`Game`、`Godot` 或源码生成器。

## 模块与包关系

- `GeWuYou.GFramework.Core`
  - 基础运行时实现，包含 `Architecture`、上下文、生命周期、事件、属性、状态、资源、日志、协程、IoC 等能力。
- `GeWuYou.GFramework.Core.Abstractions`
  - 对应的契约层，适合只依赖接口、做模块拆分或测试替身。
- `GeWuYou.GFramework.Cqrs`
  - 推荐给新功能使用的新请求模型运行时。
- `GeWuYou.GFramework.Game`
  - 在 `Core` 之上叠加游戏层配置、数据、设置、场景与 UI。
- `GeWuYou.GFramework.Core.SourceGenerators`
  - 在编译期补齐日志、上下文注入、模块自动注册等样板代码。

如果你只想先把架构跑起来，最小安装组合仍是：

```bash
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions
```

## 栏目覆盖范围

这里的页面按能力域组织，适合按“我要接什么能力”而不是“我要读完所有目录”的方式进入：

- 架构与生命周期
  - [架构](./architecture.md)
  - [上下文](./context.md)
  - [生命周期](./lifecycle.md)
  - [异步初始化](./async-initialization.md)
- 组件角色与运行时接入
  - [模型](./model.md)
  - [系统](./system.md)
  - [工具](./utility.md)
  - [环境](./environment.md)
  - [扩展方法](./extensions.md)
- 旧版命令 / 查询执行器与迁移入口
  - [命令执行](./command.md)
  - [查询执行](./query.md)
  - [CQRS 运行时](./cqrs.md)
- 状态、事件与规则
  - [事件系统](./events.md)
  - [可绑定属性](./property.md)
  - [规则系统](./rule.md)
  - [日志系统](./logging.md)
  - [状态机](./state-machine.md)
  - [状态管理](./state-management.md)
- 运行时支撑能力
  - [资源管理](./resource.md)
  - [对象池](./pool.md)
  - [协程系统](./coroutine.md)
  - [暂停系统](./pause.md)
  - [本地化](./localization.md)
  - [配置管理](./configuration.md)
  - [IoC 容器](./ioc.md)
- 通用辅助能力
  - [函数式辅助](./functional.md)

## XML 与 API 阅读入口

如果你已经知道模块归属，但想确认公开类型的契约边界，建议按下面顺序阅读：

1. 先读本页与 [Core 抽象层说明](../abstractions/core-abstractions.md)，确认运行时和契约层边界
2. 再看本栏目对应专题页，确认采用顺序、生命周期与推荐接线方式
3. 最后回到源码中的 XML 文档，重点核对这些类型族：
   - `Architecture` / `IArchitectureContext`
   - `CommandExecutor` / `QueryExecutor`
   - `ILogger` / `ILoggerFactory`
   - `IResourceManager` / `IConfigurationManager`
   - `IAsyncKeyLockManager` / `ITimeProvider`

统一入口见[API 参考](../api-reference/index.md)。

## 源码阅读入口

如果你准备直接回到源码和 XML 文档确认契约，建议按能力域分批阅读，而不是按文件数量排查：

| 类型族 | 代表类型 | 建议先确认什么 |
| --- | --- | --- |
| `Architectures/` | `Architecture`、`ArchitectureContext`、`ArchitectureLifecycle`、`ArchitecturePhaseCoordinator` | 架构启动、模块安装、阶段切换和上下文暴露边界 |
| `Services/` | `ServiceModuleManager`、`CommandExecutorModule`、`CqrsRuntimeModule` | 服务模块的注册顺序、销毁语义和默认接线 |
| `Command/` `Query/` | `CommandExecutor`、`AsyncQueryExecutor`、`AbstractCommand<TInput>`、`AbstractQuery<TResult>` | 旧入口兼容面，以及向 `CQRS` 迁移时保留的执行契约 |
| `Events/` `Property/` | `EventBus`、`EnhancedEventBus`、`BindableProperty<T>`、`OrEvent<T>` | 事件传播、解绑约束和可绑定属性的订阅语义 |
| `State/` `StateManagement/` | `StateMachine`、`StateMachineSystem`、`Store<TState>`、`StoreBuilder<TState>` | 状态切换，以及 selector / middleware / dispatch 的单向流边界 |
| `Coroutine/` `Time/` `Pause/` `Concurrency/` | `CoroutineScheduler`、`CoroutineHandle`、`WaitForSecondsRealtime`、`PauseStackManager`、`AsyncKeyLockManager` | 调度阶段、等待指令、时间源，以及暂停 / 锁的线程语义 |
| `Resource/` `Pool/` | `ResourceManager`、`AutoReleaseStrategy`、`ManualReleaseStrategy`、`AbstractObjectPoolSystem<TKey, TObject>` | 资源句柄释放策略与对象池复用约束 |
| `Logging/` `Localization/` `Configuration/` `Environment/` `Ioc/` | `ConsoleLogger`、`CompositeLogger`、`LocalizationManager`、`ConfigurationManager`、`MicrosoftDiContainer` | 日志组装、格式化 / filter、配置监听、环境对象与容器适配 |
| `Model/` `Systems/` `Utility/` `Rule/` `Extensions/` `Functional/` | `AbstractModel`、`AbstractSystem`、`NumericDisplayFormatter`、`ContextAwareBase`、`Result<T>` | 默认基类、上下文感知 helper、数值格式化和通用扩展的使用边界 |

## 最小接入路径

当前版本的最小运行时入口只有三个关键动作：

1. 继承 `Architecture`
2. 在 `OnInitialize()` 中注册模型、系统、工具或模块
3. 通过 `architecture.Context` 或 `ContextAwareBase` 的扩展方法访问上下文

最小示例：

```csharp
using GFramework.Core.Architectures;

public sealed class CounterArchitecture : Architecture
{
    protected override void OnInitialize()
    {
        RegisterModel(new CounterModel());
        RegisterSystem(new CounterSystem());
    }
}
```

对应的完整起步示例见：

- [快速开始](../getting-started/quick-start.md)

## 新项目如何选择能力

- 只需要基础架构、事件、日志、资源、协程：
  - 先停留在 `Core`
- 要写新的请求/通知处理流：
  - 优先阅读[CQRS 运行时](./cqrs.md)
- 要接入游戏内容配置、设置、数据仓库、Scene 或 UI：
  - 转到[Game 模块](../game/index.md)
- 要接入 Godot 节点、场景和项目元数据生成：
  - 转到[Godot 模块](../godot/index.md)与[源码生成器](../source-generators/index.md)栏目

## 阅读顺序

1. [快速开始](../getting-started/quick-start.md)
2. [架构](./architecture.md)
3. [上下文](./context.md)
4. [生命周期](./lifecycle.md)
5. [CQRS 运行时](./cqrs.md)

之后再按实际需要进入具体专题页，而不是把 `Core` 当成一次性读完的大杂烩。

## 对应模块入口

- [入门指南](../getting-started/index.md)
- [Core 抽象层说明](../abstractions/core-abstractions.md)
- [CQRS 运行时](./cqrs.md)
- [源码生成器总览](../source-generators/index.md)
- [API 参考入口](../api-reference/index.md)
