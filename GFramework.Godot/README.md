# GFramework.Godot

`GFramework.Godot` 是 `GFramework` 在 Godot 宿主侧的运行时适配包。

它建立在 `GFramework.Game`、`GFramework.Game.Abstractions` 与 `GFramework.Core.Abstractions` 之上，把框架已有的架构、
Scene / UI、配置、存储、设置、日志与协程能力接到 `Node`、`SceneTree`、`PackedScene`、`FileAccess` 与 `AudioServer`
等 Godot 运行时对象上。

如果你需要的是 `[GetNode]`、`[BindNodeSignal]`、`AutoLoads` 或 `InputActions` 这类编译期能力，请改为同时安装
`GFramework.Godot.SourceGenerators`。这些能力不属于本包。

## 包定位

当前包解决的是 Godot 运行时接线，而不是重新定义一套 Godot 专属框架：

- 架构生命周期与场景树绑定：`AbstractArchitecture`、`ArchitectureAnchor`
- 节点运行时辅助：`WaitUntilReadyAsync()`、`AddChildXAsync()`、`QueueFreeX()`、`UnRegisterWhenNodeExitTree(...)`
- Godot 风格的 Scene / UI 工厂与 registry：`GodotSceneFactory`、`GodotUiFactory`
- Godot 特化的配置、存储与设置实现：`GodotYamlConfigLoader`、`GodotFileStorage`、`GodotAudioSettings`
- 宿主侧辅助能力：`Signal(...)` fluent API、Godot 时间源、暂停处理、富文本效果

它不负责：

- 自动生成节点字段注入代码
- 自动生成 `_Ready()` / `_ExitTree()` 接线
- 自动扫描所有场景或页面并完成统一注册
- 提供 `GodotSceneRouter` 或 `GodotUiRouter` 这类额外 router 类型

## 与相邻包的关系

- `GFramework.Game`
  - 提供 Scene / UI / 配置 / 数据等默认运行时契约与基类。
  - `GFramework.Godot` 负责把这些能力落到 Godot 宿主。
- `GFramework.Game.Abstractions`
  - 提供 `ISceneFactory`、`IUiFactory`、设置与配置相关契约。
  - 本包的大部分工厂和适配层都实现这些接口。
- `GFramework.Core.Abstractions`
  - 提供架构、日志、环境等基础契约。
  - `AbstractArchitecture` 与日志 provider 都建立在这层之上。
- `GFramework.Godot.SourceGenerators`
  - 提供 `project.godot` 元数据、`[GetNode]`、`[BindNodeSignal]`、`[AutoScene]`、`[AutoUiPage]` 等编译期样板生成。
  - 推荐与本包配套使用，但职责边界要分开理解。

## 子系统地图

### `Architectures/`

- `AbstractArchitecture`
- `AbstractGodotModule`
- `ArchitectureAnchor`
- `IGodotModule`

用于把架构生命周期绑定到 `SceneTree`，并在需要时把 Godot 模块挂到场景树。

### `Scene/` 与 `UI/`

- `GodotSceneFactory`、`GodotSceneRegistry`
- `GodotUiFactory`、`GodotUiRegistry`
- `SceneBehaviorFactory`、`UiPageBehaviorFactory`

这部分负责把 `PackedScene`、`Control`、`CanvasLayer` 等 Godot 对象接入 `GFramework.Game` 的 Scene / UI 契约。

### `Config/`、`Storage/` 与 `Setting/`

- `GodotYamlConfigLoader`
- `GodotFileStorage`
- `GodotAudioSettings`、`GodotGraphicsSettings`、`GodotLocalizationSettings`

这部分解决的是 Godot 文件系统、音频总线、图形与本地化设置等宿主差异。

### `Extensions/`、`Coroutine/`、`Logging/`、`Pause/`、`Text/`、`Pool/`

- 节点扩展与 `Signal(...)` fluent API
- `GodotTimeSource` 与协程时间分段
- Godot 日志 provider
- 暂停处理、节点池与富文本效果支持

这些目录都是“宿主适配层”，不是新的 gameplay 抽象层。

## 最小接入路径

### 1. 先区分运行时包和生成器包

如果你只需要 Godot 运行时适配：

```bash
dotnet add package GeWuYou.GFramework
dotnet add package GeWuYou.GFramework.Godot
```

如果你还需要 `project.godot` 强类型入口、节点字段注入和信号绑定，再额外安装：

```bash
dotnet add package GeWuYou.GFramework.Core.SourceGenerators
dotnet add package GeWuYou.GFramework.Godot.SourceGenerators
```

### 2. 保持原有架构注册方式，只把宿主协作接到 Godot

常规模块继续使用 `InstallModule(...)`。

只有模块自身暴露 `Node`、需要挂到 `ArchitectureAnchor`，或要在 `OnAttach(...)` / `OnDetach()` 里处理 Godot 生命周期副作用时，
再使用 `InstallGodotModule(...)`。

这一点已有测试覆盖：锚点缺失时会先抛 `InvalidOperationException`，不会继续执行模块安装。

### 3. Scene / UI 继续沿用 `Game` 契约

当前真实边界是：

- 没有 `GodotSceneRouter`
- 没有 `GodotUiRouter`
- `GodotSceneFactory` 在 provider 缺失时回退到 `SceneBehaviorFactory`
- `GodotUiFactory` 仍要求 `IUiPageBehaviorProvider`

也就是说，项目通常仍然继承 `GFramework.Game.Scene.SceneRouterBase` 与 `GFramework.Game.UI.UiRouterBase`，只是把工厂和行为落到
Godot 上。

### 4. 按需接入配置、存储和设置

当项目已经使用 `Game` 模块的配置、存储、设置契约时，再补 Godot 侧实现：

- 配置：`GodotYamlConfigLoader`
- 存储：`GodotFileStorage`
- 设置：`GodotAudioSettings`、`GodotGraphicsSettings`、`GodotLocalizationSettings`

不要把这些宿主实现误写成 `Game` 模块的默认行为。

## 典型接入方式

- 架构侧保持普通模块注册，再按需挂接 Godot 宿主
- `project.godot` 元数据与节点样板交给 `GFramework.Godot.SourceGenerators`
- Scene / UI 继续沿用 `Game` 模块的路由语义

## 文档入口

- Godot 运行时总览：[Godot 模块总览](../docs/zh-CN/godot/index.md)
- 架构集成：[Godot 架构集成](../docs/zh-CN/godot/architecture.md)
- 场景系统：[Godot 场景系统](../docs/zh-CN/godot/scene.md)
- UI 系统：[Godot UI 系统](../docs/zh-CN/godot/ui.md)
- 节点扩展：[Godot 节点扩展](../docs/zh-CN/godot/extensions.md)
- 信号系统：[Godot 信号系统](../docs/zh-CN/godot/signal.md)
- 日志系统：[Godot 日志系统](../docs/zh-CN/godot/logging.md)
- 集成教程：[Godot 集成教程](../docs/zh-CN/tutorials/godot-integration.md)
- 生成器入口：[Godot 源码生成器说明](../GFramework.Godot.SourceGenerators/README.md)

## 什么时候不该把它当成主入口

以下场景更适合先回到其他包：

- 只需要 Scene / UI / 配置契约，不需要 Godot 宿主：
  - 选 `GFramework.Game.Abstractions`
- 需要默认运行时实现，但暂时不接 Godot：
  - 选 `GFramework.Game`
- 需要的是 `project.godot` 元数据、节点字段注入或编译期样板：
  - 选 `GFramework.Godot.SourceGenerators`
