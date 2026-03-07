---
title: ECS 系统集成
description: ECS（Entity Component System）系统集成指南，基于 Arch.Core 实现高性能的实体组件系统。
---

# ECS 系统集成

## 概述

GFramework 集成了 [Arch.Core](https://github.com/genaray/Arch) ECS 框架，提供高性能的实体组件系统（Entity Component
System）架构。通过 ECS 模式，你可以构建数据驱动、高度可扩展的游戏系统。

**主要特性**：

- 基于 Arch.Core 的高性能 ECS 实现
- 与 GFramework 架构无缝集成
- 支持组件查询和批量处理
- 零 GC 分配的组件访问
- 灵活的系统生命周期管理
- 支持多线程并行处理（Arch 原生支持）

**性能特点**：

- 10,000 个实体更新 < 100ms
- 1,000 个实体创建 < 50ms
- 基于 Archetype 的高效内存布局
- 支持 SIMD 优化

## 核心概念

### Entity（实体）

实体是游戏世界中的基本对象，本质上是一个唯一标识符（ID）。实体本身不包含数据或逻辑，只是组件的容器。

```csharp
using Arch.Core;

// 创建实体
var entity = world.Create();

// 创建带组件的实体
var entity = world.Create(new Position(0, 0), new Velocity(1, 1));
```

### Component（组件）

组件是纯数据结构，用于存储实体的状态。组件应该是简单的值类型（struct），不包含逻辑。

```csharp
using System.Runtime.InteropServices;

/// <summary>
/// 位置组件
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Position(float x, float y)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
}

/// <summary>
/// 速度组件
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Velocity(float x, float y)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
}
```

**组件设计原则**：

- 使用 `struct` 而不是 `class`
- 只包含数据，不包含逻辑
- 使用 `[StructLayout(LayoutKind.Sequential)]` 优化内存布局
- 保持组件小而专注

### System（系统）

系统包含游戏逻辑，负责处理具有特定组件组合的实体。在 GFramework 中，系统通过继承 `ArchSystemAdapter&lt;T&gt;` 来实现。

```csharp
using Arch.Core;
using GFramework.Core.ecs;

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

### World（世界）

World 是 ECS 的核心容器，管理所有实体和组件。GFramework 通过 `ArchEcsModule` 自动创建和管理 World。

```csharp
// World 由 ArchEcsModule 自动创建和注册到 IoC 容器
// 在系统中可以直接访问
public class MySystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        // 访问 World
        var entityCount = World.Size;
    }
}
```

### Arch.Core 集成

GFramework 通过以下组件桥接 Arch.Core 到框架生命周期：

- **ArchEcsModule**：ECS 模块，管理 World 和系统生命周期
- **ArchSystemAdapter&lt;T&gt;**：系统适配器，桥接 Arch 系统到 GFramework

## 基本用法

### 1. 定义组件

```csharp
using System.Runtime.InteropServices;

namespace MyGame.Components;

[StructLayout(LayoutKind.Sequential)]
public struct Health(float current, float max)
{
    public float Current { get; set; } = current;
    public float Max { get; set; } = max;
}

[StructLayout(LayoutKind.Sequential)]
public struct Damage(float value)
{
    public float Value { get; set; } = value;
}

[StructLayout(LayoutKind.Sequential)]
public struct PlayerTag
{
    // 标签组件，不需要数据
}
```

### 2. 创建系统

```csharp
using Arch.Core;
using GFramework.Core.ecs;
using MyGame.Components;

namespace MyGame.Systems;

/// <summary>
/// 伤害系统 - 处理伤害逻辑
/// </summary>
public sealed class DamageSystem : ArchSystemAdapter<float>
{
    private QueryDescription _query;

    protected override void OnArchInitialize()
    {
        // 查询所有具有 Health 和 Damage 组件的实体
        _query = new QueryDescription()
            .WithAll<Health, Damage>();
    }

    protected override void OnUpdate(in float deltaTime)
    {
        // 处理伤害
        World.Query(in _query, (Entity entity, ref Health health, ref Damage damage) =>
        {
            health.Current -= damage.Value * deltaTime;

            // 如果生命值耗尽，移除伤害组件
            if (health.Current <= 0)
            {
                health.Current = 0;
                World.Remove<Damage>(entity);
            }
        });
    }
}
```

### 3. 注册 ECS 模块

```csharp
using GFramework.Core.architecture;
using GFramework.Core.ecs;
using MyGame.Systems;

