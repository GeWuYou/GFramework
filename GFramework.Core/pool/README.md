# 对象池系统 (Object Pool System)

## 概述

GFramework 的对象池系统是一个高效的内存管理机制，旨在减少垃圾回收（GC）压力，通过复用对象实例来提高应用程序性能。该系统实现了对象的创建、获取、释放和销毁的完整生命周期管理。

**核心优势：**

- **减少 GC 压力**：复用对象实例，避免频繁的内存分配和回收
- **提高性能**：避免重复创建开销大的对象
- **灵活管理**：支持多个独立的对象池，按需分类管理
- **自动生命周期**：与 System 生命周期集成，自动管理对象销毁
- **类型安全**：基于泛型实现，编译时类型检查

## 核心组件

### IPoolableObject 接口

定义了可池化对象的行为规范，所有需要池化的对象都必须实现此接口。

```csharp
public interface IPoolableObject
{
    /// <summary>
    /// 当对象从池中获取时调用，用于初始化或重置对象状态
    /// </summary>
    void OnAcquire();

    /// <summary>
    /// 当对象被放回池中时调用，用于清理对象状态
    /// </summary>
    void OnRelease();

    /// <summary>
    /// 当对象池被销毁时调用，用于执行最终清理
    /// </summary>
    void OnPoolDestroy();
}
```

**生命周期：**

```
创建 → Acquire（从池取出）→ 使用 → Release（放回池）→ 可再次 Acquire
                                          ↓
                                    Pool Destroy → OnPoolDestroy → 销毁
```

### IObjectPoolSystem 接口

定义了对象池系统的基本操作接口。

```csharp
public interface IObjectPoolSystem<TKey, TObject>
    where TObject : IPoolableObject
    where TKey : notnull
{
    /// <summary>
    /// 从指定键的对象池中获取一个对象
    /// </summary>
    /// <param name="key">对象池的键值</param>
    /// <returns>获取到的对象实例</returns>
    TObject Acquire(TKey key);

    /// <summary>
    /// 将对象释放回指定键的对象池中
    /// </summary>
    /// <param name="key">对象池的键值</param>
    /// <param name="obj">需要释放的对象</param>
    void Release(TKey key, TObject obj);

    /// <summary>
    /// 清空所有对象池，销毁所有池中的对象
    /// </summary>
    void Clear();
}
```

### AbstractObjectPoolSystem 抽象类

实现了 `IObjectPoolSystem` 接口的具体逻辑，提供了对象池管理的完整实现。

**核心特性：**

- 使用字典存储多个对象池，以键区分不同的对象池
- 使用栈（Stack）存储池中的对象，实现 LIFO（后进先出）管理
- 提供获取和释放对象的方法
- 通过抽象方法 `Create` 让子类决定如何创建对象
- 在系统销毁时自动清理所有对象池

**内部实现：**

```csharp
public abstract class AbstractObjectPoolSystem<TKey, TObject>
    : AbstractSystem, IObjectPoolSystem<TKey, TObject>
    where TObject : IPoolableObject
    where TKey : notnull
{
    // 存储对象池的字典，键为池标识，值为对应类型的对象栈
    protected readonly Dictionary<TKey, Stack<TObject>> Pools = new();

    // 获取对象
    public TObject Acquire(TKey key)
    {
        if (!Pools.TryGetValue(key, out var pool))
        {
            pool = new Stack<TObject>();
            Pools[key] = pool;
        }

        var obj = pool.Count > 0
            ? pool.Pop()  // 从池中取出
            : Create(key); // 创建新对象

        obj.OnAcquire(); // 调用对象的获取钩子
        return obj;
    }

    // 释放对象
    public void Release(TKey key, TObject obj)
    {
        obj.OnRelease(); // 调用对象的释放钩子

        if (!Pools.TryGetValue(key, out var pool))
        {
            pool = new Stack<TObject>();
            Pools[key] = pool;
        }

        pool.Push(obj); // 放回池中
    }

    // 清空所有对象池
    public void Clear()
    {
        foreach (var obj in Pools.Values.SelectMany(pool => pool))
        {
            obj.OnPoolDestroy(); // 调用对象的销毁钩子
        }

        Pools.Clear();
    }

    // 子类实现：创建新对象
    protected abstract TObject Create(TKey key);

    // 系统销毁时自动清空对象池
    protected override void OnDestroy()
    {
        Clear();
    }
}
```

