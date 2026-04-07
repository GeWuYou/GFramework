# GetNode 生成器

> 为带 `[GetNode]` 的字段生成 Godot 节点查找辅助。

## 概述

`GetNodeGenerator` 位于 `GFramework.Godot.SourceGenerators.GetNodeGenerator`，负责为 `Godot.Node` 字段生成
`__InjectGetNodes_Generated()`。

当前实现支持：

- 显式路径
- 由字段名推导路径
- `Lookup` 查找模式
- `Required = false` 的可选节点

## 基础使用

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

public partial class PlayerHud : Control
{
    [GetNode]
    private Label _healthLabel = null!;

    [GetNode("ScoreContainer/ScoreValue")]
    private Label _scoreLabel = null!;

    public override void _Ready()
    {
        __InjectGetNodes_Generated();
        _healthLabel.Text = "100";
    }
}
```

生成器会产出类似下面的代码：

```csharp
private void __InjectGetNodes_Generated()
{
    _healthLabel = GetNode<global::Godot.Label>("%HealthLabel");
    _scoreLabel = GetNode<global::Godot.Label>("ScoreContainer/ScoreValue");
}
```

## 生命周期行为

### 你没有手写 `_Ready()`

如果目标类型没有声明 `_Ready()`，生成器会额外补出：

- 一个 `public override void _Ready()`
- 一个 `partial void OnGetNodeReadyGenerated()`

自动生成的 `_Ready()` 会先调用 `__InjectGetNodes_Generated()`，再调用 `OnGetNodeReadyGenerated()`。

### 你已经手写 `_Ready()`

如果目标类型自己声明了 `_Ready()`，生成器不会改写你的生命周期方法。

这时你需要自己在 `_Ready()` 中调用 `__InjectGetNodes_Generated()`；否则会收到诊断 `GF_Godot_GetNode_006`。

## 路径推导

- 显式 `Path` 或构造参数优先
- 未显式指定路径时，生成器会根据字段名推导节点名
- 默认 `Lookup` 为 `Auto`，当前会落到 unique-name 风格前缀 `%`

例如：

| 字段名                  | 推导结果               |
|----------------------|--------------------|
| `_healthLabel`       | `%HealthLabel`     |
| `m_confirmButton`    | `%ConfirmButton`   |
| `_player_name_label` | `%PlayerNameLabel` |

## 当前诊断

- `GF_Godot_GetNode_001`：不支持嵌套类
- `GF_Godot_GetNode_002`：字段不能是 `static`
- `GF_Godot_GetNode_003`：字段不能是 `readonly`
- `GF_Godot_GetNode_004`：字段类型必须继承 `Godot.Node`
- `GF_Godot_GetNode_005`：无法从字段名推导路径
- `GF_Godot_GetNode_006`：已有 `_Ready()` 但未调用 `__InjectGetNodes_Generated()`
