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
- 旧版 `Command` / `Query` 执行器
- 事件、属性、状态机、状态管理、资源、日志、协程等基础设施

对应文档：

- [Core 模块总览](../core/index.md)
- [快速开始](./quick-start.md)
- [安装配置](./installation.md)

### 新版 CQRS 请求流

在 `Core` 基础上补：

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

对应文档：

- [Game 模块总览](../game/index.md)
- [配置系统](../game/config-system.md)
- [安装配置](./installation.md)

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

继续阅读：

- [源码生成器总览](../source-generators/index.md)
- [配置系统](../game/config-system.md)
- [CQRS Handler Registry 生成器](../source-generators/cqrs-handler-registry-generator.md)

## 建议阅读顺序

1. [快速开始](./quick-start.md)
2. [安装配置](./installation.md)
3. 按你的目标进入 [基础运行时](../core/index.md)、[游戏运行时](../game/index.md)、[Godot 集成](../godot/index.md) 或 [源码生成器](../source-generators/index.md)
4. 需要完整示例时，再进入 [教程总览](../tutorials/index.md)
