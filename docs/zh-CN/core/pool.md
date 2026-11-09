---
title: 对象池
description: 管理池化对象的借用、归还、预热与保留容量。
---

# 对象池

`AbstractObjectPoolSystem<TKey, TObject>` 按键复用对象。只有对象创建或销毁已经成为实际热点时，池化才有明确收益；它也可能增加常驻内存与重置复杂度。

## 对象契约

IPoolableObject 定义 OnAcquire、OnRelease 和 OnPoolDestroy，分别用于借出重置、归还清理和最终销毁。池对象必须是引用类型，身份检查不使用对象自定义的值相等规则。每个借用对象应恰好归还一次，使用原来获取的键；重复归还、错误键或归还其他池及外部对象会在触发回调和修改统计之前抛出 InvalidOperationException。

OnAcquire 失败的对象会被销毁，不进入空闲栈。OnRelease 失败时对象仍由借用者持有，可以修正问题后使用原键重试；回调不得递归归还同一对象。Clear 后尚未归还的对象仍保留原始键校验，统计从清池后重新累计。

```csharp
using GFramework.Core.Abstractions.Pool;
using GFramework.Core.Pool;

public sealed class Projectile : IPoolableObject
{
    public bool Active { get; private set; }
    public void OnAcquire() => Active = true;
    public void OnRelease() => Active = false;
    public void OnPoolDestroy() => Active = false;
}

public sealed class ProjectilePool : AbstractObjectPoolSystem<string, Projectile>
{
    protected override Projectile Create(string key) => new();
}
```

```csharp
var pool = new ProjectilePool();
pool.Prewarm("basic", 8);
Projectile projectile = pool.Acquire("basic");
try
{
    // Use the projectile while it is borrowed.
}
finally
{
    pool.Release("basic", projectile);
}
pool.Clear();
```

## 容量与生命周期

SetMaxCapacity 控制归还时保留的空闲对象数量；它不是同时活动数量的硬限制。Prewarm 按指定数量预先创建对象，调用方应选择合理数量，不假定它自动遵守归还容量限制。

Clear 和 Destroy 清理池中已归还对象，不强制回收仍被调用方持有的活动对象。退出前停止生产新对象，归还活动对象，再销毁池。

池基类实现独立生命周期接口，按需使用 AddComponent 将它接入 Architecture；普通服务可直接通过构造注入接收池，不需要角色接口或上下文查找。

## 线程与统计

该通用池不保证并发借出/归还安全。集中在同一个所有者线程使用，或由宿主定义同步规则。GetStatistics、GetPoolSize 与 GetActiveCount 用于观察使用情况；统计值不是对象所有权校验器。

StringBuilderPool 提供 Rent/Return 与 GetScoped。直接 Rent 的借用者负责恰好归还一次，归还后不得继续写入。ScopedStringBuilder 是引用类型令牌，引用别名共享一次归还；重复或并发 Dispose 不会再次归还 builder。释放后访问 Value 会抛出 ObjectDisposedException。令牌保证归还次数，不使 StringBuilder 支持并发读写；不得将 Value 交给 Return，或让业务读写与 Dispose 并发。

```csharp
using var text = StringBuilderPool.GetScoped();
text.Value.Append("Score: ").Append(100);
string label = text.Value.ToString();
```

参见[性能测量](../best-practices/performance.md)与[资源租约](./resource.md)。对象池复用对象，资源管理器管理加载资源的共享所有权，两者不能互相替代。
