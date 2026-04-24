---
title: Godot 节点池系统
description: Godot 节点池系统提供了高性能的节点复用机制，减少频繁创建和销毁节点带来的性能开销。
---

# Godot 节点池系统

## 概述

Godot 节点池系统是 GFramework.Godot 中用于管理和复用 Godot
节点的高性能组件。通过对象池模式，它可以显著减少频繁创建和销毁节点带来的性能开销，特别适用于需要大量动态生成节点的场景，如子弹、特效、敌人等。

节点池系统基于 GFramework 核心的对象池系统，专门针对 Godot 节点进行了优化，提供了完整的生命周期管理和统计功能。

**主要特性**：

- 节点复用机制，减少 GC 压力
- 自动生命周期管理
- 池容量限制和预热功能
- 详细的统计信息
- 类型安全的泛型设计
- 与 Godot PackedScene 无缝集成

**性能优势**：

- 减少内存分配和垃圾回收
- 降低节点实例化开销
- 提高游戏运行时性能
- 优化大量对象场景的帧率

## 核心概念

### 节点池

节点池是一个存储可复用节点的容器。当需要节点时从池中获取，使用完毕后归还到池中，而不是销毁。这种复用机制可以显著提升性能。

### 可池化节点

实现 `IPoolableNode` 接口的节点可以被对象池管理。接口定义了节点在池中的生命周期回调：

```csharp
public interface IPoolableNode : IPoolableObject
{
    // 从池中获取时调用
    void OnAcquire();

    // 归还到池中时调用
    void OnRelease();

    // 池被销毁时调用
    void OnPoolDestroy();

    // 转换为 Node 类型
    Node AsNode();
}
```

### 节点复用

节点复用是指重复使用已创建的节点实例，而不是每次都创建新实例。这可以：

- 减少内存分配
- 降低 GC 压力
- 提高实例化速度
- 优化运行时性能

## 基本用法

### 创建可池化节点

```csharp
using Godot;
using GFramework.Godot.Pool;

public partial class Bullet : Node2D, IPoolableNode
{
    private Vector2 _velocity;
    private float _lifetime;

    public void OnAcquire()
    {
        // 从池中获取时重置状态
        _lifetime = 5.0f;
        Show();
        SetProcess(true);
    }

    public void OnRelease()
    {
        // 归还到池中时清理状态
        Hide();
        SetProcess(false);
        _velocity = Vector2.Zero;
    }

    public void OnPoolDestroy()
    {
        // 池被销毁时的清理工作
        QueueFree();
    }

    public Node AsNode()
    {
        return this;
    }

    public void Initialize(Vector2 position, Vector2 velocity)
    {
        Position = position;
        _velocity = velocity;
    }

    public override void _Process(double delta)
    {
        Position += _velocity * (float)delta;
        _lifetime -= (float)delta;

        if (_lifetime <= 0)
        {
            // 归还到池中
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        // 通过池系统归还
        var poolSystem = this.GetSystem<BulletPoolSystem>();
        poolSystem.Release("Bullet", this);
    }
}
```

### 创建节点池系统

```csharp
using Godot;
using GFramework.Godot.Pool;

public class BulletPoolSystem : AbstractNodePoolSystem<string, Bullet>
{
    protected override PackedScene LoadScene(string key)
    {
        // 根据键加载对应的场景
        return key switch
        {
            "Bullet" => GD.Load<PackedScene>("res://prefabs/Bullet.tscn"),
            "EnemyBullet" => GD.Load<PackedScene>("res://prefabs/EnemyBullet.tscn"),
            _ => throw new ArgumentException($"Unknown bullet type: {key}")
        };
    }

    protected override void OnInit()
    {
        // 预热池，提前创建一些对象
        Prewarm("Bullet", 50);
        Prewarm("EnemyBullet", 30);

        // 设置最大容量
        SetMaxCapacity("Bullet", 100);
        SetMaxCapacity("EnemyBullet", 50);
    }
}
```

### 注册节点池系统

```csharp
using GFramework.Godot.Architecture;

public class GameArchitecture : AbstractArchitecture
{
    protected override void InstallModules()
    {
        // 注册节点池系统
        RegisterSystem<BulletPoolSystem>(new BulletPoolSystem());
        RegisterSystem<EffectPoolSystem>(new EffectPoolSystem());
    }
}
```

### 使用节点池

