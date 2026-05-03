// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Internal;

/// <summary>
///     提供基于弱键语义的线程安全缓存。
///     该缓存用于跨容器复用与 <see cref="Assembly" /> 或 <see cref="Type" /> 绑定的派生元数据，
///     同时避免静态强引用阻止 collectible 程序集或热重载类型被卸载。
/// </summary>
/// <typeparam name="TKey">缓存键类型。</typeparam>
/// <typeparam name="TValue">缓存值类型。</typeparam>
/// <remarks>
///     该缓存只保证“命中时复用”，不保证“永久保留”。
///     当键对象被 GC 回收后，条目会自然失效，后续访问会重新计算对应值。
///     这是 CQRS 运行时在卸载安全与热路径性能之间的显式权衡。
/// </remarks>
internal sealed class WeakKeyCache<TKey, TValue>
    where TKey : class
    where TValue : class
{
#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _gate = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _gate = new();
#endif
    private ConditionalWeakTable<TKey, TValue> _entries = new();

    /// <summary>
    ///     获取指定键对应的缓存值；若当前未命中，则在锁保护下创建并写入。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="valueFactory">创建缓存值的工厂方法。</param>
    /// <returns>已存在或新创建的缓存值。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> 或 <paramref name="valueFactory" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException"><paramref name="valueFactory" /> 返回 <see langword="null" />。</exception>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var entries = Volatile.Read(ref _entries);
        if (entries.TryGetValue(key, out var cachedValue))
            return cachedValue;

        lock (_gate)
        {
            entries = _entries;
            if (entries.TryGetValue(key, out cachedValue))
                return cachedValue;

            var createdValue = valueFactory(key) ??
                               throw new InvalidOperationException("The value factory returned null.");
            entries.Add(key, createdValue);
            return createdValue;
        }
    }

    /// <summary>
    ///     获取指定键对应的缓存值；若当前未命中，则在锁保护下使用附加状态创建并写入。
    /// </summary>
    /// <typeparam name="TState">创建缓存值时需要携带的附加状态类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="state">创建缓存值时复用的附加状态。</param>
    /// <param name="valueFactory">基于键与附加状态创建缓存值的工厂方法。</param>
    /// <returns>已存在或新创建的缓存值。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> 或 <paramref name="valueFactory" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException"><paramref name="valueFactory" /> 返回 <see langword="null" />。</exception>
    public TValue GetOrAdd<TState>(TKey key, TState state, Func<TKey, TState, TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var entries = Volatile.Read(ref _entries);
        if (entries.TryGetValue(key, out var cachedValue))
            return cachedValue;

        lock (_gate)
        {
            entries = _entries;
            if (entries.TryGetValue(key, out cachedValue))
                return cachedValue;

            var createdValue = valueFactory(key, state) ??
                               throw new InvalidOperationException("The value factory returned null.");
            entries.Add(key, createdValue);
            return createdValue;
        }
    }

    /// <summary>
    ///     尝试读取当前缓存中的值，而不触发新的创建逻辑。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="value">命中时返回的缓存值。</param>
    /// <returns>若命中当前缓存则为 <see langword="true" />；否则为 <see langword="false" />。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> 为 <see langword="null" />。</exception>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Volatile.Read(ref _entries).TryGetValue(key, out value);
    }

    /// <summary>
    ///     清空当前缓存实例。
    /// </summary>
    /// <remarks>
    ///     该方法主要服务于测试，便于在同一进程内隔离不同用例的静态缓存状态。
    /// </remarks>
    public void Clear()
    {
        lock (_gate)
        {
            _entries = new ConditionalWeakTable<TKey, TValue>();
        }
    }

    /// <summary>
    ///     返回指定键当前命中的缓存对象；若未命中则返回 <see langword="null" />。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <returns>当前缓存对象，或 <see langword="null" />。</returns>
    /// <remarks>
    ///     该入口仅用于测试通过反射观察缓存状态，不应用于运行时代码路径。
    /// </remarks>
    public TValue? GetValueOrDefaultForTesting(TKey key)
    {
        return TryGetValue(key, out var value) ? value : null;
    }
}
