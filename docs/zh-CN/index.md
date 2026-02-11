---
# https://vitepress.dev/reference/default-theme-home-page
layout: home
title: GFramework
hero:
  name: "GFramework"
  text: 面向游戏开发的模块化 C# 架构体系
  tagline: 基于清洁架构与 CQRS 思想构建，支持可扩展设计与多引擎集成
  image:
    src: /logo.png
    alt: GFramework Logo
  actions:
    - theme: brand
      text: 快速开始
      link: /zh-CN/getting-started/quick-start
    - theme: alt
      text: 架构概览
      link: /zh-CN/getting-started/architecture

features:
  - title: 🏗 清洁架构分层
    details: 基于 Model–Controller–System–Utility 五层结构设计，实现职责清晰、可测试、可维护的代码组织方式。

  - title: 🔧 CQRS 命令查询分离
    details: 通过类型安全的命令与查询系统构建业务流程，支持可扩展操作链与可撤销机制。

  - title: 📡 类型安全事件系统
    details: 提供高性能事件总线，实现模块间松耦合通信与可扩展的业务触发机制。

  - title: 🎮 引擎集成层
    details: 核心层与引擎层解耦设计，当前提供 Godot 集成实现，支持节点扩展、协程桥接与对象池能力。

  - title: 🔄 响应式属性系统
    details: 可绑定属性模型驱动 UI 更新与状态变化，构建声明式的数据响应流程。

  - title: ⚡ Roslyn 源码生成器
    details: 自动生成日志、枚举扩展与规则代码，减少样板代码并提升开发效率。
---