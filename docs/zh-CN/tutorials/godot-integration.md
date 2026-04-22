---
title: Godot 集成教程
description: 以当前源码和真实消费者接线为准，说明 GFramework 在 Godot 项目中的最小接入路径、生成器协作顺序与常见迁移边界。
---

# Godot 集成教程

这篇教程只讲当前仓库里仍然成立的 Godot 接入路径：

- 项目级配置：`project.godot` -> `AutoLoads` / `InputActions`
- 场景级样板：`[GetNode]` / `[BindNodeSignal]`
- 运行时辅助：节点生命周期、事件解绑、异步等待

它不再把旧版长篇 API 列表当事实来源，也不把 `AbstractGodotModule` / `InstallGodotModule(...)` 当成默认接入起点。

## 先认清包关系

一个常见的 Godot + GFramework 项目，通常会同时用到这几层：

- `GeWuYou.GFramework`：聚合包，提供 Core / Game 常用能力
- `GeWuYou.GFramework.Godot`：Godot 运行时扩展，例如节点生命周期辅助、`UnRegisterWhenNodeExitTree`、`WaitUntilReadyAsync`
- `GeWuYou.GFramework.Core.SourceGenerators`：`[ContextAware]`、`[GetSystem]`、`[GetModel]` 等 Core 侧生成器
- `GeWuYou.GFramework.Godot.SourceGenerators`：`[GetNode]`、`[BindNodeSignal]`、`project.godot` 元数据生成器

如果你只装运行时包，没有装生成器包，那么 `[GetNode]`、`[BindNodeSignal]`、`AutoLoads`、`InputActions` 都不会出现。

## 第一步：接好项目级配置

### 安装包

```bash
dotnet add package GeWuYou.GFramework
dotnet add package GeWuYou.GFramework.Godot
dotnet add package GeWuYou.GFramework.Core.SourceGenerators
dotnet add package GeWuYou.GFramework.Godot.SourceGenerators
```

`GeWuYou.GFramework.Godot.SourceGenerators` 作为 NuGet 包使用时，会自动把项目根目录下的 `project.godot` 加入
`AdditionalFiles`。

### 什么时候需要手动加 `AdditionalFiles`

只有在仓库内直接用 analyzer 方式引用生成器项目时，才需要手动补：

```xml
<ItemGroup>
  <ProjectReference Include="..\GFramework.Godot.SourceGenerators\GFramework.Godot.SourceGenerators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <AdditionalFiles Include="project.godot" />
</ItemGroup>
```

### 可以改路径，不能改文件名

如果你的 `project.godot` 不在项目根目录，可以改相对路径：

```xml
<PropertyGroup>
  <GFrameworkGodotProjectFile>Config/project.godot</GFrameworkGodotProjectFile>
</PropertyGroup>
```

但文件名仍然必须是 `project.godot`。当前 `targets` 和生成器都会按这个文件名识别输入。

## 第二步：把架构和 Godot 节点分开看

当前真实消费者 `ai-libs/CoreGrid` 的默认接入方式，是：

- 架构层继续用常规 `InstallModule(new SomeModule())`
- Godot 节点脚本通过运行时扩展和源码生成器接入场景

也就是说，Godot 集成的主体并不是“所有东西都塞进 Godot 模块”，而是“架构注册”和“场景节点接线”各自负责自己的边界。

一个最小架构可以长这样：

```csharp
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Godot.Architectures;

namespace MyGame.Scripts.Core;

public sealed class GameArchitecture(
    IArchitectureConfiguration configuration,
    IEnvironment environment)
    : AbstractArchitecture(configuration, environment)
{
    protected override void InstallModules()
    {
        InstallModule(new UtilityModule());
        InstallModule(new ModelModule());
        InstallModule(new GameplayModule());
        InstallModule(new SystemModule());
    }
}
```

这里选择 `AbstractArchitecture`，是为了让架构生命周期能挂到 Godot 场景树；但模块注册本身仍然是普通的
`InstallModule(...)`。

## 第三步：把 `project.godot` 变成强类型入口

`GFramework.Godot.SourceGenerators` 会读取 `project.godot` 的两个段：

- `[autoload]` -> `GFramework.Godot.Generated.AutoLoads`
- `[input]` -> `GFramework.Godot.Generated.InputActions`

例如：

```ini
[autoload]
GameServices="*res://autoload/game_services.tscn"

[input]
ui_cancel={
}
move_up={
}
```

就可以在 C# 里直接使用：

```csharp
using GFramework.Godot.Generated;
using Godot;

if (AutoLoads.TryGetGameServices(out var gameServices))
{
}

if (Input.IsActionJustPressed(InputActions.UiCancel))
{
}
```

