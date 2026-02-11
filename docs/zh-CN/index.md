---
# https://vitepress.dev/reference/default-theme-home-page
layout: home

hero:
  name: "GFramework"
  text: "面向游戏开发场景的模块化 C# 框架"
  tagline: 基于清洁架构和CQRS模式的现代化游戏开发框架
  image:
    src: /logo.png
    alt: GFramework Logo
  actions:
    - theme: brand
      text: 快速开始
      link: /zh-CN/getting-started/quick-start
    - theme: alt
      text: 架构概览
      link: /zh-CN/getting-started/architecture-overview

features:
  - title: 🏗️ 清洁架构
    details: 基于Model-View-Controller-System-Utility五层架构，实现清晰的职责分离和高内聚低耦合
  - title: 🔧 CQRS模式
    details: 命令查询职责分离，提供类型安全的命令和查询系统，支持可撤销操作
  - title: 📡 事件驱动
    details: 强大的事件总线系统，支持类型安全的事件发布订阅，实现组件间松耦合通信
  - title: 🎮 Godot集成
    details: 深度集成Godot引擎，提供丰富的节点扩展方法和对象池化支持
  - title: 🔄 响应式编程
    details: 可绑定属性系统，自动化的数据绑定和UI更新机制
  - title: ⚡ 源码生成器
    details: 基于Roslyn的源码生成器，自动生成日志、枚举扩展等样板代码

---

