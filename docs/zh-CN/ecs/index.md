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
| `GFramework.Ecs.Arch` | 需要默认运行时、`UseArch(...)` 装配入口、`World` 注册和系统适配基类 | `ArchEcsModule`、`ArchSystemAdapter<T>`、`ArchExtensions.UseArch(...)`、示例组件与系统 | [`arch.md`](./arch.md) |
| `GFramework.Ecs.Arch.Abstractions` | 只想让共享宿主循环、测试替身或扩展模块依赖最小契约，而不引入默认运行时 | `IArchEcsModule`、`IArchSystemAdapter<T>`、`ArchOptions` 契约对象 | [`../abstractions/ecs-arch-abstractions.md`](../abstractions/ecs-arch-abstractions.md) |

## 最小采用路径

### 1. 选择包边界

- 需要默认实现时安装 `GeWuYou.GFramework.Ecs.Arch`
- 只需要契约时安装 `GeWuYou.GFramework.Ecs.Arch.Abstractions`

### 2. 在 `Initialize()` 前显式接入运行时

`UseArch(...)` 通过 `ArchitectureModuleRegistry` 注册服务模块。按当前源码与集成测试，它应在架构实例调用
`Initialize()` 之前完成。

```csharp
using Arch.Core;
using GFramework.Core.Architectures;
using GFramework.Ecs.Arch.Abstractions;
using GFramework.Ecs.Arch.Extensions;

public sealed class GameArchitecture : Architecture
{
    public GameArchitecture() : base(new ArchitectureConfiguration())
    {
    }

    protected override void OnInitialize()
    {
        RegisterSystem<MovementSystem>();
    }
}

var architecture = new GameArchitecture()
    .UseArch(options =>
    {
        options.WorldCapacity = 2048;
        options.Priority = 50;
    });

architecture.Initialize();

var world = architecture.Context.GetService<World>();
var ecsModule = architecture.Context.GetService<IArchEcsModule>();
```

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

## 阅读顺序

1. 先看本页，确认自己要的是运行时包还是契约包
2. 需要默认实现时继续读 [`arch.md`](./arch.md)
3. 只想保留共享边界时继续读 [`../abstractions/ecs-arch-abstractions.md`](../abstractions/ecs-arch-abstractions.md)
4. 统一查阅 README / docs / XML 入口时回到 [`../api-reference/index.md`](../api-reference/index.md)

## 源码阅读入口

如果你要从栏目入口页回到源码和 XML 文档，建议按下面的入口阅读：

| 包 | 类型族 | 代表类型 | 建议先确认什么 |
| --- | --- | --- | --- |
| `GFramework.Ecs.Arch` | 运行时装配与模块生命周期 | `ArchExtensions`、`ArchEcsModule` | `UseArch(...)` 的接入时机、`World` 注册、模块优先级 |
| `GFramework.Ecs.Arch` | 系统桥接层 | `ArchSystemAdapter<T>` | GFramework `ISystem` 生命周期如何桥接到 Arch `ISystem<T>` |
| `GFramework.Ecs.Arch` | 示例组件与系统 | `Position`、`Velocity`、`MovementSystem` | 查询写法、组件布局和最小可运行示例 |
| `GFramework.Ecs.Arch.Abstractions` | 契约与配置对象 | `IArchEcsModule`、`IArchSystemAdapter<T>`、`ArchOptions` | 共享宿主循环、测试替身和跨程序集配置边界 |

## 边界说明

- 当前仓库没有交付其他可直接消费的 ECS 运行时包；旧文档把“未来可能支持的其他 ECS 框架”写成现有能力，会误导采用路径。
- `GFramework.Ecs.Arch.Abstractions` 负责“边界”，`GFramework.Ecs.Arch` 负责“默认实现”。
- 站内页面只维护可构建的 docs 链路；仓库根 README 和模块 README 继续承担包目录入口职责。
