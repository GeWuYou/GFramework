---
title: 生命周期管理
description: 当前版本的架构生命周期由阶段模型、初始化顺序、逆序销毁和生命周期钩子共同组成。
---

# 生命周期管理

`GFramework.Core` 的生命周期由 `Architecture` 统一编排，而不是让每个组件各自决定初始化时机。

相关接口与行为包括：

- 阶段枚举 `ArchitecturePhase`
- 组件初始化顺序
- 逆序销毁语义
- `IArchitectureLifecycleHook`

## 阶段模型

当前公开阶段如下：

| 阶段 | 含义 |
| --- | --- |
| `None` | 尚未开始初始化 |
| `BeforeUtilityInit` | 即将初始化工具 |
| `AfterUtilityInit` | 工具初始化完成 |
| `BeforeModelInit` | 即将初始化模型 |
| `AfterModelInit` | 模型初始化完成 |
| `BeforeSystemInit` | 即将初始化系统 |
| `AfterSystemInit` | 系统初始化完成 |
| `Ready` | 架构已完成初始化并可供稳定使用 |
| `Destroying` | 正在销毁 |
| `Destroyed` | 已销毁 |
| `FailedInitialization` | 初始化流程失败 |

正常路径：

```text
None
-> BeforeUtilityInit
-> AfterUtilityInit
-> BeforeModelInit
-> AfterModelInit
-> BeforeSystemInit
-> AfterSystemInit
-> Ready
-> Destroying
-> Destroyed
```

## 初始化顺序

注册顺序和初始化顺序不是一回事。当前框架会按组件类别统一推进：

1. `Utility`
2. `Model`
3. `System`

同一类别保持登记顺序。依赖服务的构造发生在唯一 provider 构建之后，业务初始化再按角色顺序推进；普通服务不必为了使用 DI 而实现角色接口。

启动方式：

```csharp
var architecture = new GameArchitecture();
await architecture.InitializeAsync();
await architecture.WaitUntilReadyAsync();
```

注册描述写在 ConfigureServices；此阶段不读取服务或执行 IO：

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
    RegisterUtility(new SaveUtility());
    RegisterModel(new PlayerModel());
    RegisterSystem(new CombatSystem());
}
```

## 销毁语义

销毁由 DestroyAsync 或 DisposeAsync 统一触发，先阻止外部旧上下文发起新操作，再按启动顺序的逆序停止业务参与者和模块，最后异步释放正式 provider 拥有的资源。并发销毁等待同一结果，停止失败会继续其他清理并报告聚合异常。

如果组件实现了异步销毁接口，框架会优先走异步路径。也就是说，新代码应优先实现：

- `IAsyncDestroyable`
- 或其他已有的异步销毁基类路径

同步 Destroy 是兼容包装；存在异步资源时，调用方应等待异步清理。

组件的 Destroy 是业务停止，不能重复释放由 DI 拥有的依赖。类型/工厂创建的资源由 provider 释放，传入的外部实例由调用者负责最终释放。

## 组件自己的生命周期

大多数组件不需要手写 `Initialize()`；继承框架基类即可。

这里的 `OnInit()` 是组件级入口，用来初始化单个 `Model` / `System`；上面的 ConfigureServices 则用于架构级服务描述：

```csharp
public sealed class PlayerModel : AbstractModel
{
    protected override void OnInit()
    {
    }
}
```

```csharp
public sealed class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
    }
}
```

如果组件需要异步初始化，实现 IAsyncInitializable.InitializeAsync(CancellationToken cancellationToken = default) 并消费传入令牌。同步 Initialize 遇到异步参与者会明确拒绝，框架不通过 Task.Run 或 GetResult 隐藏异步工作。

## 生命周期钩子

当你需要做横切阶段逻辑时，优先实现 `IArchitectureLifecycleHook`，而不是把这些逻辑分散到某个具体 `System` 里。

```csharp
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

注册方式：

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
    RegisterLifecycleHook(new MetricsHook());
}
```

异步阶段工作实现 IAsyncArchitectureLifecycleHook.OnPhaseAsync(phase, architecture, token)。异步模式优先等待该入口，不会同时重复调用同步 OnPhase。具体示例见[异步初始化](./async-initialization.md)。

## 阶段监听

如果你只需要观察阶段变化，也可以直接订阅：

如果你从旧版本的 `PhaseChanged` 迁移过来，需要把旧写法 `phase => ...` 改成 `(_, args) => ...`，
并通过 `ArchitecturePhaseChangedEventArgs.Phase` 读取阶段值。

```csharp
architecture.PhaseChanged += (_, args) =>
{
    Console.WriteLine($"Phase changed: {args.Phase}");
};
```

## 什么时候会进入 `FailedInitialization`

如果配置、构造、组件或阶段钩子失败，或启动令牌取消，架构会自动清理已经进入启动的参与者和拥有资源，再完成 InitializeAsync 与 WaitUntilReadyAsync 的失败或取消结果。失败阶段保持 FailedInitialization；后续显式 DestroyAsync 进入终态，不重复资源清理。

同一架构只能初始化一次，失败后应创建新实例。没有启动过的架构也可以销毁；等待其 Ready 会得到终止结果。初始化和清理回调不可等待自己所属架构的启动/销毁任务。

## 推荐做法

- 新代码优先使用 `InitializeAsync()` / `DestroyAsync()`
- 把架构级注册放在 ConfigureServices；组件自己的同步初始化仍使用 OnInit，需要读取全部启动结果的宿主操作写在 OnStarted/OnStartedAsync
- 让 `Utility` 承载底层能力，让 `Model` 承载状态，再让 `System` 消费两者
- 跨组件阶段逻辑优先写成 `IArchitectureLifecycleHook`

## 继续阅读

- 架构入口：[architecture](./architecture.md)
- 上下文入口：[context](./context.md)
