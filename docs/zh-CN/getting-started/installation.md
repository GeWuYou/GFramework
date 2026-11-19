---
title: "安装与选包"
description: "为项目选择 NuGet 包，配置源码构建和 Godot 环境。"
---

# 安装与选包 {#overview}

## 选择运行时 {#packages}

| 需要 | 安装的 NuGet 包 |
| --- | --- |
| 生命周期、事件、状态、资源、协程 | `GeWuYou.GFramework.Core` |
| 请求、通知和流式处理 | `GeWuYou.GFramework.Cqrs` |
| 配置、内容包、模组、存档、场景和 UI | `GeWuYou.GFramework.Game` |
| 在 Godot 中使用 Game 能力 | `GeWuYou.GFramework.Godot` |
| 用 Arch 运行 ECS 系统 | `GeWuYou.GFramework.Ecs.Arch` |
| 为代码模组接入 Harmony 方法补丁 | `GeWuYou.GFramework.Game.Harmony` |

```bash
dotnet add package GeWuYou.GFramework.Core
```

`Game` 会带入 `Core`，`Godot` 会带入 `Game`。独立的契约项目可直接引用相应 `.Abstractions`。聚合包 `GeWuYou.GFramework` 提供 Core、Game 和 CQRS 的常用运行时组合。

## 环境 {#requirements}

运行时包提供 `net8.0`、`net9.0` 和 `net10.0` 目标。安装时按选定 NuGet 版本核对目标框架和依赖。运行仓库样例或构建源码时，按 `global.json` 使用 .NET SDK 10.0.403 或同一功能带内较新的补丁。Godot 示例使用 Godot .NET 4.7.2。

## 编译期生成 {#generators}

按功能在消费项目加入对应 `.SourceGenerators` 包。[生成器指南](../source-generators/index.md)列出了枚举、配置、CQRS 和 Godot 的生成入口。

使用 NuGet 时可在项目属性中设置 `<EnableGFrameworkGlobalUsings>true</EnableGFrameworkGlobalUsings>`，开启包提供的全局导入。用 `GFrameworkExcludedUsing` 项排除单个命名空间。

下一步：[运行最小示例](../getting-started/quick-start.md)。
