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
- 让正式 provider 在初始化阶段按需创建并拥有 `World`
- 让 ECS 系统继承 `ArchSystemAdapter<T>`
- 使用仓库自带的 `Position`、`Velocity`、`MovementSystem` 最小示例

如果你只想保留共享边界，而不依赖默认实现，请改看
[ECS 抽象层说明](../abstractions/ecs-arch-abstractions.md)。

## 最小接入路径

### 1. 安装包

```bash
dotnet add package GeWuYou.GFramework.Ecs.Arch
```

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

World 在配置阶段尚未创建。初始化解析同一个 Singleton World；若系统中途失败，模块停止已经启动的系统，provider 仍释放已创建资源。手动 `World.Destroy(world)` 不应与 provider 所有权混用。

`ArchSystemAdapter.OnDestroy()` 负责业务停止；`OnArchDispose()` 由拥有该适配器的 provider 或外部创建者释放时调用。需要自有停止逻辑时重写 `OnDestroy()` 并调用基类，不在其中重复释放 DI 拥有的资源。

## 运行时职责

| 类型 | 责任 | 证据文件 |
| --- | --- | --- |
| `ArchExtensions` | 把 `ArchEcsModule` 注册到当前架构实例 | `GFramework.Ecs.Arch/Extensions/ArchExtensions.cs` |
| `ArchEcsModule` | 声明 provider 拥有的 World 工厂，按优先级初始化系统，逆序停止已启动部分并驱动帧更新 | `GFramework.Ecs.Arch/ArchEcsModule.cs` |
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
