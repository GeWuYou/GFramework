---
title: Core
description: GFramework.Core 与 GFramework.Core.Abstractions 的运行时入口、采用顺序和 XML 阅读导航。
---

# Core

`Core` 栏目对应 `GFramework` 的基础运行时层，主要覆盖 `GFramework.Core` 与 `GFramework.Core.Abstractions`，以及与之直接相邻的旧版
`Command` / `Query` 执行器和新版 `CQRS` 迁移入口。

如果你第一次接入框架，建议先把这里当作“运行时底座说明”，再按需进入 `Game`、`Godot` 或 Source Generators 栏目。

## 先理解包关系

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

## 这个栏目应该回答什么

`Core` 栏目不是旧版“完整框架教程”的镜像，而是当前实现的入口导航。这里的页面按能力域组织：

- 架构与生命周期
  - [architecture](./architecture.md)
  - [context](./context.md)
  - [lifecycle](./lifecycle.md)
  - [async-initialization](./async-initialization.md)
- 组件角色与运行时接入
  - [model](./model.md)
  - [system](./system.md)
  - [utility](./utility.md)
  - [environment](./environment.md)
  - [extensions](./extensions.md)
- 旧版命令 / 查询执行器与迁移入口
  - [command](./command.md)
  - [query](./query.md)
  - [cqrs](./cqrs.md)
- 状态、事件与规则
  - [events](./events.md)
  - [property](./property.md)
  - [rule](./rule.md)
  - [logging](./logging.md)
  - [state-machine](./state-machine.md)
  - [state-management](./state-management.md)
- 运行时支撑能力
  - [resource](./resource.md)
  - [pool](./pool.md)
  - [coroutine](./coroutine.md)
  - [pause](./pause.md)
  - [localization](./localization.md)
  - [configuration](./configuration.md)
  - [ioc](./ioc.md)
- 通用辅助能力
  - [functional](./functional.md)

## XML 与 API 阅读入口

如果你已经知道模块归属，但想确认公开类型的契约边界，建议按下面顺序阅读：

1. 先看模块 README `GFramework.Core/README.md`，确认包关系和目录边界
2. 再看本栏目对应专题页，确认采用顺序、生命周期与推荐接线方式
3. 最后回到源码中的 XML 文档，重点核对这些类型族：
   - `Architecture` / `IArchitectureContext`
   - `CommandExecutor` / `QueryExecutor`
   - `ILogger` / `ILoggerFactory`
   - `IResourceManager` / `IConfigurationManager`
   - `IAsyncKeyLockManager` / `ITimeProvider`

统一入口见 [`../api-reference/index.md`](../api-reference/index.md)。

## XML 覆盖基线

下面这份 inventory 记录的是 `2026-04-22` 对 `GFramework.Core` 做的一轮轻量 XML 盘点结果：只统计顶层目录中的公开 /
内部类型声明是否带 XML 注释，用来确认阅读入口和治理优先级；成员级 ``<param>``、``<returns>``、异常语义与线程说明仍需要继续细审。

| 类型族 | 基线状态 | 代表类型 | 阅读重点 |
| --- | --- | --- | --- |
| `Architectures/` | `16/16` 个类型声明已带 XML 注释 | `Architecture`、`ArchitectureContext`、`ArchitectureLifecycle`、`ArchitecturePhaseCoordinator` | 看架构启动、模块安装、阶段切换和上下文暴露边界 |
| `Services/` | `6/6` 个类型声明已带 XML 注释 | `ServiceModuleManager`、`CommandExecutorModule`、`CqrsRuntimeModule` | 看服务模块的注册顺序、销毁语义和默认接线 |
| `Command/` `Query/` | `15/15` 个类型声明已带 XML 注释 | `CommandExecutor`、`AsyncQueryExecutor`、`AbstractCommand<TInput>`、`AbstractQuery<TResult>` | 看旧入口兼容面与向 `CQRS` 迁移时还保留了哪些执行契约 |
| `Events/` `Property/` | `19/19` 个类型声明已带 XML 注释 | `EventBus`、`EnhancedEventBus`、`BindableProperty<T>`、`OrEvent<T>` | 看事件传播、解绑约束和可绑定属性的订阅语义 |
| `State/` `StateManagement/` | `10/10` 个类型声明已带 XML 注释 | `StateMachine`、`StateMachineSystem`、`Store<TState>`、`StoreBuilder<TState>` | 看状态切换、selector / middleware / dispatch 的单向流边界 |
| `Coroutine/` `Time/` `Pause/` `Concurrency/` | `43/43` 个类型声明已带 XML 注释 | `CoroutineScheduler`、`CoroutineHandle`、`WaitForSecondsRealtime`、`PauseStackManager`、`AsyncKeyLockManager` | 看调度阶段、等待指令、时间源和暂停 / 锁的线程语义 |
| `Resource/` `Pool/` | `8/8` 个类型声明已带 XML 注释 | `ResourceManager`、`AutoReleaseStrategy`、`ManualReleaseStrategy`、`AbstractObjectPoolSystem<TKey, TObject>` | 看资源句柄释放策略与对象池复用约束 |
| `Logging/` `Localization/` `Configuration/` `Environment/` `Ioc/` | `31/31` 个类型声明已带 XML 注释 | `ConsoleLogger`、`CompositeLogger`、`LocalizationManager`、`ConfigurationManager`、`MicrosoftDiContainer` | 看日志组装、格式化 / filter、配置监听、环境对象与容器适配 |
| `Model/` `Systems/` `Utility/` `Rule/` `Extensions/` `Functional/` | `34/34` 个类型声明已带 XML 注释 | `AbstractModel`、`AbstractSystem`、`NumericDisplayFormatter`、`ContextAwareBase`、`Result<T>` | 看默认基类、上下文感知 helper、数值格式化和通用扩展的使用边界 |

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
  - 优先阅读 [cqrs](./cqrs.md)
- 要接入游戏内容配置、设置、数据仓库、Scene 或 UI：
  - 转到 [Game](../game/index.md)
- 要接入 Godot 节点、场景和项目元数据生成：
  - 转到 [Godot](../godot/index.md) 与 [Source Generators](../source-generators/index.md) 栏目

## 推荐阅读顺序

1. [快速开始](../getting-started/quick-start.md)
2. [architecture](./architecture.md)
3. [context](./context.md)
4. [lifecycle](./lifecycle.md)
5. [cqrs](./cqrs.md)

之后再按实际需要进入具体专题页，而不是把 `Core` 当成一次性读完的大杂烩。

## 对应模块入口

- `GFramework.Core/README.md`
- `GFramework.Core.Abstractions/README.md`
- `docs/zh-CN/api-reference/index.md`
- 仓库根 `README.md`
