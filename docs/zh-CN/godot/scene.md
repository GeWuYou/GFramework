---
title: Godot 场景系统
description: 说明 GFramework.Godot 当前如何把 Game 场景契约接到 PackedScene、行为包装和最小接入路径上。
---

# Godot 场景系统

`GFramework.Godot` 在场景这一层负责的是 Godot runtime 适配，而不是再提供一套 Godot 专用路由器类型。

当前真正参与场景接线的核心类型是：

- `IGodotSceneRegistry` / `GodotSceneRegistry`
- `GodotSceneFactory`
- `SceneBehaviorFactory`
- `SceneBehaviorBase<T>` 及其 `Node2D` / `Node3D` / `Control` / `Generic` 实现
- 项目侧实现的 `ISceneRoot`
- 项目侧继承 `SceneRouterBase` 的路由器

也就是说，Godot 集成页的重点不是“再造一套场景导航 API”，而是把 `PackedScene`、`Node` 和 `GFramework.Game` 的
`ISceneRouter` / `ISceneBehavior` 契约接起来。

## 当前公开入口

### `IGodotSceneRegistry`

Godot 侧的场景资源表，底层是 `IAssetRegistry<PackedScene>`。它只负责：

- `sceneKey -> PackedScene` 映射
- 让 `GodotSceneFactory` 能按 key 实例化场景

框架当前不会自动扫描项目里的 `.tscn` 文件并填充 registry。

### `GodotSceneFactory`

`GodotSceneFactory.Create(string sceneKey)` 的当前行为很明确：

1. 从 `IGodotSceneRegistry` 取出 `PackedScene`
2. 调用 `Instantiate()`
3. 如果节点实现了 `ISceneBehaviorProvider`，优先返回 `provider.GetScene()`
4. 否则回退到 `SceneBehaviorFactory.Create(node, sceneKey)`

当前源码允许两条路径：

- 显式 provider：项目自己决定行为对象
- 自动包装：按节点类型回退到默认 behavior

### `SceneBehaviorBase<T>`

`SceneBehaviorBase<T>` 是当前 Godot 场景行为包装基类。它把 `ISceneBehavior` 的生命周期接到 `Node` 上：

- `OnLoadAsync`
- `OnEnterAsync`
- `OnPauseAsync`
- `OnResumeAsync`
- `OnExitAsync`
- `OnUnloadAsync`

如果 owner 还实现了 `IScene`，这些阶段会继续转发到业务节点；如果没有实现 `IScene`，默认 behavior 仍会处理 Godot 节点的
process 开关和 `QueueFreeX()` 释放。

### `SceneBehaviorFactory`

自动包装的选择规则来自当前实现：

- `Node2D` -> `Node2DSceneBehavior`
- `Node3D` -> `Node3DSceneBehavior`
- `Control` -> `ControlSceneBehavior`
- 其他 `Node` -> `GenericSceneBehavior`

这意味着 Godot runtime 确实能“自动给节点补一个 behavior”，但它不会替你补项目侧路由器、root 或 registry。

## 最小接入路径

推荐按下面顺序接入。

### 1. 继续在项目层保留自己的路由器

`GFramework.Godot` 当前没有 `GodotSceneRouter` 类型。消费者项目的实际做法，是在项目层继承
`GFramework.Game.Scene.SceneRouterBase`。

最小形态通常就是：

```csharp
using LoggingTransitionHandler = GFramework.Game.Scene.Handler.LoggingTransitionHandler;

namespace GameProject.Scene;

public partial class SceneRouter : SceneRouterBase
{
    [GetUtility] private IGodotSceneRegistry _sceneRegistry = null!;

    public Node? SceneRoot => Root as Node;

    protected override void RegisterHandlers()
    {
        __InjectContextBindings_Generated();
        RegisterHandler(new LoggingTransitionHandler());
        RegisterAroundHandler(
            new SceneTransitionAnimationHandler(() => SceneTransitionManager.Instance!, _sceneRegistry.GetAll()));
    }
}
```

这里可以看到，Godot 适配点在 factory / registry / root / transition handler 上，而路由器仍然是项目类。

### 2. 注册 `IGodotSceneRegistry` 与 `ISceneFactory`

最小 wiring 需要把 registry 和 factory 装进架构：

```csharp
using GFramework.Game.Abstractions.Scene;
using GFramework.Godot.Scene;
using Godot;

public sealed class GameSceneRegistry : GodotSceneRegistry
{
    public GameSceneRegistry()
    {
        Register(nameof(SceneKey.MainMenu), GD.Load<PackedScene>("res://scenes/main_menu.tscn"));
        Register(nameof(SceneKey.Gameplay), GD.Load<PackedScene>("res://scenes/gameplay.tscn"));
    }
}

architecture.RegisterUtility<IGodotSceneRegistry>(new GameSceneRegistry());
architecture.RegisterUtility<ISceneFactory>(new GodotSceneFactory());
architecture.RegisterSystem(new SceneRouter());
```

