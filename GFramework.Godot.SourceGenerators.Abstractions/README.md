# GFramework.Godot.SourceGenerators.Abstractions

`GFramework.Godot.SourceGenerators.Abstractions` 存放 Godot 侧源码生成器公开使用的 attribute 和最小辅助类型。
它不是单独推广的消费包，而是 `GFramework.Godot.SourceGenerators` 的支撑层。

## 目录定位

这里当前主要提供：

- 节点与信号相关特性
  - `GetNodeAttribute`
  - `BindNodeSignalAttribute`
- 场景 / 集合注册相关特性
  - `AutoSceneAttribute`
  - `AutoRegisterExportedCollectionsAttribute`
  - `RegisterExportedCollectionAttribute`
- Godot 项目级辅助类型
  - `AutoLoadAttribute`
  - `GodotModuleMarker`
  - `NodeLookupMode`

这些类型负责定义“消费端项目可以声明哪些特性与参数”，而具体的生成逻辑、diagnostics 和生命周期约束仍在
`GFramework.Godot.SourceGenerators` 中。

## 与相邻模块的关系

- `GFramework.Godot.SourceGenerators`
  - 实际消费这里定义的 attribute，并生成节点注入、信号绑定、Scene 包装与项目元数据辅助代码。
- `GFramework.SourceGenerators.Common`
  - 为 Godot 侧生成器提供共享基础设施和通用约束。
- `GFramework.Godot`
  - 提供运行时宿主与集成层；生成器只负责编译期辅助。

当前目录本身 `IsPackable=false`。对 NuGet 使用者来说，更实际的入口仍然是
`GeWuYou.GFramework.Godot.SourceGenerators`；这个 abstractions DLL 会跟随对应 analyzer 包一起交付。

## 采用与维护入口

- 你在确认 `[GetNode]`、`[BindNodeSignal]`、`[AutoScene]` 等特性的公开参数语义
- 你在排查生成器文档是否和 attribute 契约一致
- 你在扩展 Godot 侧生成器，想先看清契约层边界

如果你的目标只是把生成器接进项目，优先回到 `Godot.SourceGenerators` README 和专题页。

## 继续阅读

- [Godot 源码生成器说明](../GFramework.Godot.SourceGenerators/README.md)
- [源码生成器总览](../docs/zh-CN/source-generators/index.md)
- [GetNode 生成器](../docs/zh-CN/source-generators/get-node-generator.md)
- [BindNodeSignal 生成器](../docs/zh-CN/source-generators/bind-node-signal-generator.md)
