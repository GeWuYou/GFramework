# GFramework.SourceGenerators.Common

`GFramework.SourceGenerators.Common` 是 `GFramework` 多个源码生成器共享的公共支撑目录。它不是独立推广的运行时或
生成器采用入口，而是跟随各个 `*.SourceGenerators` 模块一起演化的内部基础设施。

## 目录定位

这个目录当前主要承载三类公共能力：

- 生成器基类
  - 例如 `AttributeClassGeneratorBase`、`AttributeEnumGeneratorBase`、`MetadataAttributeClassGeneratorBase`
- 共享诊断
  - 例如 `Diagnostics/CommonDiagnostics.cs`
- 共享符号与冲突处理
  - 例如 `Extensions/GeneratedMethodConflictExtensions.cs`

如果你在 `Core`、`CQRS`、`Game`、`Godot` 侧生成器里看到相似的诊断或生成冲突语义，通常就是这里在统一约束。

## 与相邻模块的关系

- `GFramework.Core.SourceGenerators`
  - 复用这里的生成器基类和部分通用 diagnostics。
- `GFramework.Cqrs.SourceGenerators`
  - 以这里作为编译期公共依赖，并把公共 DLL 一起打进 analyzer 包。
- `GFramework.Godot.SourceGenerators`
  - 同样复用这里的公共实现和共享约束。

这个目录不会单独作为消费包提供。对 NuGet 使用者来说，更实际的入口仍然是具体的
`GeWuYou.GFramework.*.SourceGenerators` 包。

## 什么时候需要读这里

- 你在排查多个生成器共有的 diagnostics
- 你想确认“为什么不同生成器对命名冲突采用同一套规则”
- 你在扩展或维护生成器，而不是只消费它们

如果你的目标只是“选包并开始使用”，优先回到对应生成器模块 README 和文档专题页。

## 继续阅读

- [源码生成器总览](../docs/zh-CN/source-generators/index.md)
- [Core 源码生成器说明](../GFramework.Core.SourceGenerators/README.md)
- [CQRS 源码生成器说明](../GFramework.Cqrs.SourceGenerators/README.md)
- [Godot 源码生成器说明](../GFramework.Godot.SourceGenerators/README.md)
