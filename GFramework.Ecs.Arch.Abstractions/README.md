# GFramework.Ecs.Arch.Abstractions

`GFramework.Ecs.Arch.Abstractions` 承载 Arch ECS 集成层的最小契约，用来让共享业务层、宿主循环或扩展模块在不依赖
`GFramework.Ecs.Arch` 默认实现的前提下，仍然可以约定 ECS 模块边界。

如果你需要的是 `UseArch(...)` 扩展、`ArchSystemAdapter<T>` 基类、`World` 注册和默认模块实现，请改为依赖
`GFramework.Ecs.Arch`。

## 包定位

- 这是 `Ecs.Arch` 的契约层，不是默认实现层。
- 适合让上层模块只面向 `IArchEcsModule`、`IArchSystemAdapter<T>` 和 `ArchOptions` 编程。
- 常见场景：
  - 共享宿主循环只依赖更新契约，不直接引用 Arch runtime 实现
  - 多程序集之间需要共享 ECS 配置对象或接口边界
  - 测试替身、编辑器工具或外部适配层希望复用契约，但自行决定底层实现

## 与相邻包的关系

- `GFramework.Core.Abstractions`
  - 本包直接依赖它，并复用 `IServiceModule`、`ISystem` 等基础契约。
- `GFramework.Ecs.Arch.Abstractions`
  - 只定义 Arch ECS 集成相关的最小契约和配置对象。
- `GFramework.Ecs.Arch`
  - 本包的默认实现层。
  - 负责 `UseArch(...)` 扩展、默认模块注册、Arch `World` 装配，以及系统适配器基类。

## 契约地图

| 文件 | 作用 |
| --- | --- |
| `IArchEcsModule.cs` | ECS 模块服务契约，负责统一驱动系统更新 |
| `IArchSystemAdapter.cs` | 让 ECS 系统适配到 GFramework `ISystem` 生命周期的接口 |
| `ArchOptions.cs` | `WorldCapacity`、`EnableStatistics`、`Priority` 等配置对象 |

## 最小接入路径

### 1. 只想约定宿主循环与 ECS 模块边界

```csharp
using GFramework.Ecs.Arch.Abstractions;

public sealed class EcsUpdateLoop
{
    private readonly IArchEcsModule _ecsModule;

    public EcsUpdateLoop(IArchEcsModule ecsModule)
    {
        _ecsModule = ecsModule;
    }

    public void Tick(float deltaTime)
    {
        _ecsModule.Update(deltaTime);
    }
}
```

### 2. 只想共享配置对象

```csharp
using GFramework.Ecs.Arch.Abstractions;

var options = new ArchOptions
{
    WorldCapacity = 2048,
    EnableStatistics = true,
    Priority = 40
};
```

### 3. 什么时候要升级到 `GFramework.Ecs.Arch`

一旦你需要下面任一项，就不该只停留在本包：

- `UseArch(...)` 或其他 runtime 装配入口
- `ArchSystemAdapter<T>` 等默认基类
- Arch `World` 的创建、注册和查询能力
- 与 `GFramework` 架构生命周期绑定的默认模块实现

## 边界说明

- 本包不提供 Arch `World` 的默认构造与注册逻辑。
- 本包不提供系统基类、扩展方法或默认服务实现。
- 它回答的是“外部模块怎样与 Arch ECS 集成层约定边界”，不是“Arch ECS 默认怎么接入到项目里”。

## 对应文档入口

- 抽象接口总览：[`../docs/zh-CN/abstractions/index.md`](../docs/zh-CN/abstractions/index.md)
- Ecs.Arch 抽象层说明：[`../docs/zh-CN/abstractions/ecs-arch-abstractions.md`](../docs/zh-CN/abstractions/ecs-arch-abstractions.md)
- ECS 模块入口：[`../docs/zh-CN/ecs/index.md`](../docs/zh-CN/ecs/index.md)
- Arch ECS 集成：[`../docs/zh-CN/ecs/arch.md`](../docs/zh-CN/ecs/arch.md)
- 运行时实现入口：[`../GFramework.Ecs.Arch/README.md`](../GFramework.Ecs.Arch/README.md)
