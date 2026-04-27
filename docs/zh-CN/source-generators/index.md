---
title: 源码生成器
description: 按模块梳理 GFramework 当前发布的源码生成器包、运行时归属与推荐选包入口。
---

# 源码生成器

`Source Generators` 栏目对应 `GFramework` 当前按模块拆分发布的编译期工具链。

如果你当前最关心的是“我该装哪个生成器包、它服务哪个运行时、接下来该去哪看示例或专题页”，先看这一页。

## 当前包拆分

GFramework 当前发布的生成器包是：

- `GeWuYou.GFramework.Core.SourceGenerators`
- `GeWuYou.GFramework.Game.SourceGenerators`
- `GeWuYou.GFramework.Cqrs.SourceGenerators`
- `GeWuYou.GFramework.Godot.SourceGenerators`

不存在 `GeWuYou.GFramework.SourceGenerators` 或 `*.SourceGenerators.Attributes` 这类聚合包。

## 先按场景选包

| 使用场景 | 安装包 | 继续阅读 |
| --- | --- | --- |
| 减少日志、上下文注入、模块自动注册等 Core 侧样板代码 | `GeWuYou.GFramework.Core.SourceGenerators` | [Core 模块](../core/index.md)、[日志生成器](./logging-generator.md)、[ContextAware 生成器](./context-aware-generator.md) |
| 把 `schemas/**/*.schema.json` 生成成配置类型和表包装 | `GeWuYou.GFramework.Game.SourceGenerators` | [配置系统](../game/config-system.md)、[VS Code 配置工具](../game/config-tool.md) |
| 让 CQRS handler registry 在编译期生成，缩小运行时反射扫描范围 | `GeWuYou.GFramework.Cqrs.SourceGenerators` | [CQRS 运行时](../core/cqrs.md)、[CQRS Handler Registry 生成器](./cqrs-handler-registry-generator.md) |
| 在 Godot 项目里生成 AutoLoad / Input Action 入口、节点 / 信号样板，或补齐 Scene/UI 包装与导出集合注册辅助 | `GeWuYou.GFramework.Godot.SourceGenerators` | [Godot 模块总览](../godot/index.md)、[Godot 项目生成器](./godot-project-generator.md)、[GetNode 生成器](./get-node-generator.md) |

## 与运行时的关系

这些包都是编译期工具链，不是运行时库。

对应关系如下：

| 生成器包 | 主要服务的运行时 |
| --- | --- |
| `GFramework.Core.SourceGenerators` | `GFramework.Core` |
| `GFramework.Game.SourceGenerators` | `GFramework.Game` |
| `GFramework.Cqrs.SourceGenerators` | `GFramework.Cqrs` |
| `GFramework.Godot.SourceGenerators` | `GFramework.Godot` |

安装时通常保持生成器包与对应运行时包版本一致，并将生成器声明为：

```xml
<PackageReference Include="GeWuYou.GFramework.Core.SourceGenerators"
                  Version="x.y.z"
                  PrivateAssets="all"
                  ExcludeAssets="runtime" />
```

其他生成器包的安装模式相同。

## 共享支撑模块

除了上面的可直接安装包，仓库里还有三类跟随这些生成器共同演化的支撑目录：

- `GFramework.SourceGenerators.Common`
  - 承载跨生成器共享的基类、通用 diagnostics 和生成冲突规则。
- `GFramework.Core.SourceGenerators.Abstractions`
  - 承载 `Core` 侧生成器特性定义，例如 `[Log]`、`[ContextAware]`、`[GetModel]`、`[GenerateEnumExtensions]`。
- `GFramework.Godot.SourceGenerators.Abstractions`
  - 承载 Godot 侧生成器特性定义，例如 `[GetNode]`、`[BindNodeSignal]`、`[AutoScene]`、`[AutoUiPage]`。

这些目录当前都不是新的安装入口。更实用的理解方式是：

- 先判断你要装哪个 `*.SourceGenerators` 包
- 再根据 attribute 或 diagnostics 回到对应专题页
- 只有在排查生成失败原因时，才继续下钻到这些共享支撑目录

## 这个栏目怎么读

### Core 侧通用生成器

- [日志生成器](./logging-generator.md)
- [ContextAware 生成器](./context-aware-generator.md)
- [ContextGet 生成器](./context-get-generator.md)
- [枚举生成器](./enum-generator.md)
- [Priority 生成器](./priority-generator.md)
- [模块自动注册生成器](./auto-register-module-generator.md)

### Game / CQRS 相关生成器

- 配置 schema 生成与运行时接法：
  - [配置系统](../game/config-system.md)
- CQRS handler registry 生成器：
  - [CQRS Handler Registry 生成器](./cqrs-handler-registry-generator.md)
- CQRS 模块族采用入口：
  - [CQRS 运行时](../core/cqrs.md)

### Godot 专用生成器

- [Godot 项目生成器](./godot-project-generator.md)
- [GetNode 生成器](./get-node-generator.md)
- [BindNodeSignal 生成器](./bind-node-signal-generator.md)
- [AutoUiPage 生成器](./auto-ui-page-generator.md)
- [AutoScene 生成器](./auto-scene-generator.md)
- [AutoRegisterExportedCollections 生成器](./auto-register-exported-collections-generator.md)

## 推荐接入顺序

1. 先确认你已经选定运行时层，而不是先装生成器
2. 再按运行时模块补充对应的生成器包
3. 只在确实需要的项目里安装生成器，避免为了“可能以后会用”而把所有包一起引入

例如：

- 新项目只需要 Core 上下文注入和日志辅助：
  - 安装 `Core` + `Core.SourceGenerators`
- 需要静态 YAML 配置：
  - 安装 `Game` + `Game.SourceGenerators`
- 需要 CQRS 生成注册表：
  - 安装 `Cqrs` + `Cqrs.SourceGenerators`

## 对应模块入口

- [Core 模块](../core/index.md)
- [Game 模块总览](../game/index.md)
- [CQRS 运行时](../core/cqrs.md)
- [Godot 模块总览](../godot/index.md)
