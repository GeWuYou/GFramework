---
title: Ecs.Arch 抽象层
description: GFramework.Ecs.Arch.Abstractions 的契约边界、包关系和最小接入路径。
---

# Ecs.Arch 抽象层

`GFramework.Ecs.Arch.Abstractions` 是 Arch ECS 集成层的契约包。

它建立在 `GFramework.Core.Abstractions` 之上，只定义 ECS 模块更新、系统适配和配置对象，不负责默认的 Arch
`World` 装配、扩展方法或系统基类。

如果你需要开箱即用的集成实现，请改为依赖 `GFramework.Ecs.Arch`。

## 什么时候单独依赖它

- 你在做共享宿主循环、工具层或 feature 包，只需要 `IArchEcsModule`
- 你想让不同程序集共享 `ArchOptions` 或系统适配契约，但不直接绑定默认 runtime
- 你需要为测试或外部适配层提供替身实现

## 包关系

- 契约层：`GFramework.Ecs.Arch.Abstractions`
- 运行时实现：`GFramework.Ecs.Arch`
- 底层基础契约：`GFramework.Core.Abstractions`

## 契约地图

| 类型 | 作用 |
| --- | --- |
| `IArchEcsModule` | 统一更新 ECS 系统的服务模块契约 |
| `IArchSystemAdapter<T>` | 让 ECS 系统适配到 `ISystem` 生命周期 |
| `ArchOptions` | 承载 `WorldCapacity`、`EnableStatistics`、`Priority` 等配置 |

## 契约阅读入口

| 类型族 | 代表类型 | 建议先确认什么 |
| --- | --- | --- |
| 模块契约 | `IArchEcsModule` | 统一更新入口、宿主循环边界 |
| 系统契约 | `IArchSystemAdapter<T>` | 只依赖更新接口而不绑定默认 runtime |
| 配置对象 | `ArchOptions` | 共享配置字段与跨程序集采用边界 |

## 最小接入路径

### 1. 共享模块只依赖更新契约

```csharp
using GFramework.Ecs.Arch.Abstractions;

public sealed class GameplayHost
{
    private readonly IArchEcsModule _ecsModule;

    public GameplayHost(IArchEcsModule ecsModule)
    {
        _ecsModule = ecsModule;
    }

    public void Tick(float deltaTime)
    {
        _ecsModule.Update(deltaTime);
    }
}
```

### 2. 共享配置对象

```csharp
using GFramework.Ecs.Arch.Abstractions;

var options = new ArchOptions
{
    WorldCapacity = 2048,
    EnableStatistics = true,
    Priority = 40
};
```

### 3. 什么时候切到运行时包

下面这些需求都属于 `GFramework.Ecs.Arch` 的职责，而不是本包：

- 通过 `UseArch(...)` 把模块挂进架构
- 使用默认的 `ArchSystemAdapter<T>` 基类
- 访问 Arch `World` 与查询 API
- 使用默认的模块装配和生命周期实现

## 阅读顺序

1. 先读本页，确认你是否真的只需要契约层
2. 如果需要默认实现，再看 [Arch ECS 集成](../ecs/arch.md)
3. 需要统一入口时，再看：
   - [ECS 模块总览](../ecs/index.md)
   - [入门指南](../getting-started/index.md)