public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册 ECS 系统
        RegisterSystem(new MovementSystem());
        RegisterSystem(new DamageSystem());

        // 安装 ECS 模块
        InstallModule(new ArchEcsModule(enabled: true));
    }
}
```

### 4. 创建和管理实体

```csharp
using Arch.Core;
using GFramework.Core.Abstractions.controller;
using MyGame.Components;

public class GameController : IController
{
    private World _world;

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void Start()
    {
        // 获取 World
        _world = this.GetService<World>();

        // 创建玩家实体
        var player = _world.Create(
            new Position(0, 0),
            new Velocity(0, 0),
            new Health(100, 100),
            new PlayerTag()
        );

        // 创建敌人实体
        var enemy = _world.Create(
            new Position(10, 10),
            new Velocity(-1, 0),
            new Health(50, 50)
        );
    }

    public void ApplyDamage(Entity entity, float damageValue)
    {
        // 添加伤害组件
        if (_world.Has<Health>(entity))
        {
            _world.Add(entity, new Damage(damageValue));
        }
    }
}
```

### 5. 更新 ECS 系统

```csharp
// 在游戏主循环中更新 ECS
public class GameLoop
{
    private ArchEcsModule _ecsModule;

    public void Update(float deltaTime)
    {
        // 更新所有 ECS 系统
        _ecsModule.Update(deltaTime);
    }
}
```

## 高级用法

### 查询实体

Arch 提供了强大的查询 API，支持多种过滤条件：

```csharp
using Arch.Core;

public class QueryExampleSystem : ArchSystemAdapter<float>
{
    private QueryDescription _query1;
    private QueryDescription _query2;
    private QueryDescription _query3;

    protected override void OnArchInitialize()
    {
        // 查询：必须有 Position 和 Velocity
        _query1 = new QueryDescription()
            .WithAll<Position, Velocity>();

        // 查询：必须有 Health，但不能有 Damage
        _query2 = new QueryDescription()
            .WithAll<Health>()
            .WithNone<Damage>();

        // 查询：必须有 Position，可选 Velocity
        _query3 = new QueryDescription()
            .WithAll<Position>()
            .WithAny<Velocity>();
    }

    protected override void OnUpdate(in float deltaTime)
    {
        // 使用查询 1
        World.Query(in _query1, (ref Position pos, ref Velocity vel) =>
        {
            // 处理逻辑
        });

        // 使用查询 2
        World.Query(in _query2, (Entity entity, ref Health health) =>
        {
            // 处理逻辑
        });
    }
}
```

### 系统生命周期钩子

`ArchSystemAdapter&lt;T&gt;` 提供了多个生命周期钩子：

```csharp
public class LifecycleExampleSystem : ArchSystemAdapter<float>
{
    protected override void OnArchInitialize()
    {
        // Arch 系统初始化
        // 在这里创建查询、初始化资源
    }

    protected override void OnBeforeUpdate(in float deltaTime)
    {
        // 更新前调用
        // 可用于预处理逻辑
    }

    protected override void OnUpdate(in float deltaTime)
    {
        // 主更新逻辑
    }

    protected override void OnAfterUpdate(in float deltaTime)
    {
        // 更新后调用
        // 可用于后处理逻辑
    }

    protected override void OnArchDispose()
    {
        // 资源清理
    }
}
```

### 组件操作

```csharp
using Arch.Core;

public class ComponentOperations
{
    private World _world;

    public void Examples()
    {
        var entity = _world.Create();

        // 添加组件
        _world.Add(entity, new Position(0, 0));
        _world.Add(entity, new Velocity(1, 1));

        // 检查组件
        if (_world.Has<Position>(entity))
        {
            // 获取组件引用（零 GC 分配）
            ref var pos = ref _world.Get<Position>(entity);
            pos.X += 10;
        }

        // 设置组件（替换现有值）
        _world.Set(entity, new Position(100, 100));

        // 移除组件
        _world.Remove<Velocity>(entity);

        // 销毁实体
        _world.Destroy(entity);
    }
}
```

### 批量操作

```csharp
public class BatchOperations
{
    private World _world;

    public void CreateMultipleEntities()
    {
        // 批量创建实体
        for (int i = 0; i < 1000; i++)
        {
            _world.Create(
                new Position(i, i),
                new Velocity(1, 1)
            );
        }
    }

