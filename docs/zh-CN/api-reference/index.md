---
title: API 参考
description: GFramework 的 API 阅读入口，按模块映射 README、专题页、XML 文档和教程链路。
---

# API 参考

本页聚焦“先看哪个模块入口、再回哪里读 XML 文档”的 API 阅读导航。

最常见的阅读顺序是：

1. 先按模块找到对应栏目入口
2. 再进专题页确认安装、生命周期和推荐接线方式
3. 最后回到源码中的 XML 文档核对具体契约

如果你在阅读 AI-First 配置工作流相关 API，先把 `GFramework.Game` Runtime 与 `GFramework.Game.SourceGenerators` 视为正式契约入口，再把 `VS Code` 配置工具视为辅助层。当前默认采用路径围绕共享 schema 子集展开，其中 `additionalProperties: false` 表示闭合对象边界（需显式设置为 `false`）；`oneOf` / `anyOf` 在 Runtime / Generator / Tooling 层面会被直接拒绝。更复杂的 shape 应回到 raw YAML 与 schema 设计本体处理。

## 阅读顺序

### 安装与选包入口

先读站内入口页：

- 入门入口：[入门指南](../getting-started/index.md)
- 安装与选包：[安装配置](../getting-started/installation.md)

### 模块定位入口

按下面的模块映射进入对应入口：

| 模块族 | 先看什么 | 继续深入 | XML 文档关注点 |
| --- | --- | --- | --- |
| `Core` / `Core.Abstractions` | [Core 模块](../core/index.md) | [Core 抽象层说明](../abstractions/core-abstractions.md)、[快速开始](../getting-started/quick-start.md) | 架构入口、生命周期、命令 / 查询 / 事件 / 状态 / 资源 / 日志 / 配置 / 并发契约 |
| `Cqrs` / `Cqrs.Abstractions` / `Cqrs.SourceGenerators` | [CQRS 运行时](../core/cqrs.md) | [CQRS Handler Registry 生成器](../source-generators/cqrs-handler-registry-generator.md)、[协程系统](../core/coroutine.md) | request / notification / handler / pipeline / generated registry / targeted fallback contract，以及生成注册器与定向补扫的协作边界 |
| `Game` / `Game.Abstractions` / `Game.SourceGenerators` | [Game 模块总览](../game/index.md) | [Game 抽象层说明](../abstractions/game-abstractions.md)、[配置系统](../game/config-system.md)、[Schema 配置生成器](../source-generators/schema-config-generator.md) | 配置、数据、设置、场景、UI、存储、序列化契约，以及 schema 到生成代码的公开入口 |
| `Godot` / `Godot.SourceGenerators` | [Godot 模块总览](../godot/index.md) | [Godot 项目生成器](../source-generators/godot-project-generator.md)、[GetNode 生成器](../source-generators/get-node-generator.md)、[BindNodeSignal 生成器](../source-generators/bind-node-signal-generator.md) | 节点扩展、场景 / UI 适配、配置 / 存储 / 设置接线、Godot 生成器入口 |
| `Ecs.Arch` / `Ecs.Arch.Abstractions` | [ECS 模块总览](../ecs/index.md) | [Arch ECS 集成](../ecs/arch.md)、[Ecs.Arch 抽象层说明](../abstractions/ecs-arch-abstractions.md) | ECS 模块契约、系统适配、配置对象和运行时装配边界 |

## 先看 XML，还是先看教程

### 先看 XML 文档的情况

- 你在确认公开类型的约束、线程 / 生命周期语义、参数和返回值契约
- 你需要区分“抽象层保证了什么”和“默认实现额外提供了什么”
- 你在做多模块拆分、测试替身或扩展适配层

优先关注这些类型族：

- 架构 / 模块 / 服务入口
- 生命周期、注册、路由、工厂、provider 契约
- Source Generator 的 attribute、diagnostic 和 generated contract

### 先看教程和专题页的情况

- 你要的是最小接入路径，而不是逐个类型展开阅读
- 你想确认模块组合方式、目录约定和推荐接线顺序
- 你在做从旧入口迁移到新入口的采用决策

优先入口：

- 教程概览：[教程总览](../tutorials/index.md)
- 最佳实践：[最佳实践](../best-practices/index.md)
- 故障排查：[故障排查](../troubleshooting.md)

如果你阅读的是 AI-First 配置相关 API，请直接把 [配置系统](../game/config-system.md) 视为边界说明页：
像 `additionalProperties: false`、`oneOf` / `anyOf` rejection 这类采用约束不会由 VS Code 工具或 abstractions 页面单独改写。

## 共享支撑层怎么看

- `GFramework.Core.SourceGenerators.Abstractions`
- `GFramework.Godot.SourceGenerators.Abstractions`
- `GFramework.SourceGenerators.Common`

这些目录当前不作为独立采用入口；阅读它们时，优先回到所属模块页和 `source-generators` 栏目，再根据需要下钻到具体源码目录。

- `*.SourceGenerators.Abstractions`
  - 主要定义公开 attribute 和最小契约，供对应生成器与消费端项目共享。
- `GFramework.SourceGenerators.Common`
  - 主要提供共享生成器基类、通用 diagnostics，以及生成方法冲突等跨模块约束。

如果你要沿着 XML 文档和源码继续读，优先从下面这几类入口开始：

- 共享 diagnostics
  - `CommonDiagnostics`
- 共享生成流程
  - `AttributeClassGeneratorBase`
  - `AttributeEnumGeneratorBase`
- 共享冲突规则
  - `GeneratedMethodConflictExtensions`

这组入口更适合回答三类问题：

- 为什么多个生成器都会要求宿主类型满足 `partial`
- 为什么不同专题页会出现同一套生成方法名冲突诊断
- 为什么多个生成器对候选筛选、属性解析和生成阶段采用相近流程

## 使用方式

把本页当成“API 阅读导航”而不是“签名快照”：

- 先选模块
- 再进专题页确认采用路径
- 最后回到代码里的 XML 文档核对具体契约