这部分只解决“项目级配置入口”，不会处理场景里的节点查找或事件绑定。

## 第四步：把场景节点样板交给生成器

当前最稳定的场景级接入方式，是让 `[GetNode]` 负责节点字段注入，让 `[BindNodeSignal]` 负责 CLR event 订阅。

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

namespace MyGame.Scripts.Ui;

public partial class MainMenu : Control
{
    [GetNode]
    private Button _startButton = null!;

    [GetNode]
    private Button _quitButton = null!;

    public override void _Ready()
    {
        __InjectGetNodes_Generated();
        __BindNodeSignals_Generated();
    }

    public override void _ExitTree()
    {
        __UnbindNodeSignals_Generated();
    }

    [BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
    private void OnStartPressed()
    {
    }

    [BindNodeSignal(nameof(_quitButton), nameof(Button.Pressed))]
    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
```

这里有几个当前实现里的硬边界：

- `[GetNode]` 默认按字段名推导 `%UniqueName` 路径
- `[BindNodeSignal]` 只生成 `+=` / `-=` 辅助方法，不会自动生成 `_Ready()` / `_ExitTree()`
- 同时使用两者时，顺序应该是先 `__InjectGetNodes_Generated()`，再 `__BindNodeSignals_Generated()`

如果你的类型还同时用了 Core 侧的 `[ContextAware]`，则顺序继续保持为：

```csharp
public override void _Ready()
{
    __InjectGetNodes_Generated();
    __InjectContextBindings_Generated();
    __BindNodeSignals_Generated();
}
```

这也是 `ai-libs/CoreGrid` 里项目侧节点类的真实顺序。

## 第五步：只在运行时处理真正需要运行时决定的东西

源码生成器负责静态样板，运行时扩展负责生命周期和动态操作。当前最常用的几个入口是：

- `UnRegisterWhenNodeExitTree(this Node node)`：把框架事件解绑挂到节点退出树时机
- `WaitUntilReadyAsync()`：等待节点真正进入场景树
- `AddChildXAsync()`：添加子节点后等待其 ready
- `Signal(...)`：基于 `GodotObject.Connect(...)` 的 fluent 包装

例如，事件解绑可以这样写：

```csharp
using GFramework.Godot.Extensions;
using Godot;

public partial class SettingsPanel : Control, IController
{
    public override void _Ready()
    {
        this.RegisterEvent<SettingsChangedEvent>(OnSettingsChanged)
            .UnRegisterWhenNodeExitTree(this);
    }

    private void OnSettingsChanged(SettingsChangedEvent @event)
    {
    }
}
```

手动信号接线时，当前 API 叫 `Signal(...)`，不是旧文档里的 `CreateSignalBuilder(...)`：

```csharp
using GFramework.Godot.Extensions.Signal;
using Godot;

button.Signal(Button.SignalName.Pressed)
      .To(Callable.From(OnPressed));
```

## 当前最小接入路径

如果你只是把一个现有 Godot C# 项目接进 GFramework，最小步骤通常是：

1. 安装 `GeWuYou.GFramework`、`GeWuYou.GFramework.Godot`、两个生成器包
2. 确保 `project.godot` 能被 `GFramework.Godot.SourceGenerators` 读到
3. 在架构层继续用常规 `InstallModule(...)`
4. 在节点脚本里用 `[GetNode]`、`[BindNodeSignal]` 和需要的 Core 生成器
5. 对动态订阅和异步节点装配，再补运行时扩展

先走完这条链路，再决定是否需要更重的 Godot 特定抽象。

## 迁移时最容易踩错的地方

### 不要再把这些旧理解当默认事实

- “`GetNodeX<T>()` 是当前推荐的节点注入方式”
- “`[BindNodeSignal]` 会自动补 `_Ready()` / `_ExitTree()`”
- “Godot 集成的第一步是写 `AbstractGodotModule` 并在 `InstallModules()` 里直接调 `InstallGodotModule(...)`”
- “`project.godot` 的文件名可以随便改，只要路径对就行”

当前更准确的理解是：

- 节点字段注入优先用 `[GetNode]`
- `[BindNodeSignal]` 只负责生成绑定/解绑辅助方法
- 多数消费者先用常规模块注册，再在节点脚本里使用 Godot 侧能力
- `project.godot` 只能改相对路径，不能改文件名

## 继续往下读什么

如果你已经按上面接好最小路径，下一步建议按问题域继续看：

1. [Godot 项目元数据生成器](../source-generators/godot-project-generator.md)
2. [GetNode 生成器](../source-generators/get-node-generator.md)
3. [BindNodeSignal 生成器](../source-generators/bind-node-signal-generator.md)
4. [Game.Scene 集成说明](../game/scene.md)
5. [Game.UI 集成说明](../game/ui.md)
