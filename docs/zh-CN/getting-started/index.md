# 入门指南

这一部分只回答三个问题：

1. `GFramework` 由哪些模块组成
2. 第一次接入应该从哪个包开始
3. 最小可运行路径是什么

如果你还没决定具体用法，先阅读本栏目；如果你已经明确要用某个模块，直接进入对应模块目录下的 `README.md` 会更快。

## 推荐起步路径

### 只想先把架构跑起来

从 `Core` 开始：

- `GeWuYou.GFramework.Core`
- `GeWuYou.GFramework.Core.Abstractions`

这组包提供：

- `Architecture`
- `Model` / `System` / `Utility`
- 旧版 `Command` / `Query` 执行器
- 事件、属性、状态机、状态管理、资源、日志、协程等基础设施

对应文档：

- [`../core/index.md`](../core/index.md)
- [`quick-start.md`](./quick-start.md)

### 想用新版 CQRS

在 `Core` 基础上补：

- `GeWuYou.GFramework.Cqrs`
- `GeWuYou.GFramework.Cqrs.Abstractions`

这组包提供：

- 统一 request dispatcher
- notification publish
- pipeline behaviors
- handler 注册与反射回退机制

对应文档：

- [`../core/cqrs.md`](../core/cqrs.md)
- 仓库内模块入口：[`GFramework.Cqrs/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Cqrs/README.md)

### 想做游戏运行时

在 `Core` 基础上按需补：

- `GeWuYou.GFramework.Game`
- `GeWuYou.GFramework.Game.Abstractions`

这组包提供：

- 内容配置系统
- 数据存取与设置
- Scene / UI / Routing 抽象与运行时
- 文件存储和序列化

对应文档：

- [`../game/index.md`](../game/index.md)
- 仓库内模块入口：[`GFramework.Game/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Game/README.md)

### 想接入 Godot

继续叠加：

- `GeWuYou.GFramework.Godot`

对应文档：

- [`../godot/index.md`](../godot/index.md)
- 仓库内模块入口：[`GFramework.Godot/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Godot/README.md)

## Source Generators 什么时候装

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

## 建议阅读顺序

1. [`quick-start.md`](./quick-start.md)
2. 你准备使用的模块 README
3. 对应栏目页，例如 `core/`、`game/`、`godot/`
4. 需要更完整示例时，再进入 `tutorials/`

## 注意

- 旧文档里有一些早期示例已经和当前 API 漂移。本栏目以后只保留经过代码或测试核对的最小路径。
- 若根 README、模块 README 与某篇专题页冲突，以模块 README 和当前代码为准。
