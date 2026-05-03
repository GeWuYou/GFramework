// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Pool;

/// <summary>
///     对象池系统接口，定义了对象池的基本操作
/// </summary>
/// <typeparam name="TKey">池键的类型</typeparam>
/// <typeparam name="TObject">池中对象的类型，必须实现IPoolableObject接口</typeparam>
public interface IObjectPoolSystem<in TKey, TObject>
    where TObject : IPoolableObject
    where TKey : notnull
{
    /// <summary>
    ///     从对象池中获取一个对象实例
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <returns>池中的对象实例，如果池中没有可用对象则创建新实例</returns>
    TObject Acquire(TKey key);

    /// <summary>
    ///     将对象释放回对象池
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <param name="obj">要释放的对象</param>
    void Release(TKey key, TObject obj);

    /// <summary>
    ///     清空所有对象池
    /// </summary>
    void Clear();

    /// <summary>
    ///     获取指定池的当前大小
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <returns>池中可用对象的数量</returns>
    int GetPoolSize(TKey key);

    /// <summary>
    ///     获取指定池的活跃对象数量
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <returns>已被获取但未释放的对象数量</returns>
    int GetActiveCount(TKey key);

    /// <summary>
    ///     设置指定池的最大容量
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <param name="maxCapacity">池中保留的最大对象数量。超过此数量时，释放的对象将被销毁而不是放回池中。设置为 0 表示无限制。</param>
    void SetMaxCapacity(TKey key, int maxCapacity);

    /// <summary>
    ///     预热对象池，提前创建指定数量的对象
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <param name="count">要预创建的对象数量</param>
    void Prewarm(TKey key, int count);

    /// <summary>
    ///     获取指定池的统计信息
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <returns>池的统计信息</returns>
    PoolStatistics GetStatistics(TKey key);
}