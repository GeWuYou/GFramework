---
title: Godot 场景系统
description: 通过显式注册表、场景工厂、根容器和 dispatcher 使用默认场景导航。
---

# Godot 场景系统

Godot 场景适配负责 `PackedScene` 实例化和原生生命周期，通用 `SceneRouter` 负责导航、守卫、结果和停止。

## 创建与登记

```csharp
using GFramework.Game.Scene;
using GFramework.Godot.Scene;
using Godot;

var registry = new GodotSceneRegistry();
registry.Register("play", GD.Load<PackedScene>("res://scenes/play.tscn"));
var factory = new GodotSceneFactory(registry);
var scenes = new SceneRouter(factory, sceneRoot, dispatcher);
await scenes.ReplaceAsync("play", cancellationToken: cancellationToken);
```

`sceneRoot` 实现 `ISceneRoot`，`dispatcher` 是在 Godot 主线程创建的共享宿主调度器。工厂通过构造函数接收注册表，普通项目直接采用 `SceneRouter`。

## 根容器

根容器只负责挂载和移除，生命周期仍由服务驱动：

```csharp
public void AddScene(ISceneBehavior scene)
{
    if (scene.Original is Node node)
        AddChild(node);
}

public void RemoveScene(ISceneBehavior scene)
{
    if (scene.Original is Node node && node.GetParent() == this)
        RemoveChild(node);
}
```

宿主根应在服务停止前保持存活。使用 DI 时登记 `AddGameScenes(factory, sceneRoot, dispatcher)`；使用 Architecture 时将相同登记放入 `GameRuntimeModule(dispatcher, configure)`。

## 场景与行为

`GodotSceneFactory` 优先使用节点实现的 `ISceneBehaviorProvider.GetScene()`，普通节点则通过 `SceneBehaviorFactory.Create(node, key)` 得到默认适配。默认支持 `Node2D`、`Node3D`、`Control` 和其他 `Node`。

节点可按需要实现 `IScene` 的带取消令牌生命周期：加载、进入、暂停、恢复、退出、卸载。`AutoScene` 是生成 provider 包装入口的可选方式，见 [场景生成器](../source-generators/auto-scene-generator.md)。

创建包装失败时工厂回收刚实例化的节点。业务卸载失败时，适配器仍尝试排队释放原生对象。

### 暂停与原生处理设置

默认适配器首次暂停时保存节点的 Process、PhysicsProcess 和 Input 开关，业务暂停回调成功后暂时关闭这三项处理。重复暂停沿用首次快照。

恢复先等待业务 OnResumeAsync 成功，再在 Godot 主线程还原快照中的准确设置；原来关闭的处理保持关闭。快照在成功还原后消费，因此重复恢复会保留节点当前设置。暂停或恢复回调失败时保留快照，后续补偿恢复仍能还原原状态。场景卸载时清除快照并释放节点。

## 结果与关闭

导航返回 `SceneOperationResult`。守卫拒绝时保留原场景，目标准备失败时清理目标。所有宿主回调在 dispatcher 上执行，清理使用非取消令牌。

退出时等待 `StopAsync` 或 `DisposeAsync`，然后释放场景 root、资源和宿主。生命周期回调内的后续业务操作应交回外部流程，不等待同一路由器的嵌套导航。

## 相关主题

- [通用场景服务](../game/scene.md)
- [Godot UI](./ui.md)
- [运行时示例](./runtime-sample.md)
