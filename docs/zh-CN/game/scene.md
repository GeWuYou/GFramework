---
title: 场景系统
description: 说明 GFramework.Game 场景路由的当前入口、项目侧接入职责与扩展边界。
---

# 场景系统

`GFramework.Game` 的场景系统是“路由基类 + 场景契约 + 过渡管线”的组合，不是替你包办注册表、节点树和引擎对象装配的
一体化方案。

框架当前负责的是：

- 场景栈管理
- `Load -> Enter -> Pause -> Resume -> Exit -> Unload` 生命周期顺序
- 路由守卫与过渡处理器执行时机
- `SceneRouterBase` 这一层的默认切换编排

项目或引擎适配层仍然需要自己提供：

- `ISceneFactory`
- `ISceneRoot`
- 具体的 `ISceneBehavior` / `IScene`
- 场景键和资源、节点、预制体之间的映射关系

如果你把它理解为“可复用的场景路由底座”而不是“现成的完整场景框架”，后续接法会更贴近源码。

## 当前公开入口

### `IScene`

业务场景生命周期契约，描述加载、进入、暂停、恢复、退出、卸载这六个阶段。

### `ISceneBehavior`

路由器直接操作的运行时对象。它除了场景生命周期外，还携带：

- `Key`
- `Original`
- `IsLoaded`
- `IsActive`
- `IsTransitioning`

如果你的引擎对象本身就能承担这些语义，可以直接实现 `ISceneBehavior`。如果你更想把业务逻辑放在纯 C# 场景类中，也可以由
项目侧行为包装器承载真正的引擎节点，再把业务场景逻辑委托出去。

### `ISceneRouter`

当前公开的路由接口，重点入口是：

- `BindRoot(ISceneRoot root)`
- `ReplaceAsync(string sceneKey, ISceneEnterParam? param = null)`
- `PushAsync(string sceneKey, ISceneEnterParam? param = null)`
- `PopAsync()`
- `ClearAsync()`
- `Contains(string sceneKey)`

### `SceneRouterBase`

`GFramework.Game` 提供的默认实现基类。它会：

- 在 `OnInit()` 中获取 `ISceneFactory`
- 通过 `SemaphoreSlim` 串行化切换
- 调用守卫、过渡处理器和环绕处理器
- 维护场景栈与恢复顺序

通常项目不会直接修改框架里的 `SceneRouterBase`，而是在项目层继承它。

## 场景栈的真实语义

按当前实现，最常用的三个动作语义如下：

- `ReplaceAsync`
  - 清空整个栈，再加载并进入目标场景。
- `PushAsync`
  - 先检查守卫，再创建新场景，挂到 `ISceneRoot`，执行 `OnLoadAsync()`，暂停当前栈顶，最后让新场景 `OnEnterAsync()`。
- `PopAsync`
  - 对栈顶执行离开检查，通过后退出并卸载它，再从 `ISceneRoot` 移除，然后恢复新的栈顶。

当前还有两个容易被旧文档误导的点：

- `SceneRouterBase` 默认不允许同一个 `sceneKey` 在栈中重复存在；内部会先做 `Contains(sceneKey)` 检查
- 框架不会替你实现“场景键 -> 具体场景实例”的注册逻辑；这仍然是 `ISceneFactory` 或项目注册表的职责

## 最小接入路径

推荐按下面的顺序接入。

### 推荐目录与文件约定（项目侧）

场景系统的目录结构不由框架强制，但建议把“路由编排、实例创建、引擎挂载、业务场景”分开放置，避免后续把
`SceneRouterBase` 派生类写成巨型协调器。

```text
Game/Scene/
  GameSceneRouter.cs
  GameSceneFactory.cs
  SceneRoot.cs
  Scenes/
    GameplayScene.cs
    PauseMenuScene.cs
  Params/
    GameplayEnterParam.cs
  Registry/
    SceneRegistry.cs
```

推荐约定如下：

- `GameSceneRouter.cs`：项目侧 router，继承 `SceneRouterBase`，只注册 guard、transition handler 和 around handler
- `GameSceneFactory.cs`：实现 `ISceneFactory`，负责 `sceneKey -> ISceneBehavior` 的映射与实例创建
- `SceneRoot.cs`：实现 `ISceneRoot`，负责把行为对象对应的引擎节点挂到场景容器并移除
- `Scenes/*`：放具体业务场景、行为包装器或引擎节点包装类型
- `Params/*`：放实现 `ISceneEnterParam` 的进入参数，按业务场景拆分
- `Registry/*`：如果项目已有场景表或资源表，建议收口在这里，再由 `GameSceneFactory` 使用

