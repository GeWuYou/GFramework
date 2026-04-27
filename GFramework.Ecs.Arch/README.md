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

### 2. 在 `Initialize()` 之前显式接入 Arch runtime

按当前实现，`UseArch(...)` 会把 `ArchEcsModule` 提前登记到 `ArchitectureModuleRegistry`，因此调用时机应早于
`Initialize()`。

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

## 运行时职责地图

| 文件 | 作用 |
| --- | --- |
| `Extensions/ArchExtensions.cs` | 通过 `UseArch(...)` 把默认模块注册到 `ArchitectureModuleRegistry` |
| `ArchEcsModule.cs` | 创建并注册 `World`，按优先级收集 `ArchSystemAdapter<float>`，负责初始化、销毁和逐帧更新 |
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
