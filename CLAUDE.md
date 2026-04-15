# CLAUDE.md

This file provides project understanding for AI agents working in this repository.

## Project Overview

GFramework 是面向游戏开发的模块化 C# 框架，核心能力与引擎解耦。项目灵感参考 QFramework，并在模块边界、工程组织和可扩展性方面持续重构。

## AI Agent Instructions

All coding rules are defined in:

@AGENTS.md

Follow them strictly.

## Module Dependency Graph

```text
GFramework (meta package) ─→ Core + Game
GFramework.Core ─→ Core.Abstractions
GFramework.Game ─→ Game.Abstractions, Core, Core.Abstractions
GFramework.Godot ─→ Core, Game, Core.Abstractions, Game.Abstractions
GFramework.Ecs.Arch ─→ Ecs.Arch.Abstractions, Core, Core.Abstractions
GFramework.SourceGenerators ─→ SourceGenerators.Common, SourceGenerators.Abstractions
```

- **Abstractions projects** (`netstandard2.1`): 只包含接口和契约定义，不承载运行时实现逻辑。
- **Core / Game / Ecs.Arch** (`net8.0;net9.0;net10.0`): 平台无关的核心实现层。
- **Godot**: Godot 引擎集成层，负责与节点、场景和引擎生命周期对接。
- **SourceGenerators** (`netstandard2.1`): Roslyn 增量源码生成器及其公共基础设施。

## Architecture Pattern

框架核心采用 `Architecture / Model / System / Utility` 四层结构：

- **IArchitecture**: 顶层容器，负责生命周期管理、组件注册、模块安装和统一服务访问。
- **IContextAware**: 统一上下文访问接口，组件通过 `SetContext(IArchitectureContext)` 获取架构上下文。
- **IModel**: 数据与状态层，负责长期状态和业务数据建模。
- **ISystem**: 业务逻辑层，负责命令执行、流程编排和规则落地。
- **IUtility**: 通用无状态工具层，供其他层复用。

关键实现位于 `GFramework.Core/Architectures/Architecture.cs`，其职责是作为总协调器串联生命周期、组件注册和模块系统。

## Architecture Details

### Lifecycle

Architecture 负责统一生命周期编排，核心阶段包括：

- `Init`
- `Ready`
- `Destroy`

在实现层中，生命周期被拆分为更细粒度的初始化与销毁阶段，用于保证 Utility、Model、System、服务模块和钩子的顺序一致性。

### Component Coordination

框架通过独立组件协作完成架构编排：

- `ArchitectureLifecycle`: 管理生命周期阶段、阶段转换和生命周期钩子。
- `ArchitectureComponentRegistry`: 管理 Model、System、Utility 的注册与解析。
- `ArchitectureModules`: 管理模块安装、服务模块接入和扩展点注册。

这组拆分的目标是降低单个核心类的职责密度，同时保持对外 API 稳定。

### Context Propagation

`IArchitectureContext` 和相关 Provider 类型负责在组件之间传播上下文能力，使 Model、System
和外部扩展都能通过统一入口访问架构服务，而不直接耦合具体实现细节。

## Key Patterns

### CQRS

命令与查询分离，支持同步与异步执行。当前版本内建自有 CQRS runtime、行为管道和 handler 自动注册；公开 API 里仍保留少量历史
`Mediator` 命名以兼容旧调用点，但这些别名已进入正式弃用周期：新代码应使用 `Cqrs` 命名入口，旧别名会继续兼容一段时间并计划在未来
major 版本中移除。

### EventBus

类型安全事件总线支持事件发布、订阅、优先级、过滤器和弱引用订阅。它是模块之间松耦合通信的核心基础设施之一。

### BindableProperty

响应式属性模型通过值变化通知驱动界面或业务层更新，适合表达轻量级状态同步。

### Coroutine

帧驱动协程系统基于 `IYieldInstruction` 和调度器抽象，支持等待时间、事件和任务完成等常见模式。

### IoC

依赖注入通过 `MicrosoftDiContainer` 对 `Microsoft.Extensions.DependencyInjection` 进行封装，用于统一组件注册和服务解析体验。

### Service Modules

`IServiceModule` 模式用于向 Architecture 注册内置服务，例如 EventBus、CommandExecutor、QueryExecutor 等。这一模式承担“基础设施能力装配”的职责。

## Source Generators

当前仓库包含多类 Roslyn 增量源码生成器：

- `LoggerGenerator` (`[Log]`): 自动生成日志字段和日志辅助方法。
- `PriorityGenerator` (`[Priority]`): 生成优先级比较相关实现。
- `EnumExtensionsGenerator` (`[GenerateEnumExtensions]`): 生成枚举扩展能力。
- `ContextAwareGenerator` (`[ContextAware]`): 自动实现 `IContextAware` 相关样板逻辑。
- `CqrsHandlerRegistryGenerator`: 为消费端程序集生成 CQRS handler 注册器，运行时优先使用生成产物，无法覆盖时回退到反射扫描。

这些生成器的目标是减少重复代码，同时保持框架层 API 的一致性与可维护性。

## Module Structure

仓库以“抽象层 + 实现层 + 集成层 + 生成器层”的方式组织：

- `GFramework.Core.Abstractions` / `GFramework.Game.Abstractions`: 约束接口和公共契约。
- `GFramework.Core` / `GFramework.Game`: 提供平台无关实现。
- `GFramework.Godot`: 提供与 Godot 运行时集成的适配实现。
- `GFramework.Ecs.Arch`: 提供 ECS Architecture 相关扩展。
- `GFramework.SourceGenerators` 及相关 Abstractions/Common: 提供代码生成能力。

这种结构的核心设计目标是让抽象稳定、实现可替换、引擎集成隔离、生成器能力可独立演进。

## Documentation Structure

项目文档位于 `docs/`，中文内容位于 `docs/zh-CN/`。文档内容覆盖：

- 入门与安装
- Core / Game / Godot / ECS 各模块能力
- Source Generator 使用说明
- 教程、最佳实践与故障排查

阅读顺序通常建议先看根目录 `README.md` 和各子模块 `README.md`，再进入 `docs/` 查阅专题说明。

## Design Intent

GFramework 的设计重点不是把所有能力堆进单一核心类，而是通过清晰的模块边界、可组合的服务注册方式、稳定的抽象契约以及适度自动化的源码生成，构建一个适合长期演进的游戏开发基础框架。
