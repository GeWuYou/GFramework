# Godot 项目元数据生成器

> 从 `project.godot` 生成 AutoLoad 与 Input Action 的强类型访问入口。

## 概述

`GFramework.Godot.SourceGenerators` 会读取 Godot 项目根目录下的 `project.godot`，并把其中最常用的项目级元数据暴露为稳定的编译期
API。

当前覆盖：

- `[autoload]` 段：生成 `GFramework.Godot.Generated.AutoLoads`
- `[input]` 段：生成 `GFramework.Godot.Generated.InputActions`

这项能力的目标不是替代场景级生成器，而是把 Godot 工程配置和 C# 代码之间的字符串约定收敛到编译期。

## 接入方式

### NuGet 引用

当项目通过 NuGet 引用 `GeWuYou.GFramework.Godot.SourceGenerators` 时，生成器会默认把项目根目录下的 `project.godot` 加入
`AdditionalFiles`。

如需覆盖默认路径，可以设置：

```xml
<PropertyGroup>
  <GFrameworkGodotProjectFile>project.godot</GFrameworkGodotProjectFile>
</PropertyGroup>
```

### 仓库内直接引用生成器

如果你通过 `ProjectReference(OutputItemType=Analyzer)` 直接引用生成器项目，则需要手动加入：

```xml
<ItemGroup>
  <AdditionalFiles Include="project.godot" />
</ItemGroup>
```

## AutoLoad 访问层

### 基础行为

假设 `project.godot` 中声明了：

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

- 对于能唯一映射到 C# 节点类型的条目，属性会是强类型的
- 对于无法映射或对应非 C# 脚本的条目，属性会退化为 `Godot.Node`
- 生成器通过 `Godot.Engine.GetMainLoop()` 与当前 `SceneTree.Root` 解析 `/root/<AutoLoadName>` 节点

### 显式映射

当 AutoLoad 名称无法仅靠类名唯一推断时，可以使用 `[AutoLoad]` 明确指定：

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

[AutoLoad("GameServices")]
public partial class GameServices : Node
{
}
```

规则如下：

- 显式 `[AutoLoad]` 映射优先于隐式类名推断
- 标记了 `[AutoLoad]` 的类型必须继承 `Godot.Node`
- 若多个类型映射到同一个 AutoLoad，生成器会报告诊断，并退化为 `Godot.Node` 访问器，直到映射唯一

## Input Action 常量

### 基础行为

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

转换规则：

- `move_up` -> `MoveUp`
- `ui_cancel` -> `UiCancel`
- 非法字符会被清理后再转换为 PascalCase
- 如果多个动作名落到同一个标识符，生成器会追加稳定数字后缀，例如 `MoveUp_2`

## 与现有 Godot 生成器的关系

这项能力和现有的场景级生成器是互补的：

- `AutoLoads` / `InputActions` 解决的是项目级元数据访问
- `[GetNode]` 解决的是场景节点引用注入
- `[BindNodeSignal]` 解决的是节点事件订阅样板

推荐组合方式：

```csharp
using GFramework.Godot.Generated;
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

public partial class MainHud : Control
{
    [GetNode]
    private Button _startButton = null!;

    public override void _Ready()
    {
        __InjectGetNodes_Generated();

        if (Input.IsActionPressed(InputActions.UiCancel))
        {
        }

        var services = AutoLoads.GameServices;
    }
}
```

## 诊断与约束

当前会重点报告以下问题：

- `[AutoLoad]` 标记在非 `Godot.Node` 类型上
- 多个类型映射到同一个 AutoLoad 名称
- 不同 AutoLoad 名称或 Input Action 名称在清洗后发生标识符冲突
- `project.godot` 内部重复声明同名 AutoLoad 或 Input Action

这些诊断的目的不是阻断所有生成，而是在可能的情况下保留稳定输出，同时把不确定性显式暴露出来。

## 相关文档

- [GetNode 生成器](./get-node-generator)
- [BindNodeSignal 生成器](./bind-node-signal-generator)
- [Godot 集成教程](../tutorials/godot-integration)