## 基本使用

### 1. 定义池化对象

首先，创建一个实现 `IPoolableObject` 接口的类。

```csharp
public class Bullet : IPoolableObject
{
    public int Damage { get; private set; }
    public float Speed { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector3 Direction { get; private set; }
    public bool IsActive { get; private set; }

    public void OnAcquire()
    {
        // 从池中获取时调用，初始化对象状态
        IsActive = true;
    }

    public void OnRelease()
    {
        // 放回池中时调用，清理对象状态
        IsActive = false;
        Damage = 0;
        Speed = 0;
        Position = Vector3.Zero;
        Direction = Vector3.Zero;
    }

    public void OnPoolDestroy()
    {
        // 对象池销毁时调用，执行最终清理
        // 可以在这里释放非托管资源
    }

    // 设置子弹属性的方法
    public void Setup(int damage, float speed, Vector3 position, Vector3 direction)
    {
        Damage = damage;
        Speed = speed;
        Position = position;
        Direction = direction;
    }

    // 更新子弹逻辑
    public void Update(float deltaTime)
    {
        Position += Direction * Speed * deltaTime;
    }
}
```

### 2. 实现对象池系统

继承 `AbstractObjectPoolSystem<TKey, TObject>` 并实现 `Create` 方法。

```csharp
public class BulletPoolSystem : AbstractObjectPoolSystem<string, Bullet>
{
    protected override Bullet Create(string key)
    {
        // 根据键值创建不同类型的子弹
        return key switch
        {
            "standard" => new Bullet(),
            "heavy" => new Bullet(),
            "explosive" => new Bullet(),
            _ => new Bullet()
        };
    }

    protected override void OnInit()
    {
        // 可以预先创建一些对象放入池中
        for (int i = 0; i < 10; i++)
        {
            var bullet = Create("standard");
            bullet.OnAcquire();
            bullet.OnRelease();
            Release("standard", bullet);
        }
    }
}
```

### 3. 在架构中注册对象池系统

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册其他组件
        RegisterModel(new PlayerModel());
        RegisterSystem(new CombatSystem());

        // 注册对象池系统
        RegisterSystem(new BulletPoolSystem());
    }
}
```

### 4. 使用对象池

```csharp
public class ShootingSystem : AbstractSystem
{
    private BulletPoolSystem _bulletPool;
    private List<Bullet> _activeBullets = new();

    protected override void OnInit()
    {
        _bulletPool = this.GetSystem<BulletPoolSystem>();
        this.RegisterEvent<ShootEvent>(OnShoot);
        this.RegisterEvent<GameUpdateEvent>(OnUpdate);
    }

    private void OnShoot(ShootEvent e)
    {
        // 从对象池获取子弹
        var bullet = _bulletPool.Acquire("standard");

        // 设置子弹属性
        bullet.Setup(
            damage: e.Damage,
            speed: 10.0f,
            position: e.StartPosition,
            direction: e.Direction
        );

        // 添加到活跃列表
        _activeBullets.Add(bullet);
    }

    private void OnUpdate(GameUpdateEvent e)
    {
        // 更新所有活跃子弹
        for (int i = _activeBullets.Count - 1; i >= 0; i--)
        {
            var bullet = _activeBullets[i];
            bullet.Update(e.DeltaTime);

            // 检查子弹是否需要销毁（例如：超出范围或击中目标）
            if (ShouldDestroyBullet(bullet))
            {
                // 放回对象池
                _bulletPool.Release("standard", bullet);
                _activeBullets.RemoveAt(i);
            }
        }
    }

