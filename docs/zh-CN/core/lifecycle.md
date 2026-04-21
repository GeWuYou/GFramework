---
title: 生命周期管理
description: 当前版本的架构生命周期由阶段模型、初始化顺序、逆序销毁和生命周期钩子共同组成。
---

# 生命周期管理

`GFramework.Core` 的生命周期由 `Architecture` 统一编排，而不是让每个组件各自决定初始化时机。

你真正需要关注的是：

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

这保证了大多数系统在初始化时，可以安全依赖已经就绪的工具与模型。

启动方式：

```csharp
var architecture = new GameArchitecture();
await architecture.InitializeAsync();
await architecture.WaitUntilReadyAsync();
```

注册逻辑仍然写在 `OnInitialize()`：

```csharp
protected override void OnInitialize()
{
    RegisterUtility(new SaveUtility());
    RegisterModel(new PlayerModel());
    RegisterSystem(new CombatSystem());
}
```

## 销毁语义

销毁由 `DestroyAsync()` 统一触发，框架会按逆序回收组件。

如果组件实现了异步销毁接口，框架会优先走异步路径。也就是说，新代码应优先实现：

- `IAsyncDestroyable`
- 或其他已有的异步销毁基类路径

同步 `Destroy()` 主要是兼容入口。

## 组件自己的生命周期

大多数组件不需要手写 `Initialize()`；继承框架基类即可：

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

如果你的组件需要真正的异步初始化或销毁，再补对应接口。

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
architecture.RegisterLifecycleHook(new MetricsHook());
```

## 阶段监听

如果你只需要观察阶段变化，也可以直接订阅：

```csharp
architecture.PhaseChanged += phase =>
{
    Console.WriteLine($"Phase changed: {phase}");
};
```

## 什么时候会进入 `FailedInitialization`

如果初始化流程中抛出异常，架构会切到 `FailedInitialization`。这意味着：

- `Ready` 不会被触发
- 后续诊断应先回到启动路径
- 文档示例不应假设“只要 new 了 Architecture 就一定能跑到 Ready”

## 推荐做法

- 新代码优先使用 `InitializeAsync()` / `DestroyAsync()`
- 把注册逻辑放在 `OnInitialize()`，不要沿用旧文档里的 `Init()`
- 让 `Utility` 承载底层能力，让 `Model` 承载状态，再让 `System` 消费两者
- 跨组件阶段逻辑优先写成 `IArchitectureLifecycleHook`

## 继续阅读

- 架构入口：[architecture](./architecture.md)
- 上下文入口：[context](./context.md)
