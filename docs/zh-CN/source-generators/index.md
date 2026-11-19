---
title: "源码生成器"
description: "按编译期任务选择 GFramework 生成器。"
---

# 源码生成器 {#overview}

在消费项目添加对应的 `GeWuYou.GFramework.*.SourceGenerators` 包。配套特性和生成辅助随包提供。构建后可在 IDE 的生成源文件中查看输出和诊断。

| 包后缀 | 功能 |
| --- | --- |
| `Core.SourceGenerators` | [枚举扩展](../source-generators/enum-generator.md) · [优先级](../source-generators/priority-generator.md) |
| `Game.SourceGenerators` | [配置类型与表](../source-generators/schema-config-generator.md) |
| `Cqrs.SourceGenerators` | [处理器注册表](../source-generators/cqrs-handler-registry-generator.md) |
| `Godot.SourceGenerators` | [项目元数据](../source-generators/godot-project-generator.md) · [节点查找](../source-generators/get-node-generator.md) · [信号绑定](../source-generators/bind-node-signal-generator.md) · [场景辅助](../source-generators/auto-scene-generator.md) · [集合注册](../source-generators/auto-register-exported-collections-generator.md) |
