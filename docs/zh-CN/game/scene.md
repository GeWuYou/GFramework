---
title: 场景系统
description: 使用显式工厂、宿主根和调度器采用场景导航，处理操作结果、取消和停止。
---

# 场景系统

`SceneRouter` 是可直接创建的通用场景服务。它通过构造函数接收 `ISceneFactory`、`ISceneRoot` 和 `IGameDispatcher`，负责场景栈、守卫、过渡阶段与生命周期。项目无需继承路由器或绑定 Architecture 上下文。

## 接入

宿主提供场景工厂和根容器。在宿主线程取得同步上下文后创建调度器，再创建服务：

```csharp
using System;
using System.Threading;
using GFramework.Game.Runtime;
using GFramework.Game.Scene;

var context = SynchronizationContext.Current
    ?? throw new InvalidOperationException("Create the scene service on the host thread.");
var dispatcher = new SynchronizationContextGameDispatcher(context);
await using var scenes = new SceneRouter(sceneFactory, sceneRoot, dispatcher);

var result = await scenes.ReplaceAsync("play", cancellationToken: cancellationToken);
```

其中 `sceneFactory` 实现 `ISceneFactory.Create(string)`，`sceneRoot` 实现 `ISceneRoot.AddScene` 和 `RemoveScene`。通用纯托管宿主可使用 `InlineGameDispatcher`。Godot 的节点和主线程接入见 [Godot 场景系统](../godot/scene.md)。

采用标准 DI 时，将这些接口和 `SceneRouter` 登记为宿主实例的服务；使用 Architecture 时沿用相同实例和显式依赖。宿主必须在释放根节点、工厂和调度器之前等待路由停止。

需要记录过渡扩展中被 `ContinueOnError` 保留的错误时，可向 `SceneRouter` 的四参数构造函数传入 `ILogger<SceneRouter>`。标准 DI 的 `AddLogging()` 可提供该依赖；错误记录会保留原始异常及其调用栈。

## 导航与结果

- `ReplaceAsync`：准备目标后替换整份场景栈。
- `PushAsync`：保留原栈，暂停原栈顶后进入新场景。
- `PopAsync`：退出当前场景并恢复下层。
- `ClearAsync`：全部离开守卫接受后清空场景。
- `StopAsync`：关闭新操作入口、等待在途操作，再绕过守卫清理全部场景。

导航返回 `Task<SceneOperationResult>`：`Applied` 表示状态已改变，`Rejected` 表示守卫拒绝，`NoOp` 表示空栈操作或重复压入已有场景。创建、业务回调和清理错误通过异常返回。

所有导航方法将 `CancellationToken` 放在最后，并将同一令牌传给进入和离开守卫。异步守卫应观察令牌，在取消后结束等待。取消可撤销排队、守卫等待、准备和可恢复的进入步骤；已经开始的回调和破坏性清理会被完整等待，随后才允许释放相关对象。

## 生命周期和所有权

`ISceneBehavior` 包含 `Key`、原始视图和状态，以及带取消令牌的 `OnLoadAsync`、`OnEnterAsync`、`OnPauseAsync`、`OnResumeAsync`、`OnExitAsync`、`OnUnloadAsync`。`IScene` 提供同样的业务生命周期，适配器可委托给它。

进入失败时，目标得到卸载和移除，原场景尽可能恢复。替换的目标创建或加载失败时，旧栈仍保留。业务回调已产生的外部副作用由业务负责补偿。

退出和卸载分别尝试，单个错误不会跳过根移除或其他场景清理。清理回调接收 `CancellationToken.None`。重复停止共享同一个完成任务，包括首次停止的清理错误。

完整操作串行执行，宿主回调经显式调度器运行。生命周期回调不能等待同一路由器的嵌套导航或停止；后续导航应由回调外的业务流程发起。

## 扩展

在开始导航前通过 `AddGuard`、`RegisterHandler` 和 `RegisterAroundHandler` 注册策略或过渡效果。守卫按优先级执行；拒绝时不改变栈，也不发出成功后的过渡阶段。扩展应保持有限执行时间并尊重传入令牌。

## 迁移

旧的 `BindRoot`、`Initialize`、上下文取工厂和 System 注册方式改为构造注入。普通项目直接使用 `SceneRouter`；需要派生类型时，`SceneRouterBase` 使用相同的显式构造依赖。关闭时等待 `StopAsync` 或 `DisposeAsync`。

## 相关主题

- [UI 系统](./ui.md)
- [Godot 场景系统](../godot/scene.md)
- [运行时示例](../godot/runtime-sample.md)
