# GFramework.Godot.SourceGenerators

面向 Godot 场景的源码生成扩展模块，减少模板代码。

## 主要功能

- 与 Godot 场景相关的编译期生成能力
- 基于 Roslyn 的增量生成器实现
- `[GetNode]` 字段注入，减少 `_Ready()` 里的 `GetNode<T>()` 样板代码
- `[BindNodeSignal]` 方法绑定，减少 `_Ready()` / `_ExitTree()` 中重复的事件订阅样板代码

## 使用建议

- 仅在 Godot + C# 项目中启用
- 非 Godot 项目可只使用 GFramework.SourceGenerators

## GetNode 用法

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

public partial class TopBar : HBoxContainer
{
    [GetNode]
    private HBoxContainer _leftContainer = null!;

    [GetNode]
    private HBoxContainer _rightContainer = null!;

    public override void _Ready()
    {
        __InjectGetNodes_Generated();
        OnReadyAfterGetNode();
    }

    private void OnReadyAfterGetNode()
    {
    }
}
```

当未显式填写路径时，生成器会默认将字段名推导为唯一名路径：

- `_leftContainer` -> `%LeftContainer`
- `m_rightContainer` -> `%RightContainer`

## BindNodeSignal 用法

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

生成器会产出两个辅助方法：

- `__BindNodeSignals_Generated()`：负责统一订阅事件
- `__UnbindNodeSignals_Generated()`：负责统一解绑事件

当前设计只处理 CLR event 形式的 Godot 事件绑定，不会自动调用 `Connect()` / `Disconnect()`。
