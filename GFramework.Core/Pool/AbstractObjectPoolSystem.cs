// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pool;
using GFramework.Core.Abstractions.Lifecycle;

namespace GFramework.Core.Pool;

/// <summary>
///     按键复用引用对象，记录借出身份，防止错误归还污染空闲对象栈。
/// </summary>
/// <remarks>所有操作集中在拥有者线程。Clear/Destroy 只清理空闲对象，活动对象仍由借用者归还。</remarks>
/// <typeparam name="TKey">对象池的键类型，必须不为null</typeparam>
/// <typeparam name="TObject">按引用身份跟踪的池化对象类型。</typeparam>
public abstract class AbstractObjectPoolSystem<TKey, TObject>
    : ILifecycle, IObjectPoolSystem<TKey, TObject> where TObject : class, IPoolableObject where TKey : notnull
{
    // Value equality is not ownership: distinct game objects may compare equal.
    // Keep outstanding borrows across Clear so a late return is still checked against its original key.
    private readonly Dictionary<TObject, TKey> _borrowed = new(ReferenceEqualityComparer.Instance);

    /// <summary>Starts the optional object-pool participant.</summary>
    public void Initialize() => OnInit();

    /// <summary>Allows a pool to prepare game-specific state without requiring a framework role.</summary>
    protected virtual void OnInit()
    {
    }

    /// <summary>
    ///     存储对象池的字典，键为池标识，值为池信息
    /// </summary>
    protected readonly IDictionary<TKey, PoolInfo> Pools = new Dictionary<TKey, PoolInfo>();

    /// <summary>
    ///     获取对象池中的对象，如果池中没有可用对象则创建新的对象
    /// </summary>
    /// <param name="key">对象池的键值</param>
    /// <returns>获取到的对象实例</returns>
    public TObject Acquire(TKey key)
    {
        if (!Pools.TryGetValue(key, out var poolInfo))
        {
            poolInfo = new PoolInfo();
            Pools[key] = poolInfo;
        }

        TObject obj;
        if (poolInfo.Stack.Count > 0)
        {
            obj = poolInfo.Stack.Pop();
        }
        else
        {
            obj = Create(key);
            poolInfo.TotalCreated++;
        }

        if (_borrowed.ContainsKey(obj))
            throw new InvalidOperationException("Create must return a new object that is not already borrowed.");

        poolInfo.TotalAcquired++;
        poolInfo.ActiveCount++;
        try
        {
            obj.OnAcquire();
            // Only a successful reset publishes a borrow; OnAcquire cannot return an object still being prepared.
            _borrowed.Add(obj, key);
        }
        catch (Exception acquireError)
        {
            // A failed acquisition has no caller to return it. Retire it rather than retain a phantom borrow.
            poolInfo.ActiveCount--;
            try
            {
                obj.OnPoolDestroy();
                poolInfo.TotalDestroyed++;
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException("Pool acquisition and cleanup failed.", acquireError, cleanupError);
            }
            throw;
        }
        return obj;
    }

    /// <summary>
    ///     将对象释放回对象池中
    /// </summary>
    /// <param name="key">对象池的键值</param>
    /// <param name="obj">需要释放的对象</param>
    /// <exception cref="InvalidOperationException">对象不是此池在该键下尚未归还的借出对象。</exception>
    public void Release(TKey key, TObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        if (!_borrowed.TryGetValue(obj, out var borrowedKey) || !EqualityComparer<TKey>.Default.Equals(key, borrowedKey))
            throw new InvalidOperationException("Return an object exactly once to the pool and key it was borrowed from.");

        // Remove admission before invoking user code so a reentrant return cannot enqueue the same object twice.
        _borrowed.Remove(obj);
        try
        {
            obj.OnRelease();
        }
        catch
        {
            // A failed reset leaves the object with its caller, who may correct the failure and retry.
            _borrowed.Add(obj, borrowedKey);
            throw;
        }

        if (!Pools.TryGetValue(key, out var poolInfo))
        {
            poolInfo = new PoolInfo();
            Pools[key] = poolInfo;
        }

        poolInfo.TotalReleased++;

        // Clear resets pool statistics but deliberately leaves outstanding borrows with their callers.
        if (poolInfo.ActiveCount > 0)
        {
            poolInfo.ActiveCount--;
        }

        // 检查容量限制
        if (poolInfo.MaxCapacity > 0 && poolInfo.Stack.Count >= poolInfo.MaxCapacity)
        {
            // 超过容量限制，销毁对象
            obj.OnPoolDestroy();
            poolInfo.TotalDestroyed++;
        }
        else
        {
            poolInfo.Stack.Push(obj);
        }
    }

    /// <summary>
    ///     清空所有对象池，销毁所有池中的对象并清理池容器
    /// </summary>
    public void Clear()
    {
        // 遍历所有对象池，调用每个对象的销毁方法
        foreach (var poolInfo in Pools.Values)
        {
            foreach (var obj in poolInfo.Stack)
            {
                obj.OnPoolDestroy();
            }
        }

        Pools.Clear();
    }

    /// <summary>
    ///     获取指定池的当前大小
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <returns>池中可用对象的数量</returns>
    public int GetPoolSize(TKey key)
    {
        return Pools.TryGetValue(key, out var poolInfo) ? poolInfo.Stack.Count : 0;
    }

    /// <summary>
    ///     获取指定池的活跃对象数量
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <returns>已被获取但未释放的对象数量</returns>
    public int GetActiveCount(TKey key)
    {
        return Pools.TryGetValue(key, out var poolInfo) ? poolInfo.ActiveCount : 0;
    }

    /// <summary>
    ///     设置指定池的最大容量
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <param name="maxCapacity">池中保留的最大对象数量。超过此数量时，释放的对象将被销毁而不是放回池中。设置为 0 表示无限制。</param>
    public void SetMaxCapacity(TKey key, int maxCapacity)
    {
        if (!Pools.TryGetValue(key, out var poolInfo))
        {
            poolInfo = new PoolInfo();
            Pools[key] = poolInfo;
        }

        poolInfo.MaxCapacity = maxCapacity;
    }

    /// <summary>
    ///     预热对象池，提前创建指定数量的对象
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <param name="count">要预创建的对象数量</param>
    public void Prewarm(TKey key, int count)
    {
        if (!Pools.TryGetValue(key, out var poolInfo))
        {
            poolInfo = new PoolInfo();
            Pools[key] = poolInfo;
        }

        for (var i = 0; i < count; i++)
        {
            var obj = Create(key);
            poolInfo.TotalCreated++;
            obj.OnRelease();
            poolInfo.Stack.Push(obj);
        }
    }

    /// <summary>
    ///     获取指定池的统计信息
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <returns>池的统计信息</returns>
    public PoolStatistics GetStatistics(TKey key)
    {
        if (!Pools.TryGetValue(key, out var poolInfo))
        {
            return new PoolStatistics
            {
                AvailableCount = 0,
                ActiveCount = 0,
                MaxCapacity = 0,
                TotalCreated = 0,
                TotalAcquired = 0,
                TotalReleased = 0,
                TotalDestroyed = 0,
            };
        }

        return new PoolStatistics
        {
            AvailableCount = poolInfo.Stack.Count,
            ActiveCount = poolInfo.ActiveCount,
            MaxCapacity = poolInfo.MaxCapacity,
            TotalCreated = poolInfo.TotalCreated,
            TotalAcquired = poolInfo.TotalAcquired,
            TotalReleased = poolInfo.TotalReleased,
            TotalDestroyed = poolInfo.TotalDestroyed,
        };
    }

    /// <summary>
    ///     创建一个新的对象实例（由子类决定怎么创建）
    /// </summary>
    /// <param name="key">用于创建对象的键值</param>
    /// <returns>新创建且未被此池或其他调用方使用的独立对象实例。</returns>
    protected abstract TObject Create(TKey key);

    /// <summary>
    ///     系统销毁时的清理操作，清空所有对象池
    /// </summary>
    public void Destroy()
    {
        Clear();
    }

    /// <summary>
    ///     池信息类，用于管理对象池的核心数据结构和统计信息。
    ///     包含对象栈、容量限制以及各类操作的统计计数。
    /// </summary>
    protected class PoolInfo
    {
        /// <summary>
        ///     对象栈，用于存储可复用的对象实例。
        /// </summary>
        public Stack<TObject> Stack { get; } = new();

        /// <summary>
        ///     池中保留的最大对象数量。当释放对象时，如果池中对象数已达到此限制，
        ///     对象将被销毁而不是放回池中。设置为 0 表示无限制。
        /// </summary>
        public int MaxCapacity
        {
            get; set;
        }

        /// <summary>
        ///     总共创建的对象数量统计。
        /// </summary>
        public int TotalCreated
        {
            get; set;
        }

        /// <summary>
        ///     总共从池中获取的对象数量统计。
        /// </summary>
        public int TotalAcquired
        {
            get; set;
        }

        /// <summary>
        ///     总共归还到池中的对象数量统计。
        /// </summary>
        public int TotalReleased
        {
            get; set;
        }

        /// <summary>
        ///     总共销毁的对象数量统计。
        /// </summary>
        public int TotalDestroyed
        {
            get; set;
        }

        /// <summary>
        ///     当前活跃（正在使用）的对象数量统计。
        /// </summary>
        public int ActiveCount
        {
            get; set;
        }
    }
}