```csharp
using Godot;
using GFramework.Godot.Extensions;

public partial class Player : Node2D
{
    private BulletPoolSystem _bulletPool;

    public override void _Ready()
    {
        _bulletPool = this.GetSystem<BulletPoolSystem>();
    }

    public void Shoot()
    {
        // 从池中获取子弹
        var bullet = _bulletPool.Acquire("Bullet");

        // 初始化子弹
        bullet.Initialize(GlobalPosition, Vector2.Right.Rotated(Rotation) * 500);

        // 添加到场景树
        GetParent().AddChild(bullet.AsNode());
    }
}
```

## 高级用法

### 多类型节点池

```csharp
public class EffectPoolSystem : AbstractNodePoolSystem<string, PoolableEffect>
{
    protected override PackedScene LoadScene(string key)
    {
        return key switch
        {
            "Explosion" => GD.Load<PackedScene>("res://effects/Explosion.tscn"),
            "Hit" => GD.Load<PackedScene>("res://effects/Hit.tscn"),
            "Smoke" => GD.Load<PackedScene>("res://effects/Smoke.tscn"),
            "Spark" => GD.Load<PackedScene>("res://effects/Spark.tscn"),
            _ => throw new ArgumentException($"Unknown effect type: {key}")
        };
    }

    protected override void OnInit()
    {
        // 为不同类型的特效设置不同的池配置
        Prewarm("Explosion", 10);
        SetMaxCapacity("Explosion", 20);

        Prewarm("Hit", 20);
        SetMaxCapacity("Hit", 50);

        Prewarm("Smoke", 15);
        SetMaxCapacity("Smoke", 30);
    }
}

// 使用特效池
public partial class Enemy : Node2D
{
    public void Die()
    {
        var effectPool = this.GetSystem<EffectPoolSystem>();
        var explosion = effectPool.Acquire("Explosion");

        explosion.AsNode().GlobalPosition = GlobalPosition;
        GetParent().AddChild(explosion.AsNode());

        QueueFree();
    }
}
```

### 自动归还的节点

```csharp
public partial class PoolableEffect : Node2D, IPoolableNode
{
    private AnimationPlayer _animationPlayer;
    private EffectPoolSystem _poolSystem;
    private string _effectKey;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _poolSystem = this.GetSystem<EffectPoolSystem>();

        // 动画播放完毕后自动归还
        _animationPlayer.AnimationFinished += OnAnimationFinished;
    }

    public void OnAcquire()
    {
        Show();
        _animationPlayer.Play("default");
    }

    public void OnRelease()
    {
        Hide();
        _animationPlayer.Stop();
    }

    public void OnPoolDestroy()
    {
        _animationPlayer.AnimationFinished -= OnAnimationFinished;
        QueueFree();
    }

    public Node AsNode() => this;

    public void SetEffectKey(string key)
    {
        _effectKey = key;
    }

    private void OnAnimationFinished(StringName animName)
    {
        // 动画播放完毕，自动归还到池中
        _poolSystem.Release(_effectKey, this);
    }
}
```

### 池容量管理

```csharp
public class DynamicPoolSystem : AbstractNodePoolSystem<string, PoolableEnemy>
{
    protected override PackedScene LoadScene(string key)
    {
        return GD.Load<PackedScene>($"res://enemies/{key}.tscn");
    }

    protected override void OnInit()
    {
        // 初始配置
        SetMaxCapacity("Slime", 50);
        SetMaxCapacity("Goblin", 30);
        SetMaxCapacity("Boss", 5);
    }

    // 动态调整池容量
    public void AdjustPoolCapacity(string key, int newCapacity)
    {
        var currentSize = GetPoolSize(key);
        var activeCount = GetActiveCount(key);

        GD.Print($"池 '{key}' 当前状态:");
        GD.Print($"  可用: {currentSize}");
        GD.Print($"  活跃: {activeCount}");
        GD.Print($"  新容量: {newCapacity}");

        SetMaxCapacity(key, newCapacity);
    }

    // 根据游戏阶段预热
    public void PrewarmForStage(int stage)
    {
        switch (stage)
        {
            case 1:
                Prewarm("Slime", 20);
                break;
            case 2:
                Prewarm("Slime", 30);
                Prewarm("Goblin", 15);
                break;
            case 3:
                Prewarm("Goblin", 25);
                Prewarm("Boss", 2);
                break;
        }
    }
}
```

### 池统计和监控