    private bool ShouldDestroyBullet(Bullet bullet)
    {
        // 简单示例：子弹超出一定范围则销毁
        return bullet.Position.Length() > 1000.0f;
    }
}
```

## 高级用法

### 1. 多键对象池管理

```csharp
public class ParticlePoolSystem : AbstractObjectPoolSystem<string, Particle>
{
    protected override Particle Create(string key)
    {
        // 根据键值创建不同类型的粒子效果
        return key switch
        {
            "explosion" => new Particle(explosionPrefab),
            "smoke" => new Particle(smokePrefab),
            "spark" => new Particle(sparkPrefab),
            _ => throw new ArgumentException($"Unknown particle type: {key}")
        };
    }

    // 提供便捷方法
    public Particle SpawnExplosion(Vector3 position)
    {
        var particle = Acquire("explosion");
        particle.Position = position;
        particle.Play();
        return particle;
    }
}

// 使用
public class EffectSystem : AbstractSystem
{
    private ParticlePoolSystem _particlePool;

    protected override void OnInit()
    {
        _particlePool = this.GetSystem<ParticlePoolSystem>();
    }

    public void PlayExplosion(Vector3 position)
    {
        _particlePool.SpawnExplosion(position);
    }
}
```

### 2. 动态对象池管理

```csharp
public class EnemyPoolSystem : AbstractObjectPoolSystem<string, Enemy>
{
    protected override Enemy Create(string key)
    {
        // 根据敌人类型创建不同的敌人
        var enemyPrefab = LoadEnemyPrefab(key);
        return new Enemy(enemyPrefab);
    }

    // 动态注册新的敌人类型池
    public void RegisterEnemyType(string enemyType)
    {
        if (!Pools.ContainsKey(enemyType))
        {
            Pools[enemyType] = new Stack<Enemy>();

            // 预热：预先创建几个敌人放入池中
            for (int i = 0; i < 3; i++)
            {
                var enemy = Create(enemyType);
                enemy.OnAcquire();
                enemy.OnRelease();
                Release(enemyType, enemy);
            }
        }
    }
}

// 使用
public class EnemySpawnerSystem : AbstractSystem
{
    private EnemyPoolSystem _enemyPool;

    protected override void OnInit()
    {
        _enemyPool = this.GetSystem<EnemyPoolSystem>();

        // 注册不同类型的敌人
        _enemyPool.RegisterEnemyType("goblin");
        _enemyPool.RegisterEnemyType("orc");
        _enemyPool.RegisterEnemyType("dragon");
    }

    public void SpawnEnemy(string enemyType, Vector3 position)
    {
        var enemy = _enemyPool.Acquire(enemyType);
        enemy.Position = position;
        enemy.Activate();
    }
}
```

### 3. 对象池大小限制

```csharp
public class LimitedBulletPoolSystem : AbstractObjectPoolSystem<string, Bullet>
{
    private const int MaxPoolSize = 50;

    protected override Bullet Create(string key)
    {
        return new Bullet();
    }

