---
title: 入门指南
description: 概览 GFramework 的模块组成、最小接入路径与继续阅读入口。
---

# 入门指南

如果你第一次接触 `GFramework`，或者还在判断“应该先装哪些包、先看哪一组文档”，先从这里开始最省时间。

这组页面的目标只有一个：帮你用最短路径找到适合当前项目的运行时入口、安装组合和下一步阅读顺序。

## 适合谁先读本栏

- 第一次接入 `GFramework`，还没决定该从 `Core`、`Game`、`Godot` 还是 `CQRS` 开始
- 想先确认最小安装组合，再决定是否追加源码生成器
- 想先跑通一个可运行骨架，再深入某个专题页

## 按目标选择起步路线

### 基础运行时起步

从 `Core` 开始：

- `GeWuYou.GFramework.Core`
- `GeWuYou.GFramework.Core.Abstractions`

这组包提供：

- `Architecture`
- `Model` / `System` / `Utility`
- 事件、属性、状态机、状态管理、资源、日志、协程等基础设施

对应文档：

- [Core 模块总览](../core/index.md)
- [快速开始](./quick-start.md)
- [安装配置](./installation.md)

### CQRS 消息模型

在 `Core` 基础上补（也可脱离 `Core` 在标准 DI 上独立使用）：

- `GeWuYou.GFramework.Cqrs`
- `GeWuYou.GFramework.Cqrs.Abstractions`

这组包提供：

- 统一 request dispatcher
- notification publish
- pipeline behaviors
- handler 注册与反射回退机制

对应文档：

- [CQRS 运行时](../core/cqrs.md)
- [安装配置](./installation.md)

### 游戏运行时与内容配置

在 `Core` 基础上按需补：

- `GeWuYou.GFramework.Game`
- `GeWuYou.GFramework.Game.Abstractions`

这组包提供：

- 内容配置系统
- 数据存取与设置
- Scene / UI / Routing 抽象与运行时
- 文件存储和序列化

AI-First 配置工作流的正式契约以 `GFramework.Game` Runtime 与 `GFramework.Game.SourceGenerators` 的共享 schema 子集为准，`VS Code` 配置工具只负责辅助编辑与预览。默认采用路径当前以 `additionalProperties: false` 作为闭合对象边界，`oneOf` / `anyOf` 不在默认入口范围内；如果你的 schema shape 超出这组共享边界，优先回到 raw YAML 与 schema 设计本体继续建模。

对应文档：

- [Game 模块总览](../game/index.md)
- [配置系统](../game/config-system.md)
- [安装配置](./installation.md)

如果你准备采用 AI-First 配置工作流，建议尽早确认当前采用边界：对象闭合只收口到
`additionalProperties: false`，而 `oneOf` / `anyOf` 这类会改变生成类型形状的组合关键字当前不属于默认路径。
超过这组共享子集的复杂 schema shape，应回到 raw YAML 与 schema 本体设计，而不是把差异理解成工具遗漏。

### Godot 项目接入

继续叠加：

- `GeWuYou.GFramework.Godot`

对应文档：

- [Godot 模块总览](../godot/index.md)
- [Godot 集成教程](../tutorials/godot-integration.md)
- [安装配置](./installation.md)

## 什么时候追加源码生成器

只在需要编译期生成代码时再装：

- `GeWuYou.GFramework.Core.SourceGenerators`
- `GeWuYou.GFramework.Game.SourceGenerators`
- `GeWuYou.GFramework.Cqrs.SourceGenerators`
- `GeWuYou.GFramework.Godot.SourceGenerators`

典型场景：

- 自动生成日志、上下文绑定、模块注册代码
- 从 `schema` 生成游戏配置类型
- 为 CQRS handlers 生成注册表
- 生成 Godot 节点、场景和 UI 包装代码

如果你要处理的是更复杂的 schema 设计，而不是编译期生成代码本身，先确认它是否仍在 `Game` Runtime 与生成器共享子集内；超出时应优先调整 raw YAML / schema 方案，而不是假定编辑器入口少了某个开关。

继续阅读：

- [源码生成器总览](../source-generators/index.md)
- [配置系统](../game/config-system.md)
- [CQRS Handler Registry 生成器](../source-generators/cqrs-handler-registry-generator.md)

## 建议阅读顺序

1. [快速开始](./quick-start.md)
2. [安装配置](./installation.md)
3. 按你的目标进入 [基础运行时](../core/index.md)、[游戏运行时](../game/index.md)、[Godot 集成](../godot/index.md) 或 [源码生成器](../source-generators/index.md)
4. 需要完整示例时，再进入 [教程总览](../tutorials/index.md)
