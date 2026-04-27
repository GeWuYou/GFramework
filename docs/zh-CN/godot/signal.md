---
title: Godot 信号系统
description: 以当前 GFramework.Godot 源码与 CoreGrid 的动态绑定用法为准，说明 Signal(...) fluent API、SignalBuilder 行为与接入边界。
---

# Godot 信号系统

`GFramework.Godot` 当前提供的信号能力很收敛：它不是另一套事件系统，也不是自动生成绑定代码的入口，而是对
`GodotObject.Connect(...)` 的一层 fluent 包装。

当前真正公开的入口只有两个：

- `SignalFluentExtensions.Signal(...)`
- `SignalBuilder`

如果你需要的是场景节点字段注入和静态 signal 自动绑订，请看
`GFramework.Godot.SourceGenerators` 的 `[GetNode]` 与 `[BindNodeSignal]`，不要把它们和这里的运行时 fluent API 混成同一层。

## 当前公开入口

### `Signal(...)`

`Signal(...)` 是定义在 `GodotObject` 上的扩展方法：

```csharp
public static SignalBuilder Signal(this GodotObject @object, StringName signal)
```

它只做一件事：基于目标对象和 signal 名称创建一个 `SignalBuilder`。这意味着当前 fluent API 不只适用于 `Node`，也适用于
其他 Godot 对象。

### `SignalBuilder`

`SignalBuilder` 的当前行为来自运行时代码本身：

- `WithFlags(GodotObject.ConnectFlags flags)`
  - 把 flags 保存到 builder 内部，作为后续 `To(...)` / `ToAndCall(...)` 的默认连接选项
- `To(Callable callable, GodotObject.ConnectFlags? flags = null)`
  - 优先使用参数传入的 flags；如果没有，再回退到之前 `WithFlags(...)` 保存的值
  - 最终直接调用 `target.Connect(signal, callable)` 或 `target.Connect(signal, callable, (uint)flags)`
- `ToAndCall(Callable callable, GodotObject.ConnectFlags? flags = null, params Variant[] args)`
  - 先执行 `To(...)`
  - 再立即执行一次 `callable.Call(args)`
- `End()`
  - 返回原始 `GodotObject`
  - 主要用于在 fluent 语句结束后重新拿回目标对象，而不是增加新的信号语义

可以把它理解成“对原生 `Connect(...)` 做顺手的链式包装”，而不是带订阅管理、自动解绑、诊断系统的高层抽象。

## 最小接入路径

### 1. 动态绑定时直接用 `Signal(...)`

适合这类场景：

- 运行时创建的节点或弹窗
- signal 名称需要按条件选择
- 你就是想保留手写 `Callable` 的控制权

最小示例：

```csharp
using GFramework.Godot.Extensions.Signal;
using Godot;

public partial class SettingsPanel : Control
{
    public override void _Ready()
    {
        var applyButton = GetNode<Button>("%ApplyButton");

        applyButton.Signal(Button.SignalName.Pressed)
                   .To(Callable.From(OnApplyPressed));
    }

    private void OnApplyPressed()
    {
    }
}
```

### 2. 需要连接 flags 时，用 `WithFlags(...)`

`SignalBuilder` 不会解释 flags 的业务含义，只是把它们原样传给 Godot。

```csharp
button.Signal(Button.SignalName.Pressed)
      .WithFlags(GodotObject.ConnectFlags.OneShot)
      .To(Callable.From(OnStartPressed));
```

如果某一次连接想覆盖默认 flags，可以直接在 `To(...)` / `ToAndCall(...)` 上传第二个参数。

### 3. 只有在“连接后立即跑一次”时才用 `ToAndCall(...)`

`ToAndCall(...)` 的语义很直接：先连，再立刻调一次 handler。它适合“先补一次当前状态，再继续监听变化”的场景。

```csharp
slider.Signal(Range.SignalName.ValueChanged)
      .ToAndCall(Callable.From<double>(OnVolumeChanged), args: [(Variant)slider.Value]);
```

这类调用要求 handler 对“初始化时主动调用一次”是安全的；如果你的处理逻辑不是幂等的，继续用 `To(...)` 更稳妥。

### 4. 静态场景绑定优先交给 `[BindNodeSignal]`

从 [源码生成器总览](../source-generators/index.md) 和当前 Godot 接线方式看，静态场景按钮、滑条、菜单项这类固定
节点，更常见的路径仍然是 `[BindNodeSignal]`：

```csharp
[BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
private void OnStartPressed()
{
}
```

而 `Signal(...)` 更常出现在这些动态或补充性绑定里：

- 对话框确认 / 取消等运行时实例
- 运行时选出的 signal 名称
- 需要临时追加监听的 dock、panel、overlay

`ai-libs/CoreGrid` 当前就有这类用法：

```csharp
_quitConfirmDialog.Signal("Confirmed")
                  .To(Callable.From(OnQuitConfirmDialogConfirmed))
                  .End();
```

## 什么时候用 fluent API，什么时候用生成器

- 用 `Signal(...)`
  - 动态节点
  - 动态 signal 名称
  - 想保留手写 `Callable` 和连接 flags
- 用 `[BindNodeSignal]`
  - 节点字段和 signal 都是静态已知
  - 你已经在用 `[GetNode]`
  - 希望把 `_Ready()` 里的重复绑定样板交给生成器

这两条路径是互补关系，不是前后代际关系。当前源码没有“先用 `CreateSignalBuilder(...)`，再升级到生成器”这种迁移链。

## 当前边界

- 当前入口是 `Signal(...)`，不是旧文档里的 `CreateSignalBuilder(...)`
- 这里不会自动生成 `_Ready()` / `_ExitTree()`，这类能力属于 `GFramework.Godot.SourceGenerators`
- `SignalBuilder` 不提供取消订阅 token，也不会替你包装 `Disconnect(...)`
- `End()` 只返回原始对象，不会提交额外配置，也不是必须调用的终止步骤
- signal 名称是否合法、callable 签名是否匹配，仍然遵循 Godot 自身运行时规则
- `ToAndCall(...)` 会在完成连接后立刻执行 handler；如果 handler 有副作用，需要你自己确认时机

## 继续阅读

- [Godot 运行时集成](./index.md)
- [Godot 扩展方法](./extensions.md)
- [Godot 集成教程](../tutorials/godot-integration.md)
- [BindNodeSignal 生成器](../source-generators/bind-node-signal-generator.md)
