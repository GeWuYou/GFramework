# 性能优化指南

> 全面的性能优化策略和最佳实践，帮助你构建高性能的游戏应用。

## 📋 目录

- [概述](#概述)
- [核心概念](#核心概念)
- [对象池优化](#对象池优化)
- [事件系统优化](#事件系统优化)
- [协程优化](#协程优化)
- [资源管理优化](#资源管理优化)
- [ECS 性能优化](#ecs-性能优化)
- [内存优化](#内存优化)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

## 概述

性能优化是游戏开发中的关键环节。良好的性能不仅能提供流畅的用户体验，还能降低设备功耗，延长电池寿命。本指南将介绍 GFramework
中的性能优化策略和最佳实践。

### 性能优化的重要性

- **用户体验** - 流畅的帧率和快速的响应时间
- **设备兼容性** - 在低端设备上也能良好运行
- **资源效率** - 降低内存占用和 CPU 使用率
- **电池寿命** - 减少不必要的计算和内存分配

## 核心概念

### 1. 性能瓶颈

性能瓶颈是指限制系统整体性能的关键因素：

- **CPU 瓶颈** - 过多的计算、复杂的逻辑
- **内存瓶颈** - 频繁的 GC、内存泄漏
- **GPU 瓶颈** - 过多的绘制调用、复杂的着色器
- **I/O 瓶颈** - 频繁的文件读写、网络请求

### 2. 性能指标

关键的性能指标：

- **帧率 (FPS)** - 每秒渲染的帧数，目标 60 FPS
- **帧时间** - 每帧的处理时间，目标 &lt;16.67ms
- **内存占用** - 应用程序使用的内存量
- **GC 频率** - 垃圾回收的频率和耗时
- **加载时间** - 场景和资源的加载时间

### 3. 优化策略

性能优化的基本策略：

- **测量优先** - 先测量，再优化
- **找到瓶颈** - 使用性能分析工具定位问题
- **渐进优化** - 逐步优化，避免过早优化
- **权衡取舍** - 在性能和可维护性之间找到平衡

## 对象池优化

对象池是减少 GC 压力的有效手段，通过复用对象避免频繁的内存分配和释放。

### 1. 使用对象池系统

```csharp
// ✅ 好的做法：使用对象池
public class BulletPoolSystem : AbstractObjectPoolSystem&lt;string, Bullet&gt;
{
    protected override Bullet Create(string key)
    {
        // 创建新的子弹对象
        var bullet = new Bullet();
        bullet.Initialize(key);
        return bullet;
    }
}

public class Bullet : IPoolableObject
{
    public string Type { get; private set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool IsActive { get; private set; }

    public void Initialize(string type)
    {
        Type = type;
    }

    public void OnAcquire()
    {
        // 从池中获取时重置状态
        IsActive = true;
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
    }

    public void OnRelease()
    {
        // 归还到池中时清理状态
        IsActive = false;
    }

    public void OnPoolDestroy()
    {
        // 对象被销毁时的清理
    }
}

// 使用对象池
public class CombatSystem : AbstractSystem
{
    private BulletPoolSystem _bulletPool;

    protected override void OnInit()
    {
        _bulletPool = GetSystem&lt;BulletPoolSystem&gt;();

        // 预热对象池
        _bulletPool.Prewarm("normal_bullet", 50);
        _bulletPool.Prewarm("fire_bullet", 20);
    }

    private void FireBullet(string bulletType, Vector2 position, Vector2 direction)
    {
        // 从池中获取子弹
        var bullet = _bulletPool.Acquire(bulletType);
        bullet.Position = position;
        bullet.Velocity = direction * 10f;

        // 使用完毕后归还
        // bullet 会在生命周期结束时自动归还
    }
}

// ❌ 避免：频繁创建和销毁对象
public class CombatSystem : AbstractSystem
{
    private void FireBullet(string bulletType, Vector2 position, Vector2 direction)
    {
        // 每次都创建新对象，产生大量 GC
        var bullet = new Bullet();
        bullet.Type = bulletType;
        bullet.Position = position;
        bullet.Velocity = direction * 10f;

        // 使用完毕后直接丢弃，等待 GC 回收
    }
}
```

### 2. StringBuilder 池

GFramework 提供了 `StringBuilderPool` 用于高效的字符串构建：

```csharp
// ✅ 好的做法：使用 StringBuilderPool
public string FormatPlayerInfo(Player player)
{
    using var sb = StringBuilderPool.GetScoped();
    sb.Value.Append("Player: ");
    sb.Value.Append(player.Name);
    sb.Value.Append(", Level: ");
    sb.Value.Append(player.Level);
    sb.Value.Append(", HP: ");
    sb.Value.Append(player.Health);
    sb.Value.Append("/");
    sb.Value.Append(player.MaxHealth);
    return sb.Value.ToString();
}

// 或者手动管理
public string FormatPlayerInfo(Player player)
{
    var sb = StringBuilderPool.Rent();
    try
    {
        sb.Append("Player: ").Append(player.Name);
        sb.Append(", Level: ").Append(player.Level);
        sb.Append(", HP: ").Append(player.Health).Append("/").Append(player.MaxHealth);
        return sb.ToString();
    }
    finally
    {
        StringBuilderPool.Return(sb);
    }
}

// ❌ 避免：频繁的字符串拼接
public string FormatPlayerInfo(Player player)
{
    // 每次拼接都会创建新的字符串对象
    return "Player: " + player.Name +
           ", Level: " + player.Level +
           ", HP: " + player.Health + "/" + player.MaxHealth;
}
```

### 3. ArrayPool 优化

使用 `ArrayPool` 避免频繁的数组分配：

```csharp
// ✅ 好的做法：使用 ArrayPool
public void ProcessEntities(List&lt;Entity&gt; entities)
{
    using var scopedArray = ArrayPool&lt;Entity&gt;.Shared.GetScoped(entities.Count);
    var array = scopedArray.Array;

    // 复制到数组进行处理
    entities.CopyTo(array, 0);

    // 处理数组
    for (int i = 0; i &lt; entities.Count; i++)
    {
        ProcessEntity(array[i]);
    }

    // 自动归还到池中
}

// ❌ 避免：频繁创建数组
public void ProcessEntities(List&lt;Entity&gt; entities)
{
    // 每次都创建新数组
    var array = entities.ToArray();

    foreach (var entity in array)
    {
        ProcessEntity(entity);
    }

    // 数组等待 GC 回收
}
```

### 4. 对象池统计

监控对象池的使用情况：

```csharp
public class PoolMonitorSystem : AbstractSystem
{
    private BulletPoolSystem _bulletPool;

    protected override void OnInit()
    {
        _bulletPool = GetSystem&lt;BulletPoolSystem&gt;();
    }

    public void LogPoolStatistics(string poolKey)
    {
        var stats = _bulletPool.GetStatistics(poolKey);

        Logger.Info($"Pool Statistics for '{poolKey}':");
        Logger.Info($"  Available: {stats.AvailableCount}");
        Logger.Info($"  Active: {stats.ActiveCount}");
        Logger.Info($"  Total Created: {stats.TotalCreated}");
        Logger.Info($"  Total Acquired: {stats.TotalAcquired}");
        Logger.Info($"  Total Released: {stats.TotalReleased}");
        Logger.Info($"  Total Destroyed: {stats.TotalDestroyed}");

        // 检查是否需要调整池大小
        if (stats.TotalDestroyed &gt; stats.TotalCreated * 0.5)
        {
            Logger.Warning($"Pool '{poolKey}' has high destruction rate, consider increasing max capacity");
        }
    }
}
```

## 事件系统优化

事件系统是游戏架构的核心，优化事件处理可以显著提升性能。

### 1. 避免事件订阅泄漏

```csharp
using GFramework.Core.Abstractions.controller;
using GFramework.SourceGenerators.Abstractions.rule;

// ✅ 好的做法：正确管理事件订阅
[ContextAware]
public partial class PlayerController : Node, IController
{
    private IUnRegisterList _unRegisterList = new UnRegisterList();
    private PlayerModel _playerModel;

    public void Initialize()
    {
        _playerModel = this.GetModel&lt;PlayerModel&gt;();

        // 使用 UnRegisterList 管理订阅
        this.RegisterEvent&lt;PlayerDamagedEvent&gt;(OnPlayerDamaged)
            .AddTo(_unRegisterList);

        _playerModel.Health.Register(OnHealthChanged)
            .AddTo(_unRegisterList);
    }

    public void Cleanup()
    {
        // 统一取消所有订阅
        _unRegisterList.UnRegisterAll();
    }

    private void OnPlayerDamaged(PlayerDamagedEvent e) { }
    private void OnHealthChanged(int health) { }
}

// ❌ 避免：忘记取消订阅
[ContextAware]
public partial class PlayerController : Node, IController
{
    public void Initialize()
    {
        // 订阅事件但从不取消订阅
        this.RegisterEvent&lt;PlayerDamagedEvent&gt;(OnPlayerDamaged);

        var playerModel = this.GetModel&lt;PlayerModel&gt;();
        playerModel.Health.Register(OnHealthChanged);

        // 当对象被销毁时，这些订阅仍然存在，导致内存泄漏
    }
}
```

### 2. 使用结构体事件

使用值类型事件避免堆分配：

```csharp
// ✅ 好的做法：使用结构体事件
public struct PlayerMovedEvent
{
    public string PlayerId { get; init; }
    public Vector2 OldPosition { get; init; }
    public Vector2 NewPosition { get; init; }
    public float DeltaTime { get; init; }
}

public class MovementSystem : AbstractSystem
{
    private void NotifyPlayerMoved(string playerId, Vector2 oldPos, Vector2 newPos, float deltaTime)
    {
        // 结构体在栈上分配，无 GC 压力
        SendEvent(new PlayerMovedEvent
        {
            PlayerId = playerId,
            OldPosition = oldPos,
            NewPosition = newPos,
            DeltaTime = deltaTime
        });
    }
}

// ❌ 避免：使用类事件
public class PlayerMovedEvent
{
    public string PlayerId { get; set; }
    public Vector2 OldPosition { get; set; }
    public Vector2 NewPosition { get; set; }
    public float DeltaTime { get; set; }
}

// 每次发送事件都会在堆上分配对象
```

### 3. 批量事件处理

对于高频事件，考虑批量处理：

```csharp
// ✅ 好的做法：批量处理事件
public class DamageSystem : AbstractSystem
{
    private readonly List&lt;DamageInfo&gt; _pendingDamages = new();
    private float _batchInterval = 0.1f;
    private float _timeSinceLastBatch = 0f;

    public void Update(float deltaTime)
    {
        _timeSinceLastBatch += deltaTime;

        if (_timeSinceLastBatch &gt;= _batchInterval)
        {
            ProcessDamageBatch();
            _timeSinceLastBatch = 0f;
        }
    }

    public void QueueDamage(string entityId, int damage, DamageType type)
    {
        _pendingDamages.Add(new DamageInfo
        {
            EntityId = entityId,
            Damage = damage,
            Type = type
        });
    }

    private void ProcessDamageBatch()
    {
        if (_pendingDamages.Count == 0)
            return;

        // 批量处理所有伤害
        foreach (var damageInfo in _pendingDamages)
        {
            ApplyDamage(damageInfo);
        }

        // 发送单个批量事件
        SendEvent(new DamageBatchProcessedEvent
        {
            DamageCount = _pendingDamages.Count,
            TotalDamage = _pendingDamages.Sum(d =&gt; d.Damage)
        });

        _pendingDamages.Clear();
    }
}

// ❌ 避免：每次都立即处理
public class DamageSystem : AbstractSystem
{
    public void ApplyDamage(string entityId, int damage, DamageType type)
    {
        // 每次伤害都立即处理并发送事件
        ProcessDamage(entityId, damage, type);
        SendEvent(new DamageAppliedEvent { EntityId = entityId, Damage = damage });
    }
}
```

### 4. 事件优先级优化

合理使用事件优先级避免不必要的处理：

```csharp
// ✅ 好的做法：使用优先级控制事件传播
public class InputSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // UI 输入处理器优先级最高
        this.RegisterEvent&lt;InputEvent&gt;(OnUIInput, priority: 100);
    }

    private void OnUIInput(InputEvent e)
    {
        if (IsUIHandlingInput())
        {
            // 停止事件传播，避免游戏逻辑处理
            e.StopPropagation();
        }
    }
}

public class GameplayInputSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 游戏逻辑输入处理器优先级较低
        this.RegisterEvent&lt;InputEvent&gt;(OnGameplayInput, priority: 50);
    }

    private void OnGameplayInput(InputEvent e)
    {
        // 如果 UI 已经处理了输入，这里不会执行
        if (!e.IsPropagationStopped)
        {
            ProcessGameplayInput(e);
        }
    }
}
```

## 协程优化

协程是处理异步逻辑的强大工具，但不当使用会影响性能。

### 1. 避免过度嵌套

```csharp
// ✅ 好的做法：扁平化协程结构
public IEnumerator&lt;IYieldInstruction&gt; LoadGameSequence()
{
    // 显示加载界面
    yield return ShowLoadingScreen();

    // 加载配置
    yield return LoadConfiguration();

    // 加载资源
    yield return LoadResources();

    // 初始化系统
    yield return InitializeSystems();

    // 隐藏加载界面
    yield return HideLoadingScreen();
}

private IEnumerator&lt;IYieldInstruction&gt; LoadConfiguration()
{
    var config = await ConfigManager.LoadAsync();
    ApplyConfiguration(config);
    yield break;
}

// ❌ 避免：深度嵌套
public IEnumerator&lt;IYieldInstruction&gt; LoadGameSequence()
{
    yield return ShowLoadingScreen().Then(() =&gt;
    {
        return LoadConfiguration().Then(() =&gt;
        {
            return LoadResources().Then(() =&gt;
            {
                return InitializeSystems().Then(() =&gt;
                {
                    return HideLoadingScreen();
                });
            });
        });
    });
}
```

### 2. 协程池化

对于频繁启动的协程，考虑复用：

```csharp
// ✅ 好的做法：复用协程逻辑
public class EffectSystem : AbstractSystem
{
    private readonly Dictionary&lt;string, IEnumerator&lt;IYieldInstruction&gt;&gt; _effectCoroutines = new();

    public CoroutineHandle PlayEffect(string effectId, Vector2 position)
    {
        // 复用协程逻辑
        return this.StartCoroutine(EffectCoroutine(effectId, position));
    }

    private IEnumerator&lt;IYieldInstruction&gt; EffectCoroutine(string effectId, Vector2 position)
    {
        var effect = CreateEffect(effectId, position);

        // 播放效果
        yield return new Delay(effect.Duration);

        // 清理效果
        DestroyEffect(effect);
    }
}
```

### 3. 合理使用 WaitForFrames

```csharp
// ✅ 好的做法：使用 WaitForFrames 分帧处理
public IEnumerator&lt;IYieldInstruction&gt; ProcessLargeDataSet(List&lt;Data&gt; dataSet)
{
    const int batchSize = 100;

    for (int i = 0; i &lt; dataSet.Count; i += batchSize)
    {
        int end = Math.Min(i + batchSize, dataSet.Count);

        // 处理一批数据
        for (int j = i; j &lt; end; j++)
        {
            ProcessData(dataSet[j]);
        }

        // 等待一帧，避免卡顿
        yield return new WaitOneFrame();
    }
}

// ❌ 避免：一次性处理大量数据
public void ProcessLargeDataSet(List&lt;Data&gt; dataSet)
{
    // 一次性处理所有数据，可能导致帧率下降
    foreach (var data in dataSet)
    {
        ProcessData(data);
    }
}
```

### 4. 协程取消优化

```csharp
// ✅ 好的做法：及时取消不需要的协程
public class AnimationController : AbstractSystem
{
    private CoroutineHandle _currentAnimation;

    public void PlayAnimation(string animationName)
    {
        // 取消当前动画
        if (_currentAnimation.IsValid)
        {
            this.StopCoroutine(_currentAnimation);
        }

        // 播放新动画
        _currentAnimation = this.StartCoroutine(AnimationCoroutine(animationName));
    }

    private IEnumerator&lt;IYieldInstruction&gt; AnimationCoroutine(string animationName)
    {
        var animation = GetAnimation(animationName);

        while (!animation.IsComplete)
        {
            animation.Update(Time.DeltaTime);
            yield return new WaitOneFrame();
        }
    }
}
```

## 资源管理优化

高效的资源管理可以显著减少加载时间和内存占用。

### 1. 资源预加载

```csharp
// ✅ 好的做法：预加载常用资源
public class ResourcePreloader : AbstractSystem
{
    private IResourceManager _resourceManager;

    protected override void OnInit()
    {
        _resourceManager = GetUtility&lt;IResourceManager&gt;();
    }

    public async Task PreloadCommonResources()
    {
        // 预加载 UI 资源
        await _resourceManager.PreloadAsync&lt;Texture&gt;("ui/button_normal");
        await _resourceManager.PreloadAsync&lt;Texture&gt;("ui/button_pressed");
        await _resourceManager.PreloadAsync&lt;Texture&gt;("ui/button_hover");

        // 预加载音效
        await _resourceManager.PreloadAsync&lt;AudioClip&gt;("sfx/button_click");
        await _resourceManager.PreloadAsync&lt;AudioClip&gt;("sfx/button_hover");

        // 预加载特效
        await _resourceManager.PreloadAsync&lt;ParticleSystem&gt;("effects/hit_effect");
        await _resourceManager.PreloadAsync&lt;ParticleSystem&gt;("effects/explosion");
    }
}
```

### 2. 异步加载

```csharp
// ✅ 好的做法：使用异步加载避免阻塞
public class SceneLoader : AbstractSystem
{
    private IResourceManager _resourceManager;

    public async Task LoadSceneAsync(string sceneName)
    {
        // 显示加载进度
        var progress = 0f;
        UpdateLoadingProgress(progress);

        // 异步加载场景资源
        var sceneData = await _resourceManager.LoadAsync&lt;SceneData&gt;($"scenes/{sceneName}");
        progress += 0.3f;
        UpdateLoadingProgress(progress);

        // 异步加载场景依赖的资源
        await LoadSceneDependencies(sceneData);
        progress += 0.5f;
        UpdateLoadingProgress(progress);

        // 初始化场景
        await InitializeScene(sceneData);
        progress = 1f;
        UpdateLoadingProgress(progress);
    }

    private async Task LoadSceneDependencies(SceneData sceneData)
    {
        var tasks = new List&lt;Task&gt;();

        foreach (var dependency in sceneData.Dependencies)
        {
            tasks.Add(_resourceManager.LoadAsync&lt;object&gt;(dependency));
        }

        await Task.WhenAll(tasks);
    }
}

// ❌ 避免：同步加载阻塞主线程
public class SceneLoader : AbstractSystem
{
    public void LoadScene(string sceneName)
    {
        // 同步加载会阻塞主线程，导致卡顿
        var sceneData = _resourceManager.Load&lt;SceneData&gt;($"scenes/{sceneName}");
        LoadSceneDependencies(sceneData);
        InitializeScene(sceneData);
    }
}
```

### 3. 资源引用计数

```csharp
// ✅ 好的做法：使用资源句柄管理引用
public class EntityRenderer : AbstractSystem
{
    private readonly Dictionary&lt;string, IResourceHandle&lt;Texture&gt;&gt; _textureHandles = new();

    public void LoadTexture(string entityId, string texturePath)
    {
        // 获取资源句柄
        var handle = _resourceManager.GetHandle&lt;Texture&gt;(texturePath);
        if (handle != null)
        {
            _textureHandles[entityId] = handle;
        }
    }

    public void UnloadTexture(string entityId)
    {
        if (_textureHandles.TryGetValue(entityId, out var handle))
        {
            // 释放句柄，自动管理引用计数
            handle.Dispose();
            _textureHandles.Remove(entityId);
        }
    }

    protected override void OnDestroy()
    {
        // 清理所有句柄
        foreach (var handle in _textureHandles.Values)
        {
            handle.Dispose();
        }
        _textureHandles.Clear();
    }
}
```

### 4. 资源缓存策略

```csharp
// ✅ 好的做法：使用合适的释放策略
public class GameResourceManager : AbstractSystem
{
    private IResourceManager _resourceManager;

    protected override void OnInit()
    {
        _resourceManager = GetUtility&lt;IResourceManager&gt;();

        // 设置自动释放策略
        _resourceManager.SetReleaseStrategy(new AutoReleaseStrategy());
    }

    public void OnSceneChanged()
    {
        // 场景切换时卸载未使用的资源
        UnloadUnusedResources();
    }

    private void UnloadUnusedResources()
    {
        var loadedPaths = _resourceManager.GetLoadedResourcePaths().ToList();

        foreach (var path in loadedPaths)
        {
            // 检查资源是否仍在使用
            if (!IsResourceInUse(path))
            {
                _resourceManager.Unload(path);
            }
        }
    }

    private bool IsResourceInUse(string path)
    {
        // 检查资源引用计数
        return false; // 实现具体逻辑
    }
}
```

## ECS 性能优化

Entity Component System (ECS) 是高性能游戏架构的关键。

### 1. 组件设计优化

```csharp
// ✅ 好的做法：使用值类型组件
public struct Position
{
    public float X;
    public float Y;
    public float Z;
}

public struct Velocity
{
    public float X;
    public float Y;
    public float Z;
}

public struct Health
{
    public int Current;
    public int Max;
}

// ❌ 避免：使用引用类型组件
public class Position
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}
```

### 2. 查询优化

```csharp
// ✅ 好的做法：使用高效的查询
public class MovementSystem : ArchSystemAdapter
{
    private QueryDescription _movementQuery;

    public override void Initialize()
    {
        // 预先构建查询
        _movementQuery = new QueryDescription()
            .WithAll&lt;Position, Velocity&gt;()
            .WithNone&lt;Frozen&gt;();
    }

    public override void Update(float deltaTime)
    {
        // 使用预构建的查询
        World.Query(in _movementQuery, (ref Position pos, ref Velocity vel) =&gt;
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
            pos.Z += vel.Z * deltaTime;
        });
    }
}

// ❌ 避免：每帧构建查询
public class MovementSystem : ArchSystemAdapter
{
    public override void Update(float deltaTime)
    {
        // 每帧都构建新查询，性能差
        var query = new QueryDescription()
            .WithAll&lt;Position, Velocity&gt;()
            .WithNone&lt;Frozen&gt;();

        World.Query(in query, (ref Position pos, ref Velocity vel) =&gt;
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
            pos.Z += vel.Z * deltaTime;
        });
    }
}
```

### 3. 批量处理

```csharp
// ✅ 好的做法：批量处理实体
public class DamageSystem : ArchSystemAdapter
{
    private readonly List&lt;(EntityReference entity, int damage)&gt; _pendingDamages = new();

    public void QueueDamage(EntityReference entity, int damage)
    {
        _pendingDamages.Add((entity, damage));
    }

    public override void Update(float deltaTime)
    {
        if (_pendingDamages.Count == 0)
            return;

        // 批量应用伤害
        foreach (var (entity, damage) in _pendingDamages)
        {
            if (World.IsAlive(entity))
            {
                ref var health = ref World.Get&lt;Health&gt;(entity);
                health.Current = Math.Max(0, health.Current - damage);

                if (health.Current == 0)
                {
                    World.Destroy(entity);
                }
            }
        }

        _pendingDamages.Clear();
    }
}
```

### 4. 避免装箱

```csharp
// ✅ 好的做法：避免装箱操作
public struct EntityId : IEquatable&lt;EntityId&gt;
{
    public int Value;

    public bool Equals(EntityId other) =&gt; Value == other.Value;
    public override bool Equals(object obj) =&gt; obj is EntityId other &amp;&amp; Equals(other);
    public override int GetHashCode() =&gt; Value;
}

// 使用泛型避免装箱
public class EntityCache&lt;T&gt; where T : struct
{
    private readonly Dictionary&lt;EntityId, T&gt; _cache = new();

    public void Add(EntityId id, T data)
    {
        _cache[id] = data; // 无装箱
    }

    public bool TryGet(EntityId id, out T data)
    {
        return _cache.TryGetValue(id, out data); // 无装箱
    }
}

// ❌ 避免：导致装箱的操作
public class EntityCache
{
    private readonly Dictionary&lt;int, object&gt; _cache = new();

    public void Add(int id, object data)
    {
        _cache[id] = data; // 值类型会装箱
    }
}
```

## 内存优化

减少内存分配和 GC 压力是性能优化的重要方面。

### 1. 使用 Span&lt;T&gt;

```csharp
// ✅ 好的做法：使用 Span 避免分配
public void ProcessData(ReadOnlySpan&lt;byte&gt; data)
{
    // 直接在栈上处理，无堆分配
    Span&lt;int&gt; results = stackalloc int[data.Length];

    for (int i = 0; i &lt; data.Length; i++)
    {
        results[i] = ProcessByte(data[i]);
    }

    // 使用结果
    UseResults(results);
}

// 解析字符串避免分配
public bool TryParseValue(ReadOnlySpan&lt;char&gt; input, out int result)
{
    return int.TryParse(input, out result);
}

// ❌ 避免：不必要的数组分配
public void ProcessData(byte[] data)
{
    // 创建新数组，产生 GC
    var results = new int[data.Length];

    for (int i = 0; i &lt; data.Length; i++)
    {
        results[i] = ProcessByte(data[i]);
    }

    UseResults(results);
}
```

### 2. 结构体优化

```csharp
// ✅ 好的做法：使用 readonly struct
public readonly struct Vector2D
{
    public readonly float X;
    public readonly float Y;

    public Vector2D(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float Length() =&gt; MathF.Sqrt(X * X + Y * Y);

    public Vector2D Normalized()
    {
        var length = Length();
        return length &gt; 0 ? new Vector2D(X / length, Y / length) : this;
    }
}

// ❌ 避免：可变结构体
public struct Vector2D
{
    public float X { get; set; }
    public float Y { get; set; }

    // 可变结构体可能导致意外的复制
}
```

### 3. 避免闭包分配

```csharp
// ✅ 好的做法：避免闭包捕获
public class EventProcessor : AbstractSystem
{
    private readonly Action&lt;PlayerEvent&gt; _cachedHandler;

    public EventProcessor()
    {
        // 缓存委托，避免每次分配
        _cachedHandler = HandlePlayerEvent;
    }

    protected override void OnInit()
    {
        this.RegisterEvent(_cachedHandler);
    }

    private void HandlePlayerEvent(PlayerEvent e)
    {
        ProcessEvent(e);
    }
}

// ❌ 避免：每次都创建新的闭包
public class EventProcessor : AbstractSystem
{
    protected override void OnInit()
    {
        // 每次都创建新的委托和闭包
        this.RegisterEvent&lt;PlayerEvent&gt;(e =&gt;
        {
            ProcessEvent(e);
        });
    }
}
```

### 4. 字符串优化

```csharp
// ✅ 好的做法：减少字符串分配
public class Logger
{
    private readonly StringBuilder _sb = new();

    public void LogFormat(string format, params object[] args)
    {
        _sb.Clear();
        _sb.AppendFormat(format, args);
        Log(_sb.ToString());
    }

    // 使用字符串插值的优化版本
    public void LogInterpolated(ref DefaultInterpolatedStringHandler handler)
    {
        Log(handler.ToStringAndClear());
    }
}

// 使用 string.Create 避免中间分配
public string CreateEntityName(int id, string type)
{
    return string.Create(type.Length + 10, (id, type), (span, state) =&gt;
    {
        state.type.AsSpan().CopyTo(span);
        span = span.Slice(state.type.Length);
        span[0] = '_';
        state.id.TryFormat(span.Slice(1), out _);
    });
}

// ❌ 避免：频繁的字符串拼接
public string CreateEntityName(int id, string type)
{
    return type + "_" + id.ToString(); // 创建多个临时字符串
}
```

## 最佳实践

### 1. 性能测试

```csharp
// ✅ 使用性能测试验证优化效果
[TestFixture]
public class PerformanceTests
{
    [Test]
    [Performance]
    public void ObjectPool_Performance_Test()
    {
        var pool = new BulletPoolSystem();
        pool.Prewarm("bullet", 1000);

        Measure.Method(() =&gt;
        {
            var bullet = pool.Acquire("bullet");
            pool.Release("bullet", bullet);
        })
        .WarmupCount(10)
        .MeasurementCount(100)
        .Run();
    }

    [Test]
    public void CompareAllocationMethods()
    {
        // 测试对象池
        var poolTime = MeasureTime(() =&gt;
        {
            var pool = ArrayPool&lt;int&gt;.Shared;
            var array = pool.Rent(1000);
            pool.Return(array);
        });

        // 测试直接分配
        var allocTime = MeasureTime(() =&gt;
        {
            var array = new int[1000];
        });

        Assert.Less(poolTime, allocTime, "Object pool should be faster");
    }

    private long MeasureTime(Action action)
    {
        var sw = Stopwatch.StartNew();
        for (int i = 0; i &lt; 10000; i++)
        {
            action();
        }
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }
}
```

### 2. 性能监控

```csharp
// ✅ 实现性能监控系统
public class PerformanceMonitor : AbstractSystem
{
    private readonly Dictionary&lt;string, PerformanceMetric&gt; _metrics = new();
    private float _updateInterval = 1.0f;
    private float _timeSinceUpdate = 0f;

    public void Update(float deltaTime)
    {
        _timeSinceUpdate += deltaTime;

        if (_timeSinceUpdate &gt;= _updateInterval)
        {
            UpdateMetrics();
            _timeSinceUpdate = 0f;
        }
    }

    public void RecordMetric(string name, float value)
    {
        if (!_metrics.TryGetValue(name, out var metric))
        {
            metric = new PerformanceMetric(name);
            _metrics[name] = metric;
        }

        metric.AddSample(value);
    }

    private void UpdateMetrics()
    {
        foreach (var metric in _metrics.Values)
        {
            Logger.Info($"{metric.Name}: Avg={metric.Average:F2}ms, " +
                       $"Min={metric.Min:F2}ms, Max={metric.Max:F2}ms");
            metric.Reset();
        }
    }
}

public class PerformanceMetric
{
    public string Name { get; }
    public float Average =&gt; _count &gt; 0 ? _sum / _count : 0;
    public float Min { get; private set; } = float.MaxValue;
    public float Max { get; private set; } = float.MinValue;

    private float _sum;
    private int _count;

    public PerformanceMetric(string name)
    {
        Name = name;
    }

    public void AddSample(float value)
    {
        _sum += value;
        _count++;
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
    }

    public void Reset()
    {
        _sum = 0;
        _count = 0;
        Min = float.MaxValue;
        Max = float.MinValue;
    }
}
```

### 3. 性能分析工具

使用性能分析工具定位瓶颈：

- **Unity Profiler** - Unity 内置的性能分析工具
- **Godot Profiler** - Godot 的性能监控工具
- **dotTrace** - JetBrains 的 .NET 性能分析器
- **PerfView** - Microsoft 的性能分析工具

### 4. 性能优化清单

在优化性能时，遵循以下清单：

- [ ] 使用对象池减少 GC 压力
- [ ] 正确管理事件订阅，避免内存泄漏
- [ ] 使用结构体事件避免堆分配
- [ ] 合理使用协程，避免过度嵌套
- [ ] 异步加载资源，避免阻塞主线程
- [ ] 使用 ECS 架构提高数据局部性
- [ ] 使用 Span&lt;T&gt; 和 stackalloc 减少分配
- [ ] 避免装箱操作
- [ ] 缓存常用的委托和对象
- [ ] 批量处理高频操作
- [ ] 定期进行性能测试和监控

## 常见问题

### Q1: 什么时候应该使用对象池？

**A**: 当满足以下条件时考虑使用对象池：

- 对象创建成本高（如包含复杂初始化逻辑）
- 对象频繁创建和销毁（如子弹、特效粒子）
- 对象生命周期短暂但使用频繁
- 需要减少 GC 压力

### Q2: 如何判断是否存在内存泄漏？

**A**: 观察以下指标：

- 内存使用持续增长，不会下降
- GC 频率增加但内存不释放
- 事件订阅数量持续增加
- 对象池中的活跃对象数量异常

使用内存分析工具（如 dotMemory）定位泄漏源。

### Q3: 协程和 async/await 如何选择？

**A**: 选择建议：

- **协程** - 游戏逻辑、动画序列、分帧处理
- **async/await** - I/O 操作、网络请求、文件读写

协程更适合游戏帧循环，async/await 更适合异步 I/O。

### Q4: 如何优化大量实体的性能？

**A**: 使用 ECS 架构：

- 使用值类型组件减少堆分配
- 预构建查询避免每帧创建
- 批量处理实体操作
- 使用并行处理（如果支持）

### Q5: 什么时候应该进行性能优化？

**A**: 遵循以下原则：

- **先测量，再优化** - 使用性能分析工具找到真正的瓶颈
- **优先优化热点** - 集中优化占用时间最多的代码
- **避免过早优化** - 在功能完成后再进行优化
- **保持可读性** - 不要为了微小的性能提升牺牲代码可读性

### Q6: 如何减少 GC 压力？

**A**: 采取以下措施：

- 使用对象池复用对象
- 使用值类型（struct）而非引用类型（class）
- 使用 Span&lt;T&gt; 和 stackalloc 进行栈分配
- 避免闭包捕获和装箱操作
- 缓存常用对象和委托
- 使用 StringBuilder 而非字符串拼接

---

## 总结

性能优化是一个持续的过程，需要：

- ✅ **测量优先** - 使用工具定位真正的瓶颈
- ✅ **合理使用对象池** - 减少 GC 压力
- ✅ **优化事件系统** - 避免内存泄漏和不必要的处理
- ✅ **高效使用协程** - 避免过度嵌套和不必要的等待
- ✅ **智能资源管理** - 异步加载和合理缓存
- ✅ **ECS 架构优化** - 提高数据局部性和处理效率
- ✅ **内存优化** - 减少分配，使用栈内存
- ✅ **持续监控** - 建立性能监控系统

记住，性能优化要在可维护性和性能之间找到平衡，不要为了微小的性能提升而牺牲代码质量。

---

**文档版本**: 1.0.0
**更新日期**: 2026-03-07
