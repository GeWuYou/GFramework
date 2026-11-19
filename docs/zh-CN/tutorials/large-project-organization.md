---
title: "按功能组织游戏项目"
description: "让用例、领域状态和原生 UI 各有清晰的装配边界。"
---

# 按功能组织游戏项目 {#overview}

按 Gameplay、Progress、Settings 等功能组织代码。把需要独立测试的规则放入纯 C# 项目；在 Godot 工程中实现对应适配并从组合根连接。用参数和依赖接口明确数据从哪里来、结果交给谁。

参见[进阶 Godot 项目](../tutorials/godot-advanced-project.md)和[组织依赖](../best-practices/architecture-patterns.md)。
