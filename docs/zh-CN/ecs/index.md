---
title: ECS 系统集成
description: GFramework 当前 ECS 模块族的包边界、采用顺序与源码阅读入口。
---

# ECS 系统集成

GFramework 当前仓库内已经交付并持续维护的 ECS 模块族是 `Ecs.Arch`。它分成运行时实现包
`GFramework.Ecs.Arch` 和契约包 `GFramework.Ecs.Arch.Abstractions`，分别覆盖默认装配能力与共享边界约定。

## 当前模块族

| 包 | 适用场景 | 你会得到什么 | 继续阅读 |
| --- | --- | --- | --- |
| `GFramework.Ecs.Arch` | 需要默认运行时、`UseArch(...)` 装配入口、`World` 注册和系统适配基类 | `ArchEcsModule`、`ArchSystemAdapter<T>`、`ArchExtensions.UseArch(...)`、示例组件与系统 | [Arch ECS 集成](./arch.md) |
| `GFramework.Ecs.Arch.Abstractions` | 只想让共享宿主循环、测试替身或扩展模块依赖最小契约，而不引入默认运行时 | `IArchEcsModule`、`IArchSystemAdapter<T>`、`ArchOptions` 契约对象 | [ECS 抽象层说明](../abstractions/ecs-arch-abstractions.md) |

## 最小采用路径

### 1. 选择包边界

- 需要默认实现时安装 `GeWuYou.GFramework.Ecs.Arch`
- 只需要契约时安装 `GeWuYou.GFramework.Ecs.Arch.Abstractions`

### 2. 在配置阶段声明 ECS 模块与系统

在 `ConfigureServices(IServiceCollection)` 中调用 `UseArch(...)`。该入口只登记到当前架构实例；配置结束后会拒绝新增模块，不再写入全局注册表。

将 ECS 驱动的系统显式注册为 `ArchSystemAdapter<float>` 服务。模块负责注入当前实例上下文及系统的初始化、更新和停止；provider 负责 World 与自己创建的系统资源。

```csharp
using Arch.Core;
using GFramework.Core.Architectures;
using GFramework.Ecs.Arch;
using GFramework.Ecs.Arch.Abstractions;
using GFramework.Ecs.Arch.Extensions;
using GFramework.Ecs.Arch.Systems;
using Microsoft.Extensions.DependencyInjection;

await using var architecture = new GameArchitecture();
await architecture.InitializeAsync();

var world = architecture.Context.GetService<World>();
var ecsModule = architecture.Context.GetService<IArchEcsModule>();

public sealed class GameArchitecture : Architecture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        this.UseArch(options =>
        {
            options.WorldCapacity = 2048;
            options.Priority = 50;
        });
        services.AddSingleton<ArchSystemAdapter<float>, MovementSystem>();
    }
}
```

不要把同一系统同时声明为 Architecture 的独立 System 和 ECS 模块的拥有系统，也不要通过多个 owning 工厂为可释放对象建立 alias。纯契约的共享宿主循环仍可只引用抽象包。

### 3. 让 ECS 系统继承 `ArchSystemAdapter<float>`

```csharp
using Arch.Core;
using GFramework.Ecs.Arch;
using GFramework.Ecs.Arch.Components;

public sealed class MovementSystem : ArchSystemAdapter<float>
{
    private QueryDescription _query;

    protected override void OnArchInitialize()
    {
        _query = new QueryDescription()
            .WithAll<Position, Velocity>();
    }

    protected override void OnUpdate(in float deltaTime)
    {
        var frameDelta = deltaTime;

        World.Query(in _query, (ref Position position, ref Velocity velocity) =>
        {
            position.X += velocity.X * frameDelta;
            position.Y += velocity.Y * frameDelta;
        });
    }
}
```

### 4. 由宿主循环驱动更新

`IArchEcsModule` 继承自 `IServiceModule`，负责初始化和销毁；真正的帧更新通过 `Update(float deltaTime)` 显式触发。

```csharp
using GFramework.Ecs.Arch.Abstractions;

public sealed class GameLoop
{
    private readonly IArchEcsModule _ecsModule;

    public GameLoop(IArchEcsModule ecsModule)
    {
        _ecsModule = ecsModule;
    }

    public void Tick(float deltaTime)
    {
        _ecsModule.Update(deltaTime);
    }
}
```

World 在配置阶段尚未创建。初始化解析同一个 Singleton World；若系统中途失败，模块停止已经启动的系统，provider 仍释放已创建资源。手动 `World.Destroy(world)` 不应与 provider 所有权混用。

`ArchSystemAdapter.OnDestroy()` 负责业务停止；`OnArchDispose()` 由拥有该适配器的 provider 或外部创建者释放时调用。需要自有停止逻辑时重写 `OnDestroy()` 并调用基类，不在其中重复释放 DI 拥有的资源。

## 阅读顺序

1. 先看本页，确认自己要的是运行时包还是契约包
2. 需要默认实现时继续读[Arch ECS 集成](./arch.md)
3. 只想保留共享边界时继续读[ECS 抽象层说明](../abstractions/ecs-arch-abstractions.md)
4. 统一查阅模块说明、专题页与 XML 入口时回到[API 参考](../api-reference/index.md)

## 源码阅读入口

如果你要从栏目入口页回到源码和 XML 文档，建议按下面的入口阅读：

| 包 | 类型族 | 代表类型 | 建议先确认什么 |
| --- | --- | --- | --- |
| `GFramework.Ecs.Arch` | 运行时装配与模块生命周期 | `ArchExtensions`、`ArchEcsModule` | `UseArch(...)` 的接入时机、`World` 注册、模块优先级 |
| `GFramework.Ecs.Arch` | 系统桥接层 | `ArchSystemAdapter<T>` | GFramework `ISystem` 生命周期如何桥接到 Arch `ISystem<T>` |
| `GFramework.Ecs.Arch` | 示例组件与系统 | `Position`、`Velocity`、`MovementSystem` | 查询写法、组件布局和最小可运行示例 |
| `GFramework.Ecs.Arch.Abstractions` | 契约与配置对象 | `IArchEcsModule`、`IArchSystemAdapter<T>`、`ArchOptions` | 共享宿主循环、测试替身和跨程序集配置边界 |

## 模块边界

- 当前仓库面向读者提供的 ECS 采用路径就是 `Ecs.Arch` 模块族；如果你要接入 ECS，可以直接按本页列出的两个包选择。
- `GFramework.Ecs.Arch.Abstractions` 适合共享宿主循环、测试替身或扩展模块，`GFramework.Ecs.Arch` 提供默认装配与运行时实现。
- 需要进一步查看包目录、源码结构或安装说明时，可继续阅读[Arch ECS 集成](./arch.md)、[ECS 抽象层说明](../abstractions/ecs-arch-abstractions.md)和对应模块说明页。
