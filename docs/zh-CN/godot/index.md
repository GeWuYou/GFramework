---
title: Godot 运行时集成
description: 以当前 GFramework.Godot 源码、测试与 CoreGrid 接线为准，说明 Godot 运行时包的定位、最小接入路径和文档入口。
---

# Godot 运行时集成

## 模块定位

`GFramework.Godot` 是 `GFramework` 在 Godot 侧的运行时适配层。它负责把框架已有的 Core / Game 能力接到 Godot 的
`Node`、`SceneTree`、`PackedScene`、`FileAccess` 和 `AudioServer` 上，而不是重新定义一套独立架构。

当前仓库里仍然成立的 Godot 运行时职责，主要集中在这些方向：

- 架构生命周期与场景树绑定：`AbstractArchitecture`、`ArchitectureAnchor`
- 节点运行时辅助：`WaitUntilReadyAsync()`、`AddChildXAsync()`、`QueueFreeX()`、`UnRegisterWhenNodeExitTree(...)`
- Godot 风格的 Scene / UI 工厂与 registry：`GodotSceneFactory`、`GodotUiFactory`
- Godot 特化的存储、设置与配置加载：`GodotFileStorage`、`GodotAudioSettings`、`GodotYamlConfigLoader`
- 少量面向运行时交互的扩展：`Signal(...)` fluent API、暂停处理、富文本效果、协程时间源

它不是 `[GetNode]`、`[BindNodeSignal]`、`AutoLoads`、`InputActions` 的来源。这些能力属于
`GFramework.Godot.SourceGenerators`。

## 包关系

- `GeWuYou.GFramework`：聚合运行时包，提供 Core / Game 常用能力
- `GeWuYou.GFramework.Godot`：当前页面对应的 Godot 运行时适配层
- `GeWuYou.GFramework.Core.SourceGenerators`：`[ContextAware]`、`[GetModel]`、`[GetSystem]` 等 Core 侧生成器
- `GeWuYou.GFramework.Godot.SourceGenerators`：`project.godot` 元数据、`[GetNode]`、`[BindNodeSignal]`

从当前 `GFramework.Godot.csproj` 看，Godot 运行时包直接依赖：

- `GFramework.Game`
- `GFramework.Game.Abstractions`
- `GFramework.Core.Abstractions`
- `GodotSharp`

这意味着它更像是“把现有框架能力接到 Godot 宿主”的桥接层，而不是单独的 gameplay 框架。

## 最小接入路径

### 1. 先区分运行时包和生成器包

如果你只需要 Godot 运行时辅助，可以先安装：

```bash
dotnet add package GeWuYou.GFramework
dotnet add package GeWuYou.GFramework.Godot
```

如果你还需要 `project.godot` 的强类型入口、节点字段注入和 CLR event 绑定，再额外安装：

```bash
dotnet add package GeWuYou.GFramework.Core.SourceGenerators
dotnet add package GeWuYou.GFramework.Godot.SourceGenerators
```

只装运行时包时，不会生成 `AutoLoads`、`InputActions`、`__InjectGetNodes_Generated()` 或
`__BindNodeSignals_Generated()`。

### 2. 让架构继续负责注册，让 Godot 节点负责场景接线

`ai-libs/CoreGrid` 当前的真实做法，是让架构继承 `AbstractArchitecture`，但模块注册依然使用普通的
`InstallModule(...)`：

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

也就是说，`GFramework.Godot` 的默认接入点不是“把所有注册都改成 Godot 模块”，而是“保持现有架构注册方式，只把需要
Godot 生命周期协作的部分接到场景树上”。

### 3. 只在真的需要 Godot 节点挂接时使用 `InstallGodotModule(...)`

`InstallGodotModule(...)` 适合这类模块：

- 模块自身暴露一个 `Node`
- 模块希望在架构锚点就绪后被挂到场景树
- 模块需要在 `OnAttach(...)` / `OnDetach()` 里处理 Godot 生命周期副作用

如果模块只是注册 Model、System、Utility，继续使用普通 `InstallModule(...)` 更符合当前消费者接法。

### 4. 节点脚本用运行时辅助，静态样板交给生成器

当前最常见的运行时入口是：

- `UnRegisterWhenNodeExitTree(this Node node)`：把框架事件解绑挂到节点退出树
- `WaitUntilReadyAsync()`：等待节点真正进入场景树
- `AddChildXAsync()`：添加子节点后等待 ready
- `Signal(...)`：对 `GodotObject.Connect(...)` 的 fluent 包装

```csharp
using GFramework.Godot.Extensions;
using GFramework.Godot.Extensions.Signal;
using Godot;

public partial class SettingsPanel : Control
{
    public override async void _Ready()
    {
        var button = GetNode<Button>("%ApplyButton");
        await button.WaitUntilReadyAsync();

        button.Signal(Button.SignalName.Pressed)
              .To(Callable.From(OnApplyPressed));
    }

    private void OnApplyPressed()
    {
    }
}
```

字段注入、`project.godot` 解析和 signal 自动绑订仍应交给生成器，不要把它们写成 `GFramework.Godot` 运行时包的默认职责。

## 关键入口

- 架构锚点与模块挂接：[Godot 架构集成](./architecture.md)
- Scene / `PackedScene` 工厂与行为封装：[Godot 场景系统](./scene.md)
- UI page 行为、layer 语义与工厂：[Godot UI 系统](./ui.md)
- Godot 文件路径与持久化适配：[Godot 存储系统](./storage.md)
- 音频、图形与本地化设置接线：[Godot 设置系统](./setting.md)
- `Signal(...)` fluent API 与动态连接边界：[Godot 信号系统](./signal.md)

如果你要核对项目级接线，而不是运行时 API，本页之后优先看：

- [Godot 集成教程](../tutorials/godot-integration.md)
- [Godot 项目元数据生成器](../source-generators/godot-project-generator.md)

## 当前边界

- `AbstractArchitecture` 会在 `SceneTree.Root` 下创建 `ArchitectureAnchor`，并在锚点退出场景树时触发架构销毁观察流程
- `InstallGodotModule(...)` 会先检查锚点是否存在；测试覆盖了“锚点缺失时直接抛 `InvalidOperationException`，且不会执行
  `module.Install(...)`”这一路径
- `Signal(...)` 是当前 fluent API 入口，不是旧文档里的 `CreateSignalBuilder(...)`
- `NodeExtensions` 目前保留的是 `FindChildX<T>()`、`GetParentX<T>()`、`GetOrCreateNode<T>()`、`QueueFreeX()`、
  `FreeX()`、`WaitUntilReadyAsync()` 这一类运行时辅助；不要再把它描述成一个“默认覆盖所有 Godot 节点操作”的大而全层
- Scene / UI 工厂依赖显式 registry 与 `PackedScene` 资源，不存在“运行时自动扫描所有场景并完成统一注册”的当前契约
- `[GetNode]`、`[BindNodeSignal]`、`AutoLoads`、`InputActions` 来自 `GFramework.Godot.SourceGenerators`，不属于本包

## 继续阅读

1. [Godot 集成教程](../tutorials/godot-integration.md)
2. [Godot 架构集成](./architecture.md)
3. [Godot 场景系统](./scene.md)
4. [Godot UI 系统](./ui.md)
5. [Godot 项目元数据生成器](../source-generators/godot-project-generator.md)