    public new void Release(string key, Bullet obj)
    {
        // 检查对象池大小
        if (Pools.TryGetValue(key, out var pool) && pool.Count >= MaxPoolSize)
        {
            // 池已满，不回收对象，让它被 GC 回收
            return;
        }

        // 调用基类的 Release 方法
        base.Release(key, obj);
    }
}
```

### 4. 对象池统计和调试

```csharp
public class DebuggablePoolSystem : AbstractObjectPoolSystem<string, PoolableObject>
{
    public Dictionary<string, int> PoolSizes => Pools.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.Count
    );

    public int TotalPooledObjects => Pools.Values.Sum(stack => stack.Count);

    public void LogPoolStatus()
    {
        foreach (var (key, stack) in Pools)
        {
            Console.WriteLine($"Pool [{key}]: {stack.Count} objects");
        }

        Console.WriteLine($"Total pooled objects: {TotalPooledObjects}");
    }

    protected override void OnDestroy()
    {
        LogPoolStatus();
        base.OnDestroy();
    }
}
```

## 使用场景

### 1. 游戏对象池

**适用对象：**

- 子弹、箭矢、投射物
- 敌人、NPC
- 爆炸效果、粒子系统
- UI 元素（提示、对话框）

**示例：子弹池**

```csharp
// 定义子弹
public class Bullet : IPoolableObject
{
    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }
    public float Lifetime { get; private set; }

    public void OnAcquire()
    {
        Lifetime = 5.0f; // 5秒后自动销毁
    }

    public void OnRelease()
    {
        Position = Vector3.Zero;
        Velocity = Vector3.Zero;
    }

    public void OnPoolDestroy() { }

    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
        Lifetime -= deltaTime;
    }
}

// 子弹对象池
public class BulletPoolSystem : AbstractObjectPoolSystem<string, Bullet>
{
    protected override Bullet Create(string key) => new Bullet();
}

// 射击系统
public class ShootingSystem : AbstractSystem
{
    private BulletPoolSystem _bulletPool;
    private List<Bullet> _activeBullets = new();

    protected override void OnInit()
    {
        _bulletPool = this.GetSystem<BulletPoolSystem>();
        this.RegisterEvent<ShootEvent>(OnShoot);
        this.RegisterEvent<GameUpdateEvent>(OnUpdate);
    }

    private void OnShoot(ShootEvent e)
    {
        var bullet = _bulletPool.Acquire("normal");
        bullet.Position = e.StartPosition;
        bullet.Velocity = e.Direction * e.Speed;
        _activeBullets.Add(bullet);
    }

    private void OnUpdate(GameUpdateEvent e)
    {
        for (int i = _activeBullets.Count - 1; i >= 0; i--)
        {
            var bullet = _activeBullets[i];
            bullet.Update(e.DeltaTime);

            if (bullet.Lifetime <= 0)
            {
                _bulletPool.Release("normal", bullet);
                _activeBullets.RemoveAt(i);
            }
        }
    }
}
```

### 2. UI 元素池

**适用对象：**

- 对话框
- 提示框
- 菜单项
- 列表项

**示例：提示框池**

```csharp
public class Tooltip : IPoolableObject
{
    public string Text { get; set; }
    public bool IsActive { get; private set; }

    public void OnAcquire()
    {
        IsActive = true;
    }

    public void OnRelease()
    {
        IsActive = false;
        Text = "";
    }

    public void OnPoolDestroy() { }

    public void Show(string text, Vector3 position)
    {
        Text = text;
        // 更新 UI 位置和内容
    }
}

public class TooltipPoolSystem : AbstractObjectPoolSystem<string, Tooltip>
{
    protected override Tooltip Create(string key) => new Tooltip();
}

public class UISystem : AbstractSystem
{
    private TooltipPoolSystem _tooltipPool;

    protected override void OnInit()
    {
        _tooltipPool = this.GetSystem<TooltipPoolSystem>();
    }

    public void ShowTooltip(string text, Vector3 position)
    {
        var tooltip = _tooltipPool.Acquire("default");
        tooltip.Show(text, position);
    }
}
```

### 3. 网络消息对象池

**适用对象：**

- 网络包
- 协议消息
- 数据包

**示例：网络包池**

```csharp
public class NetworkPacket : IPoolableObject
{
    public byte[] Data { get; private set; }
    public int Length { get; private set; }

    public void OnAcquire()
    {
        Data = Array.Empty<byte>();
        Length = 0;
    }

    public void OnRelease()
    {
        // 清理敏感数据
        if (Data != null)
        {
            Array.Clear(Data, 0, Data.Length);
        }
        Length = 0;
    }

    public void OnPoolDestroy() { }