    public void ClearAllEntities()
    {
        // 清空所有实体
        _world.Clear();
    }
}
```

### 实体查询和迭代

```csharp
public class EntityIterationSystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        // 方式 1：使用查询和 Lambda
        var query = new QueryDescription().WithAll<Position>();
        World.Query(in query, (ref Position pos) =>
        {
            // 处理每个实体
        });

        // 方式 2：获取实体引用
        World.Query(in query, (Entity entity, ref Position pos) =>
        {
            // 可以访问实体 ID
            if (pos.X > 100)
            {
                World.Destroy(entity);
            }
        });

        // 方式 3：多组件查询
        var multiQuery = new QueryDescription()
            .WithAll<Position, Velocity, Health>();

        World.Query(in multiQuery, (
            Entity entity,
            ref Position pos,
            ref Velocity vel,
            ref Health health) =>
        {
            // 处理多个组件
        });
    }
}
```

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
        // 使用缓存的查询
        World.Query(in _cachedQuery, (ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        });
    }
}

// ❌ 不推荐：每次创建新查询
public class UnoptimizedSystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        // 每帧创建新查询（性能差）
        var query = new QueryDescription().WithAll<Position, Velocity>();
        World.Query(in query, (ref Position pos, ref Velocity vel) =>
        {
            // ...
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

### 5. 批量处理

```csharp
public class BatchProcessingSystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        // ✅ 推荐：批量处理
        var query = new QueryDescription().WithAll<Position, Velocity>();
        World.Query(in query, (ref Position pos, ref Velocity vel) =>
        {
            // 一次查询处理所有实体
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        });
    }
}

// ❌ 不推荐：逐个处理
public class IndividualProcessingSystem : ArchSystemAdapter<float>
{
    private List<Entity> _entities = new();

    protected override void OnUpdate(in float deltaTime)
    {
        foreach (var entity in _entities)
        {
            ref var pos = ref World.Get<Position>(entity);
            ref var vel = ref World.Get<Velocity>(entity);
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
    }
}
```

## 最佳实践

### 1. ECS 设计模式

**组件组合优于继承**：

```csharp
// ✅ 推荐：使用组件组合
var player = world.Create(
    new Position(0, 0),
    new Velocity(0, 0),
    new Health(100, 100),
    new PlayerTag(),
    new Controllable()
);

var enemy = world.Create(
    new Position(10, 10),
    new Velocity(-1, 0),
    new Health(50, 50),
    new EnemyTag(),
    new AI()
);

// ❌ 不推荐：使用继承
public class Player : Entity { }
public class Enemy : Entity { }
```

**单一职责系统**：

```csharp
// ✅ 推荐：每个系统只负责一件事
public class MovementSystem : ArchSystemAdapter<float>
{
    // 只负责移动
}

public class CollisionSystem : ArchSystemAdapter<float>
{
    // 只负责碰撞检测
}

public class DamageSystem : ArchSystemAdapter<float>
{
    // 只负责伤害处理
}

// ❌ 不推荐：一个系统做太多事
public class GameplaySystem : ArchSystemAdapter<float>
{
    // 移动、碰撞、伤害、AI... 太多职责
}
```

### 2. 与传统架构结合

ECS 可以与 GFramework 的传统架构（Model、System、Utility）结合使用：

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
    private float _spawnTimer;

    protected override void OnUpdate(in float deltaTime)
    {
        _spawnTimer += deltaTime;

        if (_spawnTimer >= 2.0f)
        {
            // 获取 Model
            var gameState = this.GetModel<GameStateModel>();

            // 根据关卡生成敌人
            var enemyCount = gameState.Level * 2;
            for (int i = 0; i < enemyCount; i++)
            {
                World.Create(
                    new Position(Random.Shared.Next(0, 100), 0),
                    new Velocity(0, -1),
                    new Health(50, 50),
                    new EnemyTag()
                );
            }

            _spawnTimer = 0;
        }
    }
}

// 传统 System 处理游戏逻辑
public class ScoreSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 监听敌人死亡事件
        this.RegisterEvent<EnemyDestroyedEvent>(OnEnemyDestroyed);
    }

    private void OnEnemyDestroyed(EnemyDestroyedEvent e)
    {
        var gameState = this.GetModel<GameStateModel>();
        gameState.Score += 100;
    }
}
```

### 3. 事件集成

ECS 系统可以发送和接收框架事件：

```csharp
// 定义事件
public struct EnemyDestroyedEvent
{
    public Entity Enemy { get; init; }
    public int Score { get; init; }
}

// ECS 系统发送事件
public class HealthSystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        var query = new QueryDescription()
            .WithAll<Health, EnemyTag>();

        World.Query(in query, (Entity entity, ref Health health) =>
        {
            if (health.Current <= 0)
            {
                // 发送事件
                this.SendEvent(new EnemyDestroyedEvent
                {
                    Enemy = entity,
                    Score = 100
                });

                // 销毁实体
                World.Destroy(entity);
            }
        });
    }
}

// 传统系统接收事件
public class UISystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<EnemyDestroyedEvent>(OnEnemyDestroyed);
    }

    private void OnEnemyDestroyed(EnemyDestroyedEvent e)
    {
        // 更新 UI
        Console.WriteLine($"Enemy destroyed! +{e.Score} points");
    }
}
```

### 4. 标签组件

使用空结构体作为标签来分类实体：

```csharp
// 定义标签组件
public struct PlayerTag { }
public struct EnemyTag { }
public struct BulletTag { }
public struct DeadTag { }

// 使用标签过滤实体
public class PlayerMovementSystem : ArchSystemAdapter<float>
{
    private QueryDescription _query;

    protected override void OnArchInitialize()
    {
        // 只处理玩家实体
        _query = new QueryDescription()
            .WithAll<Position, Velocity, PlayerTag>()
            .WithNone<DeadTag>();
    }

    protected override void OnUpdate(in float deltaTime)
    {
        World.Query(in _query, (ref Position pos, ref Velocity vel) =>
        {
            // 只更新活着的玩家
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        });
    }
}
```

### 5. 组件生命周期管理

```csharp
public class LifecycleManagementSystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        // 处理临时效果
        var buffQuery = new QueryDescription().WithAll<BuffComponent>();
        World.Query(in buffQuery, (Entity entity, ref BuffComponent buff) =>
        {
            buff.Duration -= deltaTime;

            if (buff.Duration <= 0)
            {
                // 移除过期的 Buff
                World.Remove<BuffComponent>(entity);
            }
        });

        // 清理死亡实体
        var deadQuery = new QueryDescription().WithAll<DeadTag>();
        World.Query(in deadQuery, (Entity entity) =>
        {
            World.Destroy(entity);
        });
    }
}
```

## 常见问题

### Q: 如何在 ECS 系统中访问其他服务？

A: `ArchSystemAdapter&lt;T&gt;` 继承自 `AbstractSystem`，可以使用所有 GFramework 的扩展方法：

```csharp
public class ServiceAccessSystem : ArchSystemAdapter<float>
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

### Q: ECS 和传统架构如何选择？

A: 根据场景选择：

- **使用 ECS**：大量相似实体、需要高性能批量处理（敌人、子弹、粒子）
- **使用传统架构**：全局状态、单例服务、UI 逻辑、游戏流程控制

### Q: 如何调试 ECS 系统？

A: 使用以下方法：

```csharp
public class DebugSystem : ArchSystemAdapter<float>
{
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

