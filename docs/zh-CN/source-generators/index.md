# 源码生成器

> GFramework 当前可用的编译时代码生成器与配套分析器总览。

## 概述

`GFramework.SourceGenerators` 与 `GFramework.Godot.SourceGenerators` 用 Roslyn 在编译期生成样板代码，减少手写接线、弱类型查找和重复注册。

当前仓库里已经落地的生成器与分析器如下：

| 分类       | 能力                                                                               | 入口文档                                                                      |
|----------|----------------------------------------------------------------------------------|---------------------------------------------------------------------------|
| Core     | `[Log]` 日志字段生成                                                                   | [日志生成器](/zh-CN/source-generators/logging-generator)                       |
| Core     | `[GenerateEnumExtensions]` 枚举扩展生成                                                | [枚举扩展](/zh-CN/source-generators/enum-generator)                           |
| Core     | `[ContextAware]` 上下文感知实现                                                         | [ContextAware 生成器](/zh-CN/source-generators/context-aware-generator)      |
| Core     | `[GetModel]` / `[GetSystem]` / `[GetUtility]` / `[GetService]` / `[GetAll]` 注入辅助 | [Context Get 注入](/zh-CN/source-generators/context-get-generator)          |
| Core     | `[Priority]` 自动实现 `IPrioritized`                                                 | [Priority 生成器](/zh-CN/source-generators/priority-generator)               |
| Config   | `*.schema.json` 到配置类型与表包装                                                        | [Schema Config 生成器](/zh-CN/source-generators/schema-config-generator)     |
| Godot    | `[GetNode]` 节点查找与注入                                                              | [GetNode 生成器](/zh-CN/source-generators/get-node-generator)                |
| Godot    | `[BindNodeSignal]` 事件绑定/解绑辅助                                                     | [BindNodeSignal 生成器](/zh-CN/source-generators/bind-node-signal-generator) |
| Analyzer | Context Get 与 Priority 相关诊断                                                      | [分析器](/zh-CN/source-generators/analyzers)                                 |

## 公共约定

- 绝大多数类型级生成器要求目标类型是 `partial class`。
- 生成器只负责产出方法、字段或包装类型，不会替你决定业务生命周期。
- 如果页面里存在“自动生成方法”描述，均以当前仓库实现为准；没有代码支持的行为不应写成默认能力。
- 对配置系统而言，生成器负责“生成类型与注册辅助”，运行时加载与热重载仍由 `GFramework.Game.Config` 承担。

## 生成器边界

### Core 生成器

- 关注 `GFramework.Core` 的上下文接入、日志字段、优先级和枚举扩展。
- 适合纯 C# 项目，不依赖 Godot。

### Config 生成器

- 面向 AI-First 内容配置工作流。
- 输入是 `*.schema.json`，输出是配置实体、表包装、单表绑定类和项目级聚合注册入口。
- 运行时接入方式见 [游戏内容配置系统](/zh-CN/game/config-system)。

### Godot 生成器

- 只处理 `Godot.Node` 场景下的节点查找和事件绑定样板代码。
- 生命周期方法是否自动补出，取决于具体生成器实现，不能一概而论。

## 分析器

`GFramework.SourceGenerators` 还包含两类与生成器配套的分析器：

- `ContextRegistrationAnalyzer`：当 `GetModel/GetSystem/GetUtility` 的使用点在所属架构里找不到静态可见注册时给出警告。
- `PriorityUsageAnalyzer`：当类型实现了 `IPrioritized` 却仍使用 `GetAll<T>()` 时，提示改用 `GetAllByPriority<T>()`。

详细规则、诊断 ID 和触发条件见 [分析器](/zh-CN/source-generators/analyzers)。