    public void SetData(byte[] data)
    {
        Data = data;
        Length = data.Length;
    }
}

public class PacketPoolSystem : AbstractObjectPoolSystem<string, NetworkPacket>
{
    protected override NetworkPacket Create(string key) => new NetworkPacket();
}
```

## 最佳实践

### 1. 对象生命周期管理

```csharp
// ✅ 好的做法：确保所有对象都放回池中
public class BulletSystem : AbstractSystem
{
    private List<Bullet> _activeBullets = new();

    protected override void OnDestroy()
    {
        // 系统销毁时，确保所有活跃子弹都放回池中
        foreach (var bullet in _activeBullets)
        {
            _bulletPool.Release("standard", bullet);
        }

        _activeBullets.Clear();
        base.OnDestroy();
    }
}

// ❌ 不好的做法：忘记放回对象，导致泄漏
public class BadBulletSystem : AbstractSystem
{
    private List<Bullet> _activeBullets = new();

    private void OnUpdate(GameUpdateEvent e)
    {
        // 子弹销毁时忘记放回池中
        if (bullet.Lifetime <= 0)
        {
            _activeBullets.RemoveAt(i);
            // 忘记调用 _bulletPool.Release(...)
        }
    }
}
```

### 2. 对象状态重置

```csharp
// ✅ 好的做法：在 OnRelease 中彻底重置对象状态
public class Bullet : IPoolableObject
{
    public int Damage { get; set; }
    public float Speed { get; set; }
    public List<string> Tags { get; set; }
    public Dictionary<string, object> Data { get; set; }

    public void OnRelease()
    {
        // 重置所有属性
        Damage = 0;
        Speed = 0;
        Tags?.Clear();
        Data?.Clear();

        // 也可以设置为新实例（如果性能允许）
        Tags = new List<string>();
        Data = new Dictionary<string, object>();
    }
}

// ❌ 不好的做法：不完全重置状态
public class BadBullet : IPoolableObject
{
    public List<string> Tags = new List<string>();

    public void OnRelease()
    {
        // 只清空列表，但列表实例本身保留
        // 这可能导致问题：如果其他代码持有列表引用
        Tags.Clear();
    }
}
```

### 3. 对象池预热

```csharp
// ✅ 好的做法：预先创建一些对象放入池中
public class BulletPoolSystem : AbstractObjectPoolSystem<string, Bullet>
{
    protected override Bullet Create(string key) => new Bullet();

    protected override void OnInit()
    {
        // 为常用的子弹类型预热对象池
        var commonTypes = new[] { "standard", "heavy", "sniper" };

        foreach (var type in commonTypes)
        {
            // 预先创建 5 个对象
            for (int i = 0; i < 5; i++)
            {
                var bullet = Create(type);
                bullet.OnAcquire();
                bullet.OnRelease();
                Release(type, bullet);
            }
        }
    }
}
```

### 4. 对象池大小管理

```csharp
// ✅ 好的做法：限制对象池大小，避免内存浪费
public class BoundedPoolSystem : AbstractObjectPoolSystem<string, PooledObject>
{
    private const int MaxPoolSize = 100;

    public new void Release(string key, PooledObject obj)
    {
        if (Pools.TryGetValue(key, out var pool) && pool.Count >= MaxPoolSize)
        {
            // 池已满，不回收对象
            return;
        }

        base.Release(key, obj);
    }
}

// ✅ 好的做法：动态调整对象池大小
public class AdaptivePoolSystem : AbstractObjectPoolSystem<string, PooledObject>
{
    private Dictionary<string, int> _peakUsage = new();

