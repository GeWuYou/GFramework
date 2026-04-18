# GFramework.Core.SourceGenerators

`GFramework.Core.SourceGenerators` 承载 Core 侧的通用源码生成器与分析器，用来减少样板代码并把部分约束前移到编译期。

## 模块定位

这个包属于编译期工具链，不是运行时库。

当前仓库中的主要目录：

- `Architectures/`
- `Analyzers/`
- `Bases/`
- `Enums/`
- `Logging/`
- `Rule/`
- `Diagnostics/`

对应的生成器家族主要包括：

- 日志相关生成器
- `ContextAware` 及上下文注入辅助
- 枚举扩展生成器
- 优先级相关生成器
- 模块自动注册
- 注册可见性分析器

## 包关系

- 运行时：`GFramework.Core`
- 契约与特性：`GFramework.Core.SourceGenerators.Abstractions`
- 公共生成器支撑：`GFramework.SourceGenerators.Common`

如果你还需要游戏配置 schema 生成或 Godot 专用生成器，应分别安装：

- `GFramework.Game.SourceGenerators`
- `GFramework.Godot.SourceGenerators`

## 主要能力

### 上下文注入与 `ContextAware`

该包支持围绕上下文感知类型生成辅助代码，例如：

- `[ContextAware]`
- `[GetModel]`
- `[GetModels]`
- `[GetSystem]`
- `[GetSystems]`
- `[GetUtility]`
- `[GetUtilities]`
- `[GetService]`
- `[GetServices]`
- `[GetAll]`

这类生成器适合用于 View、Controller、Godot 节点包装或其他需要频繁访问架构上下文的类型。

### 日志辅助

支持通过生成器减少 `ILogger` 相关样板代码。

### 注册分析器

包内还包含分析器，用来检查 `Model`、`System`、`Utility` 的使用点是否能在所属架构中找到静态可见注册，帮助尽早发现“代码可以编译、运行时却缺注册”的问题。

## 最小接入路径

```xml
<ItemGroup>
  <PackageReference Include="GeWuYou.GFramework.Core.SourceGenerators"
                    Version="x.y.z"
                    PrivateAssets="all"
                    ExcludeAssets="runtime" />
</ItemGroup>
```

如果你想查看生成代码，可在消费者项目里启用：

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

## 适用场景

- 你希望减少上下文绑定与日志相关样板代码
- 你需要在编译期发现部分注册可见性问题
- 你在做模块化架构，希望固定某些重复注册模式

## 对应文档

- 源码生成器总览：[`../docs/zh-CN/source-generators/index.md`](../docs/zh-CN/source-generators/index.md)
- Core 栏目：[`../docs/zh-CN/core/index.md`](../docs/zh-CN/core/index.md)