最小 wiring 通常是：

```csharp
architecture.RegisterUtility<ISceneFactory>(new GameSceneFactory());
architecture.RegisterSystem(new GameSceneRouter());
```

然后在 `SceneRoot` 的引擎生命周期就绪点调用 `BindRoot(this)`。如果项目已有不同的资源目录、节点层级或场景注册表，
保留原结构即可；只要最终能提供 `ISceneFactory`、`ISceneRoot` 和 `ISceneBehavior`，就不需要为了框架重排所有文件。

### 1. 准备项目自己的 router

```csharp
using GFramework.Game.Scene;
using LoggingTransitionHandler = GFramework.Game.Scene.Handler.LoggingTransitionHandler;

public sealed class GameSceneRouter : SceneRouterBase
{
    protected override void RegisterHandlers()
    {
        RegisterHandler(new LoggingTransitionHandler());
    }
}
```

这一步只解决“切换流程怎么跑”，不解决“场景从哪来”。

### 2. 提供 `ISceneFactory`

`SceneRouterBase` 会在初始化阶段通过 `GetUtility<ISceneFactory>()` 获取工厂，因此项目必须先注册它。

工厂的职责通常是：

- 按 `sceneKey` 找到项目自己的注册表、预制体或资源描述
- 创建或获取 `ISceneBehavior`
- 决定行为对象如何包裹引擎节点与业务场景逻辑

如果项目里已经有场景注册表，也建议把它收口在 factory 内部，而不是让文档继续暗示框架自带统一注册中心。

### 3. 提供 `ISceneRoot`

`ISceneRoot` 只做两件事：

- `AddScene(ISceneBehavior scene)`
- `RemoveScene(ISceneBehavior scene)`

也就是说，root 是“挂载/移除容器”，不是路由器本身。当前 `ai-libs/` 参考实现也是在项目自己的 Godot 节点里实现
`ISceneRoot`，并在 `_Ready()` 时调用 `BindRoot(this)`。

### 4. 把 router 和 factory 装进架构

```csharp
architecture.RegisterUtility<ISceneFactory>(new GameSceneFactory());
architecture.RegisterSystem(new GameSceneRouter());
```

如果你的项目还需要动画、黑幕或 loading 过渡，可以继续在 `RegisterHandlers()` 里补自己的处理器。

### 5. 在 root 就绪后绑定

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
        // 项目侧决定如何把 scene.Original 挂进引擎节点树
    }

    public void RemoveScene(ISceneBehavior scene)
    {
        // 项目侧决定如何移除并释放引擎对象
    }
}
```

### 6. 从业务代码发起导航

```csharp
await sceneRouter.ReplaceAsync(
    "Gameplay",
    new GameplayEnterParam
    {
        Seed = "new-game"
    });

await sceneRouter.PushAsync("PauseMenu");
await sceneRouter.PopAsync();
```

## 扩展点

### 路由守卫

如果你要在进入或离开场景前做业务检查，实现 `ISceneRouteGuard`：

- `CanEnterAsync(string sceneKey, ISceneEnterParam? param)`
- `CanLeaveAsync(string sceneKey)`

适合放：

- 未保存进度拦截
- 场景解锁条件检查
- 新手引导流程限制

### 过渡处理器

`SceneRouterBase` 公开了：

- `RegisterHandler(ISceneTransitionHandler handler, SceneTransitionHandlerOptions? options = null)`
- `RegisterAroundHandler(ISceneAroundTransitionHandler handler, SceneTransitionHandlerOptions? options = null)`

适合放：

- 日志
- 黑幕、淡入淡出或 loading 动画
- 切场前后的指标采集

如果你的项目已经有复杂引擎过渡逻辑，优先把这些逻辑放进 handler，而不是把 `SceneRouterBase` 派生类本身做成巨型协调器。

## 与旧写法的边界

下面这些说法不再适合作为默认接入指导：

- “框架会帮你直接注册和发现所有场景类型”
- “只要写一个 `IScene` 就能自动接入所有引擎对象”
- “场景系统本身自带统一注册表和完整项目结构”

当前更准确的理解是：

- 框架提供通用场景切换编排
- 项目提供 factory、root、资源映射和具体引擎装配
- 文档中的最小示例应优先说明职责边界，而不是继续堆叠大而全教程

## 推荐阅读

1. [game/index.md](./index.md)
2. [ui.md](./ui.md)
3. [`GFramework.Game/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Game/README.md)
4. [`GFramework.Game.Abstractions/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Game.Abstractions/README.md)