    public new void Release(string key, PooledObject obj)
    {
        // 记录峰值使用量
        if (Pools.TryGetValue(key, out var pool))
        {
            _peakUsage[key] = Math.Max(_peakUsage.GetValueOrDefault(key, 0), pool.Count);
        }

        // 根据使用情况动态调整
        int maxAllowed = _peakUsage.GetValueOrDefault(key, 10) * 2;
        if (pool.Count >= maxAllowed)
        {
            return; // 不回收
        }

        base.Release(key, obj);
    }
}
```

### 5. 调试和监控

```csharp
// ✅ 好的做法：添加对象池统计功能
public class MonitoredPoolSystem : AbstractObjectPoolSystem<string, PooledObject>
{
    private Dictionary<string, int> _acquireCount = new();
    private Dictionary<string, int> _releaseCount = new();

    public new PooledObject Acquire(string key)
    {
        _acquireCount[key] = _acquireCount.GetValueOrDefault(key, 0) + 1;
        return base.Acquire(key);
    }

    public new void Release(string key, PooledObject obj)
    {
        _releaseCount[key] = _releaseCount.GetValueOrDefault(key, 0) + 1;
        base.Release(key, obj);
    }

    public void PrintStatistics()
    {
        foreach (var key in Pools.Keys)
        {
            int acquired = _acquireCount.GetValueOrDefault(key, 0);
            int released = _releaseCount.GetValueOrDefault(key, 0);
            int leaked = acquired - released;

            Console.WriteLine($"Pool [{key}]:");
            Console.WriteLine($"  Acquired: {acquired}");
            Console.WriteLine($"  Released: {released}");
            Console.WriteLine($"  Leaked: {leaked}");
            Console.WriteLine($"  Pool size: {Pools[key].Count}");
        }
    }
}
```

## 性能优化

### 1. 减少对象创建

```csharp
// ✅ 好的做法：对象池预热，避免运行时创建
public class PreheatedPoolSystem : AbstractObjectPoolSystem<string, Bullet>
{
    protected override Bullet Create(string key) => new Bullet();

    protected override void OnInit()
    {
        // 预热常用对象池
        PreheatPool("standard", 20);
        PreheatPool("heavy", 10);
    }

    private void PreheatPool(string key, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var bullet = Create(key);
            bullet.OnAcquire();
            bullet.OnRelease();
            Release(key, bullet);
        }
    }
}
```

### 2. 使用值类型作为键

```csharp
// ✅ 好的做法：使用枚举或整数作为键，避免字符串比较
public enum BulletType
{
    Standard,
    Heavy,
    Explosive
}

public class FastBulletPoolSystem : AbstractObjectPoolSystem<BulletType, Bullet>
{
    protected override Bullet Create(BulletType key) => key switch
    {
        BulletType.Standard => new Bullet(),
        BulletType.Heavy => new Bullet(),
        BulletType.Explosive => new Bullet(),
        _ => throw new ArgumentException()
    };
}
```

### 3. 批量操作

```csharp
// ✅ 好的做法：批量获取和释放对象
public class BatchPoolSystem : AbstractObjectPoolSystem<string, PooledObject>
{
    public List<PooledObject> AcquireBatch(string key, int count)
    {
        var objects = new List<PooledObject>(count);
        for (int i = 0; i < count; i++)
        {
            objects.Add(Acquire(key));
        }
        return objects;
    }

    public void ReleaseBatch(string key, IEnumerable<PooledObject> objects)
    {
        foreach (var obj in objects)
        {
            Release(key, obj);
        }
    }
}
```

## 注意事项

1. **对象状态管理**：确保 `OnRelease` 方法彻底重置对象状态，避免状态污染
2. **对象引用**：不要在放回对象池后继续持有对象引用
3. **线程安全**：对象池本身不是线程安全的，如需多线程访问，需要自行加锁
4. **内存泄漏**：确保所有获取的对象最终都放回池中
5. **对象池大小**：合理设置对象池大小，避免内存浪费或频繁创建

## 相关包

- [`system`](../system/README.md) - 对象池系统继承自 AbstractSystem
- [`architecture`](../architecture/README.md) - 在架构中注册对象池系统

---

**许可证**: Apache 2.0
