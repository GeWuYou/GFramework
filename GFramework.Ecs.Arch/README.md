# GFramework.Ecs.Arch

`GFramework.Ecs.Arch` 是 `GFramework` 当前 Arch ECS 集成的默认运行时实现包。

它负责把 Arch `World`、GFramework 的服务模块生命周期，以及 `ArchSystemAdapter<T>` 系统桥接到同一条采用路径中。
如果你需要的只是共享契约，请改为依赖 `GFramework.Ecs.Arch.Abstractions`。

## 包定位

- 这是运行时实现层，不是纯契约层。
- 适合需要 `UseArch(...)`、`World` 自动注册、默认模块生命周期和系统桥接基类的项目。
- 常见场景：
  - 在架构实例上显式接入 Arch ECS
  - 让 `World` 由默认模块创建并放入容器
  - 让 ECS 系统复用 `ArchSystemAdapter<float>` 生命周期桥接
  - 通过 `IArchEcsModule.Update(deltaTime)` 统一驱动 ECS 帧更新

## 与相邻包的关系

- `GFramework.Core`
  - 提供架构、容器、生命周期和系统注册基础设施。
- `GFramework.Ecs.Arch.Abstractions`
  - 提供 `IArchEcsModule`、`IArchSystemAdapter<T>` 和契约层 `ArchOptions`。
- `GFramework.Ecs.Arch`
  - 提供 `UseArch(...)`、默认 `ArchEcsModule`、`World` 注册，以及系统适配器基类与示例类型。

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

### 3. 编写并注册系统

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

### 4. 初始化后获取 `World` 与 ECS 模块

```csharp
using Arch.Core;
using GFramework.Ecs.Arch.Abstractions;

var world = architecture.Context.GetService<World>();
var ecsModule = architecture.Context.GetService<IArchEcsModule>();
```

### 5. 由宿主循环驱动更新

```csharp
ecsModule.Update(deltaTime);
```

World 在配置阶段尚未创建。初始化解析同一个 Singleton World；若系统中途失败，模块停止已经启动的系统，provider 仍释放已创建资源。手动 `World.Destroy(world)` 不应与 provider 所有权混用。

`ArchSystemAdapter.OnDestroy()` 负责业务停止；`OnArchDispose()` 由拥有该适配器的 provider 或外部创建者释放时调用。需要自有停止逻辑时重写 `OnDestroy()` 并调用基类，不在其中重复释放 DI 拥有的资源。

## 运行时职责地图

| 文件 | 作用 |
| --- | --- |
| `Extensions/ArchExtensions.cs` | 通过 `UseArch(...)` 把默认模块登记到当前架构实例 |
| `ArchEcsModule.cs` | 声明 provider 拥有的 World 工厂，按优先级初始化系统，逆序停止已启动部分并驱动帧更新 |
| `ArchSystemAdapter.cs` | 把 GFramework 系统生命周期桥接到 Arch `ISystem<T>` 生命周期 |
| `ArchOptions.cs` | 承载 `WorldCapacity`、`EnableStatistics`、`Priority` 这组运行时配置 |
| `Components/*.cs`、`Systems/*.cs` | 提供最小组件与系统示例，帮助对照查询写法和更新模式 |

## XML 阅读入口

下表汇总当前模块 README 与源码可对照的类型级 XML 文档入口，方便先抓住 Arch ECS 集成的关键类型。

| 类型族 | 代表类型 | 阅读重点 |
| --- | --- | --- |
| 装配入口 | `ArchExtensions` | `UseArch(...)` 的接入时机、返回值与默认模块拼装方式 |
| 运行时模块 | `ArchEcsModule` | `World` 注册、系统排序、销毁顺序与配置对象的消费方式 |
| 系统桥接层 | `ArchSystemAdapter<T>` | `OnArchInitialize`、`OnUpdate`、`OnArchDispose` 的生命周期桥接 |
| 示例类型 | `Position`、`Velocity`、`MovementSystem` | 组件布局、查询写法与最小更新循环示例 |

## 对应文档入口

- ECS 总览：[ECS 文档首页](../docs/zh-CN/ecs/index.md)
- Arch ECS 集成：[Arch ECS 集成说明](../docs/zh-CN/ecs/arch.md)
- 抽象契约页：[ECS Arch Abstractions 文档](../docs/zh-CN/abstractions/ecs-arch-abstractions.md)
- 统一 API / XML 导航：[API 参考首页](../docs/zh-CN/api-reference/index.md)
