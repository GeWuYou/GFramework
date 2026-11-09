---
title: Godot 架构集成
description: 使用标准 DI 描述依赖、显式声明节点模块，并等待 Godot 宿主启动与清理。
---

# Godot 架构集成

## 概述

`AbstractArchitecture` 在 Core Architecture 之上连接 Godot SceneTree；`ArchitectureAnchor` 接收退出树回调，`IGodotModule` 为携带 Node 的模块提供附加和分离入口。普通服务使用标准 DI，配置型模块通过 `services.AddModule(...)` 接入。

需要随场景树结束架构生命周期、安装节点模块或访问受保护的 `ArchitectureRoot` 时，可以继承此类。无需这些宿主能力时，使用普通 Core Architecture 或直接组合服务即可。不会额外运行 Generic Host。

## 最小接入路径

### 服务与生命周期组件

`InstallModules(IServiceCollection)` 在配置阶段由 `ConfigureServices` 调用。此时只记录描述，不解析服务、不加载资源、不访问原生节点。

```csharp
using GFramework.Core.Architectures;
using GFramework.Godot.Architectures;
using Microsoft.Extensions.DependencyInjection;

public sealed class GameArchitecture : AbstractArchitecture
{
    protected override void InstallModules(IServiceCollection services)
    {
        services.AddSingleton<InventoryService>();
        services.AddComponent<GameSessionStartup>(order: 10);
        services.AddModule(new GameplayModule());
    }
}
```

普通 `AddSingleton` 不会自动触发初始化。`AddComponent` 显式加入启动/停止，按 order 升序、同值登记顺序初始化，再按实际启动顺序逆序停止。Model、System、Utility 作为业务组织概念使用，不决定容器和生命周期语义。

### 携带节点的模块

节点模块通过 `InstallGodotModule(services, module)` 声明。把 Node 的创建延迟到主线程附加阶段：

```csharp
using GFramework.Core.Architectures;
using GFramework.Godot.Architectures;
using Microsoft.Extensions.DependencyInjection;
using Godot;

public sealed class HudModule : AbstractGodotModule
{
    private Control? _root;
    public override Node Node => _root ??= new Control { Name = "HudModule" };

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<HudState>();
    }

    public override void OnAttach(Architecture architecture)
    {
        // 这里只是组合根接线；视图接收实际依赖，不保留整个架构。
        var state = architecture.Services.GetRequiredService<HudState>();
        ((Control)Node).TooltipText = state.Description;
    }

    public override void OnDetach()
    {
        // 解除模块自己的连接；锚点负责释放其子节点。
    }
}

public sealed class HudArchitecture : AbstractArchitecture
{
    protected override void InstallModules(IServiceCollection services)
    {
        InstallGodotModule(services, new HudModule());
    }
}

public sealed class HudState
{
    public string Description { get; set; } = "Ready";
}
```

真正等待启动，并在停止时等待清理：

```csharp
await using var architecture = new HudArchitecture();
await architecture.InitializeAsync(cancellationToken);
```

配置期的节点模块声明不会立即附加节点。初始化完成后不能再安装模块，也不要同步阻塞等待场景树。

## 生命周期

### 初始化

1. 执行 ConfigureServices / InstallModules，记录当前实例的依赖与组件。
2. 构建一次 provider，解析并启动显式组件。
3. 在 OnStartedAsync 中把锚点创建派发到 Godot 主线程，等待锚点进入树。
4. 在主线程依次添加模块节点、执行 OnAttach。
5. 完成宿主接线后进入 Ready。

Godot 架构需要 InitializeAsync。同步 Initialize 会拒绝异步宿主启动路径，不通过阻塞桥接等待场景树。

### 主线程与异步调用

节点模块的附加和分离由 Godot 适配器派发。普通组件、CQRS handler 和服务方法仍应遵守各自线程契约；架构不会把所有业务调用自动切回主线程。

业务服务通过构造函数取得所需依赖；Godot 创建的节点由父节点或组合根在入树前显式 Configure。实际访问 Node、SceneTree、UI 的异步操作通过 IGameDispatcher 调度并等待完成。CQRS runtime 同样通过构造注入取得，调用其异步 SendRequestAsync、SendAsync、PublishAsync 或 CreateStream。

取消可以撤销尚未开始的主线程队列动作。已经开始的节点操作要完成后再继续清理，避免同一节点被并发释放。

### 销毁

核心先关闭新工作并取消、等待正在进行的启动；OnStoppingAsync 关闭宿主接入。随后逆序停止已开始的业务组件，再通过 OnStoppedAsync 在主线程逆序 OnDetach、清空宿主引用并排队释放锚点，最后异步释放 provider。

即使一个停止或分离回调失败，也继续其他清理并最终报告错误。并发 DestroyAsync 等待同一个结果。QueueFree 的实际删除仍需后续引擎帧；需要确认原生删除完成的宿主，应在自己的生命周期边界等待相应帧。

provider 创建的普通服务由 provider 释放；外部传入实例由其创建者释放。DI 拥有的原生资源适配器应在自身 DisposeAsync 内通过 dispatcher 执行原生清理，不能假设 provider 的每次异步延续都在主线程。

ArchitectureAnchor 的退出树回调会观察异步销毁任务。Godot 的同步退出回调无法等待完整异步工作，因此需要确定退出结果的应用应主动等待 DestroyAsync，然后退出场景树。

## 使用边界

- 节点模块只在配置阶段声明，必须有活动的 SceneTree 才能附加。
- IGodotModule 的扩展点是 Node、ConfigureServices、OnAttach、OnDetach；业务初始化通过显式组件登记。
- ArchitectureRoot 只在锚点就绪后有效，销毁后不可使用。它属于当前宿主，不用于全局节点查找。
- Architecture.Services 限于组合根接线；领域对象和应用服务不接收容器或整个架构。

## 相关主题

- [Godot 运行时集成](./index.md)
- [Godot 集成教程](../tutorials/godot-integration.md)
- [Godot 进阶项目](../tutorials/godot-advanced-project.md)
- [Godot 场景系统](./scene.md)
- [Godot UI 系统](./ui.md)

## 异步宿主契约

`AbstractArchitecture` 显式实现 Core 的 `IAsyncArchitectureHost`；派生类仍通过 Godot 适配层的 `OnStartedAsync` 扩展接线。同步 `Initialize` 会在配置和组件激活前拒绝该宿主，使用 `await InitializeAsync()`。

自行直接继承 Core `Architecture` 的异步 Godot 组合根，应将旧的 Core `OnStartedAsync` 覆写改为显式 `IAsyncArchitectureHost.StartHostAsync` 实现。该契约不自动切换线程：需要场景树或引擎对象的操作继续通过宿主 dispatcher 执行，依赖已挂载节点的工作放在宿主附加阶段。