```csharp
public partial class PoolMonitor : Control
{
    private BulletPoolSystem _bulletPool;
    private Label _statsLabel;

    public override void _Ready()
    {
        _bulletPool = this.GetSystem<BulletPoolSystem>();
        _statsLabel = GetNode<Label>("StatsLabel");
    }

    public override void _Process(double delta)
    {
        // 获取统计信息
        var stats = _bulletPool.GetStatistics("Bullet");

        // 显示统计信息
        _statsLabel.Text = $@"
子弹池统计:
  可用对象: {stats.AvailableCount}
  活跃对象: {stats.ActiveCount}
  最大容量: {stats.MaxCapacity}
  总创建数: {stats.TotalCreated}
  总获取数: {stats.TotalAcquired}
  总释放数: {stats.TotalReleased}
  总销毁数: {stats.TotalDestroyed}
  复用率: {CalculateReuseRate(stats):P2}
";
    }

    private float CalculateReuseRate(PoolStatistics stats)
    {
        if (stats.TotalAcquired == 0) return 0;
        return 1.0f - (float)stats.TotalCreated / stats.TotalAcquired;
    }
}
```

### 条件释放和清理

```csharp
public class SmartPoolSystem : AbstractNodePoolSystem<string, PoolableNode>
{
    protected override PackedScene LoadScene(string key)
    {
        return GD.Load<PackedScene>($"res://poolable/{key}.tscn");
    }

    // 清理超出屏幕的对象
    public void CleanupOffscreenObjects(Rect2 screenRect)
    {
        foreach (var pool in Pools)
        {
            var stats = GetStatistics(pool.Key);
            GD.Print($"清理前 '{pool.Key}': 活跃={stats.ActiveCount}");
        }
    }

    // 根据内存压力调整池大小
    public void AdjustForMemoryPressure(float memoryUsage)
    {
        if (memoryUsage > 0.8f)
        {
            // 内存压力大，减小池容量
            foreach (var pool in Pools)
            {
                var currentCapacity = GetStatistics(pool.Key).MaxCapacity;
                SetMaxCapacity(pool.Key, Math.Max(10, currentCapacity / 2));
            }

            GD.Print("内存压力大，减小池容量");
        }
        else if (memoryUsage < 0.5f)
        {
            // 内存充足，增加池容量
            foreach (var pool in Pools)
            {
                var currentCapacity = GetStatistics(pool.Key).MaxCapacity;
                SetMaxCapacity(pool.Key, currentCapacity * 2);
            }

            GD.Print("内存充足，增加池容量");
        }
    }
}
```

## 最佳实践

1. **在 OnAcquire 中重置状态**：确保对象从池中获取时处于干净状态
   ```csharp
   public void OnAcquire()
   {
       // 重置所有状态
       Position = Vector2.Zero;
       Rotation = 0;
       Scale = Vector2.One;
       Modulate = Colors.White;
       Show();
   }
   ```

2. **在 OnRelease 中清理资源**：避免内存泄漏
   ```csharp
   public void OnRelease()
   {
       // 清理引用
       _target = null;
       _callbacks.Clear();

       // 停止所有动画和计时器
       _animationPlayer.Stop();
       _timer.Stop();

       Hide();
   }
   ```

3. **合理设置池容量**：根据实际需求设置最大容量
   ```csharp
   // 根据游戏设计设置合理的容量
   SetMaxCapacity("Bullet", 100);  // 屏幕上最多100个子弹
   SetMaxCapacity("Enemy", 50);    // 同时最多50个敌人
   ```

4. **使用预热优化启动性能**：在游戏开始前预创建对象
   ```csharp
   protected override void OnInit()
   {
       // 在加载界面预热池
       Prewarm("Bullet", 50);
       Prewarm("Effect", 30);
   }
   ```

5. **及时归还对象**：使用完毕后立即归还到池中
   ```csharp
   ✓ poolSystem.Release("Bullet", bullet);  // 使用完立即归还
   ✗ // 忘记归还，导致池耗尽
   ```

6. **监控池统计信息**：定期检查池的使用情况
   ```csharp
   var stats = poolSystem.GetStatistics("Bullet");
   if (stats.ActiveCount > stats.MaxCapacity * 0.9f)
   {
       GD.PrintErr("警告：子弹池接近容量上限");
   }
   ```

7. **避免在池中存储过大的对象**：大对象应该按需创建
   ```csharp
   ✓ 小对象：子弹、特效、UI元素
   ✗ 大对象：完整的关卡、大型模型
   ```

