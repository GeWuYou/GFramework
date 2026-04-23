---
title: Source Generators
description: 按模块梳理 GFramework 当前发布的源码生成器包、运行时归属与推荐选包入口。
---

# Source Generators

`Source Generators` 栏目对应 `GFramework` 当前按模块拆分发布的编译期工具链。

这里的重点不是“存在一个统一的大生成器包”，而是帮助你确认应该安装哪个生成器包、它服务哪个运行时模块，以及继续去看哪一类专题页。

## 当前包拆分

GFramework 当前发布的生成器包是：

- `GeWuYou.GFramework.Core.SourceGenerators`
- `GeWuYou.GFramework.Game.SourceGenerators`
- `GeWuYou.GFramework.Cqrs.SourceGenerators`
- `GeWuYou.GFramework.Godot.SourceGenerators`

不存在 `GeWuYou.GFramework.SourceGenerators` 或 `*.SourceGenerators.Attributes` 这类聚合包。

## 先按场景选包

- 想减少日志、上下文注入、模块自动注册等 Core 侧样板代码：
  - 选择 `GeWuYou.GFramework.Core.SourceGenerators`
- 想把 `schemas/**/*.schema.json` 生成成配置类型和表包装：
  - 选择 `GeWuYou.GFramework.Game.SourceGenerators`
- 想让 CQRS handler registry 在编译期生成，缩小运行时反射扫描范围：
  - 选择 `GeWuYou.GFramework.Cqrs.SourceGenerators`
- 想在 Godot 项目里生成 AutoLoad / Input Action 入口、节点 / 信号样板，或补齐 Scene/UI 包装与导出集合注册辅助：
  - 选择 `GeWuYou.GFramework.Godot.SourceGenerators`

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

## 这个栏目怎么读

### Core 侧通用生成器

- [logging-generator](./logging-generator.md)
- [context-aware-generator](./context-aware-generator.md)
- [context-get-generator](./context-get-generator.md)
- [enum-generator](./enum-generator.md)
- [priority-generator](./priority-generator.md)
- [auto-register-module-generator](./auto-register-module-generator.md)

### Game / CQRS 相关生成器

- 配置 schema 生成与运行时接法：
  - [../game/config-system.md](../game/config-system.md)
- CQRS handler registry 生成器：
  - [cqrs-handler-registry-generator](./cqrs-handler-registry-generator.md)
- CQRS 模块族采用入口：
  - [../core/cqrs.md](../core/cqrs.md)

### Godot 专用生成器

- [godot-project-generator](./godot-project-generator.md)
- [get-node-generator](./get-node-generator.md)
- [bind-node-signal-generator](./bind-node-signal-generator.md)
- [auto-ui-page-generator](./auto-ui-page-generator.md)
- [auto-scene-generator](./auto-scene-generator.md)
- [auto-register-exported-collections-generator](./auto-register-exported-collections-generator.md)

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

- [`GFramework.Core.SourceGenerators/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Core.SourceGenerators/README.md)
- [`GFramework.Game.SourceGenerators/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Game.SourceGenerators/README.md)
- [`GFramework.Cqrs.SourceGenerators/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Cqrs.SourceGenerators/README.md)
- [`GFramework.Godot.SourceGenerators/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Godot.SourceGenerators/README.md)