        Console.WriteLine($"Entities with Position: {count}");
    }
}
```

### Q: 如何处理实体之间的交互？

A: 使用查询和事件：

```csharp
public class CollisionSystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        var playerQuery = new QueryDescription()
            .WithAll<Position, PlayerTag>();
        var enemyQuery = new QueryDescription()
            .WithAll<Position, EnemyTag>();

        // 检测玩家和敌人的碰撞
        World.Query(in playerQuery, (Entity player, ref Position playerPos) =>
        {
            World.Query(in enemyQuery, (Entity enemy, ref Position enemyPos) =>
            {
                var distance = Math.Sqrt(
                    Math.Pow(playerPos.X - enemyPos.X, 2) +
                    Math.Pow(playerPos.Y - enemyPos.Y, 2)
                );

                if (distance < 1.0f)
                {
                    // 发送碰撞事件
                    this.SendEvent(new CollisionEvent
                    {
                        Entity1 = player,
                        Entity2 = enemy
                    });
                }
            });
        });
    }
}
```

### Q: 如何优化大量实体的性能？

A: 参考性能优化章节，主要策略：

1. 使用 struct 组件
2. 缓存查询
3. 使用 ref 访问组件
4. 批量处理
5. 合理设计组件大小
6. 使用 Arch 的并行查询（高级特性）

### Q: 可以在运行时动态添加/移除组件吗？

A: 可以，Arch 支持运行时修改实体的组件：

```csharp
public class DynamicComponentSystem : ArchSystemAdapter<float>
{
    protected override void OnUpdate(in float deltaTime)
    {
        var query = new QueryDescription().WithAll<Position>();

        World.Query(in query, (Entity entity, ref Position pos) =>
        {
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
        });
    }
}
```

## 相关资源

- [Arch.Core 官方文档](https://github.com/genaray/Arch)
- [Architecture 包使用说明](./architecture.md)
- [System 包使用说明](./system.md)
- [事件系统](./events.md)

---

**许可证**：Apache 2.0
