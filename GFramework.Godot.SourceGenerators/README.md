# GFramework.Godot.SourceGenerators

面向 Godot 场景的源码生成扩展模块，减少模板代码。

## 主要功能

- 与 Godot 场景相关的编译期生成能力
- 基于 Roslyn 的增量生成器实现
- `project.godot` 项目元数据生成，产出 AutoLoad 与 Input Action 的强类型访问入口
- `[GetNode]` 字段注入，减少 `_Ready()` 里的 `GetNode<T>()` 样板代码
- `[BindNodeSignal]` 方法绑定，减少 `_Ready()` / `_ExitTree()` 中重复的事件订阅样板代码

## 使用建议

- 仅在 Godot + C# 项目中启用
- 非 Godot 项目可只使用 GFramework.SourceGenerators
- 当项目通过 NuGet 包引用本模块时，根目录下的 `project.godot` 会被自动加入 `AdditionalFiles`
- 当项目通过 `ProjectReference(OutputItemType=Analyzer)` 直接引用生成器时，需要手动把 `project.godot` 加入
  `AdditionalFiles`

## project.godot 集成

默认情况下，生成器会读取 Godot 项目根目录下的 `project.godot`，并生成：

- `GFramework.Godot.Generated.AutoLoads`
- `GFramework.Godot.Generated.InputActions`

如果你需要覆盖默认项目文件路径，可以在 MSBuild 中设置：

- 路径可以调整到项目根目录下的其他位置
- 文件名必须仍然是 `project.godot`，否则生成器会发出警告并忽略该文件

```xml
<PropertyGroup>
  <GFrameworkGodotProjectFile>Config/project.godot</GFrameworkGodotProjectFile>
</PropertyGroup>
```

如果你在仓库内通过 analyzer 形式直接引用本项目，则需要显式配置：

```xml
<ItemGroup>
  <AdditionalFiles Include="project.godot" />
</ItemGroup>
```

## AutoLoad 强类型访问

当某个 AutoLoad 无法仅靠类型名唯一推断到 C# 节点类型时，可以使用 `[AutoLoad]` 显式声明映射：

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

[AutoLoad("GameServices")]
public partial class GameServices : Node
{
}
```

对应 `project.godot`：

```ini
[autoload]
GameServices="*res://autoload/game_services.tscn"
AudioBus="*res://autoload/audio_bus.gd"
```

生成器会产出统一入口：

```csharp
using GFramework.Godot.Generated;

var gameServices = AutoLoads.GameServices;

if (AutoLoads.TryGetAudioBus(out var audioBus))
{
}
```

- 显式 `[AutoLoad]` 映射优先于隐式类型名推断
- 若同名映射冲突，生成器会给出诊断并退化为 `Godot.Node` 访问
- 若无法映射到 C# 节点类型，仍会生成可用的 `Godot.Node` 访问器

## Input Action 常量生成

`project.godot` 的 `[input]` 段会自动生成稳定常量，避免手写字符串：

```ini
[input]
move_up={
}
ui_cancel={
}
```

```csharp
using GFramework.Godot.Generated;

if (Input.IsActionJustPressed(InputActions.MoveUp))
{
}
```

- 动作名会转换为可补全的 C# 标识符，例如 `move_up -> MoveUp`
- 当多个动作名映射到同一标识符时，会追加稳定后缀并给出警告

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
