---
title: Godot 架构集成
description: 说明 Godot 架构的配置声明、异步主线程启动和确定性清理方式。
---

# Godot 架构集成

## 概述

`AbstractArchitecture` 在 Core Architecture 之上连接 Godot SceneTree；`ArchitectureAnchor` 接收退出树回调，`IGodotModule` 为携带 Node 的模块提供附加和分离入口。普通模块仍使用 `InstallModule(...)`。

## 什么时候该用 `AbstractArchitecture`

需要随场景树结束架构生命周期、安装节点模块或通过受保护的 `ArchitectureRoot` 访问宿主锚点时，可继承该类。模块和组件的依赖由当前架构的同一个正式 provider 解析；不额外运行 Generic Host。

## 最小接入路径

### 常规模块仍然用 `InstallModule(...)`

`InstallModules()` 在配置阶段由 `ConfigureServices(IServiceCollection)` 调用，只描述服务、组件和模块，不解析依赖、不加载资源，也不访问原生节点。

```csharp
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Godot.Architectures;

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
    }
}
```

这些业务模块在各自 Install 方法中声明所需组件。等待 `InitializeAsync(...)` 成功之后再使用已初始化的服务。

### 只有携带 `Node` 的模块才需要 `InstallGodotModule(...)`

在配置阶段声明节点模块，将原生 Node 创建推迟到宿主启动阶段：

```csharp
using GFramework.Core.Abstractions.Architectures;
using GFramework.Godot.Architectures;
using Godot;

public sealed class HudModule : AbstractGodotModule
{
    private Control? _root;

    public override Node Node => _root ??= new Control { Name = "HudModule" };

    public override void Install(IArchitecture architecture)
    {
        // 这里仅声明服务；Node 属性在主线程附加阶段才被读取。
    }

    public override void OnAttach(GFramework.Core.Architectures.Architecture architecture)
    {
        // 此时节点已经加入锚点，可建立宿主事件连接。
    }

    public override void OnDetach()
    {
        // 解除模块自己的连接；锚点负责释放其子节点。
    }
}

public sealed class HudArchitecture : AbstractArchitecture
{
    protected override void InstallModules()
    {
        InstallGodotModule(new HudModule());
    }
}
```

启动调用应被真正等待：

```csharp
var architecture = new HudArchitecture();
await architecture.InitializeAsync(cancellationToken);
```

配置期的 InstallGodotModule 是声明动作，不是异步附加任务。不要在初始化结束后再调用它，也不要同步阻塞等待节点就绪。

## 当前生命周期

### 初始化阶段

1. 执行 ConfigureServices / InstallModules，声明当前实例的注册图。
2. 构建一次 provider，激活组件并完成业务初始化。
3. 在 OnStartedAsync 中把锚点创建派发到 Godot 主线程，等待锚点进入树。
4. 在主线程依次添加模块节点、执行 OnAttach。
5. 完成以上操作后进入 Ready。

Godot 架构需要使用 InitializeAsync。同步 Initialize 会明确拒绝该异步宿主启动路径，不通过阻塞桥接等待场景树。

### `InstallGodotModule(...)` 的执行顺序

配置阶段校验安装窗口、调用 module.Install 并登记模块。启动阶段等待锚点后才访问 Node、添加子节点和执行 OnAttach；该回调失败时也会执行对应的分离清理。

取消可以撤销尚未开始的主线程队列动作；已经开始的短节点操作先完成，再允许失败清理继续，避免清理与同一节点操作并发。

### Godot 主线程同步 CQRS 保护

未自定义 IArchitectureContext 时，默认的 GodotArchitectureContext 会在当前存在 SceneTree 且调用发生于主线程时拒绝同步 SendRequest、SendCommand 和 SendQuery。同步桥接可能把 handler 放到工作线程，无法保证 Node、SceneTree 或 UI 的线程亲和性。

使用 SendAsync、SendCommandAsync、SendQueryAsync 或协程桥接入口，并让实际访问节点的业务代码遵守 Godot 主线程约束。架构对节点模块附加/分离的派发不等于所有业务 handler 和组件回调都自动回到主线程。

### 销毁阶段

显式等待 DestroyAsync 可观察完整清理结果。核心先取消并等待正在进行的初始化，再通过 OnStoppingAsync 在主线程逆序执行已附加模块的 OnDetach、清空宿主引用并排队释放锚点，然后继续核心组件停止与 provider 异步释放。即使某个 OnDetach 失败，也继续其他模块和核心清理，最后报告错误。

DestroyAsync 等待业务和 provider 清理完成；QueueFree 排队的节点释放仍由引擎帧处理。并发销毁调用等待同一个完成任务。ArchitectureAnchor 的退出树回调会启动并观察这一任务；因为 Godot 的同步退出回调不能等待异步工作，退出驱动中的错误会写入 Godot 错误输出。

## 当前边界

### 模块只在配置阶段声明

配置窗口之外的 InstallGodotModule 会在 module.Install 之前失败。此时锚点是否存在不再决定能否注册；节点创建和挂接属于后续异步启动阶段。安装节点模块需要正在运行的 SceneTree。

### `AbstractGodotModule` 只是便捷基类，不代表自动阶段广播

IGodotModule 保证的成员为 Node、Install、OnAttach 和 OnDetach。基类中保留的 OnPhase / OnArchitecturePhase 不是自动阶段订阅；需要阶段协作时显式注册相应钩子。

### `ArchitectureRoot` 只在锚点就绪后可用

配置阶段不能访问 ArchitectureRoot。异步 InitializeAsync 完成后，或模块 OnAttach 中，可以使用本实例已进入树的锚点。销毁后该属性不再可用；不要把它用作全局节点查找入口。

## 继续阅读

1. [Godot 运行时集成](./index.md)
2. [Godot 集成教程](../tutorials/godot-integration.md)
3. [Godot 场景系统](./scene.md)
4. [Godot UI 系统](./ui.md)
