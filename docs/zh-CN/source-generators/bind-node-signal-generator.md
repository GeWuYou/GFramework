# BindNodeSignal 生成器

> 为带 `[BindNodeSignal]` 的方法生成信号绑定与解绑辅助方法。

## 概述

`BindNodeSignalGenerator` 位于 `GFramework.Godot.SourceGenerators.BindNodeSignalGenerator`，负责生成：

- `__BindNodeSignals_Generated()`
- `__UnbindNodeSignals_Generated()`

它只收敛事件订阅和退订样板代码，不负责自动创建或改写 `_Ready()`、`_ExitTree()`。

## 基础使用

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

public partial class MainMenu : Control
{
    [GetNode]
    private Button _startButton = null!;

    [BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
    private void OnStartButtonPressed()
    {
        StartGame();
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

生成器会产出类似下面的代码：

```csharp
private void __BindNodeSignals_Generated()
{
    _startButton.Pressed += OnStartButtonPressed;
}

private void __UnbindNodeSignals_Generated()
{
    _startButton.Pressed -= OnStartButtonPressed;
}
```

## 参数

`[BindNodeSignal]` 需要两个非空字符串构造参数：

| 参数              | 说明             |
|-----------------|----------------|
| `nodeFieldName` | 目标节点字段名        |
| `signalName`    | 目标字段上的 CLR 事件名 |

推荐始终使用 `nameof(...)`，避免字段或事件重命名后字符串失效。

## 当前行为边界

- 生成器不会自动帮你把 `__BindNodeSignals_Generated()` 写进 `_Ready()`
- 也不会自动帮你把 `__UnbindNodeSignals_Generated()` 写进 `_ExitTree()`
- 如果目标类型已经声明了 `_Ready()` / `_ExitTree()` 但没有调用对应生成方法，会分别得到：
    - `GF_Godot_BindNodeSignal_008`
    - `GF_Godot_BindNodeSignal_009`

## 当前诊断

- `GF_Godot_BindNodeSignal_001`：不支持嵌套类
- `GF_Godot_BindNodeSignal_002`：目标方法不能是 `static`
- `GF_Godot_BindNodeSignal_003`：引用的字段不存在
- `GF_Godot_BindNodeSignal_004`：引用字段必须是实例字段
- `GF_Godot_BindNodeSignal_005`：字段类型必须继承 `Godot.Node`
- `GF_Godot_BindNodeSignal_006`：事件名不存在
- `GF_Godot_BindNodeSignal_007`：方法签名与事件委托不兼容
- `GF_Godot_BindNodeSignal_008`：已有 `_Ready()` 但未调用 `__BindNodeSignals_Generated()`
- `GF_Godot_BindNodeSignal_009`：已有 `_ExitTree()` 但未调用 `__UnbindNodeSignals_Generated()`
- `GF_Godot_BindNodeSignal_010`：构造参数不是有效的非空字符串
