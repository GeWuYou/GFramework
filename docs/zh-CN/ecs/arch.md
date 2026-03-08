---
title: Arch ECS 集成
description: GFramework 的 Arch ECS 集成包使用指南，提供高性能的实体组件系统支持。
---

# Arch ECS 集成

## 概述

`GFramework.Ecs.Arch` 是 GFramework 的 Arch ECS 集成包，提供开箱即用的 ECS（Entity Component
System）支持。基于 [Arch.Core](https://github.com/genaray/Arch) 实现，具有极致的性能和简洁的 API。

**主要特性**：

- 🎯 **显式集成** - 符合 .NET 生态习惯的显式注册方式
- 🔌 **零依赖** - 不使用时，Core 包无 Arch 依赖
- 🎯 **类型安全** - 完整的类型系统和编译时检查
- ⚡ **高性能** - 基于 Arch ECS 的高性能实现
- 🔧 **易扩展** - 简单的系统适配器模式
- 📊 **优先级支持** - 系统按优先级顺序执行

**性能特点**：

- 10,000 个实体更新 < 100ms
- 1,000 个实体创建 < 50ms
- 基于 Archetype 的高效内存布局
- 零 GC 分配的组件访问

## 安装

```bash
dotnet add package GeWuYou.GFramework.Ecs.Arch
```

## 快速开始

### 1. 注册 ECS 模块

```csharp
using GFramework.Core.architecture;
using GFramework.Ecs.Arch.extensions;

public class GameArchitecture : Architecture
{
    public GameArchitecture() : base(new ArchitectureConfiguration())
    {
    }

    protected override void OnInitialize()
    {
        // 显式注册 Arch ECS 模块
        this.UseArch();
    }
}

// 初始化架构
var architecture = new GameArchitecture();
architecture.Initialize();
```

### 2. 带配置的注册

```csharp
public class GameArchitecture : Architecture
{
    protected override void OnInitialize()
    {
        // 带配置的注册
        this.UseArch(options =>
        {
            options.WorldCapacity = 2000;      // World 初始容量
            options.EnableStatistics = true;   // 启用统计信息
            options.Priority = 50;             // 模块优先级
        });
    }
}
```

### 3. 定义组件

组件是纯数据结构，使用 `struct` 定义：

```csharp
using System.Runtime.InteropServices;

namespace MyGame.Components;

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

[StructLayout(LayoutKind.Sequential)]
public struct Health(float current, float max)
{
    public float Current { get; set; } = current;
    public float Max { get; set; } = max;
}
```

### 4. 创建系统

系统继承自 `ArchSystemAdapter<T>`：

```csharp
using Arch.Core;
using GFramework.Ecs.Arch;
using MyGame.Components;

namespace MyGame.Systems;

/// <summary>
/// 移动系统 - 更新实体位置
/// </summary>
public sealed class MovementSystem : ArchSystemAdapter<float>
{
    private QueryDescription _query;

    protected override void OnArchInitialize()
    {
        // 创建查询：查找所有同时拥有 Position 和 Velocity 组件的实体
        _query = new QueryDescription()
            .WithAll<Position, Velocity>();
    }

    protected override void OnUpdate(in float deltaTime)
    {
        // 查询并更新所有符合条件的实体
        World.Query(in _query, (ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        });
    }
}
```

### 5. 注册系统

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

### 6. 创建实体

```csharp
using Arch.Core;
using GFramework.Core.Abstractions.rule;
using GFramework.SourceGenerators.Abstractions.rule;
using MyGame.Components;

[ContextAware]
public partial class GameController
{
    public void Start()
    {
        // 获取 World
        var world = this.GetService<World>();

        // 创建实体
        var player = world.Create(
            new Position(0, 0),
            new Velocity(0, 0),
            new Health(100, 100)
        );

        var enemy = world.Create(
            new Position(10, 10),
            new Velocity(-1, 0),
            new Health(50, 50)
        );
    }
}
```

### 7. 更新系统

```csharp
using GFramework.Ecs.Arch.Abstractions;

public class GameLoop
{
    private IArchEcsModule _ecsModule;

    public void Initialize()
    {
        // 获取 ECS 模块
        _ecsModule = architecture.Context.GetService<IArchEcsModule>();
    }

    public void Update(float deltaTime)
    {
        // 更新所有 ECS 系统
        _ecsModule.Update(deltaTime);
    }
}
```

## 配置选项

### ArchOptions

```csharp
public sealed class ArchOptions
{
    /// <summary>
    /// World 初始容量（默认：1000）
    /// </summary>
    public int WorldCapacity { get; set; } = 1000;

    /// <summary>
    /// 是否启用统计信息（默认：false）
    /// </summary>
    public bool EnableStatistics { get; set; } = false;

    /// <summary>
    /// 模块优先级（默认：50）
    /// </summary>
    public int Priority { get; set; } = 50;
}
```

### 配置示例

```csharp
this.UseArch(options =>
{
    // 设置 World 初始容量
    // 根据预期实体数量设置，避免频繁扩容
    options.WorldCapacity = 2000;

    // 启用统计信息（开发/调试时使用）
    options.EnableStatistics = true;

    // 设置模块优先级
    // 数值越小，优先级越高
    options.Priority = 50;
});
```

## 核心概念

### Entity（实体）

实体是游戏世界中的基本对象，本质上是一个唯一标识符：

```csharp
// 创建空实体
var entity = world.Create();

// 创建带组件的实体
var entity = world.Create(
    new Position(0, 0),
    new Velocity(1, 1)
);

// 销毁实体
world.Destroy(entity);
```

### Component（组件）

组件是纯数据结构，用于存储实体的状态：

```csharp
// 添加组件
world.Add(entity, new Position(0, 0));

// 检查组件
if (world.Has<Position>(entity))
{
    // 获取组件引用（零 GC 分配）
    ref var pos = ref world.Get<Position>(entity);
    pos.X += 10;
}

// 设置组件（替换现有值）
world.Set(entity, new Position(100, 100));

// 移除组件
world.Remove<Velocity>(entity);
```

### System（系统）

系统包含游戏逻辑，处理具有特定组件组合的实体：

```csharp
public sealed class DamageSystem : ArchSystemAdapter<float>
{
    private QueryDescription _query;

    protected override void OnArchInitialize()
    {
        // 初始化查询
        _query = new QueryDescription()
            .WithAll<Health, Damage>();
    }

    protected override void OnUpdate(in float deltaTime)
    {
        // 处理伤害
        World.Query(in _query, (Entity entity, ref Health health, ref Damage damage) =>
        {
            health.Current -= damage.Value * deltaTime;

            if (health.Current <= 0)
            {
                health.Current = 0;
                World.Remove<Damage>(entity);
            }
        });
    }
}
```

### World（世界）

World 是 ECS 的核心容器，管理所有实体和组件：

```csharp
// World 由 ArchEcsModule 自动创建和注册
var world = this.GetService<World>();

// 获取实体数量
var entityCount = world.Size;

// 清空所有实体
world.Clear();
```

## 系统适配器

### ArchSystemAdapter<T>

`ArchSystemAdapter<T>` 桥接 Arch.System.ISystem<T> 到 GFramework 架构：

```csharp
public sealed class MySystem : ArchSystemAdapter<float>
{
    // Arch 系统初始化
    protected override void OnArchInitialize()
    {
        // 创建查询、初始化资源
    }

    // 更新前调用
    protected override void OnBeforeUpdate(in float deltaTime)
    {
        // 预处理逻辑
    }

    // 主更新逻辑
    protected override void OnUpdate(in float deltaTime)
    {
        // 处理实体
    }

    // 更新后调用
    protected override void OnAfterUpdate(in float deltaTime)
    {
        // 后处理逻辑
    }

    // 资源清理
    protected override void OnArchDispose()
    {
        // 清理资源
    }
}
```

### 访问 World

在系统中可以直接访问 `World` 属性：

```csharp
public sealed class MySystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        // 访问 World
        var entityCount = World.Size;

        // 创建实体
        var entity = World.Create(new Position(0, 0));

        // 查询实体
        var query = new QueryDescription().WithAll<Position>();
        World.Query(in query, (ref Position pos) =>
        {
            // 处理逻辑
        });
    }
}
```

### 访问框架服务

`ArchSystemAdapter<T>` 继承自 `AbstractSystem`，可以使用所有 GFramework 的扩展方法：

```csharp
public sealed class ServiceAccessSystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        // 获取 Model
        var playerModel = this.GetModel<PlayerModel>();

        // 获取 Utility
        var timeUtility = this.GetUtility<TimeUtility>();

        // 发送命令
        this.SendCommand(new SaveGameCommand());

        // 发送查询
        var score = this.SendQuery(new GetScoreQuery());

        // 发送事件
        this.SendEvent(new GameOverEvent());
    }
}
```

## 查询实体

### 基本查询

```csharp
// 查询：必须有 Position 和 Velocity
var query = new QueryDescription()
    .WithAll<Position, Velocity>();

World.Query(in query, (ref Position pos, ref Velocity vel) =>
{
    pos.X += vel.X * deltaTime;
    pos.Y += vel.Y * deltaTime;
});
```

### 过滤查询

```csharp
// 查询：必须有 Health，但不能有 Damage
var query = new QueryDescription()
    .WithAll<Health>()
    .WithNone<Damage>();

World.Query(in query, (ref Health health) =>
{
    // 只处理没有受伤的实体
});
```

### 可选组件查询

```csharp
// 查询：必须有 Position，可选 Velocity
var query = new QueryDescription()
    .WithAll<Position>()
    .WithAny<Velocity>();

World.Query(in query, (Entity entity, ref Position pos) =>
{
    // 处理逻辑
});
```

### 访问实体 ID

```csharp
var query = new QueryDescription().WithAll<Position>();

World.Query(in query, (Entity entity, ref Position pos) =>
{
    // 可以访问实体 ID
    Console.WriteLine($"Entity {entity.Id}: ({pos.X}, {pos.Y})");

    // 可以对实体进行操作
    if (pos.X > 100)
    {
        World.Destroy(entity);
    }
});
```

## 系统优先级

系统按照优先级顺序执行，数值越小优先级越高：

```csharp
using GFramework.Core.Abstractions.bases;
using GFramework.SourceGenerators.Abstractions.bases;

// 使用 Priority 特性设置优先级
[Priority(10)]  // 高优先级，先执行
public sealed class InputSystem : ArchSystemAdapter<float>
{
    // ...
}

[Priority(20)]  // 中优先级
public sealed class MovementSystem : ArchSystemAdapter<float>
{
    // ...
}

[Priority(30)]  // 低优先级，后执行
public sealed class RenderSystem : ArchSystemAdapter<float>
{
    // ...
}
```

执行顺序：InputSystem → MovementSystem → RenderSystem

## 性能优化

### 1. 使用 struct 组件

```csharp
// ✅ 推荐：使用 struct
[StructLayout(LayoutKind.Sequential)]
public struct Position(float x, float y)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
}

// ❌ 不推荐：使用 class
public class Position
{
    public float X { get; set; }
    public float Y { get; set; }
}
```

### 2. 缓存查询

```csharp
public class OptimizedSystem : ArchSystemAdapter<float>
{
    // ✅ 推荐：缓存查询
    private QueryDescription _cachedQuery;

    protected override void OnArchInitialize()
    {
        _cachedQuery = new QueryDescription()
            .WithAll<Position, Velocity>();
    }

    protected override void OnUpdate(in float deltaTime)
    {
        World.Query(in _cachedQuery, (ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        });
    }
}
```

### 3. 使用 ref 访问组件

```csharp
// ✅ 推荐：使用 ref 避免复制
World.Query(in query, (ref Position pos, ref Velocity vel) =>
{
    pos.X += vel.X; // 直接修改，零 GC
});

// ❌ 不推荐：不使用 ref
World.Query(in query, (Position pos, Velocity vel) =>
{
    pos.X += vel.X; // 复制值，修改不会生效
});
```

### 4. 组件大小优化

```csharp
// ✅ 推荐：小而专注的组件
public struct Position(float x, float y)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
}

public struct Velocity(float x, float y)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
}

// ❌ 不推荐：大而全的组件
public struct Transform
{
    public float X, Y, Z;
    public float RotationX, RotationY, RotationZ;
    public float ScaleX, ScaleY, ScaleZ;
    public float VelocityX, VelocityY, VelocityZ;
    // ... 太多数据
}
```

## 最佳实践

### 1. 组件设计原则

- 使用 `struct` 而不是 `class`
- 只包含数据，不包含逻辑
- 使用 `[StructLayout(LayoutKind.Sequential)]` 优化内存布局
- 保持组件小而专注

### 2. 系统设计原则

- 单一职责：每个系统只负责一件事
- 缓存查询：在 `OnArchInitialize` 中创建查询
- 使用 ref：访问组件时使用 ref 参数
- 批量处理：一次查询处理所有实体

### 3. 标签组件

使用空结构体作为标签来分类实体：

```csharp
// 定义标签组件
public struct PlayerTag { }
public struct EnemyTag { }
public struct DeadTag { }

// 使用标签过滤实体
var query = new QueryDescription()
    .WithAll<Position, Velocity, PlayerTag>()
    .WithNone<DeadTag>();
```

### 4. 与传统架构结合

```csharp
// ECS 系统可以访问 Model
public class EnemySpawnSystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
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
```

## 常见问题

### Q: 如何在运行时动态添加/移除组件？

A: Arch 支持运行时修改实体的组件：

```csharp
// 动态添加组件
if (pos.X > 100 && !World.Has<FastTag>(entity))
{
    World.Add(entity, new FastTag());
}

// 动态移除组件
if (pos.X < 0 && World.Has<FastTag>(entity))
{
    World.Remove<FastTag>(entity);
}
```

### Q: 如何处理实体之间的交互？

A: 使用嵌套查询或事件：

```csharp
// 方式 1：嵌套查询
World.Query(in playerQuery, (Entity player, ref Position playerPos) =>
{
    World.Query(in enemyQuery, (Entity enemy, ref Position enemyPos) =>
    {
        // 检测碰撞
    });
});

// 方式 2：使用事件
this.SendEvent(new CollisionEvent
{
    Entity1 = player,
    Entity2 = enemy
});
```

### Q: 如何调试 ECS 系统？

A: 使用日志和统计信息：

```csharp
protected override void OnUpdate(in float deltaTime)
{
    // 打印实体数量
    Console.WriteLine($"Total entities: {World.Size}");

    // 查询特定实体
    var query = new QueryDescription().WithAll<Position>();
    var count = 0;
    World.Query(in query, (Entity entity, ref Position pos) =>
    {
        count++;
        Console.WriteLine($"Entity {entity.Id}: ({pos.X}, {pos.Y})");
    });
}
```

## 相关资源

- [Arch.Core 官方文档](https://github.com/genaray/Arch)
- [ECS 概述](./index.md)
- [ECS 最佳实践](./best-practices.md)
- [性能优化指南](./performance.md)

---

**许可证**：MIT License
