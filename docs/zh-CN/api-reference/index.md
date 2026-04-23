---
title: API 参考
description: GFramework 的 API 阅读入口，按模块映射 README、专题页、XML 文档和教程链路。
---

# API 参考

这里不再维护一份脱离源码演化的“伪 API 列表”。

当前 `GFramework` 的 API 参考链路以四类证据协同为准：

1. 模块 README：说明包关系、最小接入路径和目录边界
2. `docs/zh-CN` 专题页：说明采用顺序、生命周期和使用建议
3. 代码中的 XML 文档：说明公开 / 内部类型和关键成员的契约
4. 教程页：说明这些 API 在真实接入路径中的组合方式

## 阅读顺序

### 想确认“该装哪个包、先看哪类 API”

先读模块 README，再读对应 landing page：

- 入门入口：[`../getting-started/index.md`](../getting-started/index.md)
- 根模块地图：仓库根 [`README.md`](https://github.com/GeWuYou/GFramework/blob/main/README.md)

### 想确认“这个功能属于哪个模块”

按下面的模块映射进入对应入口：

| 模块族 | 模块 README | 站内入口 | XML 文档关注点 |
| --- | --- | --- | --- |
| `Core` / `Core.Abstractions` | [`GFramework.Core/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Core/README.md)、[`GFramework.Core.Abstractions/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Core.Abstractions/README.md) | [`../core/index.md`](../core/index.md)、[`../abstractions/core-abstractions.md`](../abstractions/core-abstractions.md) | 架构入口、生命周期、命令 / 查询 / 事件 / 状态 / 资源 / 日志 / 配置 / 并发契约 |
| `Cqrs` / `Cqrs.Abstractions` / `Cqrs.SourceGenerators` | [`GFramework.Cqrs/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Cqrs/README.md)、[`GFramework.Cqrs.Abstractions/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Cqrs.Abstractions/README.md)、[`GFramework.Cqrs.SourceGenerators/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Cqrs.SourceGenerators/README.md) | [`../core/cqrs.md`](../core/cqrs.md)、[`../source-generators/cqrs-handler-registry-generator.md`](../source-generators/cqrs-handler-registry-generator.md) | request / notification / handler / pipeline / registry / fallback contract |
| `Game` / `Game.Abstractions` / `Game.SourceGenerators` | [`GFramework.Game/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Game/README.md)、[`GFramework.Game.Abstractions/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Game.Abstractions/README.md)、[`GFramework.Game.SourceGenerators/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Game.SourceGenerators/README.md) | [`../game/index.md`](../game/index.md)、[`../abstractions/game-abstractions.md`](../abstractions/game-abstractions.md) | 配置、数据、设置、场景、UI、存储、序列化契约 |
| `Godot` / `Godot.SourceGenerators` | [`GFramework.Godot/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Godot/README.md)、[`GFramework.Godot.SourceGenerators/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Godot.SourceGenerators/README.md) | [`../godot/index.md`](../godot/index.md)、[`../source-generators/godot-project-generator.md`](../source-generators/godot-project-generator.md)、[`../source-generators/get-node-generator.md`](../source-generators/get-node-generator.md)、[`../source-generators/bind-node-signal-generator.md`](../source-generators/bind-node-signal-generator.md) | 节点扩展、场景 / UI 适配、配置 / 存储 / 设置接线、Godot 生成器入口 |
| `Ecs.Arch` / `Ecs.Arch.Abstractions` | [`GFramework.Ecs.Arch/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Ecs.Arch/README.md)、[`GFramework.Ecs.Arch.Abstractions/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Ecs.Arch.Abstractions/README.md) | [`../ecs/index.md`](../ecs/index.md)、[`../ecs/arch.md`](../ecs/arch.md)、[`../abstractions/ecs-arch-abstractions.md`](../abstractions/ecs-arch-abstractions.md) | ECS 模块契约、系统适配、配置对象和运行时装配边界 |

## 先看 XML，还是先看教程

### 先看 XML 文档的情况

- 你在确认公开类型的约束、线程 / 生命周期语义、参数和返回值契约
- 你需要区分“抽象层保证了什么”和“默认实现额外提供了什么”
- 你在做多模块拆分、测试替身或扩展适配层

优先关注这些类型族：

- 架构 / 模块 / 服务入口
- 生命周期、注册、路由、工厂、provider 契约
- Source Generator 的 attribute、diagnostic 和 generated contract

### 先看教程和专题页的情况

- 你要的是最小接入路径，而不是逐个类型审计
- 你想确认模块组合方式、目录约定和推荐接线顺序
- 你在做从旧入口迁移到新入口的采用决策

优先入口：

- 教程概览：[`../tutorials/index.md`](../tutorials/index.md)
- 最佳实践：[`../best-practices/index.md`](../best-practices/index.md)
- 故障排查：[`../troubleshooting.md`](../troubleshooting.md)

## 当前边界

- `GFramework.Core.SourceGenerators.Abstractions`
- `GFramework.Godot.SourceGenerators.Abstractions`
- `GFramework.SourceGenerators.Common`

这些目录当前不是独立消费模块，因此不单独维护站内 API 参考入口。它们的公开说明跟随所属模块 README 和
`source-generators` 栏目维护。

## 使用方式

把本页当成“API 阅读导航”而不是“签名快照”：

- 先选模块
- 再进 README 和专题页确认采用路径
- 最后回到代码里的 XML 文档核对具体契约

当 README、专题页和 XML 文档出现冲突时，以源码和测试所反映的当前实现为准。
