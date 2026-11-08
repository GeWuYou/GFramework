---
# https://vitepress.dev/reference/default-theme-home-page
layout: home
title: GFramework
description: 概览 GFramework 的模块能力、安装选包路径，以及 Core、CQRS、Game、Godot 与配置工具入口。
hero:
  name: "GFramework"
  text: 面向游戏开发的模块化 C# 架构体系
  tagline: 基于清洁架构与 CQRS 思想构建，覆盖运行时、源码生成器、Godot 集成与 AI-First 配置工作流
  image:
    src: /logo.png
    alt: GFramework Logo
  actions:
    - theme: brand
      text: 快速开始
      link: /zh-CN/getting-started/quick-start
    - theme: alt
      text: 安装与选包
      link: /zh-CN/getting-started/installation
    - theme: alt
      text: CQRS
      link: /zh-CN/core/cqrs
    - theme: alt
      text: 配置工具
      link: /zh-CN/game/config-tool

features:
  - title: 🏗 清洁架构分层
    details: 基于 Model–Controller–System–Utility 五层结构组织运行时能力，适合先从 Core 起步，再逐层叠加 Game、CQRS 和引擎集成。

  - title: 🔧 CQRS 请求模型
    details: 提供 request、notification、pipeline、handler registry 与 source generator 协作路径，适合把新业务统一收敛到 CQRS runtime。

  - title: 🧭 模块化选包路径
    details: 支持按运行时、抽象层、源码生成器和引擎集成拆分安装，而不是先引入一个难以裁剪的大而全包。

  - title: 🎮 Godot 集成
    details: 在保持 Core / Game 运行时边界的前提下，补齐节点扩展、场景与 UI 接线、协程桥接和生成器辅助。

  - title: 🧩 AI-First 配置工作流
    details: 通过 YAML + JSON Schema + Source Generator + VS Code 工具，把静态内容配置、校验、表单预览和批量编辑串成一条链路；正式契约来自 `GFramework.Game` Runtime 与 `GFramework.Game.SourceGenerators` 的共享 schema 子集，编辑器工具只负责辅助编辑与预览。

  - title: ⚡ Roslyn 源码生成器
    details: 自动生成枚举扩展、优先级、配置类型、CQRS registry 和 Godot 辅助代码，并复用共享 diagnostics 约束生成行为。
---

AI-First 配置工作流的默认采用路径以闭合对象模型为前提：当前使用 `additionalProperties: false` 作为对象边界，`oneOf` / `anyOf` 不在默认入口范围内。遇到超出共享 schema 子集的复杂 shape，请直接回到 raw YAML 与 schema 设计本体处理，而不是把它当成编辑器能力遗漏。
