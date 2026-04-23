---
title: BindNodeSignal 生成器
description: 说明 [BindNodeSignal] 当前生成什么、如何与 GetNode 协作，以及 _Ready 和 _ExitTree 的接入要求。
---

# BindNodeSignal 生成器

`[BindNodeSignal]` 把 Godot CLR event 的 `+=` / `-=` 样板收敛成生成方法。它只生成“如何订阅与解绑”，不会替你查找节点，也不会自动生成完整生命周期方法。

## 当前包关系

- 特性来源：`GFramework.Godot.SourceGenerators.Abstractions`
- 生成器实现：`GFramework.Godot.SourceGenerators`
- 目标字段基线：`nodeFieldName` 指向的字段必须继承 `Godot.Node`

## 最小用法

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

public partial class Hud : Control
{
    [GetNode]
    private Button _startButton = null!;

    [GetNode]
    private SpinBox _startOreSpinBox = null!;

    [BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
    private void OnStartButtonPressed()
    {
    }

    [BindNodeSignal(nameof(_startOreSpinBox), nameof(SpinBox.ValueChanged))]
    private void OnStartOreValueChanged(double value)
    {
    }

    public override void _Ready()
    {
        __InjectGetNodes_Generated();
        __BindNodeSignals_Generated();
    }

    public override void _ExitTree()
    {
        __UnbindNodeSignals_Generated();
    }
}
```

当前生成器会产出：

```csharp
private void __BindNodeSignals_Generated()
{
    _startButton.Pressed += OnStartButtonPressed;
    _startOreSpinBox.ValueChanged += OnStartOreValueChanged;
}

private void __UnbindNodeSignals_Generated()
{
    _startButton.Pressed -= OnStartButtonPressed;
    _startOreSpinBox.ValueChanged -= OnStartOreValueChanged;
}
```

## 生命周期边界

### 它只生成辅助方法，不生成 `_Ready()` / `_ExitTree()`

这是当前和 `[GetNode]` 最大的区别：

- `[GetNode]` 在缺少 `_Ready()` 时会补一个 override
- `[BindNodeSignal]` 只生成 `__BindNodeSignals_Generated()` 和 `__UnbindNodeSignals_Generated()`

所以你需要自己决定在哪个生命周期里调用它们。

### 已有生命周期但没调用时会给 warning

如果类型已经定义了 `_Ready()` 或 `_ExitTree()`，但没有调用对应生成方法，当前会给出 warning，提醒你完成接线。

这意味着它更像“声明式订阅语法”，而不是“自动生命周期织入”。

## 当前契约

`[BindNodeSignal(nodeFieldName, signalName)]` 的两个参数都指向现有代码里的稳定符号：

- `nodeFieldName`：目标节点字段名
- `signalName`：该节点类型上的 CLR event 名

最推荐的写法仍然是：

```csharp
[BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
```

这样字段或事件改名时，编译器能一起帮你更新。

## 当前会验证什么

生成器不是盲目拼字符串。按当前源码，它会在编译期验证：

- 方法必须是实例方法
- `nodeFieldName` 必须能解析到当前类型里的实例字段
- 该字段类型必须继承 `Godot.Node`
- `signalName` 必须能解析到该字段类型上的 CLR event
- 处理方法签名必须和 event delegate 兼容

例如：

- `Button.Pressed` 对应无参处理方法
- `SpinBox.ValueChanged` 对应 `double` 参数

如果签名不匹配，会直接报错，而不是生成一个运行时才失败的订阅。

## 多重绑定

`BindNodeSignalAttribute` 允许重复标记在同一个方法上，所以一个处理方法可以绑定多个事件：

```csharp
[BindNodeSignal(nameof(_buttonA), nameof(Button.Pressed))]
[BindNodeSignal(nameof(_buttonB), nameof(Button.Pressed))]
[BindNodeSignal(nameof(_buttonC), nameof(Button.Pressed))]
private void OnAnyButtonPressed()
{
}
```

当前生成器会为每个特性都生成一条 `+=` 和一条 `-=`。

`ai-libs/CoreGrid` 里的 `GameplayHud`、`PauseMenu` 和 `OptionBrowser` 都在大量使用这种声明式绑定方式。

## 与 GetNode 的协作边界

`[BindNodeSignal]` 不负责拿到字段实例，只负责在字段已经可用的前提下做事件接线。

因此同类型同时使用时，顺序应该是：

1. `__InjectGetNodes_Generated()`
2. `__BindNodeSignals_Generated()`
3. 在 `_ExitTree()` 调用 `__UnbindNodeSignals_Generated()`

这是当前项目侧真实采用路径，不是文档偏好。

## 当前强约束

以下约束直接来自生成器源码与测试：

- 目标类型必须是顶层 `partial class`
- 不支持嵌套类
- 方法不能是 `static`
- 节点字段必须存在且是实例字段
- 节点字段类型必须继承 `Godot.Node`
- 事件名必须是 CLR event，不是任意字符串
- 如果你自己声明了 `__BindNodeSignals_Generated()` 或 `__UnbindNodeSignals_Generated()`，会触发命名冲突诊断

## 什么时候适合用 `[BindNodeSignal]`

适合：

- UI、菜单、HUD、面板类里按钮或输入事件很多
- 你想把订阅/解绑语义放回方法声明旁边，而不是堆在 `_Ready()` / `_ExitTree()`
- 你已经用 `[GetNode]` 或其他方式稳定拿到节点字段

不适合：

- 事件目标需要在运行时动态决定
- 你用的是 `Connect()` / `Disconnect()` 风格，而不是 CLR event
- 你需要比“字段 + 事件名”更复杂的订阅条件

## 与旧写法的边界

下面这些旧说法已经不准确：

- “`[BindNodeSignal]` 会自动生成 `_Ready()` / `_ExitTree()`”
- “它能处理所有 Godot signal 连接方式”
- “有没有 `__UnbindNodeSignals_Generated()` 都无所谓”

当前更准确的理解是：

- 它只生成成对的绑定/解绑辅助方法
- 当前设计面向 CLR event，不自动调用 `Connect()` / `Disconnect()`
- 如果要避免节点退出后残留订阅，应在 `_ExitTree()` 中显式解绑

## 推荐阅读

1. [/zh-CN/source-generators/get-node-generator](./get-node-generator.md)
2. [/zh-CN/source-generators/godot-project-generator](./godot-project-generator.md)
3. [/zh-CN/godot/ui](../godot/ui.md)
4. [`GFramework.Godot.SourceGenerators/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Godot.SourceGenerators/README.md)
