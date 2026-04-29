---
title: Arch ECS 集成
description: GFramework.Ecs.Arch 的默认运行时装配路径、系统桥接方式与源码阅读入口。
---

# Arch ECS 集成

`GFramework.Ecs.Arch` 是当前仓库里负责 Arch ECS 默认接入路径的运行时包。它把 Arch `World`、GFramework 的
`IServiceModule` 生命周期，以及 `AbstractSystem` / `ISystem` 体系桥接到同一条初始化与更新链路中。

## 什么时候依赖它

当你需要下面任一能力时，应直接依赖 `GeWuYou.GFramework.Ecs.Arch`：

- 在架构实例上调用 `UseArch(...)`
- 让 `World` 在服务模块注册阶段自动创建并注入容器
- 让 ECS 系统继承 `ArchSystemAdapter<T>`
- 使用仓库自带的 `Position`、`Velocity`、`MovementSystem` 最小示例

如果你只想保留共享边界，而不依赖默认实现，请改看
[ECS 抽象层说明](../abstractions/ecs-arch-abstractions.md)。

## 最小接入路径

### 1. 安装包

```bash
dotnet add package GeWuYou.GFramework.Ecs.Arch
```

### 2. 在 `Initialize()` 之前调用 `UseArch(...)`

当前实现通过 `ArchitectureModuleRegistry.Register(...)` 提前登记 `ArchEcsModule`。这意味着调用时机应位于
`Initialize()` 之前，而不是放进 `OnInitialize()` 里。

```csharp
using GFramework.Core.Architectures;
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
```

### 3. 用 `ArchSystemAdapter<float>` 编写系统

`ArchSystemAdapter<T>` 在 `OnInit()` 中从当前上下文解析 `World`，再把 Arch 的 `Initialize / BeforeUpdate /
AfterUpdate / Dispose` 钩子桥接到可重写方法。

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

### 4. 初始化后解析 `World` 与模块服务

```csharp
using Arch.Core;
using GFramework.Ecs.Arch.Abstractions;

var world = architecture.Context.GetService<World>();
var ecsModule = architecture.Context.GetService<IArchEcsModule>();
```

### 5. 由宿主循环显式调用 `Update`

```csharp
ecsModule.Update(deltaTime);
```

这一步在 `GFramework.Ecs.Arch.Tests/Ecs/*.cs` 中也采用同样的驱动方式。

## 运行时职责

| 类型 | 责任 | 证据文件 |
| --- | --- | --- |
| `ArchExtensions` | 把 `ArchEcsModule` 注册到 `ArchitectureModuleRegistry` | `GFramework.Ecs.Arch/Extensions/ArchExtensions.cs` |
| `ArchEcsModule` | 创建并注册 `World`，按优先级收集 `ArchSystemAdapter<float>`，负责初始化、销毁和逐帧更新 | `GFramework.Ecs.Arch/ArchEcsModule.cs` |
| `ArchSystemAdapter<T>` | 从 GFramework 系统生命周期桥接到 Arch `ISystem<T>` 生命周期 | `GFramework.Ecs.Arch/ArchSystemAdapter.cs` |
| `ArchOptions` | 暴露 `WorldCapacity`、`EnableStatistics`、`Priority` 这组运行时配置对象 | `GFramework.Ecs.Arch/ArchOptions.cs` |

## 配置对象阅读提示

当前公开配置对象是 `GFramework.Ecs.Arch.ArchOptions`。从源码可直接确认：

- `WorldCapacity` 用于 `World.Create(...)` 的容量参数
- `Priority` 影响 `ArchEcsModule` 作为服务模块的排序
- `EnableStatistics` 目前保留在公开配置面上；采用时应以源码 XML 注释和实现行为为准

## 源码阅读入口

| 类型族 | 代表类型 | 建议先确认什么 |
| --- | --- | --- |
| 装配入口 | `ArchExtensions` | `UseArch(...)` 的时机、链式调用返回值 |
| 服务模块 | `ArchEcsModule` | `World` 注册、系统收集、模块销毁顺序 |
| 系统桥接层 | `ArchSystemAdapter<T>` | `OnArchInitialize` / `OnUpdate` / `OnArchDispose` |
| 示例类型 | `Position`、`Velocity`、`MovementSystem` | 组件布局、查询写法和最小集成样例 |

## 相关入口

- ECS 模块总览：[ECS 模块总览](./index.md)
- 抽象契约页：[ECS 抽象层说明](../abstractions/ecs-arch-abstractions.md)
- 选包与接入顺序：[入门指南](../getting-started/index.md)
- 统一 API / XML 导航：[API 参考](../api-reference/index.md)
