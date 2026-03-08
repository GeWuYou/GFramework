---
title: ECS 系统集成
description: GFramework 的 ECS（Entity Component System）集成方案，支持多种 ECS 框架。
---

# ECS 系统集成

## 概述

GFramework 提供了灵活的 ECS（Entity Component System）集成方案，允许你根据项目需求选择合适的 ECS 框架。ECS
是一种数据驱动的架构模式，特别适合处理大量相似实体的场景。

## 什么是 ECS？

ECS（Entity Component System）是一种架构模式，将游戏对象分解为三个核心概念：

- **Entity（实体）**：游戏世界中的基本对象，本质上是一个唯一标识符
- **Component（组件）**：纯数据结构，存储实体的状态
- **System（系统）**：包含游戏逻辑，处理具有特定组件组合的实体

### ECS 的优势

- **高性能**：数据局部性好，缓存友好
- **可扩展**：通过组合组件轻松创建新实体类型
- **并行处理**：系统之间相互独立，易于并行化
- **数据驱动**：逻辑与数据分离，便于序列化和网络同步

### 何时使用 ECS？

**适合使用 ECS 的场景**：

- 大量相似实体（敌人、子弹、粒子）
- 需要高性能批量处理
- 复杂的实体组合和变化
- 需要并行处理的系统

**不适合使用 ECS 的场景**：

- 全局状态管理
- 单例服务
- UI 逻辑
- 游戏流程控制

## 支持的 ECS 框架

GFramework 采用可选集成的设计，你可以根据需求选择合适的 ECS 框架：

### Arch ECS（推荐）

[Arch](https://github.com/genaray/Arch) 是一个高性能的 C# ECS 框架，具有以下特点：

- ✅ **极致性能**：基于 Archetype 的内存布局，零 GC 分配
- ✅ **简单易用**：清晰的 API，易于上手
- ✅ **功能完整**：支持查询、过滤、并行处理等高级特性
- ✅ **活跃维护**：社区活跃，持续更新

**安装方式**：

```bash
dotnet add package GeWuYou.GFramework.Ecs.Arch
```

**文档链接**：[Arch ECS 集成指南](./arch.md)

### 其他 ECS 框架

GFramework 的设计允许集成其他 ECS 框架，未来可能支持：

- **DefaultEcs**：轻量级 ECS 框架
- **Entitas**：成熟的 ECS 框架，Unity 生态常用
- **自定义 ECS**：你可以基于 GFramework 的模块系统实现自己的 ECS 集成

## 快速开始

### 1. 选择 ECS 框架

根据项目需求选择合适的 ECS 框架。对于大多数项目，我们推荐使用 Arch ECS。

### 2. 安装集成包

```bash
# 安装 Arch ECS 集成包
dotnet add package GeWuYou.GFramework.Ecs.Arch
```

### 3. 注册 ECS 模块

```csharp
using GFramework.Core.architecture;
using GFramework.Ecs.Arc;

public class GameArchitecture : Architecture
{
    public GameArchitecture() : base(new ArchitectureConfiguration())
    {
    }

    protected override void OnInitialize()
    {
        // 显式注册 Arch ECS 模块
        this.UseArch(options =>
        {
            options.WorldCapacity = 2000;
            options.EnableStatistics = true;
        });
    }
}
```

### 4. 定义组件

```csharp
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct Position(float x, float y)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
}

[StructLayout(LayoutKind.Sequential)]
public struct Velocity(float x, float y)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
}
```

### 5. 创建系统

```csharp
using Arch.Core;
using GFramework.Ecs.Arch;

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
        World.Query(in _query, (ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        });
    }
}
```

### 6. 注册系统

```csharp
public class GameArchitecture : Architecture
{
    protected override void OnInitialize()
    {
        this.UseArch();

        // 注册 ECS 系统
        RegisterSystem<MovementSystem>();
    }
}
```

## 设计理念

### 显式集成

GFramework 采用显式集成的设计，而不是自动注册：

```csharp
// ✅ 显式注册 - 清晰、可控
public class GameArchitecture : Architecture
{
    protected override void OnInitialize()
    {
        this.UseArch(); // 明确表示使用 Arch ECS
    }
}

// ❌ 自动注册 - 隐式、难以控制
// 只需引入包，自动注册（不推荐）
```

**优势**：

- 清晰的依赖关系
- 更好的 IDE 支持
- 易于测试和调试
- 符合 .NET 生态习惯

### 零依赖原则

如果你不使用 ECS，GFramework.Core 包不会引入任何 ECS 相关的依赖：

```xml
<!-- GFramework.Core.csproj -->
<ItemGroup>
  <!-- 无 Arch 依赖 -->
</ItemGroup>

<!-- GFramework.Ecs.Arch.csproj -->
<ItemGroup>
  <PackageReference Include="Arch" Version="2.1.0" />
  <PackageReference Include="Arch.System" Version="1.1.0" />
</ItemGroup>
```

### 模块化设计

ECS 集成基于 GFramework 的模块系统：

```csharp
// ECS 模块实现 IServiceModule 接口
public sealed class ArchEcsModule : IArchEcsModule
{
    public string ModuleName => nameof(ArchEcsModule);
    public int Priority => 50;
    public bool IsEnabled { get; }

    public void Register(IIocContainer container) { }
    public void Initialize() { }
    public ValueTask DestroyAsync() { }
    public void Update(float deltaTime) { }
}
```

## 与传统架构结合

ECS 可以与 GFramework 的传统架构（Model、System、Utility）无缝结合：

```csharp
// Model 存储全局状态
public class GameStateModel : AbstractModel
{
    public int Score { get; set; }
    public int Level { get; set; }
}

// ECS System 处理实体逻辑
public class EnemySpawnSystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        // 访问 Model
        var gameState = this.GetModel<GameStateModel>();

        // 根据关卡生成敌人
        for (int i = 0; i < gameState.Level; i++)
        {
            World.Create(
                new Position(Random.Shared.Next(0, 100), 0),
                new Velocity(0, -1),
                new Health(50, 50)
            );
        }
    }
}

// 传统 System 处理游戏逻辑
public class ScoreSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<EnemyDestroyedEvent>(OnEnemyDestroyed);
    }

    private void OnEnemyDestroyed(EnemyDestroyedEvent e)
    {
        var gameState = this.GetModel<GameStateModel>();
        gameState.Score += 100;
    }
}
```

## 下一步

- [Arch ECS 集成指南](./arch.md) - 详细的 Arch ECS 使用文档

## 相关资源

- [Architecture 架构系统](../core/architecture.md)
- [System 系统](../core/system.md)
- [事件系统](../core/events.md)