## 常见问题

### 问题：什么时候应该使用节点池？

**解答**：
以下场景适合使用节点池：

- 频繁创建和销毁的对象（子弹、特效）
- 数量较多的对象（敌人、道具）
- 生命周期短的对象（粒子、UI提示）
- 性能敏感的场景（移动平台、大量对象）

不适合使用节点池的场景：

- 只创建一次的对象（玩家、UI界面）
- 数量很少的对象（Boss、关键NPC）
- 状态复杂难以重置的对象

### 问题：如何确定合适的池容量？

**解答**：
根据游戏实际情况设置：

```csharp
// 1. 测量峰值使用量
var stats = poolSystem.GetStatistics("Bullet");
GD.Print($"峰值活跃数: {stats.ActiveCount}");

// 2. 设置容量为峰值的 1.2-1.5 倍
SetMaxCapacity("Bullet", (int)(peakCount * 1.3f));

// 3. 监控并调整
if (stats.TotalDestroyed > stats.TotalCreated * 0.1f)
{
    // 销毁过多，容量可能太小
    SetMaxCapacity("Bullet", stats.MaxCapacity * 2);
}
```

### 问题：对象没有正确归还到池中怎么办？

**解答**：
检查以下几点：

```csharp
// 1. 确保调用了 Release
poolSystem.Release("Bullet", bullet);

// 2. 检查是否使用了正确的键
✓ poolSystem.Release("Bullet", bullet);
✗ poolSystem.Release("Enemy", bullet);  // 错误的键

// 3. 避免重复释放
if (!_isReleased)
{
    poolSystem.Release("Bullet", this);
    _isReleased = true;
}

// 4. 使用统计信息诊断
var stats = poolSystem.GetStatistics("Bullet");
if (stats.ActiveCount != stats.TotalAcquired - stats.TotalReleased)
{
    GD.PrintErr("检测到对象泄漏");
}
```

### 问题：池中的对象状态没有正确重置？

**解答**：
在 OnAcquire 中完整重置所有状态：

```csharp
public void OnAcquire()
{
    // 重置变换
    Position = Vector2.Zero;
    Rotation = 0;
    Scale = Vector2.One;

    // 重置视觉
    Modulate = Colors.White;
    Visible = true;

    // 重置物理
    if (this is RigidBody2D rb)
    {
        rb.LinearVelocity = Vector2.Zero;
        rb.AngularVelocity = 0;
    }

    // 重置逻辑状态
    _health = _maxHealth;
    _isActive = true;

    // 重启动画
    _animationPlayer.Play("idle");
}
```

### 问题：如何处理节点的父子关系？

**解答**：
在归还前移除父节点：

```csharp
public void ReturnToPool()
{
    // 从场景树中移除
    if (GetParent() != null)
    {
        GetParent().RemoveChild(this);
    }

    // 归还到池中
    var poolSystem = this.GetSystem<BulletPoolSystem>();
    poolSystem.Release("Bullet", this);
}

// 获取时重新添加到场景树
var bullet = poolSystem.Acquire("Bullet");
GetParent().AddChild(bullet.AsNode());
```

### 问题：池系统对性能的提升有多大？

**解答**：
性能提升取决于具体场景：

```csharp
// 测试代码
var stopwatch = new Stopwatch();

// 不使用池
stopwatch.Start();
for (int i = 0; i < 1000; i++)
{
    var bullet = scene.Instantiate<Bullet>();
    bullet.QueueFree();
}
stopwatch.Stop();
GD.Print($"不使用池: {stopwatch.ElapsedMilliseconds}ms");

// 使用池
stopwatch.Restart();
for (int i = 0; i < 1000; i++)
{
    var bullet = poolSystem.Acquire("Bullet");
    poolSystem.Release("Bullet", bullet);
}
stopwatch.Stop();
GD.Print($"使用池: {stopwatch.ElapsedMilliseconds}ms");

// 典型结果：使用池可以提升 3-10 倍性能
```

## 相关文档

- [对象池系统](/zh-CN/core/pool.md) - 核心对象池实现
- [Godot 架构集成](/zh-CN/godot/architecture.md) - Godot 架构基础
- [Godot 场景系统](/zh-CN/godot/scene.md) - Godot 场景管理
- [性能优化](/zh-CN/core/pool.md) - 性能优化最佳实践
