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

- 默认启动会绑定全局 GameContext；同一进程的全局表只接受一个活动上下文，ScopedContextProvider 不会绕过此绑定。
- OnInitialize 是注册期入口；服务容器在组件初始化后 Freeze。按类型注册 System/Model 会先于正式 provider 创建实例，依赖 Singleton 的实例身份不能假定与 Ready 后解析一致。
- DestroyAsync 会停止已登记组件并解除全局绑定，但当前冻结容器的 Clear 不释放根 provider。若组合根显式持有 ArchitectureServices，应在组件停止后释放其拥有的 Container；外部实例仍由调用者负责。仅持有 Architecture 时没有独立的公开容器释放入口。
- 已缓存的上下文引用不会在销毁后自动清空，不应继续使用。

容器级所有权与释放方式见 [IoC 容器](./ioc.md)。这些是当前实现的使用限制，调用方需要按实际生命周期约束接入。

## 常用公开入口

最常见的成员只有这些：

- `OnInitialize()`
  - 子类唯一必须实现的入口，用来注册模型、系统、工具、模块和额外的 CQRS 行为
- `RegisterModel(...)` / `RegisterSystem(...)` / `RegisterUtility(...)`
  - 注册运行时组件
- `InstallModule(...)`
  - 安装实现了 `IArchitectureModule` 的模块
- `RegisterLifecycleHook(...)`
  - 注册阶段钩子
- `RegisterCqrsPipelineBehavior<TBehavior>()`
  - 注册 CQRS pipeline 行为
- `RegisterCqrsHandlersFromAssembly(...)` / `RegisterCqrsHandlersFromAssemblies(...)`
  - 显式接入其他程序集中的 CQRS handlers
- `InitializeAsync()` / `WaitUntilReadyAsync()`
  - 启动架构并等待进入 `Ready`
- `DestroyAsync()`
  - 逆序销毁所有已接入组件

## 最小示例

```csharp
using GFramework.Core.Architectures;

public sealed class GameArchitecture : Architecture
{
    protected override void OnInitialize()
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
```

## 初始化时机

当前 `Architecture` 的注册逻辑必须写在：

```csharp
protected override void OnInitialize()
{
}
```

框架会在 `InitializeAsync()` 内完成：

1. 基础设施准备
2. 创建并绑定 `ArchitectureContext`
3. 调用用户的 `OnInitialize()`
4. 按阶段初始化 `Utility -> Model -> System`
5. 进入 `Ready`

如果项目里仍保留 `protected override void Init()` 风格的旧代码，应迁移到 `OnInitialize()`。

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

销毁时会按逆序处理，并优先调用异步销毁接口。

## 模块与 CQRS 接入

如果你的功能以模块形式组织，优先通过 `InstallModule(...)` 接入，而不是把所有注册逻辑都堆进一个超大的 `OnInitialize()`。

如果 handlers 不只在当前架构程序集里，需要显式追加程序集：

```csharp
protected override void OnInitialize()
{
    RegisterCqrsPipelineBehavior<LoggingBehavior<,>>();

    RegisterCqrsHandlersFromAssemblies(
    [
        typeof(InventoryCqrsMarker).Assembly,
        typeof(BattleCqrsMarker).Assembly
    ]);
}
```

默认运行时会优先尝试消费端程序集上的生成注册表；缺失或不适用时回退到反射扫描。

## 阶段与钩子

`Architecture` 公开：

- `CurrentPhase`
- `IsReady`
- `PhaseChanged`
- `RegisterLifecycleHook(...)`

其中 `PhaseChanged` 现在遵循标准 `EventHandler<ArchitecturePhaseChangedEventArgs>` 约定，
阶段值通过 `args.Phase` 读取。

如果你正在从旧版本迁移，需要把单参数写法 `phase => ...` 改成 `(_, args) => ...`，
并通过 `ArchitecturePhaseChangedEventArgs.Phase` 读取阶段值。

如果你需要在 `Ready`、`Destroying` 等阶段执行横切逻辑，比起把这类逻辑塞进某个具体 `System`，更适合单独实现
`IArchitectureLifecycleHook`。

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
- 旧版命令 / 查询执行器兼容入口：阅读[命令执行](./command.md)与[查询执行](./query.md)
- 新项目的请求 / 通知模型：阅读[CQRS 运行时](./cqrs.md)
