---
title: ArchitecturePhase
description: 架构阶段枚举，定义了架构生命周期的各个阶段。
outline: deep
---

# ArchitecturePhase

## 概述

架构阶段枚举，定义了架构生命周期的各个阶段。

**命名空间**：`GFramework.Core.Abstractions.enums`
**程序集**：`GFramework.Core.Abstractions`
**基础类型**：`Enum`

## 枚举值

### None

初始阶段，架构尚未开始初始化。

**值**：`0`

### BeforeUtilityInit

工具初始化前阶段。

**值**：`1`

### AfterUtilityInit

工具初始化后阶段。

**值**：`2`

### BeforeModelInit

模型初始化前阶段。

**值**：`3`

### AfterModelInit

模型初始化后阶段。

**值**：`4`

### BeforeSystemInit

系统初始化前阶段。

**值**：`5`

### AfterSystemInit

系统初始化后阶段。

**值**：`6`

### Ready

就绪状态，架构已完全初始化并可以使用。

**值**：`7`

### FailedInitialization

初始化失败状态。

**值**：`8`

### Destroying

正在销毁阶段。

**值**：`9`

### Destroyed

已销毁阶段。

**值**：`10`

## 使用示例

### 检查架构阶段

```csharp
var architecture = new GameArchitecture();
architecture.Initialize();

// 检查架构是否已就绪
if (architecture.CurrentPhase == ArchitecturePhase.Ready)
{
    Console.WriteLine("架构已就绪，可以开始游戏");
}
```

### 监听阶段变化

```csharp
public class PhaseMonitor : IArchitectureLifecycle
{
    public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        switch (phase)
        {
            case ArchitecturePhase.BeforeUtilityInit:
                Console.WriteLine("开始初始化工具");
                break;
            case ArchitecturePhase.AfterUtilityInit:
                Console.WriteLine("工具初始化完成");
                break;
            case ArchitecturePhase.BeforeModelInit:
                Console.WriteLine("开始初始化模型");
                break;
            case ArchitecturePhase.AfterModelInit:
                Console.WriteLine("模型初始化完成");
                break;
            case ArchitecturePhase.BeforeSystemInit:
                Console.WriteLine("开始初始化系统");
                break;
            case ArchitecturePhase.AfterSystemInit:
                Console.WriteLine("系统初始化完成");
                break;
            case ArchitecturePhase.Ready:
                Console.WriteLine("架构就绪");
                break;
            case ArchitecturePhase.FailedInitialization:
                Console.WriteLine("架构初始化失败");
                break;
            case ArchitecturePhase.Destroying:
                Console.WriteLine("架构正在销毁");
                break;
            case ArchitecturePhase.Destroyed:
                Console.WriteLine("架构已销毁");
                break;
        }
    }
}

// 注册监听器
var architecture = new GameArchitecture();
architecture.RegisterLifecycleHook(new PhaseMonitor());
architecture.Initialize();
```

### 等待特定阶段

```csharp
public async Task WaitForReady(IArchitecture architecture)
{
    while (architecture.CurrentPhase != ArchitecturePhase.Ready)
    {
        if (architecture.CurrentPhase == ArchitecturePhase.FailedInitialization)
        {
            throw new Exception("架构初始化失败");
        }

        await Task.Delay(100);
    }

    Console.WriteLine("架构已就绪");
}
```

## 阶段转换顺序

正常初始化流程的阶段转换顺序：

1. `None` → `BeforeUtilityInit`
2. `BeforeUtilityInit` → `AfterUtilityInit`
3. `AfterUtilityInit` → `BeforeModelInit`
4. `BeforeModelInit` → `AfterModelInit`
5. `AfterModelInit` → `BeforeSystemInit`
6. `BeforeSystemInit` → `AfterSystemInit`
7. `AfterSystemInit` → `Ready`

销毁流程的阶段转换顺序：

1. `Ready` → `Destroying`
2. `Destroying` → `Destroyed`

异常流程：

- 任何阶段 → `FailedInitialization`（初始化过程中发生异常）

## 另请参阅

- [Architecture](./architecture.md) - 架构基类
- [IArchitectureLifecycle](./iarchitecture-lifecycle.md) - 生命周期钩子接口
- [架构组件](/zh-CN/core/architecture) - 架构使用指南
