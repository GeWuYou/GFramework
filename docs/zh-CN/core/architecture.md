---
title: 架构（Architecture）
description: 说明 GFramework.Core 的 Architecture 入口、生命周期职责与最常用注册 API。
---

# 架构（Architecture）

`Architecture` 是 `GFramework.Core` 的运行时入口。它负责三件事：

- 组织初始化与销毁阶段
- 接入模型、系统、工具和模块
- 暴露 `ArchitectureContext` 作为统一上下文入口

当前版本的 `Architecture` 将内部职责委托给初始化、组件注册、生命周期与模块管理协作者。

## 运行边界

- 每个 Architecture 拥有自己的容器、上下文和日志配置；日志工厂来自 `LoggerProperties.LoggerFactory`（缺省为实例独立的控制台工厂），架构实例之间互不影响。
- ConfigureServices 只描述依赖与参与者，不能解析服务或执行初始化 IO。配置完成后只构建一次 provider，再激活和初始化组件；构造依赖与 Ready 后解析共享同一 Singleton。
- 配置窗口关闭后不再允许追加组件、模块或钩子。需要读取服务的宿主接线写在 OnStarted 或 OnStartedAsync。
- DestroyAsync 和 DisposeAsync 等待同一次业务停止与 provider 异步释放。销毁开始后，外部保留的上下文不能继续解析服务；业务清理回调仍可访问所需依赖。
- DI 创建的对象由 provider 释放，调用者传入的实例仍由调用者负责最终资源释放。

容器的 scope、别名与所有权限制见 [IoC 容器](./ioc.md)。

## 常用公开入口

最常见的成员只有这些：

- `ConfigureServices(IServiceCollection services)`
  - 配置标准 DI 服务、组件与模块；默认可为空
- `OnStarted()` / `OnStartedAsync(CancellationToken)`
  - 组件启动完成后、Ready 前执行宿主接线，不再注册服务
- `RegisterModel(...)` / `RegisterSystem(...)` / `RegisterUtility(...)`
  - 注册运行时组件
- `InstallModule(...)` / `RegisterServiceModule(...)`
  - 分别安装架构装配模块、登记当前实例的服务模块
- `RegisterLifecycleHook(...)`
  - 注册阶段钩子
- `InitializeAsync(CancellationToken)` / `WaitUntilReadyAsync()`
  - 启动架构并等待进入 `Ready`
- `DestroyAsync()` / `DisposeAsync()`
  - 逆序停止业务参与者，异步释放当前 provider 拥有的资源

## 最小示例

```csharp
using GFramework.Core.Architectures;
using Microsoft.Extensions.DependencyInjection;

public sealed class GameArchitecture : Architecture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new CombatSystem());
        RegisterUtility(new SaveUtility());
    }
}
```

启动方式：

```csharp
var architecture = new GameArchitecture();
await architecture.InitializeAsync();
await architecture.WaitUntilReadyAsync();

// 使用完成后，等待业务停止与拥有资源释放。
await architecture.DisposeAsync();
```

## 初始化时机

当前 `Architecture` 的注册逻辑必须写在：

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
}
```

框架会在 `InitializeAsync()` 内完成：

1. 登记本实例环境、日志、上下文及内置模块描述
2. 执行 Configurator 和 ConfigureServices
3. 构建唯一 provider，激活组件并注入上下文
4. 初始化服务模块，再按 Utility → Model → System 初始化组件
5. 执行 OnStartedAsync，完成 Ready 阶段并唤醒等待者

旧 OnInitialize 或 Init 中的注册应迁移到 ConfigureServices；依赖服务的操作迁移到组件初始化或 OnStarted/OnStartedAsync。同步 Initialize 只接受同步参与者，异步组件或钩子需要 InitializeAsync。

## 组件注册顺序

`Architecture` 仍然维持清晰的组件边界：

- `Model`
  - 承载状态
- `System`
  - 承载业务流程
- `Utility`
  - 承载无状态或基础设施型能力

初始化顺序固定为：

1. `Utility`
2. `Model`
3. `System`

业务停止按实际启动顺序的逆序执行，优先等待异步销毁接口；之后才释放 provider。业务停止不应再次释放由 DI 拥有的依赖。

## 模块与 CQRS 接入

如果你的功能以模块形式组织，优先通过 `InstallModule(...)` 接入，而不是把所有注册逻辑都堆进一个超大的 `ConfigureServices(...)`。

CQRS runtime 通过显式安装的服务模块接入（不在默认模块列表中）：

```csharp
using GFramework.Cqrs.Extensions;

protected override void ConfigureServices(IServiceCollection services)
{
    services.AddCqrsPipelineBehavior<LoggingBehavior<,>>();
}
```

```csharp
var architecture = new GameArchitecture();
architecture.UseCqrsRuntime(typeof(InventoryModule).Assembly);
```

`UseCqrsRuntime(...)` 自动包含架构所在程序集，也可以改用 `RegisterServiceModule(new CqrsRuntimeModule(...))` 显式登记。详细语义见 [CQRS 运行时](./cqrs.md)。

## 阶段与钩子

`Architecture` 公开：

- `CurrentPhase`
- `IsReady`
- `PhaseChanged`
- `RegisterLifecycleHook(...)`

其中 `PhaseChanged` 现在遵循标准 `EventHandler<ArchitecturePhaseChangedEventArgs>` 约定，
阶段值通过 `args.Phase` 读取。CQRS pipeline 行为通过配置阶段的标准 `IServiceCollection` 登记（`services.AddCqrsPipelineBehavior<...>()`）。

如果你正在从旧版本迁移，需要把单参数写法 `phase => ...` 改成 `(_, args) => ...`，
并通过 `ArchitecturePhaseChangedEventArgs.Phase` 读取阶段值。

如果需要阶段横切逻辑，在 ConfigureServices 中登记 IArchitectureLifecycleHook。真正异步的阶段工作实现 IAsyncArchitectureLifecycleHook.OnPhaseAsync，并通过 InitializeAsync 启动；框架优先等待异步入口。

```csharp
architecture.PhaseChanged += (_, args) =>
{
    if (args.Phase == ArchitecturePhase.Ready)
    {
        Console.WriteLine("Architecture ready from event.");
    }
};

public sealed class MetricsHook : IArchitectureLifecycleHook
{
    public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        if (phase == ArchitecturePhase.Ready)
        {
            Console.WriteLine("Architecture ready.");
        }
    }
}
```

## 相关主题

- 上下文访问与 `ArchitectureContext`：阅读[上下文](./context.md)
- 初始化阶段、事件与销毁流程：阅读[生命周期](./lifecycle.md)
- 请求 / 通知 / 流式消息模型与 runtime 安装：阅读[CQRS 运行时](./cqrs.md)


## 宿主停止钩子

宿主可覆盖 OnStoppingAsync 关闭新操作接入，此时保留组件清理所需的节点和资源。组件及服务模块停止后，框架调用 OnStoppedAsync 分离宿主资源，再释放根 provider。正常销毁、启动失败和取消共用一次清理，单个阶段抛错不会跳过后续阶段。

异步场景退出需要跨帧或访问原生节点时，应在它们完成后才分离根节点。Godot 架构已经通过 OnStoppedAsync 完成模块分离和锚点释放。
