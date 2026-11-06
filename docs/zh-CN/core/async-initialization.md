---
title: 异步初始化指南
description: 说明配置、正式 DI 构建、可取消初始化和异步资源清理的接入方式。
---

# 异步初始化指南

异步初始化用于资源加载、文件和网络等需要等待的工作。服务注册始终是同步描述阶段；InitializeAsync 在构建唯一 provider 后激活组件，再等待组件、阶段钩子和宿主接线。

## 核心接口

IAsyncInitializable 的方法为：

```csharp
Task InitializeAsync(CancellationToken cancellationToken = default);
```

异步工作应传递该令牌，不在同步 Initialize/OnInit 中通过 GetResult 或 Task.Run 等待它。组件实现异步接口时，异步架构启动优先调用该方法，不会额外再调用同步 Initialize。

## 异步 Architecture

下面使用一个可取消的加载 Model。业务项目把 Task.Delay 替换为自身异步加载入口即可：

```csharp
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Architectures;
using GFramework.Core.Model;
using Microsoft.Extensions.DependencyInjection;

public sealed class GameArchitecture : Architecture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterModel<LoadingModel>();
    }
}

public sealed class LoadingModel : AbstractModel, IAsyncInitializable
{
    public bool IsLoaded { get; private set; }

    protected override void OnInit()
    {
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        IsLoaded = true;
    }

}
```

从组合根启动并确保异步释放：

```csharp
using System;
using System.Threading;

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await using var architecture = new GameArchitecture();
await architecture.InitializeAsync(cancellation.Token);
await architecture.WaitUntilReadyAsync();

var loading = architecture.Context.GetModel<LoadingModel>();
```

## 初始化流程

1. 登记环境、上下文、实例日志工厂和内置服务模块描述。
2. 执行 Configurator 与 ConfigureServices，只注册描述，不解析服务。
3. 构建一次正式 provider，激活组件并注入实例上下文；执行 onCreated 回调。
4. 初始化服务模块，再按 Utility → Model → System 等待组件初始化。
5. 执行 OnStartedAsync，完成 Ready 阶段后唤醒等待者。

OnStartedAsync(CancellationToken) 用于组件启动后的宿主接线，默认调用同步 OnStarted。此时注册窗口已经关闭。异步实现不能保证代码始终在引擎主线程；Godot 节点操作仍需宿主线程调度。

## 异步阶段钩子

IAsyncArchitectureLifecycleHook 继承同步阶段接口，异步启动时只调用其异步入口。同步入口可以明确拒绝不支持的调用：

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;

public sealed class LoadHook : IAsyncArchitectureLifecycleHook
{
    public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        throw new InvalidOperationException("Use InitializeAsync.");
    }

    public async ValueTask OnPhaseAsync(
        ArchitecturePhase phase,
        IArchitecture architecture,
        CancellationToken cancellationToken = default)
    {
        if (phase == ArchitecturePhase.BeforeUtilityInit)
        {
            await Task.Delay(10, cancellationToken);
        }
    }
}
```

在 ConfigureServices 中通过 RegisterLifecycleHook(new LoadHook()) 登记。示例使用的阶段与架构接口位于 GFramework.Core.Abstractions.Enums 和 GFramework.Core.Abstractions.Architectures。

## 取消、失败与清理

传给 InitializeAsync 的令牌贯穿模块、组件与异步阶段。调用 DestroyAsync 会取消并等待尚未结束的启动，再抵达同一资源清理路径。取消需要参与者合作，不能强行中断忽略令牌的阻塞工作。

初始化失败或取消时，框架自动停止已进入初始化的业务参与者并释放 provider。InitializeAsync 和 WaitUntilReadyAsync 在清理结束后报告结果；清理失败通过异常保留，不宣称资源已经成功释放。失败对象不能重新初始化，应创建新架构。

异步停止接口 IAsyncDestroyable 与标准 IAsyncDisposable 的责任不同：前者停止业务，后者释放拥有的资源。外部实例不转移最终释放责任；多个 DisposeAsync/DestroyAsync 调用等待同一结果，不重复清理。

## 同步与异步选择

- 全部参与者均为同步工作时可调用 Initialize。
- 只要有异步组件、阶段钩子或宿主工作，应调用并等待 InitializeAsync。
- 包含 async-only 资源时等待 DestroyAsync/DisposeAsync，不用同步包装掩盖释放时序。
- 回调不要等待自身所属架构的 InitializeAsync、WaitUntilReadyAsync 或 DestroyAsync，否则会形成自等待。

## 相关主题

- [架构入口](./architecture.md)
- [生命周期](./lifecycle.md)
- [模型](./model.md)
- [系统](./system.md)
- [工具](./utility.md)
