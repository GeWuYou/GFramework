---
title: GetNode 生成器
description: 说明 [GetNode] 当前生成什么、路径如何推断，以及 _Ready 生命周期里的接入边界。
---

# GetNode 生成器

`[GetNode]` 用来把 Godot 节点查找样板收敛到生成器里。它只处理“字段如何取到节点”，不负责事件订阅，也不负责其他运行时装配。

## 当前包关系

- 特性来源：`GFramework.Godot.SourceGenerators.Abstractions`
- 生成器实现：`GFramework.Godot.SourceGenerators`
- 目标类型基线：字段类型必须继承 `Godot.Node`

## 最小用法

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

public partial class TopBar : HBoxContainer
{
    [GetNode]
    private HBoxContainer _leftContainer = null!;

    [GetNode]
    private HBoxContainer m_rightContainer = null!;
}
```

如果目标类型还没有 `_Ready()`，当前生成器会补出：

```csharp
private void __InjectGetNodes_Generated()
{
    _leftContainer = GetNode<global::Godot.HBoxContainer>("%LeftContainer");
    m_rightContainer = GetNode<global::Godot.HBoxContainer>("%RightContainer");
}

partial void OnGetNodeReadyGenerated();

public override void _Ready()
{
    __InjectGetNodes_Generated();
    OnGetNodeReadyGenerated();
}
```

这个行为来自当前生成器测试，不是文档约定。

## 当前路径推断规则

### 没写路径时

如果 `[GetNode]` 没有显式路径，当前默认按字段名推导唯一名路径：

- `_leftContainer` -> `%LeftContainer`
- `m_rightContainer` -> `%RightContainer`

也就是说，默认不是普通相对路径，而是 Godot 的 `%Name` 唯一名语法。

### 显式路径优先

```csharp
[GetNode("ScoreContainer/ScoreValue")]
private Label _scoreLabel = null!;
```

显式路径会直接进入生成结果，不再按字段名推断。

## `Lookup` 与 `Required` 的当前语义

### `Lookup`

`GetNodeAttribute.Lookup` 支持 4 个模式：

- `Auto`
- `UniqueName`
- `RelativePath`
- `AbsolutePath`

对文档来说，最关键的结论是：

- `Auto` 在未给路径时默认走唯一名推断
- 显式路径会结合 `Lookup` 决定最终生成的字符串

### `Required`

默认 `Required = true`，生成器会调用 `GetNode<T>()`：

```csharp
[GetNode]
private Label _title = null!;
```

如果设为 `false`，生成器会改用 `GetNodeOrNull<T>()`：

```csharp
[GetNode(Required = false, Lookup = NodeLookupMode.RelativePath)]
private HBoxContainer? _rightContainer;
```

当前生成结果会是：

```csharp
_rightContainer = GetNodeOrNull<global::Godot.HBoxContainer>("RightContainer");
```

所以可选节点最好同时用可空字段类型表达你的意图。

## 生命周期边界

### 没有 `_Ready()` 时

生成器会补：

- `__InjectGetNodes_Generated()`
- `partial void OnGetNodeReadyGenerated()`
- 一个 `public override void _Ready()`

`OnGetNodeReadyGenerated()` 只在这种“生成器自己补 `_Ready()`”的路径里出现。

### 已经有 `_Ready()` 时

如果类型已经实现了 `_Ready()`，生成器不会覆盖它，也不会再额外生成 `OnGetNodeReadyGenerated()`。你必须自己调用：

```csharp
public override void _Ready()
{
    __InjectGetNodes_Generated();
}
```

如果 `_Ready()` 存在但没有调用生成方法，当前会给出 warning，提醒你手动接入。

## 当前强约束

这些约束都直接来自生成器源码和测试：

- 目标类型必须是顶层 `partial class`
- 不支持嵌套类
- 字段必须是实例字段
- 字段不能是 `readonly`
- 字段类型必须继承 `Godot.Node`
- 如果无法从字段名或显式参数推断出路径，会报错
- 如果你自己定义了 `__InjectGetNodes_Generated()`，会触发命名冲突诊断

## 与 BindNodeSignal 的配合顺序

如果同一个类型同时用了 `[GetNode]` 和 `[BindNodeSignal]`，当前推荐顺序是：

```csharp
public override void _Ready()
{
    __InjectGetNodes_Generated();
    __BindNodeSignals_Generated();
}
```

先注入节点，再绑定事件；否则 `BindNodeSignal` 对应的字段还没完成解析。

这也是 `ai-libs/CoreGrid` 里项目侧节点类的实际用法。

## 什么时候适合用 `[GetNode]`

适合：

- 节点字段很多，`GetNode<T>()` 样板明显重复
- 你希望把“字段名到节点路径”的约定收敛到声明式特性
- 你在 Godot `Control`、`Node`、`CanvasLayer` 等项目侧类型上频繁访问子节点

不适合：

- 目标不是 `Godot.Node`
- 节点路径完全动态，必须在运行时决定
- 你需要更复杂的节点查找策略，而不是字段级静态描述

## 与旧写法的边界

下面这些旧理解已经不准确：

- “`[GetNode]` 总会自动帮你改写 `_Ready()`”
- “不管是否已有 `_Ready()`，都会生成 `OnGetNodeReadyGenerated()`”
- “可选节点只是文档建议，生成结果不会变”

当前更准确的理解是：

- 只有缺少 `_Ready()` 时才会自动补 override
- `OnGetNodeReadyGenerated()` 只存在于自动补 `_Ready()` 的路径
- `Required = false` 会真实切换到 `GetNodeOrNull<T>()`

## 推荐阅读

1. [/zh-CN/source-generators/bind-node-signal-generator](./bind-node-signal-generator.md)
2. [/zh-CN/source-generators/godot-project-generator](./godot-project-generator.md)
3. [/zh-CN/godot/ui](../godot/ui.md)
4. [`GFramework.Godot.SourceGenerators/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Godot.SourceGenerators/README.md)
