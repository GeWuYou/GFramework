---
title: Godot 项目元数据生成器
description: 说明 project.godot 当前会生成什么、何时生效，以及 AutoLoad 和 Input Action 的映射边界。
---

# Godot 项目元数据生成器

`GodotProjectMetadataGenerator` 读取 `project.godot`，把 Godot 工程级配置转成稳定的编译期入口。

当前只覆盖两类信息：

- `[autoload]` 段生成 `GFramework.Godot.Generated.AutoLoads`
- `[input]` 段生成 `GFramework.Godot.Generated.InputActions`

它不处理场景节点注入，也不处理节点事件绑定。这两部分分别由 `/zh-CN/source-generators/get-node-generator` 和
`/zh-CN/source-generators/bind-node-signal-generator` 负责。

## 当前包关系

- 特性来源：`GFramework.Godot.SourceGenerators.Abstractions`
- 生成器实现：`GFramework.Godot.SourceGenerators`
- 运行时依赖：`GFramework.Godot`
- 消费侧生成命名空间：`GFramework.Godot.Generated`

## 最小接入路径

### NuGet 引用

常规 Godot C# 项目安装 `GeWuYou.GFramework.Godot.SourceGenerators` 后，包内 `targets` 会自动做两件事：

1. 注入 analyzer
2. 如果项目根目录存在 `project.godot`，把它加入 `AdditionalFiles`

```xml
<ItemGroup>
  <PackageReference Include="GeWuYou.GFramework.Godot.SourceGenerators"
                    Version="x.y.z"
                    PrivateAssets="all"
                    ExcludeAssets="runtime" />
</ItemGroup>
```

### 仓库内直接引用生成器

如果你通过 `ProjectReference(OutputItemType=Analyzer)` 直接引用生成器项目，需要自己把 `project.godot` 放进
`AdditionalFiles`：

```xml
<ItemGroup>
  <ProjectReference Include="..\GFramework.Godot.SourceGenerators\GFramework.Godot.SourceGenerators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <AdditionalFiles Include="project.godot" />
</ItemGroup>
```

## 当前会生成什么

### AutoLoad 入口

假设 `project.godot` 中有：

```ini
[autoload]
GameServices="*res://autoload/game_services.tscn"
AudioBus="*res://autoload/audio_bus.gd"
```

生成器会产出：

```csharp
using GFramework.Godot.Generated;

var gameServices = AutoLoads.GameServices;

if (AutoLoads.TryGetAudioBus(out var audioBus))
{
}
```

当前输出同时包含：

- `AutoLoads.<Name>`
- `AutoLoads.TryGet<Name>(out TNode? value)`

这些访问器最终都通过当前 `SceneTree.Root` 解析 `/root/<AutoLoadName>`。

### Input Action 常量

假设 `project.godot` 中有：

```ini
[input]
move_up={
}
ui_cancel={
}
```

生成器会产出：

```csharp
using GFramework.Godot.Generated;

if (Input.IsActionJustPressed(InputActions.MoveUp))
{
}
```

这部分只生成稳定字符串常量，不会替你封装 `Input` 调用。

## AutoLoad 类型推断的当前规则

### 优先级顺序

当前映射顺序是：

1. 显式 `[AutoLoad("Name")]`
2. 按 C# 类型名与 AutoLoad 名称做唯一匹配
3. 无法唯一确定时退化为 `Godot.Node`

例如：

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

[AutoLoad("GameServices")]
public partial class GameServices : Node
{
}
```

这类显式映射优先于按类名推断。

### 什么时候会退化成 `Godot.Node`

以下情况不会中断全部生成，但会把对应入口退化成 `Godot.Node` 并报告诊断：

- 多个类型显式映射到同一个 AutoLoad
- 不同命名空间下出现同名 `Node` 类型，导致隐式推断不唯一
- 对应条目实际无法唯一绑定到一个 C# 节点类型

## `project.godot` 文件约束

### 可以改路径，不能改文件名

NuGet `targets` 支持通过 `GFrameworkGodotProjectFile` 改相对路径：

```xml
<PropertyGroup>
  <GFrameworkGodotProjectFile>Config/project.godot</GFrameworkGodotProjectFile>
</PropertyGroup>
```

但当前生成器按文件名识别 `project.godot`，所以：

- `Config/project.godot` 可以
- `Config/game.project` 不可以

如果文件名不是 `project.godot`，`targets` 会给出 warning，生成器也会忽略该文件。

### 缺文件或空节时不会生成任何代码

按当前测试，下面几种情况都不会产出源码，也不会报告额外诊断：

- 没有把 `project.godot` 传进 `AdditionalFiles`
- `project.godot` 是空文件
- `[autoload]` / `[input]` 只有空节，没有有效条目

## 标识符与重复条目的当前语义

### 标识符冲突

如果不同名字清洗后落到同一个 C# 标识符，生成器会追加稳定后缀并报告诊断，例如：

- `move_up` -> `MoveUp`
- `move-up` -> `MoveUp_2`

AutoLoad 名称也遵循同样的冲突处理策略。

### 重复条目

如果同一个 `project.godot` 里重复声明同名 AutoLoad 或 Input Action，当前行为是：

- 报告诊断
- 只保留第一条声明参与生成

这和“冲突后同时生成多个重名成员”不是一回事。

## 与场景级生成器的边界

这项能力解决的是“项目级元数据入口”：

- `AutoLoads`
- `InputActions`

场景级样板仍然需要其他生成器：

- 节点字段注入：`[GetNode]`
- 节点 CLR event 订阅：`[BindNodeSignal]`

在 `ai-libs/CoreGrid` 中，这三类能力是并行使用的：`project.godot` 负责 AutoLoad / Input Action，具体 UI 或场景节点再通过
`[GetNode]` 和 `[BindNodeSignal]` 处理。

## 诊断与约束

当前最值得记住的约束有这些：

- `[AutoLoad]` 只能标在继承 `Godot.Node` 的类型上
- 显式或隐式 AutoLoad 映射不唯一时，会退化为 `Godot.Node`
- 标识符冲突会追加稳定后缀，而不是覆盖已有成员
- 重复条目只保留第一条声明

## 推荐阅读

1. [GetNode 生成器](./get-node-generator.md)
2. [BindNodeSignal 生成器](./bind-node-signal-generator.md)
3. [Godot 集成教程](../tutorials/godot-integration.md)
4. [Godot 模块总览](../godot/index.md)