项目用什么 key 类型、资源目录或配置表都可以，但最终要能落到 `sceneKey -> PackedScene`。

### 3. 提供 `ISceneRoot`

`SceneRouterBase` 只负责切换编排，真正把场景节点挂到 Godot 场景树的是项目自己的 `ISceneRoot`。

CoreGrid 的 `SceneRoot` 当前做了两件关键事：

- 在 `_Ready()` 时调用 `_sceneRouter.BindRoot(this)`
- 在 `AddScene` / `RemoveScene` 里把 `scene.Original` 当作 `Node` 挂入或移出树

最小形态可以写成：

```csharp
public sealed class SceneRoot : Node2D, ISceneRoot
{
    [GetSystem] private ISceneRouter _sceneRouter = null!;

    public override void _Ready()
    {
        __InjectContextBindings_Generated();
        _sceneRouter.BindRoot(this);
    }

    public void AddScene(ISceneBehavior scene)
    {
        if (scene.Original is not Node node)
            throw new InvalidOperationException("SceneBehavior must inherit Godot Node.");

        if (node.GetParent() == null)
            AddChild(node);
    }

    public void RemoveScene(ISceneBehavior scene)
    {
        if (scene.Original is Node node && node.GetParent() == this)
            RemoveChild(node);
    }
}
```

### 4. 让场景节点提供 behavior

当前有两种可行方式。

#### 方式 A：实现 `ISceneBehaviorProvider`

如果你想显式控制 behavior 类型，直接实现 `GetScene()`：

```csharp
public partial class GameplayRoot : Node2D, ISceneBehaviorProvider, IScene
{
    private ISceneBehavior? _scene;

    public ISceneBehavior GetScene()
    {
        return _scene ??= SceneBehaviorFactory.Create(this, nameof(SceneKey.Gameplay));
    }

    public ValueTask OnLoadAsync(ISceneEnterParam? param)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnEnterAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPauseAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnResumeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnExitAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnUnloadAsync()
    {
        return ValueTask.CompletedTask;
    }
}
```

#### 方式 B：用 `[AutoScene]` 让生成器补样板

当前更贴近真实消费者 wiring 的方式，是让 `GFramework.Godot.SourceGenerators` 生成 `SceneKeyStr` 和 `GetScene()`：

```csharp
using GFramework.Game.Abstractions.Scene;
using GFramework.Godot.SourceGenerators.Abstractions.UI;
using Godot;

[AutoScene(nameof(SceneKey.Gameplay))]
public partial class GameplayRoot : Node2D, ISceneBehaviorProvider, IScene
{
    public ValueTask OnLoadAsync(ISceneEnterParam? param)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnEnterAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPauseAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnResumeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnExitAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnUnloadAsync()
    {
        return ValueTask.CompletedTask;
    }
}
```

生成器当前会补出与源码一致的 `GetScene()`：

```csharp
public ISceneBehavior GetScene()
{
    return __autoSceneBehavior_Generated ??= SceneBehaviorFactory.Create(this, SceneKeyStr);
}
```

要注意两点：

- `[AutoScene]` 只生成方法和 key，不会替你自动补 `: ISceneBehaviorProvider`
- `IScene` 仍然是业务生命周期契约；不实现它时，默认 behavior 只会保留基础节点切换语义

### 5. 从业务代码发起导航

一旦 registry、factory、router、root 都装好，导航入口仍然是 `ISceneRouter`：

```csharp
await sceneRouter.ReplaceAsync(nameof(SceneKey.MainMenu));
await sceneRouter.ReplaceAsync(nameof(SceneKey.Gameplay), new GameplayEnterParam());
await sceneRouter.PushAsync(nameof(SceneKey.PauseMenu));
await sceneRouter.PopAsync();
```

## 当前边界

### 没有 `GodotSceneRouter`

仓库当前不存在 `GodotSceneRouter` 类型。旧文档里把它写成默认入口是失真的；实际入口仍然是项目侧继承
`SceneRouterBase` 的 router。

### 没有自动注册所有场景

当前运行时只认识你注册进 `IGodotSceneRegistry` 的 `PackedScene`。它不会扫描目录、不会从脚本类型自动反推出注册表。

### provider 是“优先路径”，不是“唯一路径”

`GodotSceneFactory` 会优先使用 `ISceneBehaviorProvider`，但没有 provider 时仍会按节点类型自动包装。这个行为和 UI 系统不同；
UI 工厂当前没有同等的自动回退。

### root 仍然是项目职责

`ISceneRoot` 的实现决定：

- 节点挂到哪里
- 移除时如何释放
- 是否保留额外的当前视图引用

Godot runtime 不会替项目生成统一的 root 节点。

## 继续阅读

1. [Godot 运行时集成](./index.md)
2. [Godot 架构集成](./architecture.md)
3. [Game 场景系统](../game/scene.md)
4. [AutoScene 生成器](../source-generators/auto-scene-generator.md)
